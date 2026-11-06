using AlignRoute.Api.Data;
using AlignRoute.Api.Endpoints;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// LLM settings — prefer env var over config for the API key
var llmSettings = new LlmSettings
{
    BaseUrl = builder.Configuration["LlmSettings:BaseUrl"] ?? "https://llm.hpc.psnc.pl",
    Model = builder.Configuration["LlmSettings:Model"] ?? "gpt-oss_120b",
    ApiKey = Environment.GetEnvironmentVariable("LLM_API_KEY")
        ?? builder.Configuration["LlmSettings:ApiKey"]
        ?? string.Empty
};
builder.Services.AddSingleton(llmSettings);

// HTTP clients
builder.Services.AddHttpClient<ILlmService, LlmService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddHttpClient<IOverpassService, OverpassService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});

// AI agents
builder.Services.AddScoped<IExplorerAgent, ExplorerAgent>();
builder.Services.AddScoped<IPlannerAgent, PlannerAgent>();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();

// Map endpoints
app.MapGroup("/api/sessions").MapSessionEndpoints();
app.MapGroup("/api/sessions").MapAttractionEndpoints();
app.MapGroup("/api/sessions").MapVotingEndpoints();
app.MapGroup("/api/sessions").MapItineraryEndpoints();

// Map SignalR hub
app.MapHub<SessionHub>("/hubs/session");

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
