package com.satrural.alert_service.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;

@Entity
@Table(name = "alert_rules")
public class AlertRule {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "sensor_type", nullable = false, length = 50)
    private String sensorType;

    @Column(name = "operator", nullable = false, length = 5)
    private String operator;

    @Column(name = "threshold_value", nullable = false, precision = 8, scale = 2)
    private BigDecimal thresholdValue;

    @Column(name = "risk_level", nullable = false, length = 10)
    private String riskLevel;

    @Column(nullable = false, length = 100)
    private String phenomenon;

    @Column(nullable = false, length = 250)
    private String description;

    @Column(name = "is_active", nullable = false)
    private boolean active;

    protected AlertRule() {
    }

    public Long getId() { return id; }
    public String getSensorType() { return sensorType; }
    public String getOperator() { return operator; }
    public BigDecimal getThresholdValue() { return thresholdValue; }
    public String getRiskLevel() { return riskLevel; }
    public String getPhenomenon() { return phenomenon; }
    public String getDescription() { return description; }
    public boolean isActive() { return active; }

    public void setId(Long id) { this.id = id; }
    public void setSensorType(String sensorType) { this.sensorType = sensorType; }
    public void setOperator(String operator) { this.operator = operator; }
    public void setThresholdValue(BigDecimal thresholdValue) { this.thresholdValue = thresholdValue; }
    public void setRiskLevel(String riskLevel) { this.riskLevel = riskLevel; }
    public void setPhenomenon(String phenomenon) { this.phenomenon = phenomenon; }
    public void setDescription(String description) { this.description = description; }
    public void setActive(boolean active) { this.active = active; }
}