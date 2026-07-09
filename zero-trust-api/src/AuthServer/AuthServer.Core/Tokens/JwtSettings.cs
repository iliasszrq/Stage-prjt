namespace AuthServer.Core.Tokens;

public class JwtSettings{
    public string SigningKey{ get; set; } = string.Empty;
    public string Issuer { get; set; } = "https://localhost:5001";
    public string Audience{get; set; } = "resource-api";
    public int AccessTokenMinutes { get; set; } = 15;
}