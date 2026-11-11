# Arquitetura — Projeto Clubes

## 1. Visão geral

```
┌────────────────────┐   ┌────────────────────┐
│ Web Administrativo │   │ App Mobile (.NET   │
│ Blazor (Server)    │   │ MAUI, MVVM, XAML)  │
└─────────┬──────────┘   └─────────┬──────────┘
          │  HTTPS + JWT (Bearer)  │
          └──────────┬─────────────┘
                     ▼
        ┌──────────────────────────┐
        │ ProjetoClubes.Api (um projeto — DT-031)  │
        │  Controllers/  autenticação, autorização,│
        │                middlewares, Swagger      │
        │  Application/  casos de uso, DTOs,       │
        │                validações, interfaces    │
        │  Domain/       entidades e regras        │
        │  Infrastructure/ EF Core + SQLite, JWT,  │
        │                e-mail, arquivos, auditoria│
        └────────────┬─────────────────────────────┘
                     ▼
              ┌─────────────┐
              │   SQLite    │  (arquivo projetoclubes.db — DT-030)
              └─────────────┘
```

Regra inegociável: Web e Mobile **só** falam com a API. Nenhum cliente conhece a string de conexão.

Dependências entre as camadas (Clean Architecture, agora como pastas de um único projeto — DT-031):

```
Controllers ──► Application ──► Domain
     └────────► Infrastructure ──► Application
```

A regra continua valendo por convenção: `Domain` não usa nada das outras pastas, `Application` não usa `Infrastructure`
nem `Controllers`.

`Application` depende das abstrações do EF Core (`DbSet`, LINQ) através da interface
`IAppDbContext`. Ver decisão DT-003 em `docs/DECISOES_TECNICAS.md`.

## 2. Estrutura de diretórios

```
VanderClub/                         (raiz do repositório = "ProjetoClubes/")
  src/ProjetoClubes.Api/            API (um projeto, abre e compila sozinho)
    Domain/                         Entidades por módulo (Clubes, Identidade, Auditoria…)
    Application/                    Casos de uso por módulo (serviços, DTOs, validações)
    Infrastructure/                 Persistence/ (DbContext, Configurations, Migrations,
                                    Interceptors, Seed), Seguranca/, Email/, Arquivos/
    Controllers/, Middleware/, Autorizacao/, Configuracao/, Program.cs
  src/ProjetoClubes.Web/            Blazor Web App (Interactive Server) + Bootstrap — ver DT-024
  src/ProjetoClubes.Mobile/         App .NET MAUI (uma tela, código no MainPage)
  database/                         Notas sobre o banco SQLite e o seed
  docs/                             Arquitetura, decisões, status, API, diagramas
  tests/ProjetoClubes.UnitTests/
  tests/ProjetoClubes.IntegrationTests/  (API real + SQLite em arquivo)
```

## 3. Multi-clubes (multi-tenancy)

Estratégia: **banco compartilhado, esquema compartilhado, coluna `ClubeId`** em toda entidade
pertencente a um clube (interface `ITenantScoped`).

Camadas de proteção (defesa em profundidade):

1. **Identidade**: o clube ativo vem do claim `clube_id` do JWT, emitido pelo servidor somente
   após validar o vínculo `UsuarioClubes` (ou se o usuário for Superadmin). O frontend nunca
   informa o `ClubeId` das operações; ele só *pede* para trocar de clube
   (`POST /api/v1/auth/trocar-clube`) e o servidor decide.
2. **Validação por requisição**: a cada requisição autenticada a sessão é verificada
   (não revogada, não expirada), o usuário (não bloqueado) e o vínculo com o clube (ativo).
3. **Filtro global nomeado do EF Core** (`"Tenant"`) em toda entidade `ITenantScoped`.
   Sem clube no contexto, o filtro não retorna nada.
4. **Interceptor de gravação**: preenche `ClubeId` em inclusões e **bloqueia** inclusões,
   alterações e exclusões de registros de outro clube, mesmo que algum código ignore filtros.
5. **Serviços** filtram explicitamente por `tenant.ClubeIdObrigatorio` nas consultas críticas.
6. **Testes de integração** comprovam que um usuário do clube A não acessa dados do clube B.

## 4. Autenticação e autorização

- Senhas: PBKDF2 (`PasswordHasher` do ASP.NET Identity, 600k iterações SHA-512 na v3+).
- **Access token** JWT (15 min) com claims `sub`, `sid` (sessão), `clube_id`, `sa` (superadmin).
- **Refresh token** opaco (256 bits), armazenado como hash SHA-256 em `SessoesUsuarios`,
  com **rotação** a cada uso e **detecção de reutilização** (reuso → sessão revogada).
- Sessões revogáveis individualmente; troca de senha revoga as demais sessões.
- Bloqueio temporário após N falhas consecutivas (configurável) e bloqueio administrativo.
- `HistoricoAcessos` registra login, falha, logout, refresh, troca de clube, recuperação etc.
- **Permissões** (não perfis) são o que a API verifica: `[RequerPermissao("associados.cadastrar")]`.
  - Catálogo global de permissões definido em código e sincronizado com a tabela `Permissoes`.
  - `Perfis` são **por clube** (criados a partir de modelos ao cadastrar o clube) e editáveis.
  - Permissões efetivas = união das permissões dos perfis do vínculo
    + concessões individuais − negações individuais (`UsuarioPermissoes`).
  - Superadmin: flag global, ignora a verificação de permissão dentro do clube selecionado.
  - Permissões são carregadas do banco com cache curto (invalidado em alterações), então
    mudanças valem sem novo login.

### Primeiro acesso

Não há credencial fixa. Enquanto não existir Superadmin, a API gera um **token de
inicialização** de uso único (ou usa `Setup__Token` do ambiente) e o escreve no log de
inicialização. `POST /api/v1/setup/inicializar` com esse token cria o primeiro Superadmin;
depois disso o endpoint fica permanentemente desativado.

## 5. Modelo de dados — Etapa 1

```
Clubes 1──1 ConfiguracoesClube
Clubes 1──N Perfis N──N Permissoes        (PerfilPermissoes)
Usuarios 1──N UsuarioClubes N──1 Clubes
UsuarioClubes N──N Perfis                 (UsuarioClubePerfis)
UsuarioClubes 1──N UsuarioPermissoes N──1 Permissoes   (conceder/negar)
Usuarios 1──N SessoesUsuarios
Usuarios 1──N TokensRecuperacaoSenha
HistoricoAcessos  (eventos de autenticação)
LogsAuditoria     (todas as alterações: entidade, id, ação, diff jsonb, usuário, clube)
```

Convenções: chaves `uuid` (UUIDv7, ordenáveis), datas `timestamptz` em UTC,
exclusão lógica (`Excluido`, `ExcluidoEm`, `ExcluidoPor`) com filtro global nomeado
`"SoftDelete"`, índices únicos parciais respeitando a exclusão lógica.

Extensão `btree_gist` já habilitada para as restrições de exclusão de reservas (Etapa 4).

Modelo completo planejado (etapas seguintes): ver `docs/diagramas/ER.md`.

## 6. API

- Rotas versionadas `/api/v1/{modulo}`; DTOs de entrada/saída; nunca entidades.
- Validação com FluentValidation; erros em `application/problem+json` (RFC 9457).
- Paginação padrão: `?pagina=1&tamanhoPagina=20&busca=&ordenarPor=nome&direcao=asc`.
- Tratamento global de exceções, `X-Correlation-Id`, logs estruturados (Serilog).
- Swagger em `/swagger` (desenvolvimento), health check em `/health`.
- Rate limiting nos endpoints de autenticação.

## 7. Integração Web e Mobile

- Ambos usam o mesmo fluxo: `login` → `access + refresh` → `GET /auth/me` (usuário, clube
  ativo, clubes disponíveis, permissões) → menus/telas conforme permissões.
- Web (Blazor Server): tokens na sessão do circuito; refresh token no `ProtectedSessionStorage`; o cliente
  `Api` renova o access token ao receber 401 (DT-024).
- Mobile: `SecureStorage` para refresh token; `HttpClient` com `DelegatingHandler`
  para anexar o token e renovar automaticamente.
- Identidade visual por clube (cor primária, logotipo) vem de `ConfiguracoesClube`.

## 8. Etapas

| Etapa | Escopo |
|------|--------|
| 1 | Fundação: repositório, Docker, banco (PostgreSQL; SQLite desde a DT-030), API, EF, migrations, autenticação, multi-clubes, usuários, perfis, permissões, clubes, auditoria |
| 2 | Associados, categorias, dependentes, documentos, aprovações, carteirinhas, QR Code, controle de acesso |
| 3 | Financeiro |
| 4 | Eventos, espaços, reservas (com restrição de exclusão), solicitações |
| 5 | Web administrativo (Blazor, simples — DT-024) |
| 6 | App .NET MAUI |
| 7 | Funcionários, patrimônio, estoque, assembleias, votações, notificações |
| 8 | Testes finais, segurança, documentação, produção |
