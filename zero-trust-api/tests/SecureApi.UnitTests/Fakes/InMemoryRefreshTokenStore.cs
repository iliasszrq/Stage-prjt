using AuthServer.Core.Abstractions;
using AuthServer.Core.Entities;

namespace SecureApi.UnitTests.Fakes;


public class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly Dictionary<string, RefreshToken> _tokens = new();

    public Task<RefreshToken?> FindAsync(string token)
    {
        _tokens.TryGetValue(token, out var found);
        return Task.FromResult(found);
    }

    public Task AddAsync(RefreshToken token)
    {
        _tokens[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RefreshToken token)
    {
        _tokens[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task RevokeFamilyAsync(string familyId)
    {
        foreach (var t in _tokens.Values.Where(t => t.FamilyId == familyId))
            t.Status = RefreshTokenStatus.Revoked;
        return Task.CompletedTask;
    }

    
    public void Seed(RefreshToken token) => _tokens[token.Token] = token;
}