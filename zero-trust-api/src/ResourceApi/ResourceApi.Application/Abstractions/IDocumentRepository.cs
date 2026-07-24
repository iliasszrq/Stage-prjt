using ResourceApi.Domain.Entities;

namespace ResourceApi.Application.Abstractions;
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Document>> GetAllAsync();
    Task AddAsync(Document document);
    Task UpdateAsync(Document document);
    Task DeleteAsync(Guid id);
}