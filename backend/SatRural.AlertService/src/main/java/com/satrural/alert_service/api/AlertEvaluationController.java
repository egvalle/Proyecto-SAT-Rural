package com.satrural.alert_service.api;

import com.satrural.alert_service.api.dto.AlertEvaluationResponse;
import com.satrural.alert_service.api.dto.EvaluateReadingRequest;
import com.satrural.alert_service.service.AlertEvaluationService;
import jakarta.validation.Valid;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Map;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/internal/alerts")
public class AlertEvaluationController {
    private final AlertEvaluationService evaluationService;
    private final String serviceKey;

    public AlertEvaluationController(
        AlertEvaluationService evaluationService,
        @Value("${satrural.integration.api-key}") String serviceKey
    ) {
        if (serviceKey == null || serviceKey.length() < 32) {
            throw new IllegalArgumentException(
                "ALERT_SERVICE_API_KEY must contain at least 32 characters."
            );
        }

        this.evaluationService = evaluationService;
        this.serviceKey = serviceKey;
    }

    @PostMapping("/evaluate")
    public ResponseEntity<?> evaluate(
        @RequestHeader(value = "X-Service-Key", required = false) String providedKey,
        @Valid @RequestBody EvaluateReadingRequest request
    ) {
        if (!hasValidKey(providedKey)) {
            return ResponseEntity
                .status(HttpStatus.UNAUTHORIZED)
                .body(Map.of("message", "Invalid service key."));
        }

        AlertEvaluationResponse response =
            evaluationService.evaluate(request);

        return ResponseEntity.ok(response);
    }

    private boolean hasValidKey(String providedKey) {
        if (providedKey == null || providedKey.isBlank()) {
            return false;
        }

        return MessageDigest.isEqual(
            serviceKey.getBytes(StandardCharsets.UTF_8),
            providedKey.getBytes(StandardCharsets.UTF_8)
        );
    }
}