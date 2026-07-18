using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StockLens.Api.Endpoints;
using StockLens.Api.Health;
using StockLens.Api.Middleware;
using StockLens.Api.Realtime;
using StockLens.Application;
using StockLens.Application.Abstractions;
using StockLens.Infrastructure;
using StockLens.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "StockLensApp";

// Structured logging via Serilog, configurable through appsettings "Serilog" section.
builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IInventoryNotifier, SignalRInventoryNotifier>();

// Serialize enums as strings and use camelCase to match what Minimal API HTTP sends.
// SignalR's AddJsonProtocol uses its own JsonSerializerOptions that do NOT inherit from
// ConfigureHttpJsonOptions, so both must be configured explicitly.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR()
    .AddJsonProtocol(o =>
    {
        o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                       ?? ["http://localhost:4200"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials())); // credentials required for the SignalR websocket

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// "ready" gates whether the app can serve traffic (its database is reachable);
// liveness stays dependency-free so an orchestrator only restarts a truly dead process.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

// Apply migrations and seed on startup so the dashboard has data immediately.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseRequestLogging();

app.UseCors(CorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapVehicleEndpoints();
app.MapActionEndpoints();
app.MapStrategyEndpoints();
app.MapDashboardEndpoints();
app.MapSalespersonEndpoints();
app.MapHub<InventoryHub>("/hubs/inventory");

// Full report (all checks) plus split readiness/liveness probes for container orchestration.
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
