// Program.cs — punkt startowy całego API AlignRoute.
//
// Tu rejestrujesz serwisy (DI), konfigurujesz middleware i mapujesz endpointy HTTP + hub SignalR.
// Frontend na /frontend odpytuje to API pod adresem http://localhost:5038/api.
//
// Schemat flow:
//   POST /api/sessions          → utwórz sesję (host)
//   GET  /api/sessions/{code}   → pobierz info o sesji po kodzie
//   POST /api/sessions/{code}/join → dołącz do sesji (uczestnik)
//   POST /api/sessions/{id}/explore → start eksploracji atrakcji przez AI
//   GET  /api/sessions/{id}/attractions → pobierz listę atrakcji
//   POST /api/sessions/{id}/vote → oddaj głos
//   POST /api/sessions/{id}/generate-plan → wygeneruj plan dnia po dniu przez AI
//   GET  /api/sessions/{id}/itinerary → pobierz gotowy plan
//
// Real-time (SignalR) hub: /hub
//   Klient wywołuje: JoinSessionGroup(sessionId)
//   Serwer rozsyła do grupy: ParticipantJoined, ExplorationComplete, PlanGenerated, itd.

using AlignRoute.Api.Data;
using AlignRoute.Api.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// TODO: zarejestruj DbContext (EF Core + SQLite)
// builder.Services.AddDbContext<AppDbContext>(opt =>
//     opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// TODO: zarejestruj SignalR
// builder.Services.AddSignalR();

// TODO: zarejestruj CORS (frontend działa na innym porcie lub pliku)
// builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
//     p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// TODO: zarejestruj własne serwisy (ExplorerAgent, PlannerAgent, LlmService, itp.)

var app = builder.Build();

// TODO: stwórz bazę przy starcie (lub użyj migracji)
// using (var scope = app.Services.CreateScope())
//     scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

// TODO: włącz middleware
// app.UseCors();

// TODO: zamapuj endpointy, np.:
// var sessions = app.MapGroup("/api/sessions");
// sessions.MapPost("/", CreateSession);
// sessions.MapGet("/{joinCode}", GetSessionByJoinCode);
// ...

// TODO: zamapuj hub SignalR
// app.MapHub<SessionHub>("/hub");

app.Run();
