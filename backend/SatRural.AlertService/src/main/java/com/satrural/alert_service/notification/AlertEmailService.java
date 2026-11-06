package com.satrural.alert_service.notification;

import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.Arrays;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;

@Service
public class AlertEmailService {
    private static final Logger LOGGER =
        LoggerFactory.getLogger(AlertEmailService.class);

    private static final DateTimeFormatter DATE_FORMAT =
        DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss 'UTC'")
            .withZone(ZoneOffset.UTC);

    private final JavaMailSender mailSender;
    private final String sender;
    private final String recipientsConfiguration;
    private final RestClient communityClient;

    public AlertEmailService(
        JavaMailSender mailSender,
        @Value("${spring.mail.username:}") String sender,
        @Value("${satrural.notifications.recipients:}") String recipientsConfiguration,
        @Value("${satrural.community.base-url}") String communityBaseUrl
    ) {
        this.mailSender = mailSender;
        this.sender = sender;
        this.recipientsConfiguration = recipientsConfiguration;
        this.communityClient = RestClient.builder().baseUrl(communityBaseUrl).build();
    }

    public void sendAlertNotification(AlertNotificationEvent event) {
        String[] recipients = Arrays.stream(
                recipientsConfiguration.split(",")
            )
            .map(String::trim)
            .filter(value -> !value.isBlank())
            .toArray(String[]::new);

        if (sender.isBlank() || recipients.length == 0) {
            LOGGER.warn(
                "No se envió el correo de la alerta {}: falta remitente o destinatarios.",
                event.alertId()
            );
            return;
        }

        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(sender);
        message.setTo(recipients);
        message.setSubject(
            "[SAT-Rural][" + event.riskLevel() + "] " + event.phenomenon()
        );
        message.setText(
            """
            Se activó o escaló una alerta en SAT-Rural.

            Comunidad: %s
            Sensor: %s
            Fenómeno: %s
            Nivel: %s
            Fecha y hora: %s
            Evento: %s
            """.formatted(
                communityName(event.communityId()),
                event.sensorCode(),
                event.phenomenon(),
                event.riskLevel(),
                DATE_FORMAT.format(event.occurredAt()),
                event.outcome()
            )
        );

        mailSender.send(message);

        LOGGER.info(
            "Correo de alerta enviado. alertId={}, nivel={}, destinatarios={}",
            event.alertId(),
            event.riskLevel(),
            recipients.length
        );
    }

    private String communityName(int communityId) {
        try {
            CommunityLookup community = communityClient.get()
                .uri("/api/communities/{id}", communityId)
                .retrieve()
                .body(CommunityLookup.class);
            if (community != null && community.name() != null && !community.name().isBlank()) {
                return community.name();
            }
        } catch (Exception exception) {
            LOGGER.warn("No se pudo consultar la comunidad {} para el correo.", communityId);
        }
        return "Comunidad " + communityId;
    }

    private record CommunityLookup(String name) {}
}
