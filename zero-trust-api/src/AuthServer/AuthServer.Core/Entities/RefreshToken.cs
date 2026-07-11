namespace AuthServer.Core.Entities;

public class RefreshToken
{
    public required string Token { get; init; }
    public required string UserId { get; init; }
    public required string FamilyId { get; init; }
    public RefreshTokenStatus Status {get; set; } = RefreshTokenStatus.Active;
    public DateTime CreatedAt {get; set; } = DateTime.UtcNow;
    public required DateTime ExpiresAt { get; set; }
}