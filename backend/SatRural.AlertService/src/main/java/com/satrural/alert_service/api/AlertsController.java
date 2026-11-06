package com.satrural.alert_service.api;

import com.satrural.alert_service.api.dto.AlertResponse;
import com.satrural.alert_service.domain.Alert;
import com.satrural.alert_service.domain.ClimateEvent;
import com.satrural.alert_service.repository.AlertRepository;
import com.satrural.alert_service.repository.ClimateEventRepository;
import com.satrural.alert_service.service.AuditLogClient;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.ResponseEntity;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/alerts")
public class AlertsController {
    private final AlertRepository alertRepository;
    private final ClimateEventRepository eventRepository;
    private final AuditLogClient auditLogClient;

    public AlertsController(AlertRepository alertRepository,
                            ClimateEventRepository eventRepository,
                            AuditLogClient auditLogClient) {
        this.alertRepository = alertRepository;
        this.eventRepository = eventRepository;
        this.auditLogClient = auditLogClient;
    }

    @GetMapping
    public List<AlertResponse> getAlerts(
        @RequestParam(required = false) String level,
        @RequestParam(required = false) Boolean isActive,
        @RequestParam(required = false) String status,
        @RequestParam(required = false) Integer communityId,
        @RequestParam(required = false) Integer sensorId,
        @RequestParam(required = false) String phenomenon,
        @RequestParam(required = false) LocalDate from,
        @RequestParam(required = false) LocalDate to,
        @RequestParam(defaultValue = "1") int page,
        @RequestParam(defaultValue = "20") int pageSize
    ) {
        Pageable pageable = PageRequest.of(
            Math.max(page, 1) - 1,
            Math.clamp(pageSize, 1, 100),
            Sort.by(Sort.Direction.DESC, "createdAt")
        );

        String normalizedLevel = level == null || level.isBlank()
            ? null
            : level.trim().toUpperCase(Locale.ROOT);
        String normalizedStatus = status == null || status.isBlank()
            ? null
            : status.trim().toUpperCase(Locale.ROOT);

        Specification<Alert> filters = (root, query, builder) -> builder.conjunction();
        if (normalizedLevel != null)
            filters = filters.and((root, query, builder) ->
                builder.equal(builder.upper(root.get("riskLevel")), normalizedLevel));
        if (isActive != null)
            filters = filters.and((root, query, builder) ->
                builder.equal(root.get("active"), isActive));
        if (normalizedStatus != null)
            filters = filters.and((root, query, builder) ->
                builder.equal(root.get("status"), normalizedStatus));
        if (communityId != null)
            filters = filters.and((root, query, builder) ->
                builder.equal(root.get("communityId"), communityId));
        if (sensorId != null)
            filters = filters.and((root, query, builder) ->
                builder.equal(root.get("sensorId"), sensorId));
        if (phenomenon != null && !phenomenon.isBlank()) {
            String term = "%" + phenomenon.trim().toLowerCase(Locale.ROOT) + "%";
            filters = filters.and((root, query, builder) ->
                builder.like(builder.lower(root.get("phenomenon")), term));
        }
        if (from != null) {
            Instant start = from.atStartOfDay(ZoneOffset.UTC).toInstant();
            filters = filters.and((root, query, builder) ->
                builder.greaterThanOrEqualTo(root.get("createdAt"), start));
        }
        if (to != null) {
            Instant end = to.plusDays(1).atStartOfDay(ZoneOffset.UTC).toInstant();
            filters = filters.and((root, query, builder) ->
                builder.lessThan(root.get("createdAt"), end));
        }

        Page<Alert> alerts = alertRepository.findAll(filters, pageable);

        return alerts.stream().map(AlertResponse::from).toList();
    }

    @GetMapping("/{id}")
    public ResponseEntity<?> getById(@PathVariable long id) {
        return alertRepository.findById(id)
            .<ResponseEntity<?>>map(alert -> ResponseEntity.ok(AlertResponse.from(alert)))
            .orElseGet(() -> ResponseEntity.status(404)
                .body(Map.of("message", "Alerta no encontrada.")));
    }

    @PatchMapping("/{id}/attend")
    @Transactional
    public ResponseEntity<Map<String, String>> attendAlert(
        @PathVariable long id, JwtAuthenticationToken authentication
    ) {
        Alert alert = alertRepository.findById(id).orElse(null);
        if (alert == null)
            return ResponseEntity.status(404).body(Map.of("message", "Alerta no encontrada."));
        if (!alert.isActive())
            return ResponseEntity.badRequest().body(Map.of("message", "La alerta ya está cerrada."));
        if ("ATTENDED".equals(alert.getStatus()))
            return ResponseEntity.ok(Map.of("message", "La alerta ya estaba atendida."));

        alert.setStatus("ATTENDED");
        alert.setHandledByUserId(Integer.parseInt(authentication.getToken().getSubject()));
        alert.setHandledAt(Instant.now());
        alertRepository.save(alert);
        TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
            @Override
            public void afterCommit() {
                auditLogClient.recordAlert(authentication.getToken(), "ATTEND", id,
                    "Alerta marcada como atendida.");
            }
        });
        return ResponseEntity.ok(Map.of("message", "Alerta marcada como atendida."));
    }

    @PatchMapping("/{id}/resolve")
    @Transactional
    public ResponseEntity<Map<String, String>> resolveAlert(
        @PathVariable long id, JwtAuthenticationToken authentication
    ) {
        Alert alert = alertRepository.findById(id).orElse(null);
        if (alert == null) {
            return ResponseEntity.status(404)
                .body(Map.of("message", "Alerta no encontrada."));
        }

        if (!alert.isActive()) {
            return ResponseEntity.ok(
                Map.of("message", "La alerta ya estaba resuelta."));
        }

        alert.setActive(false);
        alert.setResolvedAt(Instant.now());
        alert.setStatus("CLOSED");
        alert.setClosedByUserId(Integer.parseInt(authentication.getToken().getSubject()));
        alertRepository.save(alert);
        ClimateEvent event = new ClimateEvent();
        event.setAlertId(alert.getId());
        event.setCommunityId(alert.getCommunityId());
        event.setEventType(alert.getPhenomenon());
        event.setDescription("Alerta resuelta: " + alert.getMessage());
        event.setOccurredAt(alert.getResolvedAt());
        event.setSensorId(alert.getSensorId());
        event.setLevel(alert.getRiskLevel());
        event.setActive(false);
        eventRepository.save(event);
        TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
            @Override
            public void afterCommit() {
                auditLogClient.recordAlert(authentication.getToken(), "CLOSE", id,
                    "Alerta marcada como resuelta.");
            }
        });

        return ResponseEntity.ok(
            Map.of("message", "Alerta marcada como resuelta."));
    }
}
