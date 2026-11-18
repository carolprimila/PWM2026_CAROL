# Guia de configuração do clube

As políticas operacionais não estão fixas no código. Ficam em **Web → Administração → Configurações** (`/api/configuracoes`),
por clube, com validação, permissão (`settings.manage`), justificativa opcional e histórico completo na auditoria.

## Princípio: nada é presumido

Um parâmetro **não definido** aparece em "Políticas pendentes de definição" e se comporta assim:

- **Limites e penalidades** não definidos → **não são aplicados** (sem limite, sem multa).
- **Operações que dependem do parâmetro** → são **recusadas** com erro `configuracao_pendente` explicando o que falta
  (ex.: gerar mensalidades sem valor base, dia de vencimento ou política de dependentes).
- **Bloqueios** (inadimplência) só valem quando a política correspondente é marcada como "Sim".

Alterações valem **dali em diante**: cobranças já geradas e reservas confirmadas não são recalculadas.

## Parâmetros

### Financeiro

| Parâmetro | Efeito | Se não definido |
|---|---|---|
| Mensalidade base | Valor base de cada mensalidade | Geração recusada |
| Dia de vencimento (1–28) | Vencimento das mensalidades e das cobranças "na mensalidade seguinte" | Geração recusada |
| Prazo de tolerância (dias) | Dias após o vencimento até caracterizar inadimplência | Considera 0 |
| Multa por atraso (%) e juros ao dia (%) | Aplicados automaticamente após a tolerância, com os percentuais vigentes **na geração da cobrança** | Não aplicados |
| Cobrança por dependente | `Sem cobrança`, `Valor fixo por dependente`, `Progressiva` (valor do 1º, 2º, 3º… dependente) ou `Por faixas` (valor total por faixa de quantidade) | Geração recusada |
| Bloquear acesso / reservas de inadimplentes | Nega entrada na portaria / novas reservas e convidados | Sem bloqueio |
| Reservas aprovadas de inadimplentes | `Manter`, `Suspender` (restabelece ao regularizar) ou `Cancelar` | Mantidas |
| Pagamento antecipado | Permite ou não quitar cobranças com vencimento distante pelo aplicativo | Permitido |
| Geração automática de mensalidades | Gera as mensalidades do mês corrente automaticamente | Desligada (geração manual) |
| Aviso de vencimento (dias) | Antecedência da notificação "vencimento próximo" | 3 dias (0 desliga) |
| Critérios de mudança de composição familiar | Texto informativo da política | Pendente |

### Cadastros

| Parâmetro | Padrão |
|---|---|
| Máximo de dependentes por associado | 5 |
| Máximo de convidados por associado por dia | 3 |
| Associado pode habilitar permissões dos dependentes | Sim (se "Não", a liberação da administração já vale) |

### Reservas

| Parâmetro | Efeito | Se não definido |
|---|---|---|
| Antecedência máxima (dias) | Limite de data para solicitações de associados | Sem limite |
| Limite de reservas simultâneas | Reservas futuras em aberto por associado | Sem limite |
| Cancelamento sem multa (horas) + multa de cancelamento tardio | Lança penalidade quando o associado cancela fora do prazo | Sem multa |
| Penalidade por não comparecimento | Lançada ao registrar a falta | Sem penalidade |
| Pagamento antecipado obrigatório | Define a forma de cobrança padrão das reservas | Usa a forma definida em cada espaço |
| Aprovação | Manual ou automática (solicitações de dependentes são sempre manuais) | Manual |

Cada **espaço** tem ainda: horários por dia da semana, capacidade, preço por hora ou por período, taxa de limpeza, forma de cobrança
(antecipada ou na mensalidade seguinte) e modo de aprovação próprios (Web → Espaços).

### Convidados

| Parâmetro | Efeito | Se não definido |
|---|---|---|
| Taxa por convidado | Gera cobrança ao associado responsável | Sem taxa |
| Cadastro antecipado obrigatório | Associado não cadastra convidado para o mesmo dia | Livre |
| Documento obrigatório | Exige documento no cadastro e na entrada | Opcional |
| Entrada sem o associado responsável | Se "Não", o responsável precisa estar no clube | Não verificado |

### Acesso, funcionamento e LGPD

- Validade do QR Code (segundos, padrão 60).
- Horários gerais do clube (Web → Clube) e feriados/fechamentos/horários especiais (Web → Espaços → Feriados).
- Textos de política exibidos no aplicativo: feriados, menores, acompanhamento de menores, piscinas, eventos, uso dos espaços.
- Política de privacidade e versão (nova versão exige novo aceite de todos os usuários).
- Tipos de manifestação (reclamação, sugestão, outros).

## Decisões pendentes da administração do Daros

Enquanto não forem tomadas, o sistema opera conforme a coluna "Se não definido" acima.

**Financeiro**: valor da mensalidade base · dia de vencimento · prazo de tolerância · momento do bloqueio por inadimplência ·
política de cobrança por dependente · multas e juros · regras de pagamento antecipado · critérios para mudança de composição familiar.

**Reservas**: antecedência máxima · limite de reservas simultâneas · prazo de cancelamento sem multa · política de não comparecimento ·
pagamento antecipado ou cobrança posterior · regras de aprovação · valores de locação e taxa de limpeza de cada espaço.

**Convidados**: taxa por pessoa · cadastro antecipado obrigatório ou não · documentos exigidos · entrada sem o associado responsável.

**Funcionamento**: horários gerais · feriados e fechamentos · regras para menores e acompanhamento · políticas das piscinas ·
procedimentos para eventos.

**Integrações**: gateway de pagamento · credenciais de e-mail (SMTP) · credenciais do Firebase (push) · texto da política de privacidade,
encarregado (DPO) e prazos de retenção.
