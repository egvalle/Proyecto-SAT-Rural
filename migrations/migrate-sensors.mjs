import { readFile } from 'node:fs/promises';
import sql from 'mssql';

const database = process.env.MSSQL_DATABASE;
const password = process.env.MSSQL_SA_PASSWORD;
if (database !== 'SatRuralDb' || !password) {
  throw new Error('Se requiere MSSQL_DATABASE=SatRuralDb y MSSQL_SA_PASSWORD.');
}

const pool = await sql.connect({
  server: 'sqlserver',
  database,
  user: 'sa',
  password,
  requestTimeout: 300000,
  options: { encrypt: false, trustServerCertificate: true },
});

try {
  const script = await readFile(new URL('./01_sensor_data.sql', import.meta.url), 'utf8');
  const result = await pool.request().batch(script);
  for (const row of result.recordset ?? []) {
    process.stdout.write(`${row.item}: ${row.total}\n`);
  }
} finally {
  await pool.close();
}
