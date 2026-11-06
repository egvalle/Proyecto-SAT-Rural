import sql from 'mssql';
import pg from 'pg';

const required = (name) => {
  const value = process.env[name];
  if (!value) throw new Error(`Falta ${name}.`);
  return value;
};

const source = await sql.connect({
  server: 'sqlserver',
  database: required('MSSQL_DATABASE'),
  user: 'sa',
  password: required('MSSQL_SA_PASSWORD'),
  options: { encrypt: false, trustServerCertificate: true },
});
const community = new pg.Client({
  host: 'community-db', database: 'satrural_community',
  user: required('COMMUNITY_DB_USER'), password: required('COMMUNITY_DB_PASSWORD'),
});
const alerts = new pg.Client({
  host: 'alert-db', database: 'satrural_alerts',
  user: 'satrural_alerts', password: required('ALERT_DB_PASSWORD'),
});

try {
  await community.connect();
  await alerts.connect();
  for (let attempt = 0; attempt < 20; attempt += 1) {
    try {
      await alerts.query('SELECT status FROM alerts LIMIT 0');
      await alerts.query('SELECT legacy_id FROM climate_events LIMIT 0');
      break;
    } catch (error) {
      if (attempt === 19) throw error;
      await new Promise(resolve => setTimeout(resolve, 2000));
    }
  }

  const communities = (await source.request().query(`
    SELECT Id, Name, Municipality, Department, Country,
           Latitude, Longitude, Description, IsActive, CreatedAt
    FROM Communities ORDER BY Id
  `)).recordset;
  await community.query('BEGIN');
  try {
    for (const item of communities) {
      const existing = await community.query('SELECT "CreatedAt" FROM "Communities" WHERE "Id"=$1', [item.Id]);
      if (existing.rows[0] &&
          new Date(existing.rows[0].CreatedAt).getTime() !== new Date(item.CreatedAt).getTime()) {
        throw new Error(`Conflicto de identidad en la comunidad ${item.Id}.`);
      }
      await community.query(
        `INSERT INTO "Communities" ("Id", "Name", "Municipality", "Department", "Country",
         "Latitude", "Longitude", "Description", "IsActive", "CreatedAt")
         VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
         ON CONFLICT ("Id") DO UPDATE SET
           "Name"=EXCLUDED."Name", "Municipality"=EXCLUDED."Municipality",
           "Department"=EXCLUDED."Department", "Country"=EXCLUDED."Country",
           "Latitude"=EXCLUDED."Latitude", "Longitude"=EXCLUDED."Longitude",
           "Description"=EXCLUDED."Description", "IsActive"=EXCLUDED."IsActive"`,
        [item.Id, item.Name, item.Municipality, item.Department, item.Country,
          item.Latitude, item.Longitude, item.Description, item.IsActive, item.CreatedAt],
      );
    }
    await community.query(`SELECT setval(pg_get_serial_sequence('"Communities"','Id'),
      GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM "Communities"), 1), true)`);
    await community.query('COMMIT');
    process.stdout.write(`Comunidades de origen: ${communities.length}.\n`);
  } catch (error) {
    await community.query('ROLLBACK');
    throw error;
  }

  const rules = (await source.request().query(`
    SELECT Id, SensorType, [Operator], ThresholdValue, RiskLevel, Phenomenon, Description
    FROM AlertRules ORDER BY Id
  `)).recordset;
  const history = (await source.request().query(`
    SELECT a.Id, a.CommunityId, a.SensorId, a.Type, a.Level, a.Message,
           a.CreatedAt, a.ResolvedAt, a.IsActive, a.AlertRuleId,
           a.DetectedValue, a.ThresholdValue,
           s.Code AS SensorCode, s.Type AS SensorType,
           r.SensorType AS RuleSensorType
    FROM Alerts AS a
    LEFT JOIN Sensors AS s ON s.Id = a.SensorId
    LEFT JOIN AlertRules AS r ON r.Id = a.AlertRuleId
    ORDER BY a.Id
  `)).recordset;
  const events = (await source.request().query(`
    SELECT e.Id, e.AlertId, e.CommunityId, e.EventType,
           e.Description, e.OccurredAt,
           a.SensorId, a.Level, a.IsActive
    FROM Events AS e
    LEFT JOIN Alerts AS a ON a.Id = e.AlertId
    ORDER BY e.Id
  `)).recordset;
  await alerts.query('BEGIN');
  try {
    for (const rule of rules) {
      await alerts.query(
        `INSERT INTO alert_rules (id, sensor_type, operator, threshold_value,
         risk_level, phenomenon, description) VALUES ($1,$2,$3,$4,$5,$6,$7)
         ON CONFLICT (id) DO UPDATE SET sensor_type=EXCLUDED.sensor_type,
         operator=EXCLUDED.operator, threshold_value=EXCLUDED.threshold_value,
         risk_level=EXCLUDED.risk_level, phenomenon=EXCLUDED.phenomenon,
         description=EXCLUDED.description`,
        [rule.Id, rule.SensorType, rule.Operator, rule.ThresholdValue,
          rule.RiskLevel, rule.Phenomenon, rule.Description],
      );
    }
    await alerts.query(`SELECT setval(pg_get_serial_sequence('alert_rules','id'),
      GREATEST((SELECT COALESCE(MAX(id), 0) FROM alert_rules), 1), true)`);
    for (const item of history) {
      await alerts.query(
        `INSERT INTO alerts (legacy_id, alert_rule_id, sensor_id, sensor_code,
         community_id, sensor_type, phenomenon, risk_level, message,
         detected_value, threshold_value, is_active, created_at, resolved_at, status)
         VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15)
         ON CONFLICT (legacy_id) DO UPDATE SET is_active=EXCLUDED.is_active,
         resolved_at=EXCLUDED.resolved_at, detected_value=EXCLUDED.detected_value,
         threshold_value=EXCLUDED.threshold_value, status=EXCLUDED.status`,
        [item.Id, item.AlertRuleId, item.SensorId, item.SensorCode ?? 'LEGACY',
          item.CommunityId, item.SensorType ?? item.RuleSensorType ?? 'UNKNOWN',
          item.Type, item.Level, item.Message,
          item.DetectedValue, item.ThresholdValue, item.IsActive,
          item.CreatedAt, item.ResolvedAt,
          item.IsActive ? 'ACTIVE' : 'CLOSED'],
      );
    }
    for (const item of events) {
      await alerts.query(
        `INSERT INTO climate_events
         (legacy_id, alert_id, community_id, event_type, description,
          occurred_at, sensor_id, level, is_active)
         VALUES ($1, (SELECT id FROM alerts WHERE legacy_id = $2), $3, $4,
                 $5, $6, $7, $8, $9)
         ON CONFLICT (legacy_id) DO NOTHING`,
        [item.Id, item.AlertId, item.CommunityId, item.EventType,
          item.Description, item.OccurredAt, item.SensorId,
          item.Level, item.IsActive],
      );
    }
    await alerts.query('COMMIT');
    process.stdout.write(`Reglas: ${rules.length}. Alertas: ${history.length}. Eventos: ${events.length}.\n`);
  } catch (error) {
    await alerts.query('ROLLBACK');
    throw error;
  }
} finally {
  await Promise.allSettled([community.end(), alerts.end(), source.close()]);
}
