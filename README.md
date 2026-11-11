# Projeto Clubes

Plataforma de gestão de clubes sociais e recreativos (vários clubes na mesma instalação): API REST ASP.NET Core .NET 10,
banco **SQLite**, Web administrativo em **Blazor** e aplicativo do associado em **.NET MAUI** (Android, iOS e Windows).

> Situação: etapas 1 a 8 concluídas — veja [docs/STATUS_DESENVOLVIMENTO.md](docs/STATUS_DESENVOLVIMENTO.md).

## Estrutura

```
src/Backend/   ProjetoClubes.API (REST) · Domain · Application · Infrastructure (Clean Architecture)
src/Web/       ProjetoClubes.Web — Web administrativo (Blazor), consome a API
src/Mobile/    ProjetoClubes.Mobile — app do associado (.NET MAUI), consome a API
tests/         Testes unitários e de integração (API real com SQLite)
docs/          Arquitetura, decisões técnicas, endpoints, diagrama ER, produção
```

O banco é um arquivo, `src/Backend/ProjetoClubes.API/projetoclubes.db`, **criado sozinho na primeira execução** com dados
de demonstração. Não há banco para instalar nem segredo para configurar.

## Requisitos
- Visual Studio 2022 (17.14 ou mais novo) com a carga **ASP.NET e desenvolvimento Web**, ou o .NET 10 SDK.
- Para o app: carga **.NET Multi-platform App UI (MAUI)** — só é necessária para abrir/rodar o `ProjetoClubes.Mobile`.

## Rodar no Visual Studio
1. Abra `ProjetoClubes.slnx`.
2. Na barra de ferramentas, ao lado do botão ▶, escolha o perfil de inicialização:
   - **API + Web administrativo** — abre o navegador em `http://localhost:5278`;
   - **API + App (Windows)** — abre o aplicativo do associado;
   - **API + Web + App** — tudo junto.
3. Aperte **F5**.

Se o seu Visual Studio não mostrar esses perfis: botão direito na solução → **Configurar projetos de inicialização** →
**Vários projetos de inicialização** → marque `ProjetoClubes.API` e `ProjetoClubes.Web` (ou `ProjetoClubes.Mobile`) como
**Iniciar**, com a API primeiro.

## Rodar pelo terminal
Em dois terminais, na pasta do projeto:

```powershell
dotnet run --project src/Backend/ProjetoClubes.API --launch-profile http
```

```powershell
dotnet run --project src/Web/ProjetoClubes.Web --launch-profile http
```

Acesse `http://localhost:5278`. A API fica em `http://localhost:5080` (documentação em `/swagger`).

App no Windows: `dotnet build src/Mobile/ProjetoClubes.Mobile -t:Run -f net10.0-windows10.0.19041.0`.
Android (emulador): `dotnet build src/Mobile/ProjetoClubes.Mobile -t:Run -f net10.0-android` — o app usa
`http://10.0.2.2:5080` para chegar à API do computador. Para celular físico, troque o endereço em
`src/Mobile/ProjetoClubes.Mobile/Configuracao.cs` pelo IP do computador.

## Contas de demonstração
Todas com a senha **`Clubes2026!`** (definida em `appsettings.Development.json`):

| Quem | E-mail | Onde entra |
|---|---|---|
| Superadmin (todos os clubes) | `admin@demo.projetoclubes.local` | Web |
| Presidente do Clube Foz 2 | `presidente.foz@demo.projetoclubes.local` | Web |
| Tesoureiro, secretário e portaria do Foz 2 | `tesoureiro.foz@…`, `secretario.foz@…`, `portaria.foz@…` | Web |
| Presidente do Clube Guaíra 2 | `presidente.guaira@demo.projetoclubes.local` | Web |
| Associado (Carlos) | `carlos.associado@demo.projetoclubes.local` | App |

Os dados (associados, dependentes, mensalidades, eventos, espaços, comunicados, funcionários, patrimônio, estoque e uma
assembleia convocada) são fictícios, com CPFs sintéticos.

**Começar do zero:** pare a API e apague `src/Backend/ProjetoClubes.API/projetoclubes.db` (e os arquivos `-wal`/`-shm`).

## Testes

```powershell
dotnet test ProjetoClubes.slnx
```

80 testes unitários e 80 de integração (a API real sobe contra um arquivo SQLite temporário), incluindo o roteiro de
aceitação completo (`AceitacaoTests`). Levam cerca de 1 minuto.

## Migrations

```powershell
dotnet tool restore
dotnet ef migrations add <Nome> --project src/Backend/ProjetoClubes.Infrastructure --startup-project src/Backend/ProjetoClubes.API --output-dir Persistence/Migrations
```

A API aplica as migrations pendentes ao iniciar.

## Produção
A configuração de desenvolvimento (senha demo, chave JWT de exemplo) fica no repositório de propósito, para rodar em
qualquer máquina — **em produção a API se recusa a iniciar** com dados de demonstração ou com a chave de exemplo. Veja
[docs/deploy/PRODUCAO.md](docs/deploy/PRODUCAO.md) (Docker opcional, HTTPS, e-mail, backup).

## Documentação
- [Arquitetura](docs/arquitetura/ARQUITETURA.md)
- [Decisões técnicas](docs/DECISOES_TECNICAS.md)
- [Diagrama ER](docs/diagramas/ER.md)
- [Endpoints](docs/api/ENDPOINTS.md)
- [Produção](docs/deploy/PRODUCAO.md)
