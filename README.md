# VanderBakery

MVP B2B para uma distribuidora de produtos de panificação. O fornecedor usa o site Blazor; o vendedor usa o app Android nativo em .NET MAUI. Ambos consultam a mesma API ASP.NET Core e o mesmo banco SQLite.

## Estrutura

```text
VanderBakery.slnx
src/
  VanderBakery.Api/       API REST, regras comerciais e EF Core/SQLite
  VanderBakery.Web/       portal web do fornecedor em Blazor Server
  VanderBakery.Mobile/    app nativo .NET MAUI para vendedores
```

Os DTOs são locais às interfaces para manter o primeiro MVP simples. As regras importantes (carteira, preço oficial, estoque, corte e parcelas) ficam na API.

## Pré-requisitos

- .NET 10 SDK
- Carga de trabalho .NET MAUI com Android
- Android Studio/emulador Android, se for executar o app

## Executar o site e a API

Abra PowerShell na pasta deste README. Em um terminal, inicie a API:

```powershell
dotnet restore .\VanderBakery.slnx --configfile .\NuGet.Config
dotnet run --project .\src\VanderBakery.Api --urls http://localhost:5293
```

Em outro terminal, inicie o portal:

```powershell
dotnet run --project .\src\VanderBakery.Web --urls http://localhost:5168
```

Abra `http://localhost:5168`. Entre como fornecedor com `admin@vanderbakery.com` e `Vander123!`.

O banco é criado como `vanderbakery-mvp.db` no diretório de execução da API e recebe dados de demonstração no primeiro início. A API fica em `http://localhost:5293`; o documento OpenAPI de desenvolvimento fica em `/openapi/v1.json`.

## Executar o app MAUI

Com a API ativa e um emulador Android aberto:

```powershell
dotnet build .\src\VanderBakery.Mobile -f net10.0-android
dotnet run --project .\src\VanderBakery.Mobile -f net10.0-android
```

O app do emulador usa `http://10.0.2.2:5293` para alcançar a API que roda no computador. Para usar um aparelho físico, altere a URL no construtor de `MainPage` em `src/VanderBakery.Mobile/MainPage.xaml.cs` para o endereço IP local do computador e permita a conexão na rede. O projeto habilita HTTP sem TLS só para a demonstração local.

## Contas demonstrativas

| Perfil | E-mail | Senha |
|---|---|---|
| Fornecedor/admin | `admin@vanderbakery.com` | `Vander123!` |
| Vendedor | `vendedor@vanderbakery.com` | `Vander123!` |

As senhas são armazenadas com PBKDF2. Os tokens de sessão desta demonstração ficam em memória e expiram quando a API reinicia; para produção, use ASP.NET Core Identity ou um provedor OAuth/JWT com expiração, refresh token e proteção TLS.

## Fluxo demonstrativo

1. O vendedor entra no MAUI e consulta a carteira ordenada por sequência de visita.
2. Consulta resumo do cliente (última compra e saldo), escolhe produtos e condição de pagamento e envia o pedido.
3. A API confirma carteira e cliente ativo, recalcula preços, grava o pedido como `Recebido` e protege repetição com chave de idempotência.
4. O fornecedor entra no portal Blazor, abre o pedido e informa o corte. A API impede quantidade maior que o pedido ou que o estoque válido, inclusive considerando cortes já reservados.
5. O fornecedor registra a venda. A API usa quantidade aprovada, preserva o preço, baixa os lotes por validade e gera parcelas com soma igual ao total.

O modo ativo de pedidos é **envio imediato**. O agrupamento às 12h e às 19h não está implementado nesta versão.

## Endpoints principais

Todas as rotas abaixo, exceto `GET /api/health` e `POST /api/auth/login`, exigem `Authorization: Bearer <token>`.

| Método e rota | Uso |
|---|---|
| `POST /api/auth/login` | Login de vendedor/fornecedor |
| `GET /api/customers` | Carteira do vendedor autenticado, em ordem de visita |
| `POST /api/customers` | Vendedor cadastra cliente na própria carteira |
| `GET /api/customers/{id}/summary` | Última compra e saldo em aberto autorizado |
| `GET /api/products?groupId=&search=` | Catálogo, preço, disponibilidade e validade |
| `GET /api/lookups/groups` | Grupos de produtos ativos |
| `GET /api/lookups/payment-conditions` | Condições disponíveis para pedidos |
| `POST /api/orders` | Envio idempotente de pedido; API calcula o preço |
| `GET /api/orders?customerId=&status=` | Histórico/status visível ao vendedor da carteira |
| `POST /api/orders/{id}/repeat` | Copia itens para novo rascunho no cliente |
| `GET /api/admin/orders?status=` | Fila de pedidos do fornecedor |
| `POST /api/orders/{id}/cut` | Registra quantidades aprovadas e reserva estoque |
| `POST /api/orders/{id}/invoice` | Registra venda e cria contas a receber |
| `GET /api/receivables` | Parcelas dentro da permissão do usuário |
| `POST /api/admin/sellers` | Cadastro de vendedor pelo admin |
| `POST /api/admin/sellers/{id}/coverages` | Adiciona cobertura de cidade ou estado |
| `POST /api/admin/customers` | Cadastro administrativo com atribuição geográfica |
| `PUT /api/admin/customers/{id}/assignment` | Corrige vendedor e ordem de visita |
| `POST /api/admin/products` e `/api/admin/products/{id}/lots` | Cadastro de produto e lote/validade |
| `POST /api/admin/groups` | Cadastro de grupo |
| `PUT /api/admin/payment-conditions/{id}` | Ajusta parcelas e encargos configurados |
| `PUT /api/admin/receivables/{id}/paid` | Baixa manual de parcela |

Exemplo de login e envio de pedido:

```http
POST /api/auth/login
Content-Type: application/json

{"email":"vendedor@vanderbakery.com","password":"Vander123!"}
```

```http
POST /api/orders
Authorization: Bearer <token>
Content-Type: application/json

{"customerId":1,"paymentConditionId":10,"idempotencyKey":"ordem-cliente-chave-unica","items":[{"productId":1,"quantity":2}]}
```

## Compilar e conferir

```powershell
dotnet build .\src\VanderBakery.Api
dotnet build .\src\VanderBakery.Web
dotnet build .\src\VanderBakery.Mobile -f net10.0-android
```

Foi conferido o fluxo API de login, carteira, consulta do cliente e produtos, envio idempotente, corte, faturamento e soma das parcelas. Também foi conferido que não é possível cortar além do saldo total disponível.

## SQLite para SQL Server

A conexão está em `src/VanderBakery.Api/appsettings.json` (`ConnectionStrings:Default`). Para produção, adicione o pacote `Microsoft.EntityFrameworkCore.SqlServer`, troque `UseSqlite` por `UseSqlServer` em `Program.cs`, use connection string em secret manager/variável de ambiente e introduza migrations versionadas. Não use `EnsureCreated` como estratégia de atualização de esquema em produção.

## Limites do MVP e próximos passos

- Sem lote automático de pedidos às 12h/19h; o comportamento ativo é imediato.
- O MAUI permite login, consultar roteiro/resumo, catálogo, cadastrar cliente e enviar pedido. Histórico e repetição têm rotas na API, mas ainda não têm telas próprias.
- O portal cobre pedidos/corte/faturamento, produtos/grupos/lotes, vendedores/cobertura, carteira e contas a receber. Cadastro completo de cidades/estados, edição/desativação de todos os registros, auditoria visual e configuração de formas de pagamento ainda são próximos passos.
- No portal, a aba **Vendedores e regiões** permite ativar/desativar o vendedor; desativado, ele perde o acesso à API mesmo se já estava conectado. Há regiões prontas para atribuir: Campinas, Jundiaí, São Paulo, Sorocaba e Ribeirão Preto (SP), Estado de São Paulo, Curitiba (PR) e Florianópolis (SC). Elas só entram na cobertura do vendedor após seleção explícita.
- Há um alerta do NuGet sobre vulnerabilidade no pacote transitivo `SQLitePCLRaw.lib.e_sqlite3` 2.1.11. Atualize o provedor/pacotes para uma versão corrigida antes de uso real.
- Antes de produção, também é necessário TLS, autorização com tokens persistidos/expiráveis, validação completa de documentos e dados, concorrência transacional no corte, migrations, trilha de auditoria, backup, logs monitorados e revisão das regras comerciais.
