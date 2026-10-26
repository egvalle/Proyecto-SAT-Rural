using Microsoft.EntityFrameworkCore;
using SatRural.CommunityService.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("CommunityDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'CommunityDatabase' must be configured.");
}

builder.Services.AddDbContext<CommunityDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<CommunityDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();