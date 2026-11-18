# Regras de negócio implementadas

Todas as regras abaixo são aplicadas **na API** (e, quando indicado, também pelo banco). Web e Mobile apenas exibem o resultado.
Parâmetros citados como "configurável" ficam em Configurações (ver `guia-de-configuracao.md`).

## Multi-clubes
- Cada clube tem seus próprios associados, dependentes, funcionários, espaços, reservas, financeiro, eventos e configurações.
- Um usuário nunca consulta, altera ou exclui dados de outro clube; a tentativa responde "não encontrado".
- O clube é determinado pelo token do usuário, nunca por dado enviado pelo cliente.

## Associados
- Não existem categorias de associado: a pessoa é associada ou não é.
- CPF válido e único por clube. Situações: aguardando aprovação → ativo → suspenso/cancelado (com reativação). Toda mudança exige
  motivo (exceto aprovação), gera histórico e notificação.
- O acesso ao aplicativo só pode ser criado para associados aprovados; a senha temporária obriga a troca no primeiro acesso.
- Cancelar a associação encerra as sessões do associado e de seus dependentes.

## Dependentes
- Até **5** por associado (configurável); o limite é garantido mesmo com cadastros simultâneos.
- Cadastrado pela equipe: entra ativo. Cadastrado pelo associado no aplicativo: aguarda aprovação da administração.
- Cada dependente tem login, cartão digital e QR Code próprios; cada acesso é identificado individualmente.
- Dependentes **nunca votam**, não veem assembleias/votações, não veem dados financeiros e só veem comunicados destinados a dependentes.
- Permissões individuais exigem liberação da administração **e** habilitação pelo associado responsável. Mesmo com permissão,
  a reserva solicitada por dependente sempre aguarda aprovação da administração.
- Removido o dependente, seu login é excluído e as mensalidades futuras deixam de considerá-lo.

## Cartão digital e QR Code
- O QR Code é dinâmico, individual e temporário (validade configurável, padrão 60 s) e vale para **uma única operação**.
- O aplicativo pede um token novo a cada exibição; o servidor decide se é de **entrada** (pessoa fora) ou **saída** (pessoa dentro).
- Emitir um token novo invalida o anterior; o código numérico de 8 dígitos exibido abaixo do QR também muda a cada token.
- Na validação o token é consumido de forma atômica: reutilização, expiração e leituras simultâneas são recusadas.
- Cada validação (autorizada ou negada) registra pessoa, clube, operação, data/hora, funcionário, origem e resultado/motivo.
- Entrada negada para cadastro não ativo, responsável não ativo, pessoa já presente ou bloqueio financeiro (quando habilitado).
  Saída exige entrada registrada. Registro manual (sem QR) exige justificativa e fica auditado.

## Financeiro
- **Mensalidade** = mensalidade base + acréscimo por dependentes (ativos ou suspensos), conforme a política configurada
  (sem cobrança, valor fixo, progressiva ou por faixas). Sem política definida, a geração é recusada.
- A geração por competência é idempotente (não duplica) e só inclui associados ativos.
- Cada cobrança guarda o **retrato da época**: base, quantidade e nomes dos dependentes, percentuais de multa/juros e tolerância.
  Mudanças de configuração ou de composição familiar valem apenas para cobranças futuras.
- **Multa** (uma vez) e **juros diários** são aplicados automaticamente após o vencimento + tolerância, com os percentuais da cobrança.
- **Pagamentos**: na secretaria (funcionário autorizado; forma, valor, data, responsável, recibo numerado, comprovante opcional) ou
  pelo aplicativo (gateway; confirmação por notificação assinada). Pagamento parcial é permitido; nunca acima do saldo.
- **Idempotência**: a mesma chave nunca registra dois pagamentos; notificações repetidas do gateway não creditam duas vezes.
- **Estorno** mantém o pagamento no histórico como "estornado" e reabre a cobrança. Cobrança com pagamento não pode ser cancelada.
- Movimentações confirmadas nunca são apagadas. Descontos, acréscimos, multas, juros e taxas manuais exigem motivo.
- **Inadimplência**: cobrança em aberto com vencimento + tolerância ultrapassados. Quando as políticas estão habilitadas, o
  inadimplente não entra no clube, não solicita reservas nem cadastra convidados; reservas já aprovadas seguem a política
  configurada (manter, suspender ou cancelar). **Liberação excepcional** exige permissão, justificativa e validade.
- **Fechamento mensal** consolida os totais e bloqueia novas movimentações na competência; a reabertura exige motivo.

## Espaços e reservas
- Estrutura inicial do Daros: 4 quadras (tênis, vôlei, futsal, futcampo), 10 quiosques (5 fechados, 5 abertos), salão de festas e
  piscinas (não reserváveis). Novos espaços podem ser cadastrados.
- **Quadras**: 08h–22h, intervalos de 1 h, múltiplos horários, capacidade 30.
- **Quiosques**: 12h–22h; sextas e sábados 12h–00h; intervalos de 1 h; capacidade 20.
- **Salão de festas**: capacidade 250; reserva obrigatoriamente por **2 dias** (montagem, preparação, evento, retirada da decoração
  e limpeza pelo clube). A taxa de limpeza integra o valor total, apresentado com seus componentes.
- Conflitos de horário são impossíveis (restrição no banco). Solicitações pendentes também ocupam o horário até a decisão.
- Não se reserva: horário passado, fora do funcionamento, em bloqueio (administrativo/manutenção), em fechamento por feriado, acima da
  capacidade (associados + dependentes + convidados), além da antecedência ou do limite de reservas simultâneas (quando configurados).
- Aprovação manual por padrão (automática se configurado). Reservas criadas pela equipe já nascem aprovadas; reservas administrativas
  e de eventos internos não geram cobrança.
- Cobrança da reserva: antecipada (na aprovação) ou no vencimento da mensalidade seguinte, conforme o espaço/clube.
- Cancelamento libera o horário, cancela convites e a cobrança ainda não paga. Multa por cancelamento tardio e penalidade por não
  comparecimento só existem se configuradas; a falta exige descrição da ocorrência.
- Mudanças (aprovação, recusa, cancelamento, alteração de horário, lembrete) notificam o associado e o dependente solicitante.
- A disponibilidade é atualizada em tempo real nos clientes conectados.

## Convidados
- Até **3** por associado por dia (configurável), garantido sob concorrência; somam na capacidade do espaço.
- Cadastro pelo associado (ou dependente autorizado) fica "aguardando autorização"; pela equipe, já autorizado.
- Taxa por convidado (se configurada) gera cobrança ao responsável; convite cancelado ou não autorizado cancela a taxa não paga.
- A entrada exige: data da visita = hoje, autorização, responsável ativo e sem bloqueio financeiro, documento (se exigido) e
  presença do responsável (se o clube não permitir entrada sem ele).

## Comunicação
- **Comunicados** segmentados: associados, dependentes, funcionários ou destinatários específicos; rascunho não aparece para ninguém;
  leituras são registradas. Dependentes só veem o que foi marcado para dependentes.
- **Eventos**: público (todos), somente associados ou públicos específicos; inscrição única, respeitando capacidade e prazos.
- **Manifestações** (reclamação, sugestão e tipos configurados) em formulário único, com protocolo. Fluxo: recebida → em análise →
  em andamento → resolvida/recusada; sugestões podem ser aceitas e encaminhadas à assembleia. O autor é notificado em cada etapa;
  notas internas não aparecem para ele.
- **Assembleias e votações**: somente associados ativos votam, uma vez por votação (garantido no banco); critérios de participação
  (todos os ativos, em dia, presentes na assembleia); resultado parcial nunca é exposto — só após a divulgação pela administração.

## Operação
- **Vistoria** com checklist antes/depois; havendo dano, gera ocorrência vinculada à reserva e ao associado.
- **Cobrança por dano**: proposta com justificativa → aprovação administrativa (permissão específica) → só então lançada no financeiro.
- **Chaves**: entrega somente para reserva aprovada; devolução com condições e pendências; atraso sinalizado e notificado.
- **Manutenção**: solicitada → em análise → aprovada → em andamento → concluída/cancelada; pode bloquear o espaço (reflete na
  disponibilidade imediatamente) e, ao concluir, o bloqueio é encerrado e o custo pode virar despesa.
- **Estoque**: todo saldo muda por movimentação rastreável; saldo nunca negativo; alerta ao atingir o mínimo.
- **Patrimônio**: baixa com justificativa (exclusão lógica) e histórico de estado.

## Notificações
Internas (sempre), em tempo real, push e e-mail (quando configurados), com registro de envio, entrega e leitura. O usuário define
preferências por categoria; notificações obrigatórias (financeiras críticas, bloqueios, cancelamentos) ignoram a preferência.

## Auditoria
Toda criação, alteração e exclusão registra usuário, clube, operação, entidade, valores anteriores e novos, origem e justificativa.
Os registros são imutáveis.
