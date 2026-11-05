package com.satrural.alert_service.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.Instant;

@Entity
@Table(name = "alerts")
public class Alert {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "alert_rule_id")
    private Long alertRuleId;

    @Column(name = "sensor_id", nullable = false)
    private Integer sensorId;

    @Column(name = "sensor_code", nullable = false, length = 50)
    private String sensorCode;

    @Column(name = "community_id", nullable = false)
    private Integer communityId;

    @Column(name = "sensor_type", nullable = false, length = 50)
    private String sensorType;

    @Column(nullable = false, length = 100)
    private String phenomenon;

    @Column(name = "risk_level", nullable = false, length = 10)
    private String riskLevel;

    @Column(nullable = false, length = 250)
    private String message;

    @Column(name = "detected_value", nullable = false, precision = 8, scale = 2)
    private BigDecimal detectedValue;

    @Column(name = "threshold_value", nullable = false, precision = 8, scale = 2)
    private BigDecimal thresholdValue;

    @Column(name = "is_active", nullable = false)
    private boolean active;

    @Column(name = "created_at", nullable = false)
    private Instant createdAt;

    @Column(name = "resolved_at")
    private Instant resolvedAt;

    public Alert() {
    }

    public Long getId() { return id; }
    public Long getAlertRuleId() { return alertRuleId; }
    public Integer getSensorId() { return sensorId; }
    public String getSensorCode() { return sensorCode; }
    public Integer getCommunityId() { return communityId; }
    public String getSensorType() { return sensorType; }
    public String getPhenomenon() { return phenomenon; }
    public String getRiskLevel() { return riskLevel; }
    public String getMessage() { return message; }
    public BigDecimal getDetectedValue() { return detectedValue; }
    public BigDecimal getThresholdValue() { return thresholdValue; }
    public boolean isActive() { return active; }
    public Instant getCreatedAt() { return createdAt; }
    public Instant getResolvedAt() { return resolvedAt; }

    public void setId(Long id) { this.id = id; }
    public void setAlertRuleId(Long alertRuleId) { this.alertRuleId = alertRuleId; }
    public void setSensorId(Integer sensorId) { this.sensorId = sensorId; }
    public void setSensorCode(String sensorCode) { this.sensorCode = sensorCode; }
    public void setCommunityId(Integer communityId) { this.communityId = communityId; }
    public void setSensorType(String sensorType) { this.sensorType = sensorType; }
    public void setPhenomenon(String phenomenon) { this.phenomenon = phenomenon; }
    public void setRiskLevel(String riskLevel) { this.riskLevel = riskLevel; }
    public void setMessage(String message) { this.message = message; }
    public void setDetectedValue(BigDecimal detectedValue) { this.detectedValue = detectedValue; }
    public void setThresholdValue(BigDecimal thresholdValue) { this.thresholdValue = thresholdValue; }
    public void setActive(boolean active) { this.active = active; }
    public void setCreatedAt(Instant createdAt) { this.createdAt = createdAt; }
    public void setResolvedAt(Instant resolvedAt) { this.resolvedAt = resolvedAt; }
}