using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SatRural.Application.Modules.Audit;
using SatRural.Application.Modules.Audit.Interfaces;
using SatRural.Application.Modules.Audit.Services;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Services;

public sealed class AuditForwardingService : IAuditLogService
{
    private readonly AuditLogService _legacy;
    private readonly IAuditLogRepository _repository;
    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditForwardingService> _logger;

    public AuditForwardingService(
        AuditLogService legacy,
        IAuditLogRepository repository,
        AppDbContext dbContext,
        IHttpClientFactory clients,
        IConfiguration configuration,
        ILogger<AuditForwardingService> logger)
    {
        _legacy = legacy;
        _repository = repository;
        _dbContext = dbContext;
        _clients = clients;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task LogAsync(
        int userId, string action, string entityName, string entityId,
        string details, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entry, cancellationToken);

        var user = await _dbContext.Users.AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new { candidate.Username, candidate.FullName })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null) return;
        var apiKey = _configuration["AuditService:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        try
        {
            var client = _clients.CreateClient("AuditService");
            using var request = new HttpRequestMessage(
                HttpMethod.Post, "internal/audit-logs");
            request.Headers.Add("X-Service-Key", apiKey);
            request.Content = JsonContent.Create(new
            {
                userId,
                legacyId = entry.Id,
                username = user.Username,
                fullName = user.FullName,
                action,
                entityName,
                entityId,
                details,
                createdAt = entry.CreatedAt
            });
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("AuditService returned HTTP {StatusCode}.",
                    (int)response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "AuditService is unavailable.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "AuditService timed out.");
        }
    }

    public Task<List<AuditLogDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _legacy.GetAllAsync(cancellationToken);
}
