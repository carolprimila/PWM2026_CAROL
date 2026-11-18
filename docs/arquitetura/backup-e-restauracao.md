# Backup, restauração e recuperação

## O que precisa ser preservado

| Item | Onde fica | Como é copiado |
|---|---|---|
| Banco de dados PostgreSQL | volume `pgdata` | Serviço `backup` (pg_dump) → `./backups` |
| Arquivos enviados (fotos, documentos, comprovantes) | volume `storage` | Cópia do volume (comando abaixo) |
| Arquivo `.env` (segredos, chave JWT) | servidor | Cofre de senhas da administração — nunca no repositório |

## Backup automático do banco

O serviço `backup` do `docker-compose.yml` executa `database/scripts/backup-loop.sh`:

- gera `./backups/<banco>-AAAAMMDD-HHMMSS.dump` (formato *custom* do `pg_dump`, compactado) a cada `BACKUP_INTERVAL_HOURS` (padrão 24 h);
- remove arquivos mais antigos que `BACKUP_RETENTION_DAYS` (padrão 14 dias);
- registra sucesso ou falha no log (`docker compose logs backup`).

**Recomendação**: copie a pasta `./backups` diariamente para outro local (outro servidor ou armazenamento em nuvem). Um backup que fica
somente na mesma máquina não protege contra perda do servidor.

Backup manual imediato (antes de atualizações, por exemplo):

```bash
docker compose exec backup sh /scripts/backup-now.sh
```

## Backup dos arquivos enviados

```bash
docker run --rm -v clubes_storage:/data -v "$(pwd)/backups:/backup" alpine tar czf /backup/storage-$(date +%Y%m%d).tar.gz -C /data .
```

## Restauração do banco

> A restauração **substitui todos os dados** do banco de destino. Confirme o arquivo antes de executar.

1. Pare a API para que nada seja gravado durante a restauração:

```bash
docker compose stop api
```

2. Restaure o arquivo escolhido:

```bash
docker compose exec backup sh /scripts/restore.sh /backups/clubes-AAAAMMDD-HHMMSS.dump
```

3. Suba a API (ela aplica migrations mais novas que o backup, se houver):

```bash
docker compose start api
```

4. Verifique `http://localhost:8080/health` e faça um login de conferência.

A restauração roda em transação única: se falhar, o banco permanece como estava.

## Restauração dos arquivos

```bash
docker run --rm -v clubes_storage:/data -v "$(pwd)/backups:/backup" alpine sh -c "cd /data && tar xzf /backup/storage-AAAAMMDD.tar.gz"
```

## Recuperação de desastre (servidor novo)

1. Instale o Docker e copie o projeto, o `.env` original e os arquivos de backup.
2. `docker compose up -d db backup` e aguarde o banco ficar saudável.
3. Restaure o banco (passo 2 acima) e os arquivos enviados.
4. `docker compose up -d --build`.

Se a `JWT_KEY` original foi perdida, defina uma nova: todos os usuários precisarão entrar novamente (nenhum dado é perdido).

## Teste de restauração

Um backup só é confiável depois de restaurado. Recomenda-se, mensalmente, restaurar o último arquivo em um ambiente separado
(outro diretório com `POSTGRES_DB` diferente) e conferir login, lista de associados e financeiro.

## Proteções contra perda e duplicidade no uso diário

- Movimentações financeiras confirmadas não são apagadas: estornos e cancelamentos preservam o registro original.
- Exclusão lógica em cadastros (associados, dependentes, funcionários, espaços, patrimônio, comunicados).
- Operações financeiras e de acesso são idempotentes (chave de idempotência, token de uso único), então repetir uma requisição após
  queda de conexão não duplica pagamentos nem entradas.
