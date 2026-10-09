using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SatRural.Application.Modules.Alerts;
using SatRural.Application.Modules.Audit.Interfaces;
using SatRural.Application.Modules.Monitoring.Interfaces;
using SatRural.Domain.Entities;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/alert-rules")]
public class AlertRulesController : ControllerBase
{
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly IAuditLogService _auditLogService;

    public AlertRulesController(
        IAlertRuleRepository alertRuleRepository,
        IAuditLogService auditLogService)
    {
        _alertRuleRepository = alertRuleRepository;
        _auditLogService = auditLogService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<AlertRuleDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var rules = await _alertRuleRepository.GetAllAsync(
            cancellationToken);

        var response = rules
            .Select(ToDto)
            .ToList();

        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AlertRuleDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var rule = await _alertRuleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (rule is null)
        {
            return NotFound(new
            {
                message = "Alert rule not found."
            });
        }

        return Ok(ToDto(rule));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<AlertRuleDto>> Create(
        CreateAlertRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);

        if (validationError is not null)
        {
            return BadRequest(new
            {
                message = validationError
            });
        }

        var rule = new AlertRule
        {
            SensorType = request.SensorType
                .Trim()
                .ToUpperInvariant(),

            Operator = request.Operator
                .Trim(),

            ThresholdValue = request.ThresholdValue,

            RiskLevel = request.RiskLevel
                .Trim()
                .ToUpperInvariant(),

            Phenomenon = request.Phenomenon
                .Trim(),

            Description = request.Description
                .Trim()
        };

        var createdRule = await _alertRuleRepository.CreateAsync(
            rule,
            cancellationToken);

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Unable to identify the authenticated user."
            });
        }

        await _auditLogService.LogAsync(
            userId.Value,
            "CREATE",
            "AlertRule",
            createdRule.Id.ToString(),
            $"Created alert rule: {createdRule.SensorType} {createdRule.Operator} {createdRule.ThresholdValue} - {createdRule.RiskLevel}",
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdRule.Id },
            ToDto(createdRule));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AlertRuleDto>> Update(
        int id,
        UpdateAlertRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);

        if (validationError is not null)
        {
            return BadRequest(new
            {
                message = validationError
            });
        }

        var existingRule = await _alertRuleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (existingRule is null)
        {
            return NotFound(new
            {
                message = "Alert rule not found."
            });
        }

        existingRule.SensorType = request.SensorType
            .Trim()
            .ToUpperInvariant();

        existingRule.Operator = request.Operator
            .Trim();

        existingRule.ThresholdValue = request.ThresholdValue;

        existingRule.RiskLevel = request.RiskLevel
            .Trim()
            .ToUpperInvariant();

        existingRule.Phenomenon = request.Phenomenon
            .Trim();

        existingRule.Description = request.Description
            .Trim();

        var updated = await _alertRuleRepository.UpdateAsync(
            existingRule,
            cancellationToken);

        if (!updated)
        {
            return NotFound(new
            {
                message = "Alert rule not found."
            });
        }

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Unable to identify the authenticated user."
            });
        }

        await _auditLogService.LogAsync(
            userId.Value,
            "UPDATE",
            "AlertRule",
            id.ToString(),
            $"Updated alert rule: {existingRule.SensorType} {existingRule.Operator} {existingRule.ThresholdValue} - {existingRule.RiskLevel}",
            cancellationToken);

        return Ok(ToDto(existingRule));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var existingRule = await _alertRuleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (existingRule is null)
        {
            return NotFound(new
            {
                message = "Alert rule not found."
            });
        }

        var deleted = await _alertRuleRepository.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Alert rule not found."
            });
        }

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Unable to identify the authenticated user."
            });
        }

        await _auditLogService.LogAsync(
            userId.Value,
            "DELETE",
            "AlertRule",
            id.ToString(),
            $"Deleted alert rule: {existingRule.SensorType} {existingRule.Operator} {existingRule.ThresholdValue} - {existingRule.RiskLevel}",
            cancellationToken);

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var subject =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(subject, out var userId)
            ? userId
            : null;
    }

    private static string? ValidateRequest(
        CreateAlertRuleRequest request) =>
        ValidateValues(
            request.SensorType,
            request.Operator,
            request.ThresholdValue,
            request.RiskLevel,
            request.Phenomenon,
            request.Description);

    private static string? ValidateRequest(
        UpdateAlertRuleRequest request) =>
        ValidateValues(
            request.SensorType,
            request.Operator,
            request.ThresholdValue,
            request.RiskLevel,
            request.Phenomenon,
            request.Description);

    private static string? ValidateValues(
        string sensorType,
        string operatorValue,
        decimal thresholdValue,
        string riskLevel,
        string phenomenon,
        string description)
    {
        if (string.IsNullOrWhiteSpace(sensorType))
        {
            return "SensorType is required.";
        }

        var validSensorTypes = new[]
        {
            "TEMPERATURE",
            "HUMIDITY",
            "WIND_SPEED",
            "RAINFALL",
            "RIVER_LEVEL"
        };

        if (!validSensorTypes.Contains(
                sensorType.Trim().ToUpperInvariant()))
        {
            return "Invalid SensorType.";
        }

        if (operatorValue != ">=" && operatorValue != "<=")
        {
            return "Operator must be '>=' or '<='.";
        }

        if (thresholdValue < 0 &&
            sensorType.Trim().ToUpperInvariant() != "TEMPERATURE")
        {
            return "ThresholdValue cannot be negative for this sensor type.";
        }

        var validRiskLevels = new[]
        {
            "YELLOW",
            "ORANGE",
            "RED"
        };

        if (!validRiskLevels.Contains(
                riskLevel.Trim().ToUpperInvariant()))
        {
            return "Invalid RiskLevel.";
        }

        if (string.IsNullOrWhiteSpace(phenomenon))
        {
            return "Phenomenon is required.";
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return "Description is required.";
        }

        return null;
    }

    private static AlertRuleDto ToDto(AlertRule rule) => new()
    {
        Id = rule.Id,
        SensorType = rule.SensorType,
        Operator = rule.Operator,
        ThresholdValue = rule.ThresholdValue,
        RiskLevel = rule.RiskLevel,
        Phenomenon = rule.Phenomenon,
        Description = rule.Description
    };
}