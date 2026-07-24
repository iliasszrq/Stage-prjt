using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ResourceApi.Api.Authorization;
using ResourceApi.Application.Abstractions;
using ResourceApi.Domain.Entities;
using ResourceApi.Infrastructure.Persistence;
using SecureApi.Shared.Documents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:5001";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:5001",

            ValidateAudience = true,
            ValidAudience = "resource-api",

            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DocumentOwner", policy =>
        policy.Requirements.Add(new DocumentOwnerRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, DocumentOwnerHandler>();

builder.Services.AddDbContext<ResourceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ResourceDb")));

builder.Services.AddScoped<IDocumentRepository, EfDocumentRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/whoami", (HttpContext ctx) =>
{
    var userId = ctx.User.FindFirst("sub")?.Value ?? "unknown";
    var username = ctx.User.FindFirst("unique_name")?.Value ?? "unknown";
    return Results.Ok(new { userId, username });
})
.RequireAuthorization();

// POST /documents — create a document owned by the caller.
app.MapPost("/documents", async (
    CreateDocumentRequest request,
    IDocumentRepository repo,
    HttpContext ctx) =>
{
    var ownerId = ctx.User.FindFirst("sub")?.Value ?? "unknown";

    var doc = new Document
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Title = request.Title,
        Content = request.Content
    };

    await repo.AddAsync(doc);

    return Results.Ok(new DocumentResponse(
        doc.Id, doc.OwnerId, doc.Title, doc.Content, doc.CreatedAt));
})
.RequireAuthorization();

app.MapGet("/documents/{id:guid}", async (
    Guid id,
    IDocumentRepository repo,
    IAuthorizationService authz,
    HttpContext ctx) =>
{
    var doc = await repo.GetByIdAsync(id);
    if (doc is null) return Results.NotFound();

    var result = await authz.AuthorizeAsync(ctx.User, doc, "DocumentOwner");
    if (!result.Succeeded) return Results.Forbid();

    return Results.Ok(new DocumentResponse(
        doc.Id, doc.OwnerId, doc.Title, doc.Content, doc.CreatedAt));
})
.RequireAuthorization();

app.MapGet("/documents", async (
    IDocumentRepository repo,
    HttpContext ctx) =>
{
    var userId = ctx.User.FindFirst("sub")?.Value;
    if (userId is null) return Results.Forbid();

    var all = await repo.GetAllAsync();
    var mine = all.Where(d => d.OwnerId == userId);

    return Results.Ok(mine.Select(d =>
        new DocumentResponse(d.Id, d.OwnerId, d.Title, d.Content, d.CreatedAt)));
})
.RequireAuthorization();

app.Run();