using AuthServer.Core.Tokens;
using SecureApi.Shared.Auth;
using AuthServer.Api.Persistence;
using AuthServer.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using AuthServer.Core.RefreshRotation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Bind the "Jwt" config section to a JwtSettings instance.
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);

// Register the token generator so the endpoint can use it.
builder.Services.AddSingleton<AccessTokenGenerator>();
builder.Services.AddScoped<RefreshTokenRotationService>();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));
builder.Services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapPost("/token", async (
    TokenRequest request,
    AccessTokenGenerator generator,
    RefreshTokenRotationService rotation,
    JwtSettings settings) =>
{
    if (request.Username != "iliass" || request.Password != "password123")
    {
        return Results.Unauthorized();
    }

    const string userId = "user-123";

    var accessToken = generator.GenerateToken(userId: userId, username: request.Username);
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
    JwtSettings settings) =>
{
    var result = await rotation.RotateAsync(request.RefreshToken);

    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    }

    var newRefresh = result.NewToken!;
    var accessToken = generator.GenerateToken(
        userId: newRefresh.UserId,
        username: newRefresh.UserId);

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

app.Run();