using AuthServer.Core.Tokens;
using SecureApi.Shared.Auth;
using AuthServer.Api.Persistence;
using AuthServer.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Bind the "Jwt" config section to a JwtSettings instance.
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);

// Register the token generator so the endpoint can use it.
builder.Services.AddSingleton<AccessTokenGenerator>();

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

// POST /token — exchange username/password for a signed JWT.
app.MapPost("/token", (TokenRequest request, AccessTokenGenerator generator, JwtSettings settings) =>
{
    // Week 1: credentials are hardcoded. Real user store comes in Week 2.
    if (request.Username != "iliass" || request.Password != "password123")
    {
        return Results.Unauthorized();
    }

    var token = generator.GenerateToken(userId: "user-123", username: request.Username);

    return Results.Ok(new TokenResponse(
        AccessToken: token,
        ExpiresIn: settings.AccessTokenMinutes * 60)); // minutes → seconds
})
.WithName("IssueToken");

app.Run();