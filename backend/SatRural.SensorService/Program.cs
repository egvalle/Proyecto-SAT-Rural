using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SatRural.SensorService.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("SensorDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'SensorDatabase' must be configured.");
}

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var deviceApiKey = builder.Configuration["SensorIngestion:ApiKey"];
var alertServiceBaseUrl = builder.Configuration["AlertService:BaseUrl"];
var alertServiceApiKey = builder.Configuration["AlertService:ApiKey"];

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured and contain at least 32 characters.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Issuer and Jwt:Audience must be configured.");
}

if (string.IsNullOrWhiteSpace(deviceApiKey) || deviceApiKey.Length < 32)
{
    throw new InvalidOperationException(
        "SensorIngestion:ApiKey must contain at least 32 characters.");
}

if (!Uri.TryCreate(
        alertServiceBaseUrl,
        UriKind.Absolute,
        out var alertServiceUri))
{
    throw new InvalidOperationException(
        "AlertService:BaseUrl must be a valid absolute URL.");
}

if (string.IsNullOrWhiteSpace(alertServiceApiKey) ||
    alertServiceApiKey.Length < 32)
{
    throw new InvalidOperationException(
        "AlertService:ApiKey must contain at least 32 characters.");
}

builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("AlertService", client =>
{
    client.BaseAddress = alertServiceUri;
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<SensorDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();