# API REST

- Contrato completo (OpenAPI 3): [`openapi.json`](openapi.json) · lista de rotas: [`rotas.txt`](rotas.txt)
- Documentação interativa: `/swagger` (habilitada em desenvolvimento ou com `SWAGGER_ENABLED=true`)

## Convenções

| Tema | Regra |
|---|---|
| Formato | JSON em camelCase; enumerações como texto (`"Approved"`); datas-horas ISO-8601 em UTC; datas de calendário `aaaa-mm-dd`; competência `aaaa-mm`; horários de funcionamento em minutos desde 00:00 |
| Autenticação | `Authorization: Bearer <accessToken>` em tudo, exceto login, renovação, recuperação de senha e webhook do gateway |
| Clube | Vem do token. Somente o superadministrador envia `X-Club-Id` para escolher o clube em que opera |
| DTOs | Entidades internas nunca são expostas; cada operação tem DTOs de entrada e saída próprios |
| Paginação | `page` (1…), `pageSize` (1–200, padrão 20), `search`, `sort`, `desc` → `{ items, total, page, pageSize }` |
| Idempotência | Pagamentos exigem `idempotencyKey`; repetir a mesma chave devolve o pagamento já registrado |
| Limite de requisições | 429 com erro padronizado quando excedido |

## Erros

Toda resposta de erro tem o mesmo formato:

```json
{ "code": "limite_dependentes", "message": "Limite de 5 dependentes por associado atingido.", "errors": null, "traceId": null }
```

| HTTP | Quando | `code` (exemplos) |
|---|---|---|
| 400 | Dados inválidos | `validacao` (com `errors` por campo), `cpf_invalido`, `reserva_invalida` |
| 401 | Não autenticado ou sessão expirada | `nao_autenticado`, `login_invalido`, `sessao_expirada` |
| 403 | Sem permissão ou bloqueado por regra | `acesso_negado`, `bloqueio_financeiro` |
| 404 | Inexistente **ou de outro clube/titular** (não se revela a existência) | `nao_encontrado` |
| 409 | Conflito de estado ou concorrência | `conflito`, `espaco_bloqueado`, `evento_lotado` |
| 422 | Regra de negócio | `regra_de_negocio`, `configuracao_pendente`, `capacidade_excedida`, `limite_convidados`, `limite_reservas` |
| 429 | Muitas requisições / muitas tentativas de login | `muitas_requisicoes`, `login_invalido` |
| 500 | Erro inesperado (com `traceId` para suporte) | `erro_interno` |
| 503 | Integração não configurada | `gateway_nao_configurado` |

`configuracao_pendente` indica que a operação depende de uma política ainda não definida pela administração (ver guia de configuração).

## Fluxo de autenticação

1. `POST /api/auth/login` `{ email, password, device }` → `{ accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, user }`.
2. `user` informa `kind`, `clubId`/`clubName`, `permissions`, `dependentPermissions`, `mustChangePassword` e `privacyAccepted`.
3. Ao receber 401, o cliente chama `POST /api/auth/refresh` `{ refreshToken }` **uma vez** e repete a requisição. O token de renovação
   muda a cada uso; o cliente deve guardar o novo.
4. `POST /api/auth/logout` `{ refreshToken }` encerra a sessão.

## Domínios

| Prefixo | Conteúdo |
|---|---|
| `/api/auth` | Login, renovação, logout, senha, sessões, consentimento de privacidade |
| `/api/clubes` | Clubes da plataforma (superadministrador), dados e horários do clube atual |
| `/api/usuarios`, `/api/funcoes` | Usuários, bloqueio, senhas, sessões, histórico de acessos, funções e permissões |
| `/api/associados`, `/api/dependentes`, `/api/funcionarios` | Cadastros, situação, acesso ao aplicativo, permissões de dependentes, LGPD |
| `/api/cartoes`, `/api/qr-code` | Cartão digital e emissão de QR Code dinâmico |
| `/api/portaria`, `/api/acessos` | Validação de QR, entradas e saídas, presentes, ocupação, histórico |
| `/api/financeiro`, `/api/mensalidades`, `/api/pagamentos` | Cobranças, ajustes, mensalidades, pagamentos, estornos, lançamentos, fluxo de caixa, inadimplência, fechamento |
| `/api/espacos`, `/api/reservas`, `/api/convidados`, `/api/calendario` | Espaços, disponibilidade, bloqueios, feriados, reservas, convidados, calendário |
| `/api/eventos`, `/api/comunicados`, `/api/notificacoes`, `/api/manifestacoes` | Eventos, comunicados segmentados, notificações, reclamações e sugestões |
| `/api/assembleias`, `/api/votacoes` | Assembleias, presença, atas, votações e resultados |
| `/api/patrimonio`, `/api/estoque`, `/api/manutencoes`, `/api/vistorias`, `/api/chaves`, `/api/ocorrencias` | Operação |
| `/api/relatorios`, `/api/auditoria`, `/api/configuracoes` | Painel, indicadores (PDF/Excel), auditoria, políticas do clube |
| `/api/arquivos`, `/api/anexos`, `/api/historico` | Arquivos protegidos, anexos de registros, histórico de situação |
| `/hubs/clube` | SignalR: `notification`, `availabilityChanged`, `occupancyChanged` |
| `/health` | Verificação de disponibilidade (API + banco) |

## Gateway de pagamento

A integração é feita pela interface `IPaymentGateway` (`backend/Clubes.Application/Common/Abstractions.cs`):
`CreateCheckoutAsync` (inicia a cobrança no provedor) e `ParseNotification` (valida a assinatura e interpreta o webhook
`POST /api/pagamentos/webhook`). O sistema nunca recebe dados de cartão. Hoje existem `DisabledPaymentGateway` (padrão: o aplicativo
informa que o pagamento é feito na secretaria) e `SandboxPaymentGateway` (homologação). Para contratar um provedor real
(ex.: Asaas, Mercado Pago, Pagar.me), implemente a interface na camada de infraestrutura e registre-a em `DependencyInjection.cs`.
