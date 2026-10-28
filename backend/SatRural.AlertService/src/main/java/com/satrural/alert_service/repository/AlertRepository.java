package com.satrural.alert_service.repository;

import com.satrural.alert_service.domain.Alert;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.List;

public interface AlertRepository extends JpaRepository<Alert, Long> {
    List<Alert> findBySensorIdAndSensorTypeAndActiveTrue(
        Integer sensorId,
        String sensorType
    );
}