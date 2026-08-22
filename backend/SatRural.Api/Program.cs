using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using SatRural.Domain.Entities;
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured and contain at least 32 characters.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

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

builder.Services.AddSingleton<SimulationState>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Health Check
app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");

app.Run();