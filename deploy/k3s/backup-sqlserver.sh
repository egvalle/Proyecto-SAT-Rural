#!/bin/sh
set -eu

export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"
/opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -C -b \
  -Q "BACKUP DATABASE [satrural_sensors] TO DISK = N'/var/opt/mssql/data/satrural_sensors.bak' WITH INIT, COPY_ONLY, CHECKSUM"
