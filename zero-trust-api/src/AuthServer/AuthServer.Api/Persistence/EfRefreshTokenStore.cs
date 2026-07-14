using AuthServer.Core.Abstractions;
using AuthServer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Api.Persistence;
public class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly AuthDbContext _db;
    public EfRefreshTokenStore(AuthDbContext db)
    {
        _db = db;
    }
    public async Task<RefreshToken?> FindAsync(string token)
    {
        return await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);
    }
    public async Task AddAsync(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }
        public async Task UpdateAsync(RefreshToken token)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync();
    }
    public async Task RevokeFamilyAsync(string familyId)
    {
        var family = await _db.RefreshTokens
            .Where(t => t.FamilyId == familyId)
            .ToListAsync();
        foreach (var t in family)
            t.Status = RefreshTokenStatus.Revoked;
        await _db.SaveChangesAsync();

    }
}