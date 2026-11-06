#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/../.."

sudo docker build -t sat-rural/auth-next:deploy auth-service
sudo docker build -t sat-rural/community-service:deploy -f backend/SatRural.CommunityService/Dockerfile backend
sudo docker build -t sat-rural/sensor-service:deploy -f backend/SatRural.SensorService/Dockerfile backend
sudo docker build -t sat-rural/alert-service:deploy backend/SatRural.AlertService
sudo docker build -t sat-rural/audit-service:deploy audit-service
sudo docker build -t sat-rural/report-service:deploy report-service
sudo docker build -t sat-rural/gateway:deploy -f backend/SatRural.Gateway/Dockerfile backend
sudo docker build -t sat-rural/frontend:deploy frontend

sudo docker save \
  sat-rural/auth-next:deploy \
  sat-rural/community-service:deploy \
  sat-rural/sensor-service:deploy \
  sat-rural/alert-service:deploy \
  sat-rural/audit-service:deploy \
  sat-rural/report-service:deploy \
  sat-rural/gateway:deploy \
  sat-rural/frontend:deploy \
  -o /tmp/sat-rural-images.tar

sudo k3s ctr -n k8s.io images import /tmp/sat-rural-images.tar
sudo rm /tmp/sat-rural-images.tar

echo 'Imágenes disponibles para K3s.'
