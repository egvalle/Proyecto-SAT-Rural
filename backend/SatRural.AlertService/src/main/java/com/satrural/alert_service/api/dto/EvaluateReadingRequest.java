package com.satrural.alert_service.api.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;
import jakarta.validation.constraints.Size;
import java.math.BigDecimal;
import java.time.Instant;

public record EvaluateReadingRequest(
    @NotNull @Positive Integer sensorId,
    @NotBlank @Size(max = 50) String sensorCode,
    @NotNull @Positive Integer communityId,
    @NotBlank @Size(max = 50) String sensorType,
    @NotNull BigDecimal value,
    @NotNull Instant recordedAt
) {
}