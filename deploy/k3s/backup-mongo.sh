#!/bin/sh
set -eu

for database in satrural_audit satrural_reports; do
  mongodump \
    --username "$MONGO_INITDB_ROOT_USERNAME" \
    --password "$MONGO_INITDB_ROOT_PASSWORD" \
    --authenticationDatabase admin \
    --db "$database" \
    --archive="/tmp/$database.archive.gz" \
    --gzip
done
