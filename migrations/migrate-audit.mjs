import sql from 'mssql';
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
const uri = `mongodb://${encodeURIComponent(required('MONGO_ROOT_USERNAME'))}:` +
  `${encodeURIComponent(required('MONGO_ROOT_PASSWORD'))}@mongodb:27017/?authSource=admin`;
const mongo = new MongoClient(uri);

try {
  await mongo.connect();
  const database = mongo.db('satrural_audit');
  const entries = (await source.request().query(`
    SELECT Id, Description, IdMovimentType, [User], DateHour
    FROM Binnacles ORDER BY Id
  `)).recordset;
  const audit = (await source.request().query(`
    SELECT a.Id, a.UserId, u.Username, u.FullName, a.Action,
           a.EntityName, a.EntityId, a.Details, a.CreatedAt
    FROM AuditLogs AS a
    JOIN Users AS u ON u.Id = a.UserId
    ORDER BY a.Id
  `)).recordset;

  for (const [collectionName, rows, transform] of [
    ['binnacle', entries, item => ({
      description: item.Description, idMovimentType: item.IdMovimentType,
      user: item.User, dateHour: item.DateHour,
    })],
    ['audit_logs', audit, item => ({
      userId: item.UserId, username: item.Username, fullName: item.FullName,
      action: item.Action, entityName: item.EntityName,
      entityId: item.EntityId, details: item.Details, createdAt: item.CreatedAt,
    })],
  ]) {
    const collection = database.collection(collectionName);
    await collection.createIndex({ legacyId: 1 }, { unique: true, sparse: true });
    let added = 0;
    for (const item of rows) {
      const existing = await collection.findOne({ legacyId: item.Id });
      if (existing) continue;
      const counter = await database.collection('counters').findOneAndUpdate(
        { _id: collectionName }, { $inc: { value: 1 } },
        { upsert: true, returnDocument: 'after' },
      );
      await collection.insertOne({
        id: counter.value, legacyId: item.Id, ...transform(item),
      });
      added += 1;
    }
    process.stdout.write(`${collectionName}: origen ${rows.length}, migrados ${added}.\n`);
  }
} finally {
  await Promise.allSettled([mongo.close(), source.close()]);
}
