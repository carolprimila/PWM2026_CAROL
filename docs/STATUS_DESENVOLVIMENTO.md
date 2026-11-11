# Status do desenvolvimento

Última atualização: 2026-11-11 — **Todas as etapas entregues + pedidos do cliente (contrato, diretoria, eventos com vários locais, reservas no app — DT-032). Roda em qualquer notebook pelo Visual Studio (SQLite).**

## Etapas

| Etapa | Situação |
|---|---|
| 1 — Fundação (API, EF, banco, auth, multi-clubes, usuários, perfis, permissões, clubes, auditoria) | ✅ concluída e testada |
| 2 — Associados, categorias, dependentes, documentos, aprovações, carteirinhas, QR Code, controle de acesso, portal | ✅ backend concluído e testado (telas nas Etapas 5 e 6) |
| 3 — Financeiro | ✅ concluída e testada (API, Web e app) |
| 4 — Eventos, espaços, reservas, solicitações | ✅ concluída e testada (API, Web e app) |
| 5 — Web administrativo (Blazor, simples — DT-024) | ✅ concluído (telas de todos os módulos, perfis, configurações, auditoria, relatórios) |
| 6 — App .NET MAUI (simples — DT-025) | ✅ concluído (testado no Windows; Android compila, não testado em aparelho) |
| 7 — Funcionários, patrimônio, estoque, assembleias, votações, notificações | ✅ concluída e testada (API, Web e app) |
| 8 — Finalização, segurança, produção | ✅ concluída e testada (Docker escrito, não executado) |

## Como roda (desde a DT-030)
- Abrir `ProjetoClubes.slnx` no Visual Studio e escolher o perfil "API + Web administrativo" (ou "API + App"); F5.
- Banco SQLite `src/ProjetoClubes.Api/projetoclubes.db`, criado sozinho com dados de demonstração (senha `Clubes2026!`).
- Nada para instalar além do Visual Studio/.NET 10 SDK (e a carga MAUI para o app). Sem user-secrets.
- Migrations: `20261111180346_Inicial` (esquema completo para SQLite, com os gatilhos de conflito de reservas) e
  `ContratosEventosDiretoria` (DT-032).
  As migrations e scripts do PostgreSQL foram removidos.

## Etapa 1 — entregue
Ver histórico em `docs/DECISOES_TECNICAS.md` (DT-001 a DT-015). Autenticação JWT + refresh com rotação, sessões
revogáveis, bloqueio por tentativas, recuperação de senha, primeiro acesso seguro, multi-clubes em 6 camadas,
perfis por clube, permissões configuráveis, anti-escalonamento, clubes, auditoria automática.

## Etapa 2 — entregue

### Domínio
`Pessoa` (por clube, CPF único no clube, token de carteirinha), `CategoriaAssociado` (voto, ser votado, dependentes,
limite), `Interesse`, `Associado` (matrícula, categoria, interesses), `Dependente` (parentesco, número, permissões,
exceção de múltiplos titulares), `VinculoDependente`, `PermissoesDependente`, `ConfiguracaoDependentes`,
`HistoricoAssociado`/`HistoricoDependente`, `Arquivo`, `DocumentoPessoal`, `AcessoClube`, `SequenciaClube`;
`RegistroCadastral` (máquina de estados) e `RegrasDependentes` (regras puras).

### Casos de uso e endpoints (ver `docs/api/ENDPOINTS.md`)
- Categorias e interesses configuráveis; padrões criados para clubes novos e existentes.
- Associados: cadastro com matrícula sequencial, edição, busca (nome/CPF/matrícula), filtros, CPF mascarado em
  listas, aprovação/rejeição/suspensão/reativação/inativação com motivo, exclusão lógica, histórico, resumo
  por situação/categoria, "apto a votar" calculado pela categoria, criação de login no app.
- Dependentes: inclusão pela administração ou pelo titular, regras do clube (limite, cônjuge, idade, parentesco,
  categoria), aprovação, exceção autorizada para dois titulares, transferência de titularidade com histórico de
  vínculos, permissões individuais (restrição a menores), exclusão lógica, histórico, login próprio por idade.
- Documentos e fotos privados com validação por assinatura binária; análise (aprovar/rejeitar) de documentos;
  documento comprobatório obrigatório configurável.
- Carteirinha digital (associado e dependentes) com QR Code opaco (PNG), regeneração do código.
- Portaria: validar, registrar entrada/saída (inclusive recusas) com funcionário responsável, histórico e presentes.
- Portal do associado/dependente: perfil, contato, carteirinhas, dependentes (titular), autorização de login.
- Seed de demonstração: Foz 2 (3 associados, 3 dependentes, um pendente) e Guaíra 2 (1 associado, 1 dependente);
  login de app `carlos.associado@demo.projetoclubes.local`.

### Testes (`dotnet test ProjetoClubes.slnx`)
- Unitários: **50/50** aprovados (+15: máquina de estados, regras de dependentes, idade, CPF mascarado, QR, formatos).
- Integração (PostgreSQL real): **48/48** aprovados (+24), cobrindo critérios de aceitação 8–13 e 23–26:
  padrões do clube; matrícula sequencial; CPF duplicado/ inválido; CPF igual em clubes diferentes; fluxo de
  aprovação; suspensão com motivo; inativação em cascata; isolamento de associados, carteirinhas, QR e documentos
  entre clubes; numeração de dependentes; limite, idade, parentesco, cônjuge e categoria; dois titulares com e sem
  exceção; associado como dependente; aprovação, transferência e histórico; permissões e menores; documento
  obrigatório; upload/download/análise; executável disfarçado; foto; permissões de documentos; QR sem CPF;
  entrada/saída/presentes; dependente pendente/sem permissão/titular suspenso; carteirinha de outro clube;
  regeneração; permissão de portaria; portal do titular (perfil, carteirinhas, 403 no admin); inclusão pelo app,
  edição só pendente, documentos pelo app; outro associado sem acesso (404); login de dependente por idade.
- Teste manual (API local + seed): listagem, portal, carteirinha, entrada na portaria, QR em PNG.

### Bug encontrado e corrigido durante os testes
- Transferência de dependente gerava `DbUpdateConcurrencyException` (EF emitia UPDATE para o vínculo novo).
  Causa e correção em DT-016.

### Erros pendentes / limitações conhecidas
- Notificações ao titular sobre aprovação de dependentes: resolvido na Etapa 7 (aviso no app; push real ainda pendente).
- Regras financeiras de dependentes (incluídos na mensalidade, valor adicional) já configuráveis; aplicadas na Etapa 3.
- Arquivo órfão se o banco falhar após o upload (DT-021); limite de dependentes sob inclusões simultâneas no mesmo
  titular: resolvido na Etapa 8 (bloqueio por titular; testado com 5 inclusões simultâneas).
- Um usuário que já tem acesso administrativo ao clube não recebe perfil de associado automaticamente ao criar
  o acesso ao app (409); a administração deve adicionar o perfil "Associado" ao usuário.

## Web administrativo (Blazor) — primeira versão
Projeto `src/ProjetoClubes.Web` (Blazor Web App, Interactive Server, Bootstrap), no estilo do VanderBakery.
- Telas: entrar; início (indicadores reais da API); associados (lista com busca/filtro/paginação, novo, detalhe com
  aprovar/rejeitar/suspender/reativar/inativar com motivo, QR Code da carteirinha, inclusão de dependente, convite do app);
  dependentes (lista, aprovar/rejeitar pendentes); portaria (entrada/saída por código, presentes, últimos registros);
  usuários (lista, criar com convite, remover/reativar acesso); clubes (superadmin: cadastrar, ativar/desativar, convidar presidente).
- Menu por permissão, troca de clube, cor do clube no menu, sessão mantida ao recarregar (F5).
- Verificado no navegador contra a API real: login (presidente e portaria), indicadores, lista e aprovação de associado,
  cadastro de associado, dependentes, usuários, portaria autorizando dependente e recusando código inválido.
- Ainda não existem no Web: edição de associado/dependente, documentos e foto, transferência e permissões de dependente,
  perfis e permissões, configurações do clube e de dependentes, auditoria. A API já oferece tudo isso.
- Sem testes automatizados de interface (a lógica está na API, coberta pelos 98 testes).

## App .NET MAUI — primeira versão
Projeto `src/ProjetoClubes.Mobile`, uma tela no estilo do VanderBakery.Mobile.
- Login (só associados/dependentes), sessão salva no SecureStorage, saudação e situação do cadastro.
- Carteirinhas do titular e dos dependentes, com a cor do clube e o QR Code vindo da API.
- Titular: lista de dependentes e inclusão (fica pendente para o clube aprovar). Dependente vê só a própria carteirinha.
- Atualização de telefone e e-mail (bloqueada para menores). Puxar para baixo atualiza.
- Compila para Android e Windows (e iOS/Mac Catalyst no build da solução). Testado no Windows contra a API real,
  automatizando a interface: login do associado de demonstração, 3 carteirinhas com QR Code e lista de dependentes.
- Não testado em emulador/celular Android nesta máquina. Mensalidades (Etapa 3) e eventos, reservas e solicitações
  (Etapa 4) foram acrescentados depois. Ainda não tem: comunicados, foto, documentos e recuperação de senha pelo app.
- Correção na API feita junto: `PUT /portal/perfil/contato` sem endereço apagava o endereço salvo; agora só altera o que for enviado.

## Etapa 3 — Financeiro (entregue)
- API (`/api/v1/financeiro/*`, `/api/v1/portal/financeiro`): regras de multa/juros, planos por categoria, geração idempotente
  de mensalidades (com adicional de dependentes), pagamento com recibo, estorno, isenção, cancelamento, situação financeira
  do associado, receitas/despesas (contas a pagar/receber), resumo do período, fluxo de caixa e inadimplentes.
- Web: menu Financeiro com Mensalidades (resumo, gerar, receber, estornar), Receitas e despesas, Fluxo de caixa, Planos e regras.
- App: seção "Minhas mensalidades" para o titular (em dia/vencidas, valor, recibo).
- Seed demo: planos para as categorias dos clubes Foz 2 e Guaíra 2.
- Testes: unitários **57/57** (+7: encargos, mensalidade, plano); integração **56/56** (+8: geração idempotente e por plano,
  adicional de dependentes, pagamento e recibo, atraso com encargos e inadimplência, valor insuficiente, estorno e isenção,
  permissões e isolamento, portal só com as próprias mensalidades, receitas/despesas e fluxo de caixa).
- Testado no navegador (gerar mensalidades do mês, receber com encargos, resumo atualizado) e no app Windows (mensalidade paga com recibo).
- Pendências: exportação PDF/Excel dos relatórios, parcelamento de débitos e integração real com Pix/boleto (só a arquitetura
  está prevista — DT-026).

## Etapa 4 — Eventos, espaços, reservas e solicitações (entregue)
- API (`/api/v1/eventos`, `/espacos`, `/reservas`, `/solicitacoes`, `/portal/{eventos,reservas,solicitacoes}`):
  eventos com público, capacidade, acompanhantes, lista de espera com promoção automática, presença pelo QR, cancelamento;
  espaços com valores e aprovação; reservas com pedido pelo app, aprovação/recusa com motivo, cancelamento, calendário;
  solicitações com protocolo anual, respostas, anotações internas e situação. Regras de concorrência e privacidade em DT-027.
- Web: Eventos (lista, novo, detalhe com inscrições, inscrever associado, presença, encerrar/cancelar), Reservas
  (calendário de 30 dias, nova reserva, aprovar/recusar pendentes), Espaços, Solicitações (lista, detalhe com conversa).
- App: Próximos eventos ("Quero participar"/cancelar), Reservas (pedido e situação), Solicitações (abrir e acompanhar respostas).
- Seed demo: Salão social, Churrasqueira 1, Quadra poliesportiva e a "Feijoada beneficente" (dia 7 do próximo mês, ocupando o salão).
- Testes: unitários **65/65** (+8); integração **66/66** (+10: capacidade/acompanhantes/lista de espera/promoção, público e
  presença por QR, inscrição pelo app com dependente e cancelamento do evento, pedido de reserva/aprovação/conflito, evento
  ocupando o espaço inteiro, pedidos simultâneos (só um passa), restrição do banco sem passar pela API, calendário sem dados
  de terceiros e dependente menor sem reservar, protocolo/respostas internas/encerramento, isolamento de solicitações).
- Testado no navegador (calendário, reserva do clube, conflito com a Feijoada, espaços, evento, resposta à solicitação) e no
  app Windows (inscrição na Feijoada → aparece no Web como confirmada; solicitação aberta no app → respondida no Web).
- Bug encontrado nos testes de tela e corrigido: o calendário dava 500 quando o Web enviava o filtro em horário de Brasília
  (-03:00). Agora todo `DateTimeOffset` é convertido para UTC também nos parâmetros de consulta (DT-027); teste de integração
  passou a usar -03:00.
- Pendências: cobrança da locação de espaço/ingresso de evento no financeiro (valores já são guardados), bloqueios de
  agenda (manutenção) e calendário visual mensal no Web. (Avisos de reserva aprovada/recusada: feitos na Etapa 7.)

## Etapa 7 — Comunicação, funcionários, patrimônio, estoque e assembleias (entregue)
- API: comunicados com confirmação de leitura, caixa de notificações (gravadas; push preparado atrás de `IEnvioPush`),
  funcionários (cargo, departamento, escala, férias/afastamentos, desligamento, histórico), patrimônio (código, local, estado,
  manutenções, baixa, histórico), estoque (produtos, entradas/saídas/ajustes, mínimo, pedidos de compra) e assembleias
  (convocação, pautas, presença por QR, aptidão pelo estatuto, votação pelo app, apuração, decisões, ata imutável). Regras em DT-028.
- Notificações também nos módulos anteriores: reserva aprovada/recusada, resposta de solicitação, dependente aprovado/rejeitado.
- Web: menus Comunicados, Assembleias, Funcionários, Patrimônio e Estoque (com abas de compras e movimentações).
- App: Avisos (notificações, marcar como lidos), Comunicados (ler e confirmar leitura), Assembleias (convocação, voto com confirmação, resultado).
- Seed demo: 2 comunicados, 3 funcionários, 3 bens, 4 produtos (2 abaixo do mínimo) e uma AGO convocada com 3 pautas, nos dois clubes.
- Testes: unitários **79/79** (+14: aptidão para votar, ciclo da assembleia, ata imutável, opções de voto, estoque, compra,
  manutenção/baixa, férias sobrepostas, comunicado publicado); integração **75/75** (+9: comunicado → notificação → confirmação,
  isolamento e permissão de comunicados, notificações de reserva/solicitação (sem anotação interna), fluxo completo da
  assembleia com dependente recusado, categoria sem voto, voto duplicado e apuração só no fim, adimplência exigida,
  estoque com compra, 8 saídas simultâneas + CHECK no banco, patrimônio com histórico, funcionários com férias/desligamento).
- Testado no navegador (instalar AGO, presença de 3 associados com as três situações de aptidão, abrir/encerrar votação,
  apuração, decisão, ata; compras abaixo do mínimo e recebimento; manutenção de bem; férias de funcionário) e no app Windows
  (aviso "Votação aberta", comunicados, voto "Sim" com confirmação → "Voto registrado" → apuração no Web).
- Bugs encontrados nos testes de tela e corrigidos: produto sem pedido mostrava "compra pedida: 0"; decisão podia ser
  registrada antes da votação; data da ata sem ano; lista de funcionários com filtro dava 500 (projeção não traduzível).
- Pendências: enquetes e pesquisas de satisfação, push real (FCM/APNs), custo de manutenção/compra lançado no financeiro,
  escala de trabalho em grade, notificações no Web.

## Etapa 8 — Finalização (entregue)
- **Segurança**: revisão das 42 rotas sem atributo de permissão (todas protegidas no serviço); cabeçalhos de segurança na
  API e no Web; limite geral de requisições; `X-Forwarded-For` só de proxies configurados (**falha corrigida**: antes
  qualquer cliente podia trocar o IP e burlar o limite de login); API recusa partir em produção com demo ligado ou CORS
  aberto; envio de e-mail por SMTP; limite de dependentes protegido contra inclusões simultâneas. Detalhes em DT-029.
- **LGPD**: exportação dos dados pessoais (app e administração) e anonimização de quem saiu do clube, limpando também os
  valores antigos da auditoria.
- **Web — telas que faltavam**: Perfis e permissões (criar perfil, marcar permissões, trocar perfis de um usuário),
  Configurações do clube (identidade, aprovações, regras de dependentes), Auditoria (alterações com o que mudou e acessos
  ao sistema), Relatórios (associados, inadimplentes, portaria e estoque, com CSV), edição completa do associado,
  exportação/anonimização LGPD, "Esqueci minha senha" e a página `/redefinir-senha` (os links de convite e recuperação
  apontavam para uma página que não existia).
- **App**: "Esqueci minha senha" no login, "Exportar meus dados" (compartilhamento do sistema) e endereço da API por
  configuração de build.
- **Produção**: Dockerfile do Web, compose com API/Web em rede própria, `.env.example` revisado, chaves do
  DataProtection em volume, guia `docs/deploy/PRODUCAO.md` (HTTPS, backup, monitoramento, atualização, publicação do app).
- **Testes**: unitários **80/80** (+1: anonimização); integração **80/80** (+5: cabeçalhos, limite de dependentes com inclusões simultâneas, exportação LGPD com isolamento,
  anonimização com login/QR/auditoria, e o **roteiro de aceitação 1–27** inteiro num único teste em clube novo).
  Build de toda a solução sem avisos; Android compila; `dotnet publish -c Release` de API e Web ok.
- Testado no navegador (perfis, perfis de usuário, configurações salvas e revertidas, auditoria com o diff, relatórios e
  CSV, edição de associado mantendo o endereço, download LGPD, recuperação de senha pelo link do e-mail) e no app Windows
  (esqueci minha senha, login, exportação gerando o JSON).

## Pedidos do cliente após a Etapa 8 (DT-032)
- **Reserva pelo app**: o associado já conseguia reservar, mas não via o que estava ocupado. Agora o app mostra os horários
  ocupados do espaço no dia escolhido. No Web, o calendário mostra "Finalidade — nome de quem reservou" (no app, só "Ocupado").
- **Cadastro do associado**: profissão, endereço completo, gênero, data de cadastro, "Titular" e histórico de alterações com
  quem fez e quais campos mudaram. Filtro de associados por gênero.
- **Contrato do associado** (`ContratoAssociado`): número `ano-0001`, em análise → aprovado/reprovado → desligado/encerrado,
  valor mensal (padrão: plano da categoria + adicional de dependentes), dependentes e parentesco, anual com renovação.
  Ao aprovar, gera as 12 mensalidades vinculadas ao contrato; a ficha mostra parcelas com emissão, vencimento, valor, multa,
  juros e situação. Desligar cancela as parcelas futuras em aberto e inativa o associado.
- **Dependente vira associado**: dependente ativo e maior de idade (com CPF) ganha matrícula própria; o vínculo se encerra.
- **Eventos**: vários locais por evento (salão + quadra + quiosque…), pop-up de locais disponíveis no período com capacidade
  e agenda, capacidade do evento limitada à soma dos locais, divulgação (aviso no app, e-mail e link de compartilhamento no
  WhatsApp; texto pronto para redes sociais quando aberto ao público).
- **Limite de reservas por tipo de espaço**: por família (titular + dependentes) por mês, configurável em cada espaço.
- **Login com mais de um clube**: depois de e-mail e senha, escolhe o clube (Web e app).
- **Associação e diretoria**: dados e endereço da associação editáveis em Configurações; diretoria com cargos e mandato
  (presidente: 2 anos, um por vez, aviso 60 dias antes do fim e quando falta presidente); gestões anteriores guardadas.
- Migration nova `ContratosEventosDiretoria` (aplicada sobre o banco existente, sem perder dados).
- Testes: unitários **87/87** (+7) e integração **85/85** (+5). Bug encontrado pelos testes e corrigido: reprovar/desligar
  com motivo em branco mudava a situação do contrato antes de recusar.
- Conferido no navegador (cópia em portas separadas): Configurações com diretoria, ficha do associado com contrato gerando
  12 parcelas, evento com segundo local pelo pop-up e divulgação. App Windows compila; a escolha de clube no app não foi
  testada em tela (nenhum associado de demonstração participa de dois clubes).

## Três projetos como o VanderBakery (DT-031)
- `src/ProjetoClubes.Api` (com `Domain/`, `Application/`, `Infrastructure/` dentro), `src/ProjetoClubes.Web` e
  `src/ProjetoClubes.Mobile`; cada `.csproj` compila sozinho. Arquivos de build da raiz removidos.
- Build da solução sem avisos; unitários **80/80** e integração **80/80**.

## Troca para SQLite (DT-030)
- Pedido para rodar como o VanderBakery: clonar, abrir no Visual Studio e rodar, sem instalar banco.
- Provedor, migrations, travas de concorrência, gatilhos de conflito de reservas, CHECKs e mapeamento de erros adaptados.
- Testes: unitários **80/80**, integração **80/80** (agora ~40 s). Build da solução sem avisos.
- Verificado numa cópia limpa (só os arquivos do Git, sem bin/obj/.db, sem user-secrets): build de tudo, API criou o banco
  sozinha com os dados demo, login do presidente e do Superadmin pela API e login no Web pelo navegador.
- Os históricos das etapas abaixo citam PostgreSQL: valem para a época; as garantias equivalentes estão na DT-030.

## Situação final e pendências conhecidas
- **Não executado**: containers Docker (Docker não instalado aqui) e o app em aparelho/emulador Android.
- **Não implementado** (fora do escopo entregue ou deixado para uma próxima fase): push real (FCM/APNs), Pix/boleto
  integrados, enquetes e pesquisas de satisfação, bloqueios de agenda, calendário visual mensal, escala de trabalho em
  grade, foto/documentos pelo app, lançamento automático de custos de manutenção/compras no financeiro, parcelamento.
- Limitação LGPD: textos livres digitados (motivos, descrições) não são reescritos na anonimização (DT-029).
- O Web segue o padrão simples combinado (Blazor + Bootstrap), no lugar do React do escopo original (DT-024).

## Como continuar
```bash
dotnet build ProjetoClubes.slnx
dotnet test ProjetoClubes.slnx
dotnet run --project src/ProjetoClubes.Api --launch-profile http
dotnet ef migrations add <Nome> --project src/ProjetoClubes.Api/Infrastructure --startup-project src/ProjetoClubes.Api --output-dir Persistence/Migrations
```
Ler antes: `docs/arquitetura/ARQUITETURA.md`, `docs/DECISOES_TECNICAS.md`.
