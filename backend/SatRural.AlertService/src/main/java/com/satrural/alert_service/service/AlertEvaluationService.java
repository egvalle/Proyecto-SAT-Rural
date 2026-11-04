package com.satrural.alert_service.service;

import com.satrural.alert_service.api.dto.AlertEvaluationResponse;
import com.satrural.alert_service.api.dto.EvaluateReadingRequest;
import com.satrural.alert_service.domain.Alert;
import com.satrural.alert_service.domain.AlertRule;
import com.satrural.alert_service.notification.AlertNotificationEvent;
import com.satrural.alert_service.repository.AlertRepository;
import com.satrural.alert_service.repository.AlertRuleRepository;
import java.math.BigDecimal;
import java.util.Comparator;
import java.util.List;
import java.util.Locale;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AlertEvaluationService {
    private final AlertRuleRepository ruleRepository;
    private final AlertRepository alertRepository;
    private final ApplicationEventPublisher eventPublisher;

    public AlertEvaluationService(
        AlertRuleRepository ruleRepository,
        AlertRepository alertRepository,
        ApplicationEventPublisher eventPublisher
    ) {
        this.ruleRepository = ruleRepository;
        this.alertRepository = alertRepository;
        this.eventPublisher = eventPublisher;
    }

    @Transactional
    public AlertEvaluationResponse evaluate(EvaluateReadingRequest request) {
        String sensorType = request.sensorType()
            .trim()
            .toUpperCase(Locale.ROOT);

        List<AlertRule> rules =
            ruleRepository.findBySensorTypeIgnoreCaseAndActiveTrue(sensorType);

        AlertRule matchedRule = rules.stream()
            .filter(rule -> matches(request.value(), rule))
            .max(
                Comparator
                    .comparingInt(
                        (AlertRule rule) -> severity(rule.getRiskLevel())
                    )
                    .thenComparing(AlertRule::getThresholdValue)
            )
            .orElse(null);

        List<Alert> activeAlerts =
            alertRepository.findBySensorIdAndSensorTypeAndActiveTrue(
                request.sensorId(),
                sensorType
            );

        if (matchedRule == null) {
            if (activeAlerts.isEmpty()) {
                return response("NORMAL", false, null, false);
            }

            activeAlerts.forEach(alert -> {
                alert.setActive(false);
                alert.setResolvedAt(request.recordedAt());
            });

            alertRepository.saveAll(activeAlerts);

            Alert resolvedAlert = activeAlerts.get(0);

            return new AlertEvaluationResponse(
                "RESOLVED",
                false,
                resolvedAlert.getId(),
                resolvedAlert.getRiskLevel(),
                resolvedAlert.getPhenomenon(),
                resolvedAlert.getMessage(),
                request.value(),
                resolvedAlert.getThresholdValue(),
                false
            );
        }

        String matchedPhenomenon = matchedRule.getPhenomenon();

        Alert currentAlert = activeAlerts.stream()
            .filter(alert -> alert.getPhenomenon().equals(matchedPhenomenon))
            .findFirst()
            .orElse(null);

        List<Alert> alertsToResolve = activeAlerts.stream()
            .filter(alert -> !alert.getPhenomenon().equals(matchedPhenomenon))
            .toList();

        alertsToResolve.forEach(alert -> {
            alert.setActive(false);
            alert.setResolvedAt(request.recordedAt());
        });

        if (currentAlert == null) {
            currentAlert = new Alert();
            currentAlert.setSensorId(request.sensorId());
            currentAlert.setSensorCode(request.sensorCode());
            currentAlert.setCommunityId(request.communityId());
            currentAlert.setSensorType(sensorType);
            currentAlert.setPhenomenon(matchedPhenomenon);
            currentAlert.setCreatedAt(request.recordedAt());
            currentAlert.setActive(true);
        }

        boolean newlyActivated = currentAlert.getId() == null;

        int previousSeverity = newlyActivated
            ? 0
            : severity(currentAlert.getRiskLevel());

        currentAlert.setAlertRuleId(matchedRule.getId());
        currentAlert.setRiskLevel(matchedRule.getRiskLevel());
        currentAlert.setMessage(matchedRule.getDescription());
        currentAlert.setDetectedValue(request.value());
        currentAlert.setThresholdValue(matchedRule.getThresholdValue());
        currentAlert.setActive(true);
        currentAlert.setResolvedAt(null);

        if (!alertsToResolve.isEmpty()) {
            alertRepository.saveAll(alertsToResolve);
        }

        Alert savedAlert = alertRepository.save(currentAlert);

        boolean escalated = !newlyActivated
            && severity(matchedRule.getRiskLevel()) > previousSeverity;

        String outcome = newlyActivated
            ? "ACTIVATED"
            : escalated ? "ESCALATED" : "UPDATED";

        boolean notificationRequired = newlyActivated || escalated;

        if (notificationRequired) {
            eventPublisher.publishEvent(
                new AlertNotificationEvent(
                    savedAlert.getId(),
                    savedAlert.getCommunityId(),
                    savedAlert.getSensorCode(),
                    savedAlert.getPhenomenon(),
                    savedAlert.getRiskLevel(),
                    request.recordedAt(),
                    outcome
                )
            );
        }

        return new AlertEvaluationResponse(
            outcome,
            true,
            savedAlert.getId(),
            savedAlert.getRiskLevel(),
            savedAlert.getPhenomenon(),
            savedAlert.getMessage(),
            savedAlert.getDetectedValue(),
            savedAlert.getThresholdValue(),
            notificationRequired
        );
    }

    private static boolean matches(BigDecimal value, AlertRule rule) {
        int comparison = value.compareTo(rule.getThresholdValue());

        return switch (rule.getOperator()) {
            case ">=" -> comparison >= 0;
            case "<=" -> comparison <= 0;
            default -> false;
        };
    }

    private static int severity(String riskLevel) {
        return switch (riskLevel.toUpperCase(Locale.ROOT)) {
            case "YELLOW" -> 1;
            case "ORANGE" -> 2;
            case "RED" -> 3;
            default -> 0;
        };
    }

    private static AlertEvaluationResponse response(
        String outcome,
        boolean active,
        Alert alert,
        boolean notificationRequired
    ) {
        return new AlertEvaluationResponse(
            outcome,
            active,
            alert == null ? null : alert.getId(),
            alert == null ? null : alert.getRiskLevel(),
            alert == null ? null : alert.getPhenomenon(),
            alert == null ? null : alert.getMessage(),
            alert == null ? null : alert.getDetectedValue(),
            alert == null ? null : alert.getThresholdValue(),
            notificationRequired
        );
    }
}