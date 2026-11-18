# Modelagem do banco de dados

PostgreSQL 17, modelado por EF Core (código em `backend/Clubes.Domain`, mapeamento em
`backend/Clubes.Infrastructure/Persistence/AppDbContext.cs`, migrations versionadas em `Persistence/Migrations`).

- [Diagrama entidade-relacionamento](diagrama-er.md) (Mermaid, por área)
- [Dicionário de dados](dicionario-de-dados.md) (todas as tabelas, colunas, chaves, unicidades e restrições)

Ambos são gerados do esquema real: `python database/scripts/gerar-doc-banco.py`.

## Convenções

- Chave primária `Id` (UUID v7, ordenável por tempo) em todas as tabelas; `CreatedAt`/`UpdatedAt` em UTC.
- Enumerações gravadas como texto (legíveis em consultas e relatórios).
- Valores monetários `numeric(14,2)`; percentuais `numeric(9,4)`; quantidades de estoque `numeric(14,3)`.
- Datas-horas em `timestamptz` (UTC); datas de calendário (`vencimento`, `nascimento`, `visita`) em `date`.
- Nomes de tabelas e colunas iguais aos das entidades (entre aspas no SQL).

## Isolamento multi-clubes no banco

Toda tabela de clube tem `ClubId` (FK para `Club`). Além disso, cada tabela de clube possui a chave alternativa `(ClubId, Id)`, e os
relacionamentos entre tabelas de clube usam **chave estrangeira composta** `(ClubId, XxxId) → (ClubId, Id)`. Consequência: é impossível,
até por SQL manual, vincular um dependente, cobrança, reserva etc. a um registro de outro clube.

## Integridade referencial

- Toda coluna `XxxId` do tipo UUID possui chave estrangeira (gerada por convenção no `AppDbContext`), com `ON DELETE RESTRICT` —
  nada é apagado em cascata.
- Única exceção deliberada: `Charge.GuestId` é referência informativa sem FK (o vínculo íntegro é `Guest.ChargeId`), para evitar
  dependência circular na gravação.
- Referências polimórficas (`PersonType` + `PersonId`, `OwnerType` + `OwnerId`, `EntityType` + `EntityId`) não têm FK por natureza.

## Regras garantidas pelo banco

| Regra | Mecanismo |
|---|---|
| Sem sobreposição de reservas no mesmo espaço | `EX_ReservationSlot_NoOverlap`: `EXCLUDE USING gist (SpaceId WITH =, tstzrange(StartAt, EndAt) WITH &&) WHERE (IsActive)` |
| Um voto por associado em cada votação | Índice único `(PollId, MemberId)` |
| Uma mensalidade ativa por associado e competência | Índice único filtrado em `Charge` (`Type = 'Monthly' AND Status <> 'Cancelled'`) |
| Pagamento não duplicado | Índice único `(ClubId, IdempotencyKey)` e `(ClubId, ReceiptNumber)` |
| Pessoa não entra duas vezes sem sair | Índice único `(ClubId, PersonType, PersonId)` em `Presence` |
| Token de QR único | Índice único em `QrToken.TokenHash` |
| CPF único por clube | Índices únicos filtrados em `Member`, `Dependent`, `Employee` |
| E-mail de login único | Índice único filtrado em `User.Email` |
| Valores coerentes | `CHECK` em `Charge` (valores ≥ 0, pago ≤ total), `Payment`/`FinancialEntry` (valor > 0), `StockItem` (saldo ≥ 0), períodos com fim > início |
| Auditoria imutável | Gatilhos que rejeitam `UPDATE`, `DELETE` e `TRUNCATE` em `AuditLog` |
| Concorrência em cobranças e estoque | Versão de linha (`xmin`) como token de concorrência otimista |

## Exclusão lógica

`Club`, `User`, `Member`, `Dependent`, `Employee`, `Space`, `Asset`, `Supplier`, `StockItem` e `Announcement` usam `IsDeleted`/`DeletedAt`
(filtro global oculta os excluídos). Registros financeiros nunca são excluídos: cobranças são canceladas, pagamentos são estornados e
lançamentos são cancelados, sempre preservando o original e o motivo.

## Histórico

- **Financeiro**: cada cobrança guarda o retrato da época (mensalidade base, quantidade e nomes dos dependentes, percentuais de multa e
  juros, tolerância). Ajustes ficam em `ChargeAdjustment`; pagamentos e estornos em `Payment`.
- **Situações**: `StatusHistory` registra as mudanças de situação de associados, dependentes, funcionários, reservas, manutenções,
  ocorrências, patrimônio, assembleias e votações.
- **Auditoria**: `AuditLog` (valores anteriores e posteriores em `jsonb`).

## Sequências por clube

`ClubSequence (ClubId, Name, Value)` fornece numeração independente por clube para matrícula de associado, número do cartão, código
de reserva, protocolo de manifestação, recibo, ocorrência e ordem de manutenção.

## Migrations

| Migration | Conteúdo |
|---|---|
| `Inicial` | Todas as tabelas, chaves, índices e a extensão `btree_gist` |
| `RestricoesAvancadas` | Restrição de exclusão das reservas, gatilhos de imutabilidade da auditoria e `CHECK`s |
| `ChargeGuestSemChaveEstrangeira` | Remove a FK `Charge.GuestId` (evita ciclo de gravação) |

Aplicadas automaticamente na inicialização da API. Script SQL idempotente: ver `docs/arquitetura/instalacao-e-execucao.md`.
