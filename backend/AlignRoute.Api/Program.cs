using AlignRoute.Api.Data;
using AlignRoute.Api.Endpoints;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();

// CORS — Caddy działa na :8080, backend na :5038. AllowCredentials wymagane przez SignalR.
// Na produkcji: zastąpić localhost:8080 adresem domeny Caddy.
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:8080", "https://localhost:8080")
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

builder.Services.AddHttpClient<ExplorerAgent>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

app.UseCors();

// Healthcheck dla kolegi od YARP / Caddy
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapSessionEndpoints();
app.MapExplorationEndpoints();
app.MapHub<SessionHub>("/hub");

app.Run();