using AuthServer.Core.Entities;
using AuthServer.Core.RefreshRotation;
using SecureApi.UnitTests.Fakes;
using Xunit;

namespace SecureApi.UnitTests;

public class RefreshTokenRotationTests
{
    // Helper: build a service with a fresh in-memory store,
    // and seed one active token. Returns both so tests can inspect.
    private static (RefreshTokenRotationService service, InMemoryRefreshTokenStore store, RefreshToken active)
        Arrange()
    {
        var store = new InMemoryRefreshTokenStore();
        var active = new RefreshToken
        {
            Token = "token-1",
            UserId = "user-123",
            FamilyId = "family-A",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        store.Seed(active);
        var service = new RefreshTokenRotationService(store);
        return (service, store, active);
    }

    [Fact]
    public async Task Valid_token_rotates_and_issues_a_new_one()
    {
        var (service, _, _) = Arrange();

        var result = await service.RotateAsync("token-1");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.NewToken);
        Assert.NotEqual("token-1", result.NewToken!.Token);   // a genuinely new value
        Assert.Equal("family-A", result.NewToken.FamilyId);   // same family
    }

    [Fact]
    public async Task Rotated_old_token_becomes_retired()
    {
        var (service, store, active) = Arrange();

        await service.RotateAsync("token-1");

        var old = await store.FindAsync("token-1");
        Assert.Equal(RefreshTokenStatus.Retired, old!.Status);
    }

    [Fact]
    public async Task Reusing_a_retired_token_is_detected_as_theft()
    {
        var (service, _, _) = Arrange();

        // First use: legitimate rotation. token-1 is now retired.
        await service.RotateAsync("token-1");

        // Second use of the SAME token: this is the attack.
        var result = await service.RotateAsync("token-1");

        Assert.False(result.Succeeded);
        Assert.Equal(RotationFailure.ReuseDetected, result.Failure);
    }

    [Fact]
    public async Task Reuse_revokes_the_entire_family()
    {
        var (service, store, _) = Arrange();

        // Rotate twice legitimately to grow the chain: token-1 -> token-2 -> token-3
        var r1 = await service.RotateAsync("token-1");
        var r2 = await service.RotateAsync(r1.NewToken!.Token);

        // Attacker replays the stolen token-1 (now retired).
        await service.RotateAsync("token-1");

        // The whole family must be dead — including the currently-live token.
        var live = await store.FindAsync(r2.NewToken!.Token);
        Assert.Equal(RefreshTokenStatus.Revoked, live!.Status);
    }

    [Fact]
    public async Task Unknown_token_is_rejected()
    {
        var (service, _, _) = Arrange();

        var result = await service.RotateAsync("never-issued");

        Assert.False(result.Succeeded);
        Assert.Equal(RotationFailure.NotFound, result.Failure);
    }
}