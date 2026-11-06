ALTER TABLE alerts ADD COLUMN legacy_id BIGINT UNIQUE;
ALTER TABLE alerts ALTER COLUMN sensor_id DROP NOT NULL;
ALTER TABLE alerts ALTER COLUMN detected_value DROP NOT NULL;
ALTER TABLE alerts ALTER COLUMN threshold_value DROP NOT NULL;

DROP INDEX ux_alerts_one_active_per_sensor_phenomenon;
CREATE UNIQUE INDEX ux_alerts_one_active_per_sensor_phenomenon
    ON alerts (sensor_id, phenomenon)
    WHERE is_active = TRUE AND legacy_id IS NULL;
