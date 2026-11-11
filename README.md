# Projeto Clubes

Plataforma de gestão de múltiplos clubes sociais e recreativos: API REST (ASP.NET Core .NET 10),
PostgreSQL, Web administrativo em Blazor e aplicativo Android/iOS (.NET MAUI).

> Situação: **etapas 1 a 8 concluídas** (API, Web e app) — veja [docs/STATUS_DESENVOLVIMENTO.md](docs/STATUS_DESENVOLVIMENTO.md).
> Produção: [docs/deploy/PRODUCAO.md](docs/deploy/PRODUCAO.md).

## Estrutura

```
src/Backend/   Domain · Application · Infrastructure · API
src/Web/       ProjetoClubes.Web — Web administrativo em Blazor (consome a API)
src/Mobile/    ProjetoClubes.Mobile — app do associado em .NET MAUI (consome a API)
database/      Scripts SQL de migrations e inicialização
docs/          Arquitetura, decisões técnicas, diagramas, API
tests/         Testes unitários e de integração (PostgreSQL real)
```

## Pré-requisitos
- .NET SDK 10 (`global.json`)
- PostgreSQL 17 (nativo ou via Docker)
- Docker + Docker Compose (opcional)

## Executar com Docker

```bash
cp .env.example .env    # preencha POSTGRES_PASSWORD e JWT_CHAVE
docker compose up -d --build
docker compose logs api | grep "token de inicialização"
```

Sobe PostgreSQL (5432, só local), API (5080) e Web (5278). Os containers ainda não foram executados nesta máquina
(sem Docker); o build dos Dockerfiles é o mesmo `dotnet publish -c Release` testado localmente.

## Executar localmente (sem Docker)

1. Crie o banco e o usuário no PostgreSQL:
   ```sql
   CREATE ROLE projetoclubes LOGIN CREATEDB PASSWORD '<senha>';
   CREATE DATABASE projetoclubes OWNER projetoclubes;
   ```
2. Configure os segredos da API (nunca em arquivos versionados):
   ```bash
   dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=projetoclubes;Username=projetoclubes;Password=<senha>" --project src/Backend/ProjetoClubes.API
   dotnet user-secrets set "Jwt:Chave" "<no mínimo 32 caracteres aleatórios>" --project src/Backend/ProjetoClubes.API
   # opcional: dados de demonstração
   dotnet user-secrets set "Seed:Demo" "true" --project src/Backend/ProjetoClubes.API
   dotnet user-secrets set "Seed:SenhaDemo" "<senha dos usuários demo>" --project src/Backend/ProjetoClubes.API
   ```
3. Rode a API (aplica as migrations em desenvolvimento):
   ```bash
   dotnet run --project src/Backend/ProjetoClubes.API --launch-profile http
   ```
4. Abra `http://localhost:5080/swagger`.
5. Em outro terminal, rode o Web administrativo e acesse `http://localhost:5278`:
   ```bash
   dotnet run --project src/Web/ProjetoClubes.Web --launch-profile http
   ```
   O endereço da API fica em `src/Web/ProjetoClubes.Web/appsettings.json` (`ApiBaseUrl`).

## Executar o aplicativo (.NET MAUI)

Com a API rodando:

```bash
dotnet build src/Mobile/ProjetoClubes.Mobile -f net10.0-windows10.0.19041.0
dotnet run --project src/Mobile/ProjetoClubes.Mobile -f net10.0-windows10.0.19041.0
```

Android (emulador): `dotnet run --project src/Mobile/ProjetoClubes.Mobile -f net10.0-android` — o app usa
`http://10.0.2.2:5080` para chegar à API do computador. Para celular físico, troque o endereço
pelo IP do computador (em `Configuracao.cs`; versões Release usam `UrlApiProducao`). Entre com um associado que tenha acesso ao app (na demonstração, `carlos.associado`).

## Primeiro acesso (Superadmin)

Não há usuário ou senha padrão. Na primeira partida, a API escreve no log um **token de inicialização**:

```
Sistema ainda não inicializado ... token de inicialização: <TOKEN>
```

Use-o uma única vez:

```bash
curl -X POST http://localhost:5080/api/v1/setup/inicializar -H "Content-Type: application/json" \
  -d '{"tokenInicializacao":"<TOKEN>","nome":"Seu Nome","email":"voce@exemplo.com","senha":"<senha forte>"}'
```

Depois disso o endpoint é desativado permanentemente.

## Dados de demonstração

Com `Seed:Demo=true` e `Seed:SenhaDemo` definida, são criados no banco os clubes **Clube Foz 2** e
**Clube Guaíra 2** (marcados como demonstração) e os usuários:
`presidente.foz`, `tesoureiro.foz`, `secretario.foz`, `portaria.foz`, `presidente.guaira`, `tesoureiro.guaira`
e o associado `carlos.associado` (login do aplicativo) — todos `@demo.projetoclubes.local`, com a senha configurada.
Também são criados associados e dependentes de exemplo (CPFs sintéticos válidos, sem relação com pessoas reais).

## Testes

```bash
dotnet test ProjetoClubes.slnx
```

São 80 testes unitários e 80 de integração, incluindo o roteiro de aceitação completo (`AceitacaoTests`).
Os testes de integração criam um banco temporário no mesmo servidor PostgreSQL (conexão dos user-secrets
da API ou da variável `PROJETOCLUBES_TESTES_POSTGRES`; o usuário precisa de `CREATEDB`) e o removem ao final.

## Migrations

```bash
dotnet tool restore
dotnet ef migrations add <Nome> --project src/Backend/ProjetoClubes.Infrastructure --startup-project src/Backend/ProjetoClubes.API --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/Backend/ProjetoClubes.Infrastructure --startup-project src/Backend/ProjetoClubes.API -o database/migrations/<arquivo>.sql
```

Em produção: `Banco__AplicarMigrationsAoIniciar=false` e aplique o script SQL revisado.

## Documentação
- [Arquitetura](docs/arquitetura/ARQUITETURA.md)
- [Decisões técnicas](docs/DECISOES_TECNICAS.md)
- [Diagrama ER](docs/diagramas/ER.md)
- [Endpoints](docs/api/ENDPOINTS.md)
- [Produção](docs/deploy/PRODUCAO.md)
