import sql from 'mssql';
import pg from 'pg';
import { MongoClient } from 'mongodb';

const required = (name) => {
  const value = process.env[name];
  if (!value) throw new Error(`Falta ${name}.`);
  return value;
};

const source = await sql.connect({
  server: 'sqlserver', database: required('MSSQL_DATABASE'), user: 'sa',
  password: required('MSSQL_SA_PASSWORD'),
  options: { encrypt: false, trustServerCertificate: true },
});
const auth = new pg.Client({
  host: 'auth-db', database: 'satrural_auth', user: 'satrural_auth',
  password: required('AUTH_DB_PASSWORD'),
});
const community = new pg.Client({
  host: 'community-db', database: 'satrural_community',
  user: required('COMMUNITY_DB_USER'), password: required('COMMUNITY_DB_PASSWORD'),
});
const alert = new pg.Client({
  host: 'alert-db', database: 'satrural_alerts',
  user: 'satrural_alerts', password: required('ALERT_DB_PASSWORD'),
});
const mongo = new MongoClient(
  `mongodb://${encodeURIComponent(required('MONGO_ROOT_USERNAME'))}:` +
  `${encodeURIComponent(required('MONGO_ROOT_PASSWORD'))}@mongodb:27017/?authSource=admin`,
);

let failed = false;
const check = (label, sourceItems, targetItems) => {
  const target = new Set(targetItems.map(String));
  const missing = sourceItems.filter(item => !target.has(String(item)));
  process.stdout.write(`${label}: origen ${sourceItems.length}, faltantes ${missing.length}.\n`);
  if (missing.length > 0) {
    process.stdout.write(`  Primeros faltantes: ${missing.slice(0, 5).join(', ')}\n`);
    failed = true;
  }
};

try {
  await Promise.all([auth.connect(), community.connect(), alert.connect(), mongo.connect()]);

  const users = (await source.request().query('SELECT Id FROM Users')).recordset.map(row => row.Id);
  const authUsers = (await auth.query('SELECT id FROM users')).rows.map(row => row.id);
  check('Usuarios', users, authUsers);

  const communities = (await source.request().query('SELECT Id FROM Communities')).recordset.map(row => row.Id);
  const targetCommunities = (await community.query('SELECT "Id" FROM "Communities"')).rows.map(row => row.Id);
  check('Comunidades', communities, targetCommunities);

  const rules = (await source.request().query('SELECT Id FROM AlertRules')).recordset.map(row => row.Id);
  const targetRules = (await alert.query('SELECT id FROM alert_rules')).rows.map(row => row.id);
  check('Reglas', rules, targetRules);

  const alerts = (await source.request().query('SELECT Id FROM Alerts')).recordset.map(row => row.Id);
  const targetAlerts = (await alert.query('SELECT legacy_id FROM alerts WHERE legacy_id IS NOT NULL')).rows
    .map(row => row.legacy_id);
  check('Alertas históricas', alerts, targetAlerts);

  const events = (await source.request().query('SELECT Id FROM Events')).recordset.map(row => row.Id);
  const targetEvents = (await alert.query(
    'SELECT legacy_id FROM climate_events WHERE legacy_id IS NOT NULL',
  )).rows.map(row => row.legacy_id);
  check('Eventos históricos', events, targetEvents);

  const sensorCodes = (await source.request().query('SELECT Code FROM Sensors')).recordset.map(row => row.Code);
  const targetCodes = (await source.request().query('SELECT Code FROM satrural_sensors.dbo.Sensors'))
    .recordset.map(row => row.Code);
  check('Sensores', sensorCodes, targetCodes);

  const readings = (await source.request().query(`
    SELECT Source, COUNT(*) AS Total
    FROM satrural_sensors.dbo.SensorReadings
    GROUP BY Source
  `)).recordset;
  process.stdout.write(`Lecturas conservadas: ${readings.map(row => `${row.Source} ${row.Total}`).join(', ')}.\n`);

  const auditDb = mongo.db('satrural_audit');
  const oldBinnacle = (await source.request().query('SELECT Id FROM Binnacles'))
    .recordset.map(row => row.Id);
  const oldAudit = (await source.request().query('SELECT Id FROM AuditLogs'))
    .recordset.map(row => row.Id);
  const newBinnacle = await auditDb.collection('binnacle')
    .find({ legacyId: { $exists: true } }, { projection: { legacyId: 1 } }).toArray();
  const newAudit = await auditDb.collection('audit_logs')
    .find({ legacyId: { $exists: true } }, { projection: { legacyId: 1 } }).toArray();
  check('Bitácora', oldBinnacle, newBinnacle.map(row => row.legacyId));
  check('Auditoría', oldAudit, newAudit.map(row => row.legacyId));

  if (failed) {
    throw new Error('La migración aún no está completa; conserva las rutas actuales.');
  }
  process.stdout.write('La migración conserva todos los registros de origen revisados.\n');
} finally {
  await Promise.allSettled([
    auth.end(), community.end(), alert.end(), mongo.close(), source.close(),
  ]);
}
