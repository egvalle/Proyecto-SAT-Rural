package com.satrural.alert_service.repository;

import com.satrural.alert_service.domain.AlertRule;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.List;

public interface AlertRuleRepository extends JpaRepository<AlertRule, Long> {
    List<AlertRule> findBySensorTypeIgnoreCaseAndActiveTrue(String sensorType);
}