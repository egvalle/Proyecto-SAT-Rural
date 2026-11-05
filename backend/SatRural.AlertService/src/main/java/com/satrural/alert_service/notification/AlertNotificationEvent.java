package com.satrural.alert_service.notification;

import java.time.Instant;

public record AlertNotificationEvent(
    Long alertId,
    Integer communityId,
    String sensorCode,
    String phenomenon,
    String riskLevel,
    Instant occurredAt,
    String outcome
) {
}