package com.satrural.alert_service.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;

@Entity
@Table(name = "climate_events")
public class ClimateEvent {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "alert_id")
    private Long alertId;

    @Column(name = "community_id", nullable = false)
    private Integer communityId;

    @Column(name = "event_type", nullable = false, columnDefinition = "text")
    private String eventType;

    @Column(nullable = false, columnDefinition = "text")
    private String description;

    @Column(name = "occurred_at", nullable = false)
    private Instant occurredAt;

    @Column(name = "sensor_id")
    private Integer sensorId;

    @Column(length = 10)
    private String level;

    @Column(name = "is_active")
    private Boolean active;

    public Long getId() { return id; }
    public Long getAlertId() { return alertId; }
    public Integer getCommunityId() { return communityId; }
    public String getEventType() { return eventType; }
    public String getDescription() { return description; }
    public Instant getOccurredAt() { return occurredAt; }
    public Integer getSensorId() { return sensorId; }
    public String getLevel() { return level; }
    public Boolean isActive() { return active; }

    public void setAlertId(Long alertId) { this.alertId = alertId; }
    public void setCommunityId(Integer communityId) { this.communityId = communityId; }
    public void setEventType(String eventType) { this.eventType = eventType; }
    public void setDescription(String description) { this.description = description; }
    public void setOccurredAt(Instant occurredAt) { this.occurredAt = occurredAt; }
    public void setSensorId(Integer sensorId) { this.sensorId = sensorId; }
    public void setLevel(String level) { this.level = level; }
    public void setActive(Boolean active) { this.active = active; }
}
