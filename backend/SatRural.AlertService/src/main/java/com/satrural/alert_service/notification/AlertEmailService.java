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

    public AlertEmailService(
        JavaMailSender mailSender,
        @Value("${spring.mail.username:}") String sender,
        @Value("${satrural.notifications.recipients:}") String recipientsConfiguration
    ) {
        this.mailSender = mailSender;
        this.sender = sender;
        this.recipientsConfiguration = recipientsConfiguration;
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
                event.communityId(),
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
}