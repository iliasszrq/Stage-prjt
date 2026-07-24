using Microsoft.EntityFrameworkCore;
using ResourceApi.Domain.Entities;

namespace ResourceApi.Infrastructure.Persistence;
public class ResourceDbContext : DbContext
{
    public ResourceDbContext(DbContextOptions<ResourceDbContext> options)
        : base(options) { }

    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var doc = modelBuilder.Entity<Document>();

        doc.HasKey(d => d.Id);
        doc.HasIndex(d => d.OwnerId);
    }
}