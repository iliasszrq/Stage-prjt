using System.IdentityModel.Tokens.Jwt;
using AuthServer.Core.Tokens;
using Xunit;

namespace SecureApi.UnitTests;
public class AccessTokenGeneratorTests
{
    private static AccessTokenGenerator CreateGenerator() =>
        new(new JwtSettings
        {
            SigningKey = "test-signing-key-at-least-32-chars-long",
            Issuer = "https://test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15
        });
    [Fact]
    public void GenerateToken_produces_a_readable_jwt()
    {
        var generator = CreateGenerator();
        var tokenString = generator.GenerateToken(userId: "user-123", username: "iliass");
        Assert.Equal(2,tokenString.Split('.').Length -1);
    }
    [Fact]
    public void GenerateToken_embeds_the_expected_claims()
    {
        var generator = CreateGenerator();
        var tokenString = generator.GenerateToken(userId: "user-123", username: "iliass");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        Assert.Equal("https://test-issuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "test-audience");
        Assert.Equal("user-123", jwt.Subject);
        Assert.Contains(jwt.Claims, c => c.Type == "unique_name" && c.Value == "iliass");
    }
    [Fact]
    public void GenerateToken_sets_an_expiry_in_the_future()
    {
        var generator = CreateGenerator();
        var tokenString = generator.GenerateToken(userId: "user-123", username: "iliass");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }
}