using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using AuthServer.Core.Tokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace SecureApi.UnitTests;

public class AccessTokenGeneratorTests
{
    private static AccessTokenGenerator CreateGenerator() =>
        new(new JwtSettings
        {
            SigningKey = "test-signing-key-at-least-32-chars-long!",
            Issuer = "https://test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15
        });

    // A throwaway RSA key for signing in tests.
    private static SecurityKey TestKey() =>
        new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-kid" };

    [Fact]
    public void GenerateToken_produces_a_readable_jwt()
    {
        var generator = CreateGenerator();

        var tokenString = generator.GenerateToken("user-123", "iliass", TestKey());

        Assert.Equal(2, tokenString.Split('.').Length - 1);
    }

    [Fact]
    public void GenerateToken_embeds_the_expected_claims()
    {
        var generator = CreateGenerator();

        var tokenString = generator.GenerateToken("user-123", "iliass", TestKey());
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

        var tokenString = generator.GenerateToken("user-123", "iliass", TestKey());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }
}