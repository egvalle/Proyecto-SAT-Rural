CREATE TABLE alert_rules (
    id BIGSERIAL PRIMARY KEY,
    sensor_type VARCHAR(50) NOT NULL,
    operator VARCHAR(5) NOT NULL,
    threshold_value NUMERIC(8, 2) NOT NULL,
    risk_level VARCHAR(10) NOT NULL,
    phenomenon VARCHAR(100) NOT NULL,
    description VARCHAR(250) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT ck_alert_rules_operator
        CHECK (operator IN ('>=', '<=')),
    CONSTRAINT ck_alert_rules_risk_level
        CHECK (risk_level IN ('YELLOW', 'ORANGE', 'RED')),
    CONSTRAINT uq_alert_rules_type_phenomenon_operator_level
        UNIQUE (sensor_type, phenomenon, operator, risk_level)
);

CREATE TABLE alerts (
    id BIGSERIAL PRIMARY KEY,
    alert_rule_id BIGINT NULL
        REFERENCES alert_rules(id) ON DELETE SET NULL,
    sensor_id INTEGER NOT NULL,
    sensor_code VARCHAR(50) NOT NULL,
    community_id INTEGER NOT NULL,
    sensor_type VARCHAR(50) NOT NULL,
    phenomenon VARCHAR(100) NOT NULL,
    risk_level VARCHAR(10) NOT NULL,
    message VARCHAR(250) NOT NULL,
    detected_value NUMERIC(8, 2) NOT NULL,
    threshold_value NUMERIC(8, 2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMPTZ NULL,

    CONSTRAINT ck_alerts_risk_level
        CHECK (risk_level IN ('YELLOW', 'ORANGE', 'RED'))
);

CREATE INDEX ix_alerts_sensor_created
    ON alerts (sensor_id, created_at DESC);

CREATE UNIQUE INDEX ux_alerts_one_active_per_sensor_phenomenon
    ON alerts (sensor_id, phenomenon)
    WHERE is_active = TRUE;

INSERT INTO alert_rules
    (id, sensor_type, operator, threshold_value, risk_level, phenomenon, description)
VALUES
    (1,  'TEMPERATURE', '>=', 32.00, 'YELLOW', 'Ola de calor', 'Temperatura elevada'),
    (2,  'TEMPERATURE', '>=', 35.00, 'ORANGE', 'Ola de calor', 'Temperatura muy elevada'),
    (3,  'TEMPERATURE', '>=', 38.00, 'RED',    'Ola de calor', 'Temperatura extremadamente elevada'),
    (4,  'TEMPERATURE', '<=',  8.00, 'YELLOW', 'Helada', 'Temperatura baja'),
    (5,  'TEMPERATURE', '<=',  4.00, 'ORANGE', 'Helada', 'Temperatura muy baja'),
    (6,  'TEMPERATURE', '<=',  0.00, 'RED',    'Helada', 'Temperatura extremadamente baja'),
    (7,  'HUMIDITY',    '<=', 40.00, 'YELLOW', 'Sequía', 'Humedad baja'),
    (8,  'HUMIDITY',    '<=', 30.00, 'ORANGE', 'Sequía', 'Humedad muy baja'),
    (9,  'HUMIDITY',    '<=', 20.00, 'RED',    'Sequía', 'Humedad extremadamente baja'),
    (10, 'WIND_SPEED',  '>=', 30.00, 'YELLOW', 'Tormenta', 'Velocidad del viento elevada'),
    (11, 'WIND_SPEED',  '>=', 50.00, 'ORANGE', 'Tormenta', 'Velocidad del viento muy elevada'),
    (12, 'WIND_SPEED',  '>=', 70.00, 'RED',    'Tormenta', 'Velocidad del viento extremadamente elevada'),
    (13, 'RAINFALL',    '>=', 15.00, 'YELLOW', 'Inundación', 'Nivel de lluvia elevado'),
    (14, 'RAINFALL',    '>=', 30.00, 'ORANGE', 'Inundación', 'Nivel de lluvia muy elevado'),
    (15, 'RAINFALL',    '>=', 50.00, 'RED',    'Inundación', 'Nivel de lluvia extremadamente elevado'),
    (16, 'RIVER_LEVEL', '>=', 60.00, 'YELLOW', 'Inundación', 'Nivel del río elevado'),
    (17, 'RIVER_LEVEL', '>=', 75.00, 'ORANGE', 'Inundación', 'Nivel del río muy elevado'),
    (18, 'RIVER_LEVEL', '>=', 90.00, 'RED',    'Inundación', 'Nivel del río extremadamente elevado'),
    (22, 'HUMIDITY',    '>=', 60.00, 'YELLOW', 'Humedad ambiental alta', 'Humedad ambiental elevada'),
    (23, 'HUMIDITY',    '>=', 70.00, 'ORANGE', 'Humedad ambiental alta', 'Humedad ambiental muy elevada'),
    (24, 'HUMIDITY',    '>=', 80.00, 'RED',    'Humedad ambiental alta', 'Humedad ambiental extremadamente elevada');

SELECT setval(
    pg_get_serial_sequence('alert_rules', 'id'),
    (SELECT MAX(id) FROM alert_rules),
    TRUE
);