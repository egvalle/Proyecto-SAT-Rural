import sql from 'mssql';
import pg from 'pg';

const required = (name) => {
  const value = process.env[name];
  if (!value) throw new Error(`Falta ${name}.`);
  return value;
};

const source = await sql.connect({
  server: process.env.LEGACY_SQL_HOST ?? 'sqlserver',
  database: required('MSSQL_DATABASE'),
  user: 'sa',
  password: required('MSSQL_SA_PASSWORD'),
  options: { encrypt: false, trustServerCertificate: true },
});
const target = new pg.Client({
  host: process.env.AUTH_DB_HOST ?? 'auth-db',
  database: 'satrural_auth',
  user: 'satrural_auth',
  password: required('AUTH_DB_PASSWORD'),
});

try {
  await target.connect();
  const sourceUsers = (await source.request().query(`
    SELECT Id, Username, PasswordHash, FullName, RolId, IsActive, CreatedAt
    FROM Users ORDER BY Id
  `)).recordset;

  await target.query('BEGIN');
  try {
    let inserted = 0;
    for (const user of sourceUsers) {
      if (![1, 2, 3].includes(user.RolId)) {
        throw new Error(`Rol no reconocido para el usuario ${user.Id}.`);
      }
      const existing = await target.query('SELECT username FROM users WHERE id = $1', [user.Id]);
      if (existing.rows[0] && existing.rows[0].username !== user.Username.toLowerCase()) {
        throw new Error(`Conflicto de identidad en el usuario ${user.Id}.`);
      }
      await target.query(
        `INSERT INTO users (id, username, password_hash, full_name, role_id, is_active, created_at)
         VALUES ($1, $2, $3, $4, $5, $6, $7)
         ON CONFLICT (id) DO UPDATE SET
           password_hash = EXCLUDED.password_hash,
           full_name = EXCLUDED.full_name,
           role_id = EXCLUDED.role_id,
           is_active = EXCLUDED.is_active`,
        [user.Id, user.Username.toLowerCase(), user.PasswordHash, user.FullName,
          user.RolId, user.IsActive, user.CreatedAt],
      );
      if (!existing.rows[0]) inserted += 1;
    }
    const count = Number((await target.query('SELECT count(*)::int AS total FROM users')).rows[0].total);
    if (count < sourceUsers.length) {
      throw new Error('El total migrado es menor que el total de origen.');
    }
    await target.query(`
      SELECT setval(pg_get_serial_sequence('users', 'id'),
        GREATEST((SELECT COALESCE(MAX(id), 0) FROM users), 1), true)
    `);
    await target.query('COMMIT');
    process.stdout.write(`Usuarios de origen: ${sourceUsers.length}. Nuevos: ${inserted}. Total destino: ${count}.\n`);
  } catch (error) {
    await target.query('ROLLBACK');
    throw error;
  }
} finally {
  await target.end();
  await source.close();
}
