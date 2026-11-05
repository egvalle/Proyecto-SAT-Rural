package com.satrural.alert_service.api.dto;

import java.math.BigDecimal;

public record AlertEvaluationResponse(
    String outcome,
    boolean active,
    Long alertId,
    String riskLevel,
    String phenomenon,
    String message,
    BigDecimal detectedValue,
    BigDecimal thresholdValue,
    boolean notificationRequired
) {
}