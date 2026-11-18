# Instalação e execução

## 1. Produção / homologação com Docker Compose

Pré-requisito: Docker (Engine + Compose v2).

1. Copie `.env.example` para `.env` e preencha as variáveis (tabela abaixo).
2. Suba os serviços:

```bash
docker compose up -d --build
```

3. Acompanhe a inicialização:

```bash
docker compose logs -f api
```

Serviços:

| Serviço | Função | Porta padrão |
|---|---|---|
| `db` | PostgreSQL 17 (volume `pgdata`; publicado apenas em `127.0.0.1`) | 5432 |
| `api` | API REST (volumes `storage` para arquivos enviados e `logs`) | 8080 |
| `web` | Aplicação Web servida por nginx | 8081 |
| `backup` | Backup periódico do banco em `./backups` | — |

Em produção, publique API e Web atrás de um proxy reverso com HTTPS (a API já respeita `X-Forwarded-For`/`X-Forwarded-Proto`),
defina `CORS_ORIGIN` com a URL pública do Web e compile o Web com `VITE_API_URL` apontando para a URL pública da API.

### Variáveis de ambiente (`.env`)

| Variável | Obrigatória | Descrição |
|---|---|---|
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | sim | Banco e credenciais |
| `JWT_KEY` | sim | Chave de assinatura dos tokens (32+ caracteres aleatórios). Trocar a chave invalida as sessões |
| `CORS_ORIGIN` | sim | Origem do Web autorizada a chamar a API |
| `VITE_API_URL` | sim | URL da API vista pelo navegador (usada no build do Web) |
| `SUPERADMIN_NAME/EMAIL/PASSWORD` | primeira subida | Superadministrador criado em banco vazio |
| `CLUB_NAME`, `CLUB_ADMIN_NAME/EMAIL/PASSWORD` | primeira subida | Primeiro clube (Daros) e seu administrador |
| `PASSWORD_RESET_URL` | recomendado | URL da tela "redefinir senha" enviada por e-mail |
| `SMTP_HOST/PORT/USER/PASSWORD/FROM` | opcional | E-mail (recuperação de senha, credenciais e avisos obrigatórios) |
| `FIREBASE_CREDENTIALS_FILE` | opcional | JSON da conta de serviço do Firebase (push) |
| `PAYMENTS_PROVIDER`, `PAYMENTS_SANDBOX_SECRET` | opcional | `None` (padrão) ou `Sandbox` (somente homologação; recusado em produção) |
| `BACKUP_INTERVAL_HOURS`, `BACKUP_RETENTION_DAYS` | opcional | Frequência e retenção dos backups |
| `SWAGGER_ENABLED` | opcional | Publica a documentação interativa da API em `/swagger` |
| `API_PORT`, `WEB_PORT`, `DB_PORT` | opcional | Portas publicadas |

As senhas iniciais valem só para o primeiro acesso: o sistema obriga a troca. Depois da primeira subida, as variáveis
`SUPERADMIN_*` e `CLUB_ADMIN_*` deixam de ter efeito e podem ser removidas do `.env`.

### Atualização

```bash
docker compose up -d --build
```

As migrations pendentes são aplicadas automaticamente na inicialização da API (`Bootstrap__AutoMigrate`). Faça um backup antes
(`docker compose exec backup sh /scripts/backup-now.sh`).

## 2. Desenvolvimento

Pré-requisitos: .NET SDK 10, Node 22+, Docker.

```bash
docker compose up -d db
```

```powershell
powershell -ExecutionPolicy Bypass -File backend/run-dev.ps1
```

```bash
npm --prefix frontend/clubes-web install
```

```bash
npm --prefix frontend/clubes-web run dev
```

- API: http://localhost:5080 · Swagger: http://localhost:5080/swagger
- Web: http://localhost:5173
- Mobile: ver `mobile/README.md`

### Migrations

```bash
cd backend
dotnet ef migrations add NomeDaMigration -p Clubes.Infrastructure -s Clubes.Infrastructure -o Persistence/Migrations
```

Para gerar o script SQL das migrations (revisão ou aplicação manual):

```bash
dotnet ef migrations script -p Clubes.Infrastructure -s Clubes.Infrastructure -o ../database/migrations/esquema.sql --idempotent
```

### Contrato da API e tipos do Web

Depois de alterar endpoints ou DTOs, atualize o contrato e os tipos:

```bash
curl -s http://localhost:5080/swagger/v1/swagger.json -o docs/api/openapi.json
```

```bash
npm --prefix frontend/clubes-web run gen:api
```

### Testes

```bash
dotnet test backend/Clubes.Tests
```

Se a API estiver em execução pelo `run-dev.ps1`, use uma pasta de artefatos separada para não disputar os binários:

```bash
dotnet test backend/Clubes.Tests --artifacts-path backend/.artifacts-tests
```

## 3. Monitoramento

- `GET /health`: 200 quando API e banco respondem; 503 quando o banco está indisponível (usado pelo healthcheck do Compose).
- Logs estruturados (Serilog) no console e em arquivo diário (`logs`, 30 dias). Falhas de login, acessos negados e erros não tratados
  são registrados com contexto.
- Entrega de notificações: Web → Administração → Auditoria → "Entrega de notificações".
- Quando a API está indisponível, Web e Mobile exibem erro com opção de tentar novamente e nunca apresentam a operação como concluída.
