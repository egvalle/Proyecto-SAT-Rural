using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.EntityFrameworkCore;
using SatRural.Application.Modules.Audit.Interfaces;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditLogDocument> _auditLogs;
    private readonly IMongoCollection<BsonDocument> _counters;
    private readonly AuthDbContext _authDbContext;

    public AuditLogRepository(
        IMongoDatabase database,
        AuthDbContext authDbContext)
    {
        _auditLogs = database.GetCollection<AuditLogDocument>("audit_logs");
        _counters = database.GetCollection<BsonDocument>("counters");
        _authDbContext = authDbContext;
    }

    public async Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        var user = await _authDbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == auditLog.UserId,
                cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                $"Cannot write audit log: user {auditLog.UserId} does not exist.");
        }

        var counter = await _counters.FindOneAndUpdateAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "audit_logs"),
            Builders<BsonDocument>.Update.Inc("sequence", 1),
            new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);

        auditLog.Id = counter["sequence"].ToInt64();
        await _auditLogs.InsertOneAsync(
            ToDocument(auditLog, user),
            cancellationToken: cancellationToken);
    }

    public async Task<List<AuditLog>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var documents = await _auditLogs
            .Find(FilterDefinition<AuditLogDocument>.Empty)
            .SortByDescending(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(document => new AuditLog
        {
            Id = document.Id,
            UserId = document.UserId,
            User = new User
            {
                Id = document.UserId,
                Username = document.Username,
                FullName = document.FullName
            },
            Action = document.Action,
            EntityName = document.EntityName,
            EntityId = document.EntityId,
            Details = document.Details,
            CreatedAt = document.CreatedAt
        }).ToList();
    }

    private static AuditLogDocument ToDocument(AuditLog log, User user) =>
        new()
        {
            Id = log.Id,
            UserId = log.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Action = log.Action,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Details = log.Details,
            CreatedAt = log.CreatedAt
        };
}

public sealed class AuditLogDocument
{
    [MongoDB.Bson.Serialization.Attributes.BsonId]
    public long Id { get; set; }

    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
