# Guia de permissões

## Tipos de usuário

| Tipo | Onde atua | Como é autorizado |
|---|---|---|
| Superadministrador da plataforma | Web | Cadastra e ativa clubes; opera em qualquer clube selecionando-o no topo da tela (validado no servidor) |
| Administrador do clube | Web | Todas as permissões dentro do seu clube |
| Funcionário | Web (e Mobile para portaria) | Permissões das funções atribuídas + exceções individuais |
| Associado | Mobile | Somente os próprios dados e os de seus dependentes; único perfil que vota |
| Dependente | Mobile | Somente o próprio cadastro; funcionalidades conforme permissões individuais |

## Como funciona (RBAC configurável)

1. **Catálogo de permissões** fixo no sistema (`backend/Clubes.Application/Common/Permissions.cs`, também em `GET /api/funcoes/permissoes`).
2. **Funções** são combinações livres dessas permissões, criadas e editadas por clube em **Web → Administração → Funções e permissões**,
   sem alterar código.
3. Um funcionário pode ter várias funções. **Exceções individuais** concedem ou negam uma permissão a um usuário específico
   (a negação prevalece sobre as funções).
4. As permissões são lidas do banco a cada requisição: mudanças valem imediatamente.
5. A interface esconde o que o usuário não pode fazer, mas **toda verificação é refeita na API**.

## Funções padrão (criadas com cada clube, editáveis)

| Função | Acesso |
|---|---|
| **Secretaria** | Painel, calendário, associados e dependentes (cadastro e documentos), consulta financeira e registro de pagamentos, espaços, reservas, convidados, eventos, comunicados, manifestações, assembleias (consulta), vistorias (consulta), ocorrências |
| **Financeiro** | Painel, consulta de associados/dependentes, todo o financeiro (cobranças, pagamentos, estornos, ajustes, lançamentos, fechamento, liberações), relatórios |
| **Portaria** | Validar QR Codes e registrar entradas/saídas, histórico de acessos e ocupação, consulta de convidados, ocorrências. **Sem acesso** a financeiro, cadastros completos ou configurações |

## Catálogo de permissões

| Grupo | Código | Descrição |
|---|---|---|
| Geral | `dashboard.view` | Visualizar o painel inicial |
| Geral | `calendar.view` | Visualizar o calendário administrativo |
| Geral | `reports.view` | Visualizar e exportar relatórios |
| Geral | `audit.view` | Consultar a auditoria e a entrega de notificações |
| Clube | `club.manage` | Editar dados institucionais do clube |
| Clube | `settings.view` / `settings.manage` | Visualizar / alterar configurações e políticas |
| Acesso | `users.view` / `users.manage` | Visualizar usuários e sessões / gerenciar usuários, bloqueios e senhas |
| Acesso | `roles.manage` | Gerenciar funções e permissões (inclusive atribuí-las) |
| Associados | `members.view` / `members.manage` / `members.approve` | Consultar / cadastrar e editar / aprovar, suspender, cancelar e anonimizar |
| Associados | `dependents.view` / `dependents.manage` | Consultar / gerenciar dependentes e suas permissões |
| Associados | `documents.view` | Visualizar documentos pessoais |
| Equipe | `employees.view` / `employees.manage` | Consultar / gerenciar funcionários |
| Financeiro | `finance.view` | Consultar cobranças, pagamentos e fluxo de caixa |
| Financeiro | `finance.charges` | Gerar e cancelar cobranças e mensalidades |
| Financeiro | `finance.payments` | Registrar pagamentos na secretaria |
| Financeiro | `finance.refund` | Estornar pagamentos |
| Financeiro | `finance.adjust` | Aplicar descontos, multas, juros e taxas |
| Financeiro | `finance.entries` | Gerenciar receitas e despesas |
| Financeiro | `finance.close` | Fechamento mensal (e reabertura) |
| Financeiro | `finance.release` | Liberar excepcionalmente associados inadimplentes |
| Espaços | `spaces.view` / `spaces.manage` | Consultar / gerenciar espaços, horários, bloqueios e feriados |
| Espaços | `reservations.view` / `reservations.manage` | Consultar / aprovar, recusar, cancelar, reagendar e criar reservas |
| Portaria | `guests.view` / `guests.manage` | Consultar / autorizar e gerenciar convidados |
| Portaria | `gate.operate` | Validar QR Codes e registrar entradas e saídas |
| Portaria | `gate.history` | Consultar histórico de acessos e ocupação |
| Comunicação | `events.view` / `events.manage` | Consultar / gerenciar eventos e presença |
| Comunicação | `announcements.manage` | Publicar e gerenciar comunicados |
| Comunicação | `manifestations.view` / `manifestations.manage` | Consultar / tratar reclamações e sugestões |
| Comunicação | `assemblies.view` / `assemblies.manage` | Consultar / gerenciar assembleias e votações |
| Operação | `assets.view` / `assets.manage` | Patrimônio |
| Operação | `stock.view` / `stock.manage` | Estoque |
| Operação | `maintenance.view` / `maintenance.manage` | Manutenções |
| Operação | `inspections.view` / `inspections.manage` | Vistorias e entrega de chaves |
| Operação | `occurrences.view` / `occurrences.manage` | Ocorrências |
| Operação | `occurrences.approve-charge` | Aprovar cobranças por danos |

Somente administradores podem criar ou alterar outros administradores.

## Associados e dependentes

Não usam o catálogo acima. A API restringe cada endpoint ao **próprio titular**:

- **Associado**: próprio cadastro, cartão e QR Code, dependentes, cobranças e pagamentos, reservas, convidados, eventos, comunicados
  destinados a associados, manifestações, assembleias e votações.
- **Dependente**: próprio cadastro, cartão e QR Code, comunicados **explicitamente** destinados a dependentes, manifestações.
  Nunca acessa financeiro, assembleias ou votações.

### Permissões individuais do dependente

| Código | Permite |
|---|---|
| `reservations.request` | Solicitar reservas (sempre sujeitas à aprovação da administração) |
| `events.register` | Inscrever-se em eventos |
| `guests.register` | Cadastrar convidados em nome do associado responsável |

Uma permissão só é **efetiva** quando (1) a administração a libera para aquele dependente e (2) o associado responsável a habilita.
Se o clube desligar a opção "associado pode habilitar permissões dos dependentes", a liberação da administração já basta.
Toda mudança notifica o dependente (e o associado, quando feita pela administração).
