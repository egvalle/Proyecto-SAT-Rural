SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO satrural_sensors.dbo.Sensors
    (CommunityId, Code, Name, Type, Unit, Location,
     InstallationDate, Description, IsActive, Source, CreatedAt)
SELECT s.CommunityId, s.Code, s.Name, s.Type, s.Unit, s.Location,
       s.InstallationDate, s.Description, s.IsActive, 'SIMULATED', s.CreatedAt
FROM SatRuralDb.dbo.Sensors AS s
WHERE NOT EXISTS (
    SELECT 1 FROM satrural_sensors.dbo.Sensors AS target
    WHERE target.Code = s.Code
);

UPDATE target
SET target.CommunityId = source.CommunityId,
    target.Name = source.Name,
    target.Type = source.Type,
    target.Unit = source.Unit,
    target.Location = source.Location,
    target.InstallationDate = source.InstallationDate,
    target.Description = source.Description,
    target.IsActive = source.IsActive
FROM satrural_sensors.dbo.Sensors AS target
JOIN SatRuralDb.dbo.Sensors AS source ON source.Code = target.Code;

DELETE targetReading
FROM satrural_sensors.dbo.SensorReadings AS targetReading
JOIN satrural_sensors.dbo.Sensors AS targetSensor
    ON targetSensor.Id = targetReading.SensorId
JOIN SatRuralDb.dbo.Sensors AS sourceSensor
    ON sourceSensor.Code = targetSensor.Code
JOIN SatRuralDb.dbo.SensorReadings AS sourceReading
    ON sourceReading.SensorId = sourceSensor.Id
   AND sourceReading.Value = targetReading.Value
   AND sourceReading.RecordedAt = targetReading.RecordedAt
WHERE targetReading.Source = 'SIMULATED';

SELECT 'origen_sensores' AS item, COUNT(*) AS total
FROM SatRuralDb.dbo.Sensors
UNION ALL
SELECT 'destino_sensores', COUNT(*) FROM satrural_sensors.dbo.Sensors
UNION ALL
SELECT 'lecturas_conservadas', COUNT(*) FROM satrural_sensors.dbo.SensorReadings;

COMMIT TRANSACTION;
