using System.Security.Cryptography;
using AuthServer.Core.Entities;
using AuthServer.Core.Abstractions;

namespace AuthServer.Core.RefreshRotation;
public class RefreshTokenRotationService
{
    private readonly IRefreshTokenStore _store;
    private readonly int _refreshTokenDays;
    public RefreshTokenRotationService(IRefreshTokenStore store, int refreshTokenDays = 7)
    {
        _store = store;
        _refreshTokenDays = refreshTokenDays;
    }
    public async Task<RotationResult> RotateAsync(string presentedToken)
    {
        var existing = await _store.FindAsync(presentedToken);
        if(existing is null)
            return RotationResult.Fail(RotationFailure.NotFound);

        
        if (existing.Status == RefreshTokenStatus.Retired)
        {
            await _store.RevokeFamilyAsync(existing.FamilyId);
            return RotationResult.Fail(RotationFailure.ReuseDetected);
        }
        if (existing.Status == RefreshTokenStatus.Revoked)
            return RotationResult.Fail(RotationFailure.Revoked);
        if (existing.ExpiresAt <= DateTime.UtcNow)
            return RotationResult.Fail(RotationFailure.Expired);
        existing.Status = RefreshTokenStatus.Retired;
        await _store.UpdateAsync(existing);
        var newToken = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = existing.UserId,
            FamilyId = existing.FamilyId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays)
        };
        await _store.AddAsync(newToken);
        return RotationResult.Success(newToken);
    }
    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+','-').Replace('/','_');
    }
}