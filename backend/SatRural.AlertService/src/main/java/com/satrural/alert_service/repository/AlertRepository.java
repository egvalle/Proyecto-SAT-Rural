package com.satrural.alert_service.repository;

import com.satrural.alert_service.domain.Alert;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import java.util.List;

public interface AlertRepository extends JpaRepository<Alert, Long>, JpaSpecificationExecutor<Alert> {
    Page<Alert> findByRiskLevelIgnoreCase(String riskLevel, Pageable pageable);

    Page<Alert> findByActive(boolean active, Pageable pageable);

    Page<Alert> findByRiskLevelIgnoreCaseAndActive(
        String riskLevel,
        boolean active,
        Pageable pageable
    );

    List<Alert> findBySensorIdAndSensorTypeAndActiveTrue(
        Integer sensorId,
        String sensorType
    );
}
