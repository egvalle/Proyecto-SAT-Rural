package com.satrural.alert_service.notification;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Component;
import org.springframework.transaction.event.TransactionPhase;
import org.springframework.transaction.event.TransactionalEventListener;

@Component
public class AlertEmailNotificationListener {
    private static final Logger LOGGER =
        LoggerFactory.getLogger(AlertEmailNotificationListener.class);

    private final AlertEmailService emailService;

    public AlertEmailNotificationListener(AlertEmailService emailService) {
        this.emailService = emailService;
    }

    @Async
    @TransactionalEventListener(phase = TransactionPhase.AFTER_COMMIT)
    public void handle(AlertNotificationEvent event) {
        try {
            emailService.sendAlertNotification(event);
        } catch (RuntimeException exception) {
            LOGGER.error(
                "No se pudo enviar el correo de la alerta {}.",
                event.alertId(),
                exception
            );
        }
    }
}