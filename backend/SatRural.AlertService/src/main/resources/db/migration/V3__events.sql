CREATE TABLE climate_events (
    id BIGSERIAL PRIMARY KEY,
    legacy_id BIGINT UNIQUE,
    alert_id BIGINT REFERENCES alerts(id) ON DELETE SET NULL,
    community_id INTEGER NOT NULL,
    event_type TEXT NOT NULL,
    description TEXT NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    sensor_id INTEGER,
    level VARCHAR(10),
    is_active BOOLEAN
);

CREATE INDEX ix_climate_events_occurred_at
    ON climate_events (occurred_at DESC);

CREATE INDEX ix_climate_events_community
    ON climate_events (community_id, occurred_at DESC);
