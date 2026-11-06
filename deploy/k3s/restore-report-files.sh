#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/../.."
sudo k3s kubectl -n sat-rural rollout status deployment/report-service --timeout=5m
pod="$(sudo k3s kubectl -n sat-rural get pod -l app=report-service -o jsonpath='{.items[0].metadata.name}')"
sudo k3s kubectl -n sat-rural cp deploy-backups/report-files/. "$pod:/app/storage/"
echo 'Archivos de reportes restaurados.'
