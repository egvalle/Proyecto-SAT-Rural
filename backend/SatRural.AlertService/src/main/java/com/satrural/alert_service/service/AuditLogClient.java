package com.satrural.alert_service.service;

import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;

@Service
public class AuditLogClient {
    private static final Logger logger = LoggerFactory.getLogger(AuditLogClient.class);
    private static final String LEGACY_USERNAME_CLAIM =
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

    private final RestClient client;
    private final String serviceKey;

    public AuditLogClient(
        @Value("${satrural.audit.base-url}") String baseUrl,
        @Value("${satrural.audit.api-key}") String serviceKey
    ) {
        this.client = RestClient.builder().baseUrl(baseUrl).build();
        this.serviceKey = serviceKey;
    }

    public void record(Jwt jwt, String action, long ruleId, String details) {
        send(jwt, action, "AlertRule", ruleId, details);
    }

    public void recordAlert(Jwt jwt, String action, long alertId, String details) {
        send(jwt, action, "Alert", alertId, details);
    }

    private void send(Jwt jwt, String action, String entityName, long entityId, String details) {
        String username = jwt.getClaimAsString("email");
        if (username == null) username = jwt.getClaimAsString(LEGACY_USERNAME_CLAIM);
        if (username == null) username = "usuario-" + jwt.getSubject();

        try {
            client.post()
                .uri("/internal/audit-logs")
                .header("X-Service-Key", serviceKey)
                .body(Map.of(
                    "userId", Integer.parseInt(jwt.getSubject()),
                    "username", username,
                    "action", action,
                    "entityName", entityName,
                    "entityId", Long.toString(entityId),
                    "details", details
                ))
                .retrieve()
                .toBodilessEntity();
        } catch (Exception exception) {
            logger.warn("No se pudo registrar la auditoría de {} {}.",
                entityName, entityId, exception);
        }
    }
}
