# Status do desenvolvimento

Atualizado em 17/11/2026. Clube: **Daros**, com duas sedes — **Guaíra** e **Foz** — cadastradas como clubes isolados na plataforma
(a equipe de cada sede, inclusive o presidente, só acessa a própria; o superadministrador acessa as duas, sempre separadas).

## Resumo

| Componente | Situação | Verificação realizada |
|---|---|---|
| Banco de dados (PostgreSQL) | Concluído | 63 tabelas, 3 migrations aplicadas em banco real; restrições testadas |
| API REST (.NET 10) | Concluída — 238 rotas | 89 testes automatizados aprovados + cenário ponta a ponta manual |
| Web (React) | Concluído — 26 módulos, 33 rotas | Verificação de tipos e build de produção sem erros; todas as rotas abertas no navegador com dados reais |
| Mobile (.NET MAUI) | Implementado — homologação parcial | Compila para Android, iOS (código gerenciado) e Windows com 0 erros; executado no emulador Android (login e tela inicial com dados reais) e aberto no Windows |
| Docker / backup | Concluído | Ver "Testes realizados" |
| Documentação | Concluída | — |

## Funcionalidades concluídas

**Fundação**
- Arquitetura em camadas (Domain, Application, Infrastructure, API, Tests) com injeção de dependência.
- Multi-clubes com quatro barreiras de isolamento (token, filtro global, guarda de gravação, chaves estrangeiras compostas no banco).
- Autenticação JWT com renovação rotativa, detecção de reuso, bloqueio por tentativas, recuperação e alteração de senha, sessões,
  histórico de acessos, bloqueio/desbloqueio de usuários.
- RBAC configurável: catálogo de permissões, funções por clube, exceções individuais, funções padrão (Secretaria, Financeiro, Portaria).
- Tratamento global de erros com respostas padronizadas, validação de entrada, logs (Serilog), rate limiting, Swagger/OpenAPI.
- Auditoria automática e imutável (valores anteriores/posteriores, origem, justificativa).

**Cadastros**
- Clubes (dados institucionais, logotipo, horários) e criação de clubes pelo superadministrador com estrutura física padrão.
- Associados: cadastro, edição, aprovação, suspensão, reativação, cancelamento, histórico, documentos, acesso ao aplicativo, filtros.
- Dependentes: limite por associado, aprovação, permissões individuais (administração + associado), login próprio, documentos.
- Funcionários e usuários de acesso; cartão digital individual.
- LGPD: consentimento versionado, exportação dos dados do titular, anonimização.

**Financeiro**
- Mensalidades por competência (base + política de dependentes: fixa, progressiva ou por faixas), geração idempotente e automática opcional.
- Cobranças avulsas, ajustes (desconto, acréscimo, multa, juros, taxa), multa e juros automáticos, cancelamento.
- Pagamentos na secretaria e pelo aplicativo (gateway abstrato + webhook assinado), idempotência, estornos, comprovante em PDF.
- Inadimplência com tolerância, bloqueios configuráveis, liberação excepcional justificada.
- Receitas e despesas, contas a pagar/receber, fluxo de caixa, fechamento mensal com reabertura auditada.
- Histórico financeiro preservado por cobrança (retrato da época).

**Operação**
- Espaços (horários por dia, capacidade, preços, limpeza, forma de cobrança, aprovação), bloqueios, feriados e horários especiais.
- Reservas por horário e por período de dias (salão: 2 dias), simulação de valores, aprovação, recusa, cancelamento, reagendamento,
  comparecimento/falta, penalidades configuráveis, disponibilidade em tempo real, bloqueio de conflitos no banco.
- Convidados (limite por associado/dia, taxa, autorização, entrada e saída).
- Portaria: validação de QR Code dinâmico de uso único, código numérico, registro manual justificado, presentes, ocupação em tempo real,
  horários de pico, histórico e acessos negados, ocorrências.
- Calendário administrativo unificado (dia/semana/mês) com identificação de conflitos.

**Comunicação**
- Comunicados segmentados com imagens, anexos e registro de leitura; eventos com inscrições e presença.
- Manifestações (reclamações/sugestões) com protocolo, fluxo completo e notificações.
- Assembleias (pauta, documentos, presença, ata) e votações (somente associados, voto único, critérios, resultados).
- Notificações internas, em tempo real (SignalR), push (Firebase) e e-mail (SMTP), com preferências e registro de entrega.

**Administração**
- Patrimônio, estoque (movimentações, fornecedores, estoque mínimo), manutenções (com bloqueio de espaço e despesa),
  vistorias com checklist, entrega e devolução de chaves, ocorrências com cobrança por dano sujeita a aprovação.
- Painel com indicadores reais, relatórios com filtros de período e exportação em PDF e Excel.
- Painel de configurações por clube com pendências explícitas.

**Infraestrutura**
- Docker Compose (db, api, web, backup), `.env.example`, backup automático com retenção, scripts de backup manual e restauração.
- Rotinas automáticas por clube (multas/juros, avisos financeiros, lembretes, política de inadimplência, devolução de chaves, limpeza).

## Mobile

Aplicativo .NET MAUI 10 (MVVM, CommunityToolkit) em `mobile/Clubes.Mobile`, consumindo a mesma API do Web.

**Implementado**
- Acesso: login, recuperação e redefinição de senha, troca obrigatória da senha temporária, aceite da política de privacidade,
  tokens somente em `SecureStorage`, renovação automática com rotação.
- Navegação inferior conforme o perfil: associado (Início, Carteirinha, Reservas, Comunicados, Perfil), dependente (mesmas áreas,
  com Reservas apenas se permitido) e portaria (Leitor, Presentes, Perfil).
- Associado e dependente: início com situação cadastral/financeira e atalhos; carteirinha com QR Code dinâmico (contagem regressiva,
  renovação automática só com a tela visível); reservas (disponibilidade por horário e por período, convidados, simulação de valores,
  confirmação, cancelamento, atualização em tempo real); convidados; comunicados com anexos; eventos e inscrições; manifestações com
  foto; notificações e preferências; perfil, sessões e políticas do clube.
- Somente associado: financeiro (cobranças, composição, pagamento com chave de idempotência, Pix copia-e-cola/checkout, acompanhamento,
  histórico e comprovante em PDF), dependentes (cadastro, permissões, acesso ao aplicativo), assembleias e votações.
- Portaria (funcionário com permissão `gate.operate`): leitor de QR Code pela câmera, código numérico manual, convidados do dia,
  presentes e ocupação, busca de pessoas e registro manual com justificativa.
- Tempo real (SignalR) para notificações, disponibilidade e ocupação; notificações locais do sistema.

**Verificado**
- Compilação: Android Debug e Release (APK gerado), iOS (somente código gerenciado) e Windows — 0 erros, 0 avisos.
- Execução no emulador Android (Pixel 7, API 36): tela de login, entrada como associado, permissão de notificações e tela inicial
  com dados reais da API (carteirinha, situação financeira, atalhos, navegação inferior). No Windows: abertura até o login.

**Não verificado / pendente**
- **As demais telas após o login ainda não foram percorridas em emulador ou dispositivo.** Câmera, tempo real, notificações do
  sistema, seleção de foto, abertura de arquivos e pagamento estão implementados contra o contrato da API, mas precisam de homologação.
- iOS: a compilação nativa e a execução exigem um Mac.
- Push real: depende das credenciais Firebase/APNs do clube (passos em `mobile/README.md`).
- Identificador do aplicativo provisório (`br.com.clubes.app`), ícone e tela de abertura definitivos.
- Tema escuro não implementado (o aplicativo usa o tema claro); o formulário de dependente no aplicativo não envia fotografia.
- A lista "próximas reservas" do Início pode incluir reservas canceladas ou recusadas (exibidas com a situação).

## Funcionalidades pendentes (dependem de terceiros ou de decisão do clube)

| Item | Situação | O que falta |
|---|---|---|
| Gateway de pagamento real | Arquitetura pronta (`IPaymentGateway`, checkout, webhook assinado, idempotência); existe um gateway de homologação | Escolher o provedor e implementar o adaptador com as credenciais do clube. Até lá, o aplicativo informa que o pagamento é feito na secretaria |
| Push (FCM/APNs) | Envio pelo servidor implementado (Firebase Admin) e registro de dispositivo na API | Credenciais do Firebase/APNs do clube e arquivos de configuração do aplicativo (ver `mobile/README.md`) |
| E-mail | Envio SMTP implementado | Informar as credenciais SMTP no `.env`. Sem elas, senhas temporárias são exibidas na tela para entrega manual e a recuperação de senha por e-mail fica indisponível |
| Políticas do clube | Mecanismos de configuração prontos | Decisões da administração — lista em `docs/regras-de-negocio/guia-de-configuracao.md` |
| Publicação nas lojas | — | Contas de desenvolvedor, identidade visual (ícone/splash definitivos) e assinatura dos aplicativos |

## Regras de negócio pendentes de definição pela administração

Nenhum valor foi presumido. Cada item abaixo é um parâmetro em Configurações, hoje **não definido**:

- **Financeiro**: mensalidade base · dia de vencimento · tolerância · momento do bloqueio por inadimplência · cobrança por dependente ·
  multas e juros · pagamento antecipado · critérios de mudança de composição familiar.
- **Reservas**: antecedência máxima · limite simultâneo · prazo de cancelamento sem multa · política de não comparecimento ·
  pagamento antecipado ou posterior · regras de aprovação · valores de locação e taxa de limpeza por espaço.
- **Convidados**: taxa por pessoa · cadastro antecipado · documentos exigidos · entrada sem o associado · regras por espaço.
- **Funcionamento**: horários gerais · feriados e fechamentos · regras para menores e acompanhamento · políticas das piscinas ·
  procedimentos para eventos.
- **LGPD**: texto da política de privacidade, encarregado e prazos de retenção.

Comportamento enquanto não definidos: limites e penalidades não são aplicados; operações que dependem do parâmetro (ex.: gerar
mensalidades) são recusadas com mensagem indicando o que falta.

## Problemas conhecidos e limitações

- O painel Web é voltado à equipe; associados e dependentes usam o aplicativo (ao entrar no Web, veem uma orientação).
- Regras específicas de convidados **por espaço** não têm parâmetros próprios: valem as regras gerais do clube e a capacidade do espaço.
- Reagendamento de reserva por período (salão) pelo Web não permite escolher data que se sobreponha ao período atual da própria reserva
  (a API permite; a tela de disponibilidade por período não distingue a própria reserva).
- Os totais da tela de mensalidades somam a página carregada (até 100 cobranças) e são rotulados assim quando há mais páginas.
- Mensagens de validação de formato vindas do framework são traduzidas por padrões; mensagens incomuns podem aparecer em inglês.
- O relatório PDF/Excel exporta os indicadores consolidados (não as listagens detalhadas).
- Arquivos enviados ficam em volume local; para múltiplas instâncias da API é necessário armazenamento compartilhado (a interface
  `IFileStorage` permite trocar por armazenamento em nuvem).
- Tempo real (SignalR) usa memória da instância; para escalar horizontalmente é necessário um backplane (Redis).

## Testes realizados

**Automatizados** — `dotnet test backend/Clubes.Tests`: **89 aprovados, 0 falhas** (API real + PostgreSQL real via Testcontainers).

| Área | Cobertura |
|---|---|
| Autenticação | Login, bloqueio por tentativas, rotação e reuso de token, usuário bloqueado, política de senha, endpoints protegidos |
| Autorização | Portaria sem acesso ao financeiro, exceções individuais, função criada pela administração, associado fora das áreas administrativas |
| Isolamento entre clubes | Leitura e alteração cruzadas recusadas, cabeçalho de clube ignorado, superadministrador, FK composta no banco, guarda de gravação |
| Associados e dependentes | CPF, duplicidade, fluxo de situação, perda de acesso, limite de 5 (inclusive concorrente), login individual, permissões em duas etapas |
| Financeiro | Cálculo por dependente (unitário), geração idempotente, preservação histórica, pagamento idempotente e concorrente, estorno, multa e juros, inadimplência e liberação, pagamento pelo aplicativo com webhook assinado, fechamento mensal, unicidade no banco |
| Reservas | Fluxo completo, conflito, concorrência, restrição no banco, horários de quadras e quiosques, capacidade, convidados, salão 2 dias com limpeza, bloqueios e feriados, inadimplência, limites, dependente, cancelamento e multa, privacidade entre associados |
| QR Code e portaria | Uso único, entrada/saída, invalidação, expiração, concorrência, dependente identificado, inadimplência e suspensão, permissão da portaria, convidados |
| Comunicação | Voto único e exclusivo de associados (inclusive concorrente), critério de presença, segmentação de comunicados, manifestações, eventos, notificações e entregas |
| Operação e LGPD | Vistoria → ocorrência → cobrança aprovada, manutenção com bloqueio, estoque, chaves, configurações e auditoria, relatórios e exportações, arquivos protegidos, anonimização, consentimento |

**Manuais (ambiente de desenvolvimento)**
- Cenário ponta a ponta pela API: configuração do clube → associados e dependentes → mensalidades (com multa e juros) → pagamentos →
  reservas (conflito e inadimplência recusados) → comunicado, evento, manutenção, estoque → login do associado → QR Code validado na
  portaria (reuso recusado) → manifestação e reserva pendente.
- Web: login, painel, associados e cobranças conferidos; todas as 33 rotas abertas com dados reais, sem erros.
- Verificação de tipos e build de produção do Web sem erros.
- Docker: imagens da API e do Web construídas; pilha completa (`db`, `api`, `web`, `backup`) iniciada em projeto isolado com banco
  vazio — migrations aplicadas, superadministrador e clube Daros criados, `/health` 200, Web servindo rotas da SPA com cabeçalhos de
  segurança, login, geração de PDF e Excel dentro do contêiner.
- Backup e restauração: backup automático e manual gerados; registro criado após o backup deixou de existir após a restauração.

**Não realizado**
- Percurso completo das telas do aplicativo mobile em emulador/dispositivo (ver seção "Mobile").
- Teste de carga e teste de invasão.
- Envio real de e-mail, push e pagamento (dependem de credenciais externas).

## Ambiente de desenvolvimento: sedes, contas de teste e dados de exemplo

- `python database/scripts/contas-dev.py` cria as sedes Guaíra e Foz e as contas de teste (superadministrador, administrador,
  presidente, tesouraria e portaria de cada sede, secretaria, associado e dependente) e gera os arquivos locais de entrada rápida.
- `python database/scripts/dados-exemplo-dev.py` popula as duas sedes com dados de exemplo.
- A entrada rápida só existe em `npm run dev` (Web) e em build Debug (Mobile); verificado que o build de produção do Web não contém
  as contas. As políticas gravadas pelo script de exemplo são valores de demonstração, não decisões do clube.
- Verificado pela API: o presidente de Guaíra recebe "não encontrado" ao abrir um associado de Foz, o cabeçalho de sede é ignorado
  para ele e a lista de sedes é negada; o superadministrador consulta as duas sedes separadamente.

## Decisões técnicas

| Decisão | Justificativa |
|---|---|
| Cada sede (Guaíra, Foz) é um clube da plataforma | Dados separados por sede com o isolamento já garantido em quatro camadas; o superadministrador alterna entre as sedes. Não há visão consolidada das duas sedes em uma única tela |
| Banco único com `ClubId` + chaves estrangeiras compostas | Isolamento garantido também pelo banco, com operação e backup simples; evita o custo de um banco por clube |
| Serviços de aplicação usam o EF Core diretamente (via `IAppDb`) e referenciam o provedor Npgsql | O banco é PostgreSQL por requisito; evita uma camada de repositórios sem ganho real e permite usar `ILIKE` e bloqueios de linha |
| Restrição de exclusão (`EXCLUDE USING gist`) para reservas | Única forma de impedir conflitos sob concorrência sem depender da aplicação |
| Solicitação pendente já ocupa o horário | Evita que duas solicitações do mesmo horário sejam aprovadas; o horário é liberado na recusa ou no cancelamento |
| Cobrança guarda o retrato das regras | Requisito de preservação histórica; mudanças de configuração nunca alteram cobranças existentes |
| Token de QR opaco, de uso único, com hash no banco | Não há o que falsificar ou reutilizar; a validação é sempre no servidor |
| Notificações gravadas na transação e entregues por despachante | A operação de negócio nunca falha por causa de push/e-mail; a entrega é rastreável |
| Token de renovação com rotação; no Web em `sessionStorage` | Reduz a janela de uso de um token vazado; sem cookies, não há CSRF |
| Parâmetros nulos = "não definido" | Atende à exigência de não presumir políticas: o sistema diferencia "sem limite" de "ainda não decidido" e lista as pendências |
| E-mail de login único na plataforma | Simplifica o login (não é preciso informar o clube); a mesma pessoa em dois clubes usa e-mails distintos |
| Reserva "na mensalidade seguinte" vira cobrança própria com vencimento no próximo dia de vencimento | Mantém a mensalidade inalterável e a reserva rastreável separadamente |
| Web com módulos autodescobertos e tipos gerados do OpenAPI | O contrato da API é a fonte única; erros de integração aparecem na verificação de tipos |
| Gateway de homologação (Sandbox) recusado em produção | Permite validar o fluxo completo de pagamento sem simular pagamentos em ambiente real |

## Próximas etapas

1. Administração do Daros define as políticas pendentes em Configurações e os valores dos espaços.
2. Contratar o gateway de pagamento e implementar o adaptador.
3. Configurar SMTP e Firebase; publicar a política de privacidade.
4. Homologar o aplicativo em dispositivos Android e iOS e preparar a publicação nas lojas.
5. Implantar com HTTPS e cópia externa dos backups; executar um teste de restauração.
