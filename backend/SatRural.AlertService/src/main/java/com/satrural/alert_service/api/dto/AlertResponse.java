package com.satrural.alert_service.api.dto;

import com.satrural.alert_service.domain.Alert;
import java.math.BigDecimal;
import java.time.Instant;

public record AlertResponse(
    Long id,
    Integer communityId,
    Integer sensorId,
    String type,
    String level,
    String message,
    Instant createdAt,
    Instant resolvedAt,
    boolean isActive,
    Long alertRuleId,
    BigDecimal detectedValue,
    BigDecimal thresholdValue,
    String status,
    Integer handledByUserId,
    Instant handledAt,
    Integer closedByUserId
) {
    public static AlertResponse from(Alert alert) {
        return new AlertResponse(
            alert.getId(),
            alert.getCommunityId(),
            alert.getSensorId(),
            alert.getPhenomenon(),
            alert.getRiskLevel(),
            alert.getMessage(),
            alert.getCreatedAt(),
            alert.getResolvedAt(),
            alert.isActive(),
            alert.getAlertRuleId(),
            alert.getDetectedValue(),
            alert.getThresholdValue(),
            alert.getStatus(),
            alert.getHandledByUserId(),
            alert.getHandledAt(),
            alert.getClosedByUserId()
        );
    }
}
