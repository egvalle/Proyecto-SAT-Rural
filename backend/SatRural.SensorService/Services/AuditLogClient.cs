using System.Net.Http.Json;
using System.Security.Claims;

namespace SatRural.SensorService.Services;

public sealed class AuditLogClient
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditLogClient> _logger;

    public AuditLogClient(IHttpClientFactory clients, IConfiguration configuration,
        ILogger<AuditLogClient> logger)
    {
        _clients = clients;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RecordAsync(ClaimsPrincipal user, string action,
        string entityName, int entityId, string details,
        CancellationToken cancellationToken)
    {
        var identifier = user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub");
        if (!int.TryParse(identifier, out var userId)) return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "internal/audit-logs");
            request.Headers.Add("X-Service-Key", _configuration["AuditService:ApiKey"]);
            request.Content = JsonContent.Create(new
            {
                userId,
                username = user.FindFirstValue(ClaimTypes.Email) ??
                    user.Identity?.Name ?? $"usuario-{userId}",
                action,
                entityName,
                entityId = entityId.ToString(),
                details
            });
            using var response = await _clients.CreateClient("AuditService")
                .SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception,
                "Could not record audit event for {EntityName} {EntityId}.",
                entityName, entityId);
        }
    }
}
