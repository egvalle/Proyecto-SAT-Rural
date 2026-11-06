#!/bin/sh
set -eu

export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b \
  -Q "RESTORE VERIFYONLY FROM DISK = N'/var/opt/mssql/data/satrural_sensors.bak'"
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b \
  -Q "IF DB_ID(N'satrural_sensors') IS NOT NULL BEGIN RAISERROR('La base satrural_sensors ya existe; no se sobreescribe.', 16, 1); RETURN; END; RESTORE DATABASE [satrural_sensors] FROM DISK = N'/var/opt/mssql/data/satrural_sensors.bak' WITH RECOVERY"
