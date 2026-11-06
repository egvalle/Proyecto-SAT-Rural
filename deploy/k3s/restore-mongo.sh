#!/bin/sh
set -eu

for database in satrural_audit satrural_reports; do
  mongorestore \
    --username "$MONGO_INITDB_ROOT_USERNAME" \
    --password "$MONGO_INITDB_ROOT_PASSWORD" \
    --authenticationDatabase admin \
    --archive="/tmp/$database.archive.gz" \
    --gzip \
    --stopOnError
done
