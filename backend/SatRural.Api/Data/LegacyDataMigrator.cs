using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;
using SatRural.Infrastructure.Persistence.Repositories;

namespace SatRural.Api.Data;

public static class LegacyDataMigrator
{
    private const string MigrationId = "sqlserver-auth-alert-audit-v1";

    public static async Task MigrateAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var database = services.GetRequiredService<IMongoDatabase>();
        var markers = database.GetCollection<BsonDocument>("migration_markers");
        var markerFilter = Builders<BsonDocument>.Filter.Eq("_id", MigrationId);

        if (await markers.Find(markerFilter).AnyAsync(cancellationToken))
        {
            return;
        }

        var sql = services.GetRequiredService<AppDbContext>();
        var auth = services.GetRequiredService<AuthDbContext>();
        var alerts = services.GetRequiredService<AlertsDbContext>();

        await MigrateUsersAsync(sql, auth, cancellationToken);
        await MigrateAlertsAsync(sql, alerts, cancellationToken);
        await MigrateAuditLogsAsync(sql, database, cancellationToken);

        await markers.InsertOneAsync(
            new BsonDocument
            {
                ["_id"] = MigrationId,
                ["completedAt"] = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Migrated legacy SQL Server users, alerts, and audit logs.");
    }

    private static async Task MigrateUsersAsync(
        AppDbContext source,
        AuthDbContext target,
        CancellationToken cancellationToken)
    {
        var sourceRoles = await source.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var role in sourceRoles)
        {
            var existing = await target.Roles
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == role.Id,
                    cancellationToken);

            if (existing is not null && existing.Descripcion != role.Descripcion)
            {
                throw new InvalidOperationException(
                    $"Role ID {role.Id} differs between SQL Server and PostgreSQL.");
            }

            if (existing is null)
            {
                target.Roles.Add(new Rol
                {
                    Id = role.Id,
                    Descripcion = role.Descripcion
                });
            }
        }

        var sourceUsers = await source.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var user in sourceUsers)
        {
            var existingById = await target.Users
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == user.Id,
                    cancellationToken);

            if (existingById is not null)
            {
                continue;
            }

            if (await target.Users.AnyAsync(
                    candidate => candidate.Username == user.Username,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Username '{user.Username}' already exists in PostgreSQL with another user ID.");
            }

            target.Users.Add(new User
            {
                Id = user.Id,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                FullName = user.FullName,
                RolId = user.RolId,
                IsActive = user.IsActive,
                CreatedAt = AsUtc(user.CreatedAt)
            });
        }

        await target.SaveChangesAsync(cancellationToken);
        await ResetIdentityAsync(target, "Users", "Id", cancellationToken);
    }

    private static async Task MigrateAlertsAsync(
        AppDbContext source,
        AlertsDbContext target,
        CancellationToken cancellationToken)
    {
        var sourceRules = await source.AlertRules
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var rule in sourceRules)
        {
            var existing = await target.AlertRules
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == rule.Id,
                    cancellationToken);

            if (existing is null)
            {
                target.AlertRules.Add(CopyRule(rule));
            }
            else
            {
                existing.SensorType = rule.SensorType;
                existing.Operator = rule.Operator;
                existing.ThresholdValue = rule.ThresholdValue;
                existing.RiskLevel = rule.RiskLevel;
                existing.Phenomenon = rule.Phenomenon;
                existing.Description = rule.Description;
            }
        }

        var sourceAlerts = await source.Alerts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var alert in sourceAlerts)
        {
            if (await target.Alerts.AnyAsync(
                    candidate => candidate.Id == alert.Id,
                    cancellationToken))
            {
                continue;
            }

            target.Alerts.Add(new Alert
            {
                Id = alert.Id,
                CommunityId = alert.CommunityId,
                SensorId = alert.SensorId,
                Type = alert.Type,
                Level = alert.Level,
                Message = alert.Message,
                CreatedAt = AsUtc(alert.CreatedAt),
                ResolvedAt = AsUtc(alert.ResolvedAt),
                IsActive = alert.IsActive,
                AlertRuleId = alert.AlertRuleId,
                DetectedValue = alert.DetectedValue,
                ThresholdValue = alert.ThresholdValue
            });
        }

        await target.SaveChangesAsync(cancellationToken);
        await ResetIdentityAsync(target, "Alerts", "Id", cancellationToken);
        await ResetIdentityAsync(target, "AlertRules", "Id", cancellationToken);
    }

    private static async Task MigrateAuditLogsAsync(
        AppDbContext source,
        IMongoDatabase database,
        CancellationToken cancellationToken)
    {
        var logs = await source.AuditLogs
            .AsNoTracking()
            .Include(log => log.User)
            .ToListAsync(cancellationToken);

        var collection =
            database.GetCollection<AuditLogDocument>("audit_logs");
        var writes = logs.Select(log =>
            new ReplaceOneModel<AuditLogDocument>(
                Builders<AuditLogDocument>.Filter.Eq(document => document.Id, log.Id),
                new AuditLogDocument
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    Username = log.User.Username,
                    FullName = log.User.FullName,
                    Action = log.Action,
                    EntityName = log.EntityName,
                    EntityId = log.EntityId,
                    Details = log.Details,
                    CreatedAt = AsUtc(log.CreatedAt)
                })
            {
                IsUpsert = true
            }).ToList();

        if (writes.Count > 0)
        {
            await collection.BulkWriteAsync(
                writes,
                cancellationToken: cancellationToken);
        }

        var maxId = logs.Count == 0 ? 0 : logs.Max(log => log.Id);
        var counters = database.GetCollection<BsonDocument>("counters");
        await counters.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "audit_logs"),
            Builders<BsonDocument>.Update.Max("sequence", maxId),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    private static AlertRule CopyRule(AlertRule rule) =>
        new()
        {
            Id = rule.Id,
            SensorType = rule.SensorType,
            Operator = rule.Operator,
            ThresholdValue = rule.ThresholdValue,
            RiskLevel = rule.RiskLevel,
            Phenomenon = rule.Phenomenon,
            Description = rule.Description
        };

    private static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static DateTime? AsUtc(DateTime? value) =>
        value.HasValue ? AsUtc(value.Value) : null;

    private static async Task ResetIdentityAsync(
        DbContext context,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            $"""SELECT setval(pg_get_serial_sequence('"{table}"', '{column}'), COALESCE(MAX("{column}"), 1), MAX("{column}") IS NOT NULL) FROM "{table}";""",
            cancellationToken);
    }
}
