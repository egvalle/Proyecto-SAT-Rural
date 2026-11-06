#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/../.."
KUBECTL=(sudo k3s kubectl -n sat-rural)
BACKUPS=deploy-backups

for name in auth-db community-db alert-db mongodb sqlserver; do
  "${KUBECTL[@]}" rollout status "deployment/$name" --timeout=5m
done

pod_name() {
  "${KUBECTL[@]}" get pod -l "app=$1" -o jsonpath='{.items[0].metadata.name}'
}

copy_stream() {
  local source="$1" pod="$2" destination="$3"
  "${KUBECTL[@]}" exec -i "$pod" -- sh -c "cat > '$destination'" < "$source"
}

auth_pod="$(pod_name auth-db)"
copy_stream "$BACKUPS/satrural_auth.dump" "$auth_pod" /tmp/satrural_auth.dump
"${KUBECTL[@]}" exec "$auth_pod" -- pg_restore -U satrural_auth -d satrural_auth --no-owner --no-privileges /tmp/satrural_auth.dump

community_pod="$(pod_name community-db)"
copy_stream "$BACKUPS/satrural_community.dump" "$community_pod" /tmp/satrural_community.dump
"${KUBECTL[@]}" exec "$community_pod" -- pg_restore -U satrural_community -d satrural_community --no-owner --no-privileges /tmp/satrural_community.dump

alert_pod="$(pod_name alert-db)"
copy_stream "$BACKUPS/satrural_alerts.dump" "$alert_pod" /tmp/satrural_alerts.dump
"${KUBECTL[@]}" exec "$alert_pod" -- pg_restore -U satrural_alerts -d satrural_alerts --no-owner --no-privileges /tmp/satrural_alerts.dump

mongo_pod="$(pod_name mongodb)"
for database in satrural_audit satrural_reports; do
  copy_stream "$BACKUPS/$database.archive.gz" "$mongo_pod" "/tmp/$database.archive.gz"
done
copy_stream deploy/k3s/restore-mongo.sh "$mongo_pod" /tmp/restore-mongo.sh
"${KUBECTL[@]}" exec "$mongo_pod" -- sh /tmp/restore-mongo.sh

sql_pod="$(pod_name sqlserver)"
copy_stream "$BACKUPS/satrural_sensors.bak" "$sql_pod" /var/opt/mssql/data/satrural_sensors.bak
copy_stream deploy/k3s/restore-sqlserver.sh "$sql_pod" /tmp/restore-sqlserver.sh
"${KUBECTL[@]}" exec "$sql_pod" -- sh /tmp/restore-sqlserver.sh

echo 'Restauración terminada. Comprueba los conteos antes de usar la aplicación.'
