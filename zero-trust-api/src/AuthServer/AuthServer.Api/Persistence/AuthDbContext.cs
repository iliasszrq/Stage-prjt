using AuthServer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Api.Persistence;
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        :base(options){}
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var token = modelBuilder.Entity<RefreshToken>();
        token.HasKey(t => t.Token);
        token.HasIndex(t => t.FamilyId);
        token.Property(t => t.Status).HasConversion<string>();
    }
}