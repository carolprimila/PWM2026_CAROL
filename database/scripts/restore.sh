#!/bin/sh
# Restauração de um backup. ATENÇÃO: substitui todos os dados do banco de destino.
# Uso: docker compose stop api && docker compose exec backup sh /scripts/restore.sh /backups/<arquivo>.dump && docker compose start api
set -eu
FILE="${1:?informe o arquivo .dump a restaurar}"
[ -f "${FILE}" ] || { echo "Arquivo não encontrado: ${FILE}" >&2; exit 1; }
echo "Restaurando ${FILE} em ${PGDATABASE}..."
pg_restore --clean --if-exists --no-owner --single-transaction --dbname="${PGDATABASE}" "${FILE}"
echo "Restauração concluída."
