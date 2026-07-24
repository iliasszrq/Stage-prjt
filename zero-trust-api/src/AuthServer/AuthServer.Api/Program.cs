using AuthServer.Core.Tokens;
using SecureApi.Shared.Auth;
using AuthServer.Api.Persistence;
using AuthServer.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using AuthServer.Core.RefreshRotation;
using AuthServer.Api.Signing;
using Microsoft.IdentityModel.Tokens;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);


builder.Services.AddSingleton<AccessTokenGenerator>();
builder.Services.AddScoped<RefreshTokenRotationService>();
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));
builder.Services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();
builder.Services.AddSingleton<RsaKeyProvider>();


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapPost("/token", async (
    TokenRequest request,
    AccessTokenGenerator generator,
    RefreshTokenRotationService rotation,
    RsaKeyProvider keys,
    JwtSettings settings) =>
{
    string? userId = (request.Username, request.Password) switch
    {
        ("alice", "password123") => "user-alice",
        ("bob",   "password123") => "user-bob",
        _ => null
    };

    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var accessToken = generator.GenerateToken(userId, request.Username, keys.GetPrivateKey());
    var refreshToken = await rotation.CreateInitialTokenAsync(userId);

    return Results.Ok(new TokenResponse(
        AccessToken: accessToken,
        RefreshToken: refreshToken.Token,
        ExpiresIn: settings.AccessTokenMinutes * 60));
})
.WithName("IssueToken");
app.MapPost("/refresh", async (
    RefreshRequest request,
    RefreshTokenRotationService rotation,
    AccessTokenGenerator generator,
    RsaKeyProvider keys,
    JwtSettings settings) =>
{
    var result = await rotation.RotateAsync(request.RefreshToken);

    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    }

    var newRefresh = result.NewToken!;
    var accessToken = generator.GenerateToken(
        newRefresh.UserId,
        newRefresh.UserId,
        keys.GetPrivateKey());

    return Results.Ok(new TokenResponse(
        AccessToken: accessToken,
        RefreshToken: newRefresh.Token,
        ExpiresIn: settings.AccessTokenMinutes * 60));
})
.WithName("RefreshToken");

app.MapPost("/revoke", async(
    RevokeRequest request,
    RefreshTokenRotationService rotation) =>
{
    await rotation.RevokeAsync(request.RefreshToken);
    return Results.Ok();
})
.WithName("RevokeToken");
app.MapGet("/.well-known/jwks.json", (RsaKeyProvider keys) =>
{
    var publicKey = keys.GetPublicKey();
    var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(publicKey);
    jwk.Use = "sig";
    jwk.Alg = SecurityAlgorithms.RsaSha256;
    return Results.Ok(new { keys = new[] { jwk } });
})
.WithName("Jwks");
app.MapGet("/.well-known/openid-configuration", (JwtSettings settings) =>
{
    var issuer = settings.Issuer;
    return Results.Ok(new
    {
        issuer = issuer,
        jwks_uri = $"{issuer}/.well-known/jwks.json",
        response_types_supported = new[] { "token" },
        subject_types_supported = new[] { "public" },
        id_token_signing_alg_values_supported = new[] { "RS256" }
    });
})
.WithName("OpenIdConfiguration");

app.Run();