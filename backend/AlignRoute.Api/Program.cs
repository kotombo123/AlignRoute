using AlignRoute.Api.Data;
using AlignRoute.Api.Endpoints;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();

// CORS skonfigurowany pod SignalR (AllowCredentials wymaga konkretnego originu lub SetIsOriginAllowed)
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

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