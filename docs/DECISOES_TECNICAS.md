# Decisões técnicas

Registro das decisões arquiteturais. Cada item: contexto, decisão e consequências.

## DT-001 — Raiz do repositório
A pasta de trabalho `VanderClub/` é a raiz do repositório e corresponde ao `ProjetoClubes/` da especificação.
Estrutura `src/Backend`, `src/Web`, `src/Mobile`, `database`, `docs`, `tests` mantida (depois simplificada — DT-031).

## DT-002 — .NET 10 fixado por `global.json`
A máquina tem SDK 10.0.401 e 11 RC. `global.json` fixa `10.0.401` com `rollForward: latestFeature`
(aceita 10.0.4xx/5xx, nunca 11). Pacotes com versões centrais em `Directory.Packages.props`
(EF Core 10.0.15, Npgsql 10.0.3, Swashbuckle 10.3.1, FluentValidation 12.2, Serilog.AspNetCore 10).

## DT-003 — Application depende de abstrações do EF Core (`IAppDbContext`)
Os casos de uso consultam `DbSet<T>` via `IAppDbContext` em vez de repositórios genéricos.
- Prós: LINQ com projeções diretas para DTO, sem camada de repositório que só repassa chamadas.
- Contras: Application referencia `Microsoft.EntityFrameworkCore` (não o provedor PostgreSQL).
- Regras específicas do PostgreSQL (ex.: `ILIKE`, restrições de exclusão) ficam na Infrastructure ou
  em configurações de modelo. Buscas textuais usam `ToLower().Contains()` (portável).

## DT-004 — Multi-tenancy: banco e esquema compartilhados com `ClubeId`
Ver `docs/arquitetura/ARQUITETURA.md` §3. Defesa em profundidade:
1. clube vem do JWT emitido pelo servidor após validar o vínculo;
2. a cada requisição a sessão, o usuário e o vínculo são revalidados no banco (`ValidacaoSessaoJwt`);
3. filtro global **nomeado** `"Tenant"` (EF Core 10) em toda entidade `ITenantScoped`; sem clube → nada;
4. `GravacaoInterceptor` impede inclusão/alteração/exclusão em outro clube e mudança de `ClubeId`;
5. serviços filtram explicitamente por `ClubeIdObrigatorio`;
6. registros de outro clube respondem **404** (não revelam existência).

Superadmin e rotinas `Sistema` podem gravar em qualquer clube; nas consultas o Superadmin vê o clube
selecionado (troca via `POST /auth/trocar-clube`). `IgnoreQueryFilters([Tenant])` é usado somente em
consultas de identidade (login, resolução de permissões) que precisam enxergar vínculos de outros clubes.

## DT-005 — `ConfiguracaoClube` não é `ITenantScoped`
É parte do agregado `Clube` (1:1, carregada com `AutoInclude`). Se tivesse o filtro de tenant, a lista
de clubes do usuário (vários clubes) perderia a configuração dos clubes não selecionados. O acesso é
controlado pelo `ClubeService` (exige ser o clube atual + permissão `clubes.configurar`).

## DT-006 — Permissões, não nomes de perfil
- Catálogo de permissões em código (`Permissoes.Todas`), sincronizado com a tabela na partida.
- Perfis são **por clube**, copiados de modelos (`ModelosPerfis`) no cadastro do clube e editáveis.
- Efetivas = perfis ∪ concessões individuais − negações individuais (negação prevalece).
- Ninguém concede permissão que não possui (anti-escalonamento), exceto Superadmin.
- Cache em memória de 60 s com versão global invalidada a cada alteração. **Limitação**: com várias
  instâncias da API será preciso um cache distribuído/pub-sub (Redis) — registrado para a Etapa 8.

## DT-007 — Sessões e tokens
- Access token JWT HS256 de 15 min; refresh token opaco de 256 bits com rotação e detecção de reuso.
- Apenas hashes SHA-256 de refresh/recuperação são armazenados.
- Revalidação da sessão a cada requisição (1–3 consultas indexadas). Escolhido em favor de revogação
  imediata. Se virar gargalo: cache curto com invalidação por evento.
- Reuso do refresh anterior revoga a sessão. Consequência: o cliente Web deve serializar renovações
  (uma única renovação em andamento por vez) — será tratado no interceptor HTTP da Etapa 5.

## DT-008 — Primeiro acesso sem credenciais fixas
Token de inicialização de uso único, aleatório e exibido apenas no log da partida (ou `Setup__Token`).
`POST /api/v1/setup/inicializar` só funciona enquanto não existir Superadmin.

## DT-009 — Dados de demonstração no banco, separados dos reais
`Seed__Demo=true` cria "Clube Foz 2" e "Clube Guaíra 2" com `Clube.Demonstracao = true` e usuários
`@demo.projetoclubes.local`. A senha vem de `Seed__SenhaDemo`; sem ela, nada é criado.

## DT-010 — Auditoria automática
Toda gravação gera `LogsAuditoria` (entidade, id, ação, diff jsonb, usuário, clube, IP, correlation id),
exceto entidades `INaoAuditavel` (sessões, tokens, históricos e os próprios logs). Campos sensíveis são
mascarados. O histórico de autenticação fica em `HistoricoAcessos`, sem FKs, para sobreviver a exclusões.

## DT-011 — Enums persistidos como texto
Legibilidade no banco e em relatórios; evita quebra ao reordenar enums.

## DT-012 — Migrations
EF Core exige as migrations dentro da Infrastructure (`Persistence/Migrations`). Para DBAs/produção, o
script SQL idempotente é gerado em `database/migrations/` (`dotnet ef migrations script --idempotent`).
Em desenvolvimento, `Banco__AplicarMigrationsAoIniciar=true` aplica na partida.

## DT-013 — PostgreSQL local nativo nesta máquina
A máquina de desenvolvimento não tem Docker; foi instalado o PostgreSQL 17 nativo (serviço
`postgresql-x64-17`). O `docker-compose.yml` continua sendo o caminho padrão para outros ambientes.
Segredos locais ficam em `dotnet user-secrets` (API e testes de integração compartilham o mesmo `UserSecretsId`).

## DT-014 — Testes de integração com PostgreSQL real
`WebApplicationFactory` + banco descartável `projetoclubes_testes_<guid>` criado e removido por execução,
com as migrations reais. Sem provider em memória (não reproduziria filtros, índices parciais nem
restrições de exclusão).

## DT-016 — Ids gerados pela aplicação (`ValueGeneratedNever`)
Todas as entidades usam UUIDv7 gerado no construtor. Sem `ValueGeneratedNever`, o EF tratava um filho novo
adicionado à coleção de uma entidade já rastreada (ex.: novo `VinculoDependente` numa transferência) como
existente e emitia `UPDATE` → `DbUpdateConcurrencyException`. Encontrado pelo teste de transferência.
A migration `ChavesGeradasNaAplicacao` não altera o esquema; só atualiza o modelo.

## DT-017 — `Pessoas` por clube
Dados pessoais ficam em `Pessoas` (CPF único **por clube**), referenciados por `Associados` e `Dependentes`.
A mesma pessoa em dois clubes são dois registros: um clube nunca altera dados vistos por outro (LGPD,
isolamento). Uma pessoa não pode ser associada vigente e dependente vigente ao mesmo tempo no clube.

## DT-018 — Situação cadastral única e regras no domínio
`RegistroCadastral` (base de Associado e Dependente) concentra a máquina de estados
Pendente → Ativo|Rejeitado; Ativo → Suspenso|Inativo; Suspenso|Inativo → Ativo, com motivo obrigatório.
`RegrasDependentes` (limite, cônjuge, idade, parentesco, categoria, permissões iniciais, login próprio) é pura e
testada em unidade; os valores vêm de `ConfiguracoesDependentes` e `CategoriasAssociados` — nada fixo por clube.
Quem tem permissão de aprovar dispensa a etapa de aprovação ao cadastrar; o portal sempre segue a configuração.

## DT-019 — Dependente com dois titulares
Índice único parcial `UX_Dependentes_PessoaVigente (ClubeId, PessoaId) WHERE vigente AND NOT ExcecaoMultiplosTitulares`
garante a regra no banco. A exceção exige permissão `dependentes.aprovar` + justificativa, registrada no próprio
dependente. Mesmo com exceção, `UX_Dependentes_TitularPessoaVigente` impede duplicar com o mesmo titular.
Transferência encerra o vínculo atual e abre outro em `VinculosDependentes` (histórico de titularidade).

## DT-020 — Numeração
Matrícula: sequência atômica por clube (`SequenciasClube`, `INSERT … ON CONFLICT DO UPDATE … RETURNING`) com
prefixo configurável; matrículas nunca são reutilizadas. Dependente: `<matrícula do titular>-NN`, onde NN conta
os vínculos já abertos com o titular (números nunca reutilizados). Concorrência no mesmo titular → índice único → 409.

## DT-021 — Documentos e fotos em armazenamento privado
Tabela única `DocumentosPessoais` (ligada à pessoa) em vez de `DocumentosAssociados`/`DocumentosDependentes`
separadas: o mesmo fluxo serve aos dois e sobrevive à mudança de papel da pessoa. Conteúdo em pasta privada
(`Arquivos__Pasta`, fora de wwwroot) com chave gerada no servidor e proteção contra path traversal; download só
por endpoint autorizado com `Cache-Control: private, no-store`. Formato validado pela assinatura binária
(PDF/JPEG/PNG/WEBP), limite 10 MB (documentos) e 5 MB (fotos). Limitação: se a gravação no banco falhar após
salvar o arquivo, o arquivo fica órfão — limpeza periódica prevista para a Etapa 8.

## DT-022 — Carteirinha e QR Code
O QR contém `PCLUBES:1:<token>` — token opaco aleatório de 256 bits por pessoa, sem CPF ou dados pessoais.
Validação sempre no servidor, restrita ao clube do funcionário (carteirinha de outro clube = "não reconhecida",
sem revelar nome). O token pode ser regenerado (celular perdido), invalidando o anterior. Dependente só entra se
ativo, com titular ativo e com permissão `AcessoClube`. Saídas de pessoas reconhecidas são sempre registradas;
tentativas recusadas também ficam em `AcessosClube` com o funcionário responsável.

## DT-023 — Portal do associado
`portal.acessar` + vínculo `Pessoas.UsuarioId` no clube atual. O titular vê a si e aos próprios dependentes;
dependentes veem só a si. Inclusões pelo app seguem a configuração de aprovação; edição pelo app só enquanto
pendente. Menores não alteram os próprios dados. Login de dependente só a partir de
`IdadeMinimaLoginProprio`, criado pela administração ou autorizado pelo titular (convite por e-mail).
Acesso a dados de pessoa fora do escopo responde 404 (não revela existência).

## DT-024 — Web administrativo em Blazor (simples), no lugar de React
Decisão do usuário (2026-11-11): seguir a estrutura e a simplicidade do projeto VanderBakery — um único projeto
`src/ProjetoClubes.Web` em Blazor Web App (Interactive Server) com Bootstrap, páginas diretas e sem design
system próprio. Isso substitui o requisito original de React + TypeScript + Vite; o início de projeto React foi removido.
- O Web continua consumindo **somente a API REST** (`HttpClient` nomeado "api", `ApiBaseUrl`), nunca o banco.
- Sessão por circuito (`Sessao`): access/refresh token ficam no servidor do Web; só o refresh token vai para o
  `sessionStorage` do navegador, criptografado com `ProtectedSessionStorage` (sobrevive ao F5, some ao fechar a aba).
  Renderização sem pré-render para poder ler esse armazenamento na primeira carga.
- Renovação automática do token em 401; o menu mostra só o que o perfil permite, e a API valida tudo de novo.
- Modelos de resposta próprios do Web (`Servicos/Modelos.cs`) com só os campos usados — sem referenciar Application.

## DT-025 — App MAUI simples (uma tela), como o VanderBakery.Mobile
`src/ProjetoClubes.Mobile`: .NET MAUI (.NET 10) com uma `MainPage` (XAML + code-behind), painel de login e área do
associado, sem MVVM nem camadas extras — escolha do usuário por simplicidade, no lugar do MVVM pedido no requisito original.
- Consome só a API (`/api/v1/portal/*`, `/auth/*`, QR em `/pessoas/{id}/qrcode`). Endereço: `http://10.0.2.2:5080/` no
  emulador Android e `http://localhost:5080/` nas demais plataformas (troque a constante `BaseApi` para um celular físico).
- Refresh token no `SecureStorage` do aparelho; renovação automática em 401; sessão retomada ao abrir o app.
- Só entra quem tem `portal.acessar` (associado/dependente); usuários administrativos são orientados a usar o Web.
- `src/Mobile` tem `Directory.Build.props`/`Directory.Packages.props` próprios: o app tem vários TargetFrameworks e usa as
  versões de pacote do workload MAUI, então não herda o TargetFramework único nem a gestão central de versões da raiz.
- Android libera HTTP sem TLS (`usesCleartextTraffic`) para desenvolvimento local; em produção a API deve usar HTTPS.

## DT-026 — Financeiro simples e verificável
- **Plano por categoria** (um ativo por categoria, garantido por índice parcial). Associados de categorias sem plano não
  recebem cobrança e aparecem no resultado da geração.
- **Mensalidade por competência** (dia 1 do mês), gerada sob demanda e idempotente (`UX_Mensalidades_AssociadoCompetencia`).
  Inclui `QuantidadeDependentes × ValorAdicionalPorDependente` quando o clube não inclui dependentes na mensalidade.
- **Encargos** (`ConfiguracaoFinanceira`): multa única % + juros simples % ao mês pró-rata dia, após dias de tolerância.
  "Vencida" não é gravada: é calculada (Aberta e vencimento < hoje).
- **Pagamento = confirmação manual da tesouraria**, com recibo numerado por clube; um pagamento válido por mensalidade
  (`UX_Pagamentos_MensalidadeValido`). Valor menor que o devido é recusado (pode-se dispensar encargos explicitamente).
  Estorno reabre a mensalidade e mantém o histórico. Pix/boleto/gateways futuros devem entrar pelo mesmo caminho,
  somente após confirmação do provedor (webhook) — nunca marcar como pago por suposição.
- **Receitas/despesas avulsas** em `LancamentosFinanceiros`: sem `PagoEm` = conta a receber/pagar. Categoria precisa ser
  do mesmo tipo do lançamento. Fluxo de caixa = pagamentos de mensalidades + lançamentos pagos no período.
- **Portal**: só o titular vê suas mensalidades (já incluem a cobrança dos dependentes); dependentes não veem finanças.

## DT-027 — Eventos, reservas e solicitações sem disputa de vaga ou horário
- **Vaga e horário serializados**: inscrição em evento e criação/alteração de reserva rodam em transação com
  `pg_advisory_xact_lock(hashtext('evento:{id}' | 'espaco:{id}'))` (`ITransacoes.ComBloqueioAsync`, com a execution
  strategy do Npgsql). Dois pedidos simultâneos para a última vaga ou o mesmo horário: só um passa (testado).
- **Última barreira no banco**: `EX_Reservas_SemConflito` — `EXCLUDE USING gist ("EspacoId" WITH =,
  tstzrange("Inicio","Fim",'[)') WITH &&) WHERE Situacao IN ('Pendente','Aprovada')`. Intervalo semiaberto: uma reserva
  pode começar exatamente quando a outra termina. A violação vira 409 `conflito_horario`.
- **Evento com espaço** cria uma reserva aprovada do período inteiro (inclusive eventos que viram a madrugada);
  cancelar o evento cancela a reserva.
- **Capacidade** conta o inscrito + acompanhantes; sem vaga → lista de espera; cancelamento promove em ordem de inscrição
  enquanto couber. Uma inscrição ativa por pessoa (`UX_Inscricoes_PessoaEvento`, parcial).
- **Público**: Geral (associados e dependentes), SomenteAssociados, SomenteTitulares. Presença pelo QR da carteirinha
  (mesmo token opaco da portaria).
- **Privacidade no app**: o calendário mostra apenas "Ocupado" para reservas de terceiros; respostas internas das
  solicitações nunca saem no portal. Protocolo de solicitação `AAAA-NNNNNN` por clube e ano (`SequenciaClube`).
- **Datas em UTC**: o Npgsql só aceita `DateTimeOffset` com offset 0 em `timestamptz`. O Web e o app enviam horário de
  Brasília (-03:00). Há um conversor global (`ConfigureConventions`) que normaliza para UTC na gravação **e nos parâmetros
  de consulta** (o calendário com `?de=...-03:00` dava 500 antes disso); as entidades também normalizam no domínio.

## DT-028 — Etapa 7: comunicação, operacional e assembleias
- **Notificações = caixa de entrada gravada no banco** (`Notificacoes`, por usuário e clube). O push é um passo extra
  atrás de `IEnvioPush`; a implementação atual (`EnvioPushRegistro`) só registra em log. Para FCM/APNs: nova implementação
  com credenciais em configuração segura + registro do token do aparelho pelo app. Falha de push nunca desfaz a operação.
  Avisam: comunicado publicado, convocação/cancelamento de assembleia, votação aberta (só presentes aptos), reserva
  aprovada/recusada (solicitante e titular), resposta pública/encerramento de solicitação, dependente aprovado/rejeitado.
  Anotação interna de solicitação não notifica.
- **Comunicados**: rascunho → publicado; público (associados e dependentes / só titulares) e confirmação de leitura não mudam
  depois de publicados. Dependente com `RecebimentoComunicados = false` não vê. Leitura única por pessoa (`UX_Leituras_ComunicadoPessoa`);
  a administração vê quem leu/confirmou e quem está pendente.
- **Funcionários**: cargo e departamento em texto (sem tabelas só para listas); acesso ao sistema = usuário comum do clube
  ligado por `UsuarioId`. Férias/afastamento são períodos sem sobreposição; a situação do dia (Férias/Afastado) é calculada
  na leitura, não gravada. CPF único por clube quando informado. Histórico próprio (`HistoricoFuncionarios`).
- **Patrimônio**: código `PAT-NNNNNN` pela sequência do clube; uma manutenção aberta por bem (`UX_Manutencoes_BemAberta`);
  baixa exige motivo e encerra alterações. Histórico de local/estado/manutenção/baixa.
- **Estoque**: saldo só muda por movimentação (entrada, saída, ajuste com motivo), em transação com
  `pg_advisory_xact_lock('produto:{id}')`; o banco recusa saldo negativo (`CK_Produtos_SaldoNaoNegativo`, 422 `regra_violada`).
  8 saídas simultâneas de 2 sobre saldo 10 → 5 passam, 3 recusadas (testado). Pedido de compra recebido dá entrada na
  mesma transação; "pedir o que está abaixo do mínimo" sugere o dobro do mínimo menos o saldo.
- **Assembleias**: Rascunho → Convocada → Em andamento → Encerrada (ou Cancelada antes da instalação). Presença só de
  **associado titular** (QR da carteirinha ou busca; QR de dependente é recusado). Aptidão avaliada no registro da presença
  (`RegrasVoto`): cadastro ativo, categoria com direito a voto e, se a assembleia exigir, sem mensalidades vencidas.
  A regra de adimplência não muda depois da convocação. Voto só pelo próprio associado no app, presente e apto, com votação
  aberta; um voto por pauta (`UX_Votos_PautaAssociado`). A API nunca expõe quem votou em quê; a apuração por opção só aparece
  depois do encerramento da votação. Decisão de pauta com votação só depois de encerrada. Ata: rascunho gerado com presenças,
  apuração e decisões; registrada uma única vez e imutável.
- Fora do escopo por ora: enquetes e pesquisas de satisfação, escalas em grade (a escala é texto), anexos de manutenção,
  lançamento automático do custo de manutenção/compra no financeiro.

## DT-032 — Contrato, diretoria, eventos com vários locais (pedidos do cliente)
- O **contrato gera a cobrança**: ao ser aprovado, cria as 12 mensalidades do período (pula competências que já existem) e
  as vincula pelo `Mensalidade.ContratoId`. Só um contrato em análise por associado (índice filtrado). Renovar cria o contrato
  do ano seguinte em análise; o anterior se encerra quando o novo é aprovado.
- Multa e juros mostrados por parcela usam o mesmo cálculo do financeiro (`CalculoFinanceiro.Detalhar`).
- Locais de evento são **reservas com `EventoId`**: o mesmo controle de conflito (API + gatilho no banco) vale para todos.
- Divulgação simples, sem integração paga (projeto acadêmico): notificação no app, e-mail pelo `IEmailSender` e link
  `wa.me` para compartilhar. Sem disparo automático por WhatsApp nem postagem em redes sociais.
- Diretoria em tabela própria (`MembrosDiretoria`), cargo em texto livre; "Presidente" tem regra de não sobreposição e
  mandato padrão de 2 anos. Não está ligada a usuários do sistema (o perfil Presidente continua sendo de acesso).
- Login: a API já devolvia os clubes do usuário; Web e app perguntam qual quando há mais de um (`auth/trocar-clube`).

## DT-031 — Três projetos, como o VanderBakery (Api, Web, Mobile)
Pedido da usuária: a mesma organização do VanderBakery — três pastas em `src/`, abrir um `.csproj` e compilar.
- `ProjetoClubes.Domain`, `.Application` e `.Infrastructure` viraram as pastas `Domain/`, `Application/` e `Infrastructure/`
  dentro de `src/ProjetoClubes.Api` (histórico preservado com `git mv`). Namespaces e código não mudaram; a separação em
  camadas continua por convenção, não mais imposta pelo compilador.
- Cada `.csproj` tem suas versões de pacotes e configurações; saíram `Directory.Build.props`, `Directory.Packages.props`,
  `global.json`, `.editorconfig`, `dotnet-tools.json` e `.claude/`. Ficaram `NuGet.Config`, a solução e os perfis do VS.
- O namespace da API passou de `ProjetoClubes.API` para `ProjetoClubes.Api` (mesmo nome do projeto).
- Testes, documentação e Docker continuam (testes em `tests/`, referenciando só o projeto da API).

## DT-030 — SQLite em arquivo, no lugar do PostgreSQL (rodar em qualquer notebook)
Pedido da usuária: o projeto deve funcionar como o VanderBakery — clonar o repositório, abrir no Visual Studio e rodar,
sem instalar banco nem configurar segredos. Troca feita em 2026-11-11; substitui o PostgreSQL das decisões anteriores.
- **Banco**: `Microsoft.EntityFrameworkCore.Sqlite`; arquivo `projetoclubes.db` na pasta da API (caminho relativo resolvido
  a partir dela), criado e atualizado pelas migrations na partida, modo WAL. Migrations recriadas do zero (`Inicial`).
- **Configuração no repositório** (`appsettings.Development.json`): dados de demonstração ligados, senha demo pública e
  chave JWT de desenvolvimento. Sem user-secrets. Em produção a API recusa partir com demo ligado ou com a chave de exemplo.
- **Datas**: `DateTimeOffset` gravado como instante UTC em ticks (`INTEGER`) por conversor global — comparações, ordenação
  e filtros com horário local continuam no banco.
- **Equivalências das garantias do PostgreSQL**:
  - restrição de exclusão de reservas → gatilhos `TR_Reservas_SemConflito_Insert/Update` (`RAISE(ABORT,'conflito_horario')`),
    também testados com SQL direto, sem passar pela API;
  - `pg_advisory_xact_lock` → trava em memória por chave (64 semáforos) + transação do SQLite (`BEGIN IMMEDIATE`, um gravador
    por vez). Correto porque só um processo da API usa o arquivo;
  - CHECKs com expressão regular → `GLOB` (CPF, CNPJ, UF, cor); `EXTRACT(DAY ...)` → `substr(data, 9, 2)`;
  - `jsonb`/`text[]` → texto JSON (coleção primitiva do EF Core);
  - erros únicos/CHECK/FK/gatilho mapeados pelos códigos estendidos do SQLite (2067/1555, 275, 787, 1811).
- **Limites conhecidos**: um único servidor da API por arquivo (sem várias réplicas); busca sem diferenciar maiúsculas só
  para letras sem acento (`lower()` do SQLite); valores decimais guardados como texto (as regras e os testes de dinheiro
  continuam passando). Para crescer além disso, voltar a um servidor de banco é trocar o provedor e gerar novas migrations.
- **Testes**: cada execução sobe a API contra um `.db` temporário; os 80 de integração caíram de ~7 min para ~40 s.
- **Visual Studio**: `ProjetoClubes.slnLaunch` com perfis "API + Web", "API + App" e "API + Web + App"; `global.json` aceita
  qualquer SDK .NET 10 (10.0.100+); `NuGet.Config` aponta só para o nuget.org.

## DT-029 — Etapa 8: segurança, LGPD e produção
- **Revisão de autorização**: 42 ações sem `[RequerPermissao]` conferidas uma a uma — todas checam dentro do serviço
  (Superadmin, `ExigirAsync`, dono no portal via `AcessoPessoas`) ou são do próprio usuário (logout, sessões, notificações).
- **Cabeçalhos**: API com `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, CSP `default-src 'none'`
  (exceto Swagger) e `Cache-Control: no-store` em `/api`; Web com `nosniff`, `SAMEORIGIN` e `frame-ancestors 'self'`
  (sem CSP de scripts para não quebrar o Blazor).
- **Limites**: login/refresh/recuperação por IP (20/min) + limite geral por usuário (ou IP sem login), 600/min, configurável.
  `X-Forwarded-For` só é aceito de `Proxy:RedesConfiaveis` (antes era aceito de qualquer origem, o que permitia burlar o
  limite de login trocando o cabeçalho).
- **Limite de dependentes sob concorrência**: inclusão (Web e app) roda com `pg_advisory_xact_lock('titular:{id}')`;
  5 inclusões simultâneas numa categoria com limite 2 → 2 passam, 3 recusadas (testado).
- **Partida em produção recusada** com `Seed:Demo=true` ou CORS com origem `*`/HTTP (testado com a API publicada).
- **E-mail**: `Email:Provedor=Smtp` (System.Net.Mail com TLS; senha só por variável de ambiente). "Arquivo" continua o
  padrão de desenvolvimento. O log registra só o assunto, não o destinatário.
- **LGPD — acesso/portabilidade**: `GET /portal/meus-dados` (app) e `GET /pessoas/{id}/dados-pessoais` (administração)
  geram um JSON com cadastro, vínculos, mensalidades, inscrições, reservas, solicitações (sem anotações internas),
  entradas na portaria, documentos (metadados), comunicados lidos, presenças em assembleias e acessos ao login.
  **Votos não entram** (são secretos).
- **LGPD — eliminação**: `POST /pessoas/{id}/anonimizar` (permissão de excluir associados/dependentes) só para quem
  não tem vínculo vigente. Apaga nome, CPF, RG, nascimento, contatos, endereço, foto e documentos (conteúdo e nome do
  arquivo), troca o token do QR, desfaz o login (a conta é anonimizada e bloqueada se não tiver outro clube) e limpa os
  valores antigos guardados na auditoria (`Alteracoes`) e o e-mail do histórico de login. Fica o registro de que as
  operações aconteceram e o histórico financeiro/participação sem identificar a pessoa. Tudo numa transação com
  bloqueio; arquivos físicos só são apagados depois do commit. Limitação conhecida: textos livres digitados por
  usuários (motivos, descrições de solicitações) e notificações de terceiros que citem o nome não são reescritos.
- **CSV dos relatórios**: `;` + BOM UTF-8 (Excel em português) e proteção contra injeção de fórmula (`=`, `+`, `-`, `@`).
- **Web**: chaves do DataProtection em volume (`DataProtection:Pasta`) — sem isso a sessão cai a cada reinício.
- **App**: endereço da API por configuração de build (`Configuracao.cs`: Debug = máquina local, Release = HTTPS de produção).

## DT-015 — Avisos de navegação obrigatória com filtro
O EF avisa que `Usuario` (exclusão lógica) é ponta obrigatória de `SessaoUsuario`, `Perfil` de
`UsuarioClubePerfil` e `Interesse` de `AssociadoInteresse`. É intencional: sessões de usuários excluídos devem "desaparecer" (falha fechada).
