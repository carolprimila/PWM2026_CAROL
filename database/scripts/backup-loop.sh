#!/bin/sh
# Backup periódico do PostgreSQL (formato custom do pg_dump, compactado) com retenção.
set -u
INTERVAL_HOURS="${BACKUP_INTERVAL_HOURS:-24}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-14}"
mkdir -p /backups

while true; do
  STAMP="$(date +%Y%m%d-%H%M%S)"
  FILE="/backups/${PGDATABASE}-${STAMP}.dump"
  if pg_dump --format=custom --compress=9 --no-owner --file="${FILE}.tmp"; then
    mv "${FILE}.tmp" "${FILE}"
    echo "[backup] $(date -Iseconds) concluído: ${FILE} ($(du -h "${FILE}" | cut -f1))"
    find /backups -name "${PGDATABASE}-*.dump" -type f -mtime "+${RETENTION_DAYS}" -print -delete | sed 's/^/[backup] removido por retenção: /'
  else
    rm -f "${FILE}.tmp"
    echo "[backup] $(date -Iseconds) FALHA ao gerar o backup" >&2
  fi
  sleep "$((INTERVAL_HOURS * 3600))"
done
