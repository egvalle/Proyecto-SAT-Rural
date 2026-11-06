package com.satrural.alert_service.api;

import com.satrural.alert_service.domain.AlertRule;
import com.satrural.alert_service.repository.AlertRuleRepository;
import com.satrural.alert_service.service.AuditLogClient;
import java.math.BigDecimal;
import java.net.URI;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.ResponseEntity;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/alert-rules")
public class AlertRulesController {
    private static final Set<String> SENSOR_TYPES = Set.of(
        "TEMPERATURE", "HUMIDITY", "WIND_SPEED", "RAINFALL", "RIVER_LEVEL"
    );
    private static final Set<String> RISK_LEVELS = Set.of("YELLOW", "ORANGE", "RED");

    private final AlertRuleRepository repository;
    private final AuditLogClient auditLogClient;

    public AlertRulesController(AlertRuleRepository repository, AuditLogClient auditLogClient) {
        this.repository = repository;
        this.auditLogClient = auditLogClient;
    }

    @GetMapping
    public List<AlertRuleResponse> getAll() {
        return repository.findAll().stream()
            .sorted((left, right) -> left.getId().compareTo(right.getId()))
            .map(AlertRuleResponse::from)
            .toList();
    }

    @GetMapping("/{id}")
    public ResponseEntity<?> getById(@PathVariable long id) {
        return repository.findById(id)
            .<ResponseEntity<?>>map(rule -> ResponseEntity.ok(AlertRuleResponse.from(rule)))
            .orElseGet(() -> ResponseEntity.status(404)
                .body(Map.of("message", "Alert rule not found.")));
    }

    @PostMapping
    public ResponseEntity<?> create(@RequestBody AlertRuleRequest request,
                                    JwtAuthenticationToken authentication) {
        String error = validate(request);
        if (error != null) {
            return ResponseEntity.badRequest().body(Map.of("message", error));
        }

        AlertRule rule = new AlertRule();
        copy(request, rule);
        rule.setActive(true);
        try {
            AlertRule saved = repository.save(rule);
            auditLogClient.record(authentication.getToken(), "CREATE", saved.getId(),
                "Created alert rule: " + saved.getSensorType() + " " + saved.getRiskLevel());
            return ResponseEntity.created(URI.create("/api/alert-rules/" + saved.getId()))
                .body(AlertRuleResponse.from(saved));
        } catch (DataIntegrityViolationException exception) {
            return ResponseEntity.status(409)
                .body(Map.of("message", "An equivalent alert rule already exists."));
        }
    }

    @PutMapping("/{id}")
    public ResponseEntity<?> update(@PathVariable long id, @RequestBody AlertRuleRequest request,
                                    JwtAuthenticationToken authentication) {
        String error = validate(request);
        if (error != null) {
            return ResponseEntity.badRequest().body(Map.of("message", error));
        }

        AlertRule rule = repository.findById(id).orElse(null);
        if (rule == null) {
            return ResponseEntity.status(404).body(Map.of("message", "Alert rule not found."));
        }

        copy(request, rule);
        try {
            AlertRule saved = repository.save(rule);
            auditLogClient.record(authentication.getToken(), "UPDATE", saved.getId(),
                "Updated alert rule: " + saved.getSensorType() + " " + saved.getRiskLevel());
            return ResponseEntity.ok(AlertRuleResponse.from(saved));
        } catch (DataIntegrityViolationException exception) {
            return ResponseEntity.status(409)
                .body(Map.of("message", "An equivalent alert rule already exists."));
        }
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<?> delete(@PathVariable long id,
                                    JwtAuthenticationToken authentication) {
        if (!repository.existsById(id)) {
            return ResponseEntity.status(404).body(Map.of("message", "Alert rule not found."));
        }
        repository.deleteById(id);
        auditLogClient.record(authentication.getToken(), "DELETE", id, "Deleted alert rule.");
        return ResponseEntity.noContent().build();
    }

    private static String validate(AlertRuleRequest request) {
        if (request == null || request.sensorType() == null ||
            !SENSOR_TYPES.contains(request.sensorType().trim().toUpperCase(Locale.ROOT))) {
            return "Invalid SensorType.";
        }
        if (!">=".equals(request.operator()) && !"<=".equals(request.operator())) {
            return "Operator must be '>=' or '<='.";
        }
        if (request.thresholdValue() == null ||
            (request.thresholdValue().signum() < 0 &&
                !"TEMPERATURE".equalsIgnoreCase(request.sensorType().trim()))) {
            return "Invalid ThresholdValue.";
        }
        if (request.riskLevel() == null ||
            !RISK_LEVELS.contains(request.riskLevel().trim().toUpperCase(Locale.ROOT))) {
            return "Invalid RiskLevel.";
        }
        if (request.phenomenon() == null || request.phenomenon().isBlank() ||
            request.phenomenon().trim().length() > 100) {
            return "Phenomenon is required and must be at most 100 characters.";
        }
        if (request.description() == null || request.description().isBlank() ||
            request.description().trim().length() > 250) {
            return "Description is required and must be at most 250 characters.";
        }
        return null;
    }

    private static void copy(AlertRuleRequest request, AlertRule rule) {
        rule.setSensorType(request.sensorType().trim().toUpperCase(Locale.ROOT));
        rule.setOperator(request.operator());
        rule.setThresholdValue(request.thresholdValue());
        rule.setRiskLevel(request.riskLevel().trim().toUpperCase(Locale.ROOT));
        rule.setPhenomenon(request.phenomenon().trim());
        rule.setDescription(request.description().trim());
    }

    public record AlertRuleRequest(
        String sensorType, String operator, BigDecimal thresholdValue,
        String riskLevel, String phenomenon, String description
    ) {}

    public record AlertRuleResponse(
        Long id, String sensorType, String operator, BigDecimal thresholdValue,
        String riskLevel, String phenomenon, String description
    ) {
        static AlertRuleResponse from(AlertRule rule) {
            return new AlertRuleResponse(
                rule.getId(), rule.getSensorType(), rule.getOperator(),
                rule.getThresholdValue(), rule.getRiskLevel(),
                rule.getPhenomenon(), rule.getDescription()
            );
        }
    }
}
