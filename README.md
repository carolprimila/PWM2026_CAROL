# VanderBakery · sede Foz do Iguaçu

MVP renovado do sistema de distribuição de produtos para panificação. Inclui API ASP.NET Core, banco SQLite, portal do fornecedor em Blazor e aplicativo Android em .NET MAUI. O visual usa rosa e branco; cidades atendidas: Foz do Iguaçu e Santa Terezinha de Itaipu.

## Estrutura

- `src/VanderBakery.Api`: API REST, banco central, clientes, roteiro, estoque, vendas e relatórios.
- `src/VanderBakery.Web`: portal do fornecedor com resumo diário, histórico de vendas e lucro, clientes, produtos e roteiros.
- `src/VanderBakery.Mobile`: app do vendedor com roteiro do dia, consulta de estoque, venda, cadastro e histórico de clientes.

O banco único `vanderbakery-foz-v2.db` é criado automaticamente dentro de `src/VanderBakery.Api` no primeiro início e recebe produtos, clientes e condições de pagamento de demonstração. O schema novo usa um arquivo próprio para não misturar dados do MVP anterior.

## Executar API e portal

Requer .NET 10 SDK. Em dois terminais PowerShell na pasta do projeto:

```powershell
dotnet restore .\VanderBakery.slnx --configfile .\NuGet.Config
dotnet run --project .\src\VanderBakery.Api --urls http://localhost:5293
```

```powershell
dotnet run --project .\src\VanderBakery.Web --urls http://localhost:5168
```

Acesse `http://localhost:5168`. O portal consulta a API em `http://localhost:5293`.

O portal web abre na tela de acesso do **administrador**; o aplicativo MAUI abre no acesso do **vendedor**. Contas de demonstração: `admin@vanderbakery.com` e `vendedor@vanderbakery.com`, ambas com senha `Vander123!`. As duas telas também permitem cadastro do respectivo perfil. A API guarda senhas com hash e sal, e protege as rotas de dados com token de sessão (as sessões em memória expiram quando a API é reiniciada).

## Executar Android

Requer a carga de trabalho MAUI/Android e um emulador Android:

```powershell
dotnet build .\src\VanderBakery.Mobile -f net10.0-android
dotnet run --project .\src\VanderBakery.Mobile -f net10.0-android
```

No emulador, o app usa `http://10.0.2.2:5293` para acessar a API do computador. Para um celular físico, troque o endereço em `MainPage.xaml.cs` pelo IP local do computador e permita acesso pela rede.

## Funcionalidades deste MVP

- Cadastro de clientes com nome, nome fantasia opcional, CPF/CNPJ, e-mail, cidade e bairro.
- Bairros e localidades pré-carregados no seletor para as duas cidades. Foz inclui os 37 bairros da Lei Complementar 303/2018 e nomes locais conhecidos; Santa Terezinha inclui bairros da base postal e divisões urbanas locais.
- Roteiro de visitas separado por dia da semana e posição, como 01, 02 e 03. O mesmo cliente pode ter posições diferentes em dias diferentes. A demonstração inclui Michel e Marcos na segunda e Maria e Ana na quarta.
- Cadastro e consulta de produtos com preço, custo, quantidade em estoque e validade.
- Venda registrada com preço e custo unitários preservados; o estoque é baixado no banco na mesma transação.
- A API rejeita quantidades acima do estoque, não permite saldo negativo e não confia em preço enviado pelo app.
- Consulta do histórico de compras do cliente no aplicativo.
- Formas de pagamento boleto, PIX, cartão de crédito e débito; opção à vista ou em 2 e 3 parcelas. A forma escolhida fica registrada em cada parcela.
- Condições 10 (7/14 dias) e 20 (7/14/21 dias), com parcelas que fecham exatamente o total da venda e multa de 5%, juros de 2% ao dia e desconto de 1% ao dia. A condição à vista fica disponível como 1 parcela.
- O histórico informa se cada venda está em aberto, parcialmente quitada, atrasada ou quitada, além do status de cada parcela.
- A demonstração inclui compra de Michel Padaria com as parcelas quitadas e compra de Marcos Alimentos com uma parcela quitada e outra em aberto.
- Contas a receber por cliente; o fornecedor pode marcar parcelas como recebidas.
- Relatório histórico por data com faturamento, custo e lucro bruto; o lucro também aparece por produto e por venda.

## Rotas da API

- `GET /api/health`
- `GET /api/locations`
- `GET|POST /api/customers`
- `GET /api/customers/{id}`
- `POST /api/customers/{id}/route`
- `GET /api/routes?day=Quarta-feira`
- `GET|POST /api/products`
- `PUT /api/products/{id}/stock?quantity=12`
- `GET /api/payment-conditions`
- `GET /api/payment-methods`
- `GET|POST /api/sales`
- `GET /api/receivables`
- `PUT /api/receivables/{id}/paid?paid=true`
- `GET /api/reports/daily?date=2026-10-29`

Swagger/OpenAPI está disponível em `/openapi/v1.json` no ambiente de desenvolvimento.

## Observações

Esta reconstrução mantém o fluxo direto de venda com validação obrigatória de estoque. O MVP não tem sincronização offline ou hospedagem configurada. Cadastro aberto de administrador e tokens em memória servem para demonstração local; antes de publicar, restrinja a criação de administradores e configure HTTPS e gestão persistente de sessões. O projeto de API pode exibir um alerta do NuGet relacionado a `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, dependência transitiva do provedor SQLite.
