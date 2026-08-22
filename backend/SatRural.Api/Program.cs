using Microsoft.EntityFrameworkCore;
using SatRural.Infrastructure.Persistence;
using SatRural.Infrastructure.Services.Monitoring;
using SatRural.Application.Modules.Monitoring.Services;
using SatRural.Api.Hubs;
using SatRural.Api.Realtime;
using SatRural.Application.Modules.Monitoring.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Entity Framework Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Health Checks
builder.Services.AddHealthChecks();
builder.Services.AddScoped<RiskEvaluationService>();

builder.Services.AddSignalR();

builder.Services.AddSingleton<
    IMonitoringNotifier,
    SignalRMonitoringNotifier
>();

// Simulación de sensores
builder.Services.AddHostedService<SensorSimulationService>();

var app = builder.Build();

// Aplicar migraciones pendientes automáticamente
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// OpenAPI solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health Check
app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");

app.Run();