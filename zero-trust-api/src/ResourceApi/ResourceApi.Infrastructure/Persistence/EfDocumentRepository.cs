using Microsoft.EntityFrameworkCore;
using ResourceApi.Application.Abstractions;
using ResourceApi.Domain.Entities;

namespace ResourceApi.Infrastructure.Persistence;
public class EfDocumentRepository : IDocumentRepository
{
    private readonly ResourceDbContext _db;

    public EfDocumentRepository(ResourceDbContext db)
    {
        _db = db;
    }

    public async Task<Document?> GetByIdAsync(Guid id) =>
        await _db.Documents.FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IReadOnlyList<Document>> GetAllAsync() =>
        await _db.Documents.ToListAsync();

    public async Task AddAsync(Document document)
    {
        _db.Documents.Add(document);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Document document)
    {
        _db.Documents.Update(document);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is not null)
        {
            _db.Documents.Remove(doc);
            await _db.SaveChangesAsync();
        }
    }
}