using AuthServer.Core.Entities;

namespace AuthServer.Core.Abstractions;

public interface IRefreshTokenStore
{
    Task<RefreshToken?> FindAsync(string token);
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeFamilyAsync(string familyId);
}