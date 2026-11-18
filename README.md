# Clubes — Sistema de Gestão de Clubes

Plataforma SaaS multi-clubes para gestão completa de clubes sociais: associados e dependentes, cartão digital com QR Code dinâmico,
portaria, financeiro, espaços e reservas, convidados, comunicação, assembleias e votações, patrimônio, estoque, manutenção, vistorias,
relatórios e auditoria. O primeiro clube da plataforma é o **Daros**.

| Componente | Tecnologia | Pasta |
|---|---|---|
| API REST | ASP.NET Core 10, EF Core 10, Npgsql, JWT, SignalR, Swagger | `backend/` |
| Banco de dados | PostgreSQL 17 (migrations EF Core) | `backend/Clubes.Infrastructure/Persistence/Migrations`, `database/` |
| Web (administração) | React 19, TypeScript, Vite, Tailwind CSS | `frontend/clubes-web/` |
| Mobile (associados, dependentes, portaria) | .NET MAUI 10 (Android/iOS), MVVM | `mobile/Clubes.Mobile/` |
| Infraestrutura | Docker Compose (db, api, web, backup) | `docker-compose.yml` |

Web e Mobile consomem **a mesma API**; nenhuma regra de negócio vive nas interfaces e nenhum cliente acessa o banco diretamente.

## Início rápido (Docker)

Pré-requisitos: Docker Desktop.

```bash
cp .env.example .env
```

Edite o `.env` e preencha, no mínimo: `POSTGRES_PASSWORD`, `JWT_KEY` (32+ caracteres aleatórios), `SUPERADMIN_EMAIL`/`SUPERADMIN_PASSWORD`
e `CLUB_ADMIN_EMAIL`/`CLUB_ADMIN_PASSWORD` (senhas com 8+ caracteres, letras e números). Depois:

```bash
docker compose up -d --build
```

- Web: http://localhost:8081
- API: http://localhost:8080 (verificação de saúde em `/health`; Swagger em `/swagger` quando `SWAGGER_ENABLED=true`)

Na primeira subida, com o banco vazio, a API aplica as migrations e cria o superadministrador e o clube definido em `CLUB_NAME`
(com a estrutura física padrão: 4 quadras, 10 quiosques, salão de festas e piscinas). **Nenhum dado de exemplo é criado.**
No primeiro acesso, cada usuário é obrigado a trocar a senha e a aceitar a política de privacidade.

## Desenvolvimento local

Pré-requisitos: .NET SDK 10, Node 22, Docker (para o PostgreSQL) e, para o mobile, os workloads MAUI.

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

A API sobe em http://localhost:5080 (Swagger em `/swagger`) e o Web em http://localhost:5173. O aplicativo mobile está descrito em
[`mobile/README.md`](mobile/README.md).

### Sedes, contas de teste e dados de exemplo (somente desenvolvimento)

O Daros tem **duas sedes — Guaíra e Foz**. Cada sede é um clube isolado na plataforma: a equipe de uma sede (inclusive o presidente)
só acessa os dados da própria sede; o **superadministrador** alterna entre as sedes pelo seletor no topo do Web e enxerga tudo de
cada uma, sempre separado por sede. Em produção, a primeira sede nasce do `.env` (`CLUB_NAME`) e a segunda é criada pelo
superadministrador em **Plataforma → Clubes**.

Com a API de desenvolvimento no ar:

```bash
python database/scripts/contas-dev.py
```

```bash
python database/scripts/dados-exemplo-dev.py
```

O primeiro script cria as duas sedes e as contas de teste; o segundo popula cada sede com exemplos (10 associados com dependentes,
mensalidades e pagamentos, um inadimplente, reservas de quiosques/churrasqueiras, quadras e salão, convidados aguardando autorização,
eventos, comunicados, manifestações, assembleia com votação, manutenções, estoque, patrimônio e uma entrada na portaria).

| Onde | Contas | Como aparece |
|---|---|---|
| Web (`npm run dev`) | Superadmin (todas as sedes); Administrador, Presidente, Tesouraria e Portaria de cada sede; Secretaria (Guaíra) | Botões "Entrada rápida" na tela de login |
| Mobile (build Debug) | Associado, Dependente e Portaria (sede Guaíra) | Botões "Entrar como…" na tela de login |

- "Presidência" é uma função criada pelo script com todas as permissões da sede (editável em Funções e permissões).
- As senhas de teste ficam só em arquivos locais **não versionados** (`frontend/clubes-web/.env.local` e
  `mobile/Clubes.Mobile/Services/DevLogins.local.cs`). Os botões **não existem** no build de produção do Web nem no build Release
  do aplicativo, e os scripts só rodam contra a API local.
- Os valores de política gravados pelo segundo script (mensalidade, vencimento, taxas, preços) são **apenas exemplos** de demonstração.
- Depois de gerar os arquivos, recompile o aplicativo para os botões aparecerem. Para executar no emulador Android:

```bash
dotnet build mobile/Clubes.Mobile/Clubes.Mobile.csproj -t:Run -f net10.0-android
```

## Testes

```bash
dotnet test backend/Clubes.Tests
```

Os testes de integração sobem a API real contra um PostgreSQL descartável (Testcontainers — requer Docker em execução) e cobrem
autenticação, autorização, isolamento entre clubes, cadastros, limite de dependentes, mensalidades, inadimplência, pagamentos
idempotentes, reservas e conflitos, capacidade, convidados, QR Codes, portaria, votações, segmentação de comunicados, notificações e auditoria.

## Documentação

| Documento | Conteúdo |
|---|---|
| [STATUS_DESENVOLVIMENTO.md](STATUS_DESENVOLVIMENTO.md) | O que está concluído, pendente, problemas conhecidos, testes e decisões |
| [docs/arquitetura/arquitetura.md](docs/arquitetura/arquitetura.md) | Camadas, multi-tenancy, autenticação, tempo real, rotinas automáticas |
| [docs/arquitetura/instalacao-e-execucao.md](docs/arquitetura/instalacao-e-execucao.md) | Instalação, variáveis de ambiente, execução e atualização |
| [docs/arquitetura/seguranca-e-lgpd.md](docs/arquitetura/seguranca-e-lgpd.md) | Medidas de segurança e tratamento de dados pessoais |
| [docs/arquitetura/backup-e-restauracao.md](docs/arquitetura/backup-e-restauracao.md) | Backup automático, retenção, restauração e recuperação |
| [docs/banco-de-dados/modelagem.md](docs/banco-de-dados/modelagem.md) | Modelagem, integridade e restrições; [diagrama ER](docs/banco-de-dados/diagrama-er.md); [dicionário de dados](docs/banco-de-dados/dicionario-de-dados.md) |
| [docs/api/api.md](docs/api/api.md) | Convenções da API, autenticação, erros, paginação; [rotas](docs/api/rotas.txt); [OpenAPI](docs/api/openapi.json) |
| [docs/regras-de-negocio/regras-de-negocio.md](docs/regras-de-negocio/regras-de-negocio.md) | Regras implementadas, módulo a módulo |
| [docs/regras-de-negocio/guia-de-permissoes.md](docs/regras-de-negocio/guia-de-permissoes.md) | Perfis, permissões e funções padrão |
| [docs/regras-de-negocio/guia-de-configuracao.md](docs/regras-de-negocio/guia-de-configuracao.md) | Políticas configuráveis e decisões pendentes da administração |
