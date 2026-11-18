#!/bin/sh
# Backup manual imediato. Uso: docker compose exec backup sh /scripts/backup-now.sh
set -eu
FILE="/backups/${PGDATABASE}-manual-$(date +%Y%m%d-%H%M%S).dump"
pg_dump --format=custom --compress=9 --no-owner --file="${FILE}"
echo "Backup gerado: ${FILE}"
