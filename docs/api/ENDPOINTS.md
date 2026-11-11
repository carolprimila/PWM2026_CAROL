# API REST — endpoints disponíveis (Etapas 1 e 2)

Documentação interativa: `http://localhost:5080/swagger` (desenvolvimento).
Todos os endpoints exigem `Authorization: Bearer <accessToken>`, exceto os marcados como públicos.
Erros seguem RFC 9457 (`application/problem+json`) com `codigo` e `correlationId`.

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/v1/setup/status` | público | Sistema já inicializado? |
| POST | `/api/v1/setup/inicializar` | público + token de inicialização | Cria o primeiro Superadmin |
| POST | `/api/v1/auth/login` | público | Login (`clubeId` opcional) |
| POST | `/api/v1/auth/refresh` | público | Renova tokens (rotação) |
| POST | `/api/v1/auth/logout` | autenticado | Encerra a sessão |
| GET | `/api/v1/auth/me` | autenticado | Usuário, clube atual, clubes, permissões |
| POST | `/api/v1/auth/trocar-clube` | autenticado | Seleciona outro clube |
| POST | `/api/v1/auth/esqueci-senha` | público | Envia link de redefinição |
| POST | `/api/v1/auth/redefinir-senha` | público | Define nova senha via token |
| POST | `/api/v1/auth/alterar-senha` | autenticado | Altera senha e encerra outras sessões |
| GET | `/api/v1/auth/sessoes` | autenticado | Sessões ativas do usuário |
| DELETE | `/api/v1/auth/sessoes/{id}` | autenticado | Revoga uma sessão própria |
| GET | `/api/v1/auth/historico` | autenticado | Histórico de autenticação próprio |
| GET | `/api/v1/clubes` | autenticado | Clubes visíveis (Superadmin: todos) |
| GET | `/api/v1/clubes/{id}` | membro/Superadmin | Detalhe do clube |
| POST | `/api/v1/clubes` | Superadmin | Cadastra clube (cria perfis padrão) |
| PUT | `/api/v1/clubes/{id}` | `clubes.editar` | Edita cadastro |
| POST | `/api/v1/clubes/{id}/ativar` · `/desativar` | Superadmin | Situação |
| PUT | `/api/v1/clubes/{id}/configuracoes` | `clubes.configurar` | Configurações e identidade visual |
| POST | `/api/v1/clubes/{id}/administradores` | Superadmin | Cria/vincula administrador |
| GET | `/api/v1/clubes/{id}/historico` | `auditoria.visualizar` | Histórico do clube |
| GET | `/api/v1/usuarios` | `usuarios.visualizar` | Usuários do clube (busca, perfil, situação, paginação) |
| GET | `/api/v1/usuarios/{id}` | `usuarios.visualizar` | Detalhe com permissões efetivas |
| POST | `/api/v1/usuarios` | `usuarios.gerenciar` | Cria/vincula usuário (convite por e-mail sem senha) |
| PUT | `/api/v1/usuarios/{id}` | `usuarios.gerenciar` | Edita nome |
| PUT | `/api/v1/usuarios/{id}/acesso` | `usuarios.gerenciar` | Perfis + conceder/negar individuais |
| POST | `/api/v1/usuarios/{id}/inativar` · `/reativar` | `usuarios.gerenciar` | Acesso ao clube |
| POST | `/api/v1/usuarios/{id}/bloquear` · `/desbloquear` | Superadmin | Conta inteira |
| POST | `/api/v1/usuarios/{id}/revogar-sessoes` | `usuarios.gerenciar` | Sessões no clube |
| GET/POST | `/api/v1/perfis` | ver/`perfis.gerenciar` | Perfis do clube |
| GET/PUT/DELETE | `/api/v1/perfis/{id}` | ver/`perfis.gerenciar` | Perfil |
| GET | `/api/v1/permissoes` | autenticado | Catálogo agrupado por módulo |
| GET | `/api/v1/auditoria` | `auditoria.visualizar` | Logs de alteração do clube |
| GET | `/api/v1/auditoria/acessos` | `auditoria.visualizar` | Histórico de autenticação do clube |
| GET | `/health` | público | Saúde da API e do banco |

## Etapa 2 — cadastros, carteirinha, portaria e portal

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/v1/categorias-associados` | ver associados/configurar | Categorias (`?incluirInativas=true`) |
| POST/PUT/DELETE | `/api/v1/categorias-associados[/{id}]` | `clubes.configurar` | Categorias e direitos (voto, dependentes, limite) |
| GET/POST/DELETE | `/api/v1/interesses[/{id}]` | autenticado / `clubes.configurar` | Interesses |
| GET | `/api/v1/associados` | `associados.visualizar` | Busca nome/CPF/matrícula; `situacao`, `categoriaId`, `interesseId` |
| GET | `/api/v1/associados/resumo` | `associados.visualizar` | Totais por situação e categoria |
| GET/POST/PUT | `/api/v1/associados[/{id}]` | ver/`cadastrar`/`editar` | Detalhe, cadastro, edição |
| POST | `/api/v1/associados/{id}/aprovar·rejeitar·suspender·reativar·inativar` | `associados.aprovar` | Situação (`{ motivo }` quando exigido) |
| DELETE | `/api/v1/associados/{id}` | `associados.excluir` | Exclusão lógica (sem dependentes vigentes) |
| GET | `/api/v1/associados/{id}/historico` | `associados.visualizar` | Histórico cadastral |
| GET | `/api/v1/associados/{id}/carteirinha` | administração ou o próprio | Carteirinha digital |
| POST | `/api/v1/associados/{id}/acesso-app` | `usuarios.gerenciar` | Login do associado no app |
| GET/POST/PUT | `/api/v1/dependentes[/{id}]` | `dependentes.*` | Lista (`titularId`, `situacao`, `parentesco`), inclusão, edição |
| POST | `/api/v1/dependentes/{id}/aprovar·rejeitar·suspender·reativar·inativar` | `dependentes.aprovar` | Situação |
| POST | `/api/v1/dependentes/{id}/transferir` | `dependentes.aprovar` | Transferência de titularidade |
| PUT | `/api/v1/dependentes/{id}/permissoes` | `dependentes.editar` | Permissões individuais |
| DELETE | `/api/v1/dependentes/{id}` | `dependentes.excluir` | Exclusão lógica |
| GET | `/api/v1/dependentes/{id}/historico` · `/carteirinha` | ver / admin ou titular | Histórico e carteirinha |
| POST | `/api/v1/dependentes/{id}/acesso-app` | `usuarios.gerenciar` | Login próprio (idade mínima) |
| GET/PUT | `/api/v1/dependentes/configuracoes` | ver / `clubes.configurar` | Regras de dependentes do clube |
| GET/PUT | `/api/v1/pessoas/{id}/foto` | admin ou próprio/titular | Foto (multipart `arquivo`) |
| GET/POST | `/api/v1/pessoas/{id}/documentos` | `documentos.*` ou próprio/titular | Documentos (multipart `arquivo`, `tipo`, `descricao`) |
| GET | `/api/v1/pessoas/{id}/qrcode` | admin ou próprio/titular | PNG do QR Code |
| POST | `/api/v1/pessoas/{id}/carteirinha/regenerar` | editar ou próprio/titular | Novo código (invalida o anterior) |
| GET | `/api/v1/documentos/{id}/conteudo` | `documentos.visualizar` ou próprio/titular | Download |
| POST | `/api/v1/documentos/{id}/analisar` | `documentos.gerenciar` | Aprovar/rejeitar documento |
| DELETE | `/api/v1/documentos/{id}` | `documentos.gerenciar` ou titular (pendente) | Remover |
| POST | `/api/v1/controle-acesso/validar` | `acesso.validar` | Consulta sem registrar |
| POST | `/api/v1/controle-acesso/registrar` | `acesso.validar` | Entrada/Saída (`{ codigo, tipo }`) |
| GET | `/api/v1/controle-acesso` | `acesso.validar`/`relatorios.visualizar` | Histórico de acessos |
| GET | `/api/v1/controle-acesso/presentes` | `acesso.validar` | Pessoas dentro do clube |
| GET | `/api/v1/portal/perfil` · PUT `/portal/perfil/contato` | `portal.acessar` | Perfil e contato |
| GET | `/api/v1/portal/carteirinhas` | `portal.acessar` | Carteirinhas próprias e dos dependentes |
| GET/POST/PUT | `/api/v1/portal/dependentes[/{id}]` | titular | Dependentes do titular (edição só pendente) |
| POST | `/api/v1/portal/dependentes/{id}/acesso-app` | titular | Autoriza login do dependente |

## Etapa 3 — financeiro

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET/PUT | `/api/v1/financeiro/configuracoes` | ver / `financeiro.gerenciar` | Multa %, juros % ao mês, dias de tolerância |
| GET/POST/PUT/DELETE | `/api/v1/financeiro/planos[/{id}]` | ver / gerenciar | Planos por categoria (valor, dia de vencimento) |
| POST | `/api/v1/financeiro/mensalidades/gerar` | `financeiro.gerenciar` | `{ competencia: "AAAA-MM" }` — idempotente |
| GET | `/api/v1/financeiro/mensalidades` | `financeiro.visualizar` | `competencia`, `situacao` (Aberta/Vencida/Paga/Isenta/Cancelada), `associadoId`, `busca` |
| POST | `/api/v1/financeiro/mensalidades/{id}/pagamento` | `financeiro.registrar_pagamentos` | `{ forma, valor?, data?, isentarEncargos? }` |
| POST | `/api/v1/financeiro/mensalidades/{id}/isentar` · `/cancelar` | `financeiro.gerenciar` | `{ motivo }` |
| POST | `/api/v1/financeiro/pagamentos/{id}/estornar` | `financeiro.gerenciar` | `{ motivo }` |
| GET | `/api/v1/financeiro/pagamentos/{id}/recibo` | tesouraria ou o próprio associado | Recibo |
| GET | `/api/v1/financeiro/associados/{id}` | `financeiro.visualizar` | Situação financeira do associado |
| GET | `/api/v1/financeiro/categorias` | ver | Categorias de receita/despesa |
| GET/POST/PUT/DELETE | `/api/v1/financeiro/lancamentos[/{id}]` | ver / gerenciar | Receitas e despesas (`tipo`, `pago`, `de`, `ate`) |
| POST | `/api/v1/financeiro/lancamentos/{id}/pagar` | gerenciar | Marca como pago/recebido |
| GET | `/api/v1/financeiro/resumo` · `/fluxo-caixa` | ver | `?de=&ate=` (padrão: mês atual) |
| GET | `/api/v1/financeiro/inadimplentes` | ver | Associados com mensalidades vencidas |
| GET | `/api/v1/portal/financeiro` | titular (app) | Próprias mensalidades e situação |

## Etapa 4 — eventos, espaços, reservas e solicitações

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/v1/eventos[/{id}]` | `eventos.visualizar` | Lista (`busca`, `situacao`, `de`, `ate`) e detalhe com inscrições |
| POST/PUT | `/api/v1/eventos[/{id}]` | `eventos.gerenciar` | Nome, período, público (Geral/SomenteAssociados/SomenteTitulares), capacidade, valor, acompanhantes, `espacoId?` (ocupa o espaço com reserva aprovada) |
| POST | `/api/v1/eventos/{id}/encerrar-inscricoes` · `/reabrir-inscricoes` · `/cancelar` | `eventos.gerenciar` | Cancelar também libera o espaço |
| POST | `/api/v1/eventos/{id}/inscricoes` | `eventos.gerenciar` | Inscreve pessoa do clube (`{ pessoaId, acompanhantes }`); lotado → lista de espera |
| DELETE | `/api/v1/eventos/inscricoes/{id}` | `eventos.gerenciar` ou o próprio inscrito | Cancela e promove o próximo da lista de espera |
| POST | `/api/v1/eventos/{id}/presenca` | `eventos.gerenciar` ou `acesso.validar` | `{ codigo }` do QR da carteirinha |
| GET | `/api/v1/espacos` | autenticado | Espaços ativos (`incluirInativos=true` para a administração) |
| POST/PUT/DELETE | `/api/v1/espacos[/{id}]` | `espacos.gerenciar` | Tipo, capacidade, locação, taxa, se exige aprovação |
| GET | `/api/v1/reservas` | `reservas.visualizar` | `de`, `ate`, `espacoId`, `situacao` |
| GET | `/api/v1/reservas/calendario` | autenticado | `?de=&ate=&espacoId=` — reservas e eventos; no app aparece só "Ocupado" |
| POST/PUT | `/api/v1/reservas[/{id}]` | `reservas.aprovar` | Reserva da administração (já aprovada) |
| POST | `/api/v1/reservas/{id}/aprovar` · `/recusar` | `reservas.aprovar` | Recusar exige `{ motivo }` |
| POST | `/api/v1/reservas/{id}/cancelar` | `reservas.aprovar` ou o solicitante | `{ motivo }` |
| GET/POST | `/api/v1/solicitacoes[/{id}]` | `solicitacoes.visualizar` / `solicitacoes.atender` | Lista (`somenteAbertas`, `situacao`, `busca`), detalhe com anotações internas, abertura pela secretaria |
| POST | `/api/v1/solicitacoes/{id}/respostas` | `solicitacoes.atender` | `{ texto, interna }` — resposta pública passa para "Em andamento" |
| PUT | `/api/v1/solicitacoes/{id}/situacao` | `solicitacoes.atender` | `{ situacao }` Concluida/Cancelada/EmAndamento |
| GET | `/api/v1/portal/eventos` · POST `/portal/eventos/{id}/inscricoes` | app | Eventos abertos ao público do usuário; inscrição própria (`acompanhantes`) |
| GET/POST | `/api/v1/portal/reservas` | app | Minhas reservas; pedido (titular ativo ou dependente com permissão de reservar) |
| GET/POST | `/api/v1/portal/solicitacoes[/{id}]` · POST `/{id}/respostas` · `/{id}/cancelar` | app | Próprias solicitações, sem anotações internas |

Conflito de horário (inclusive o barrado pela restrição do banco) → **409** `conflito_horario` com o período ocupado.

## Etapa 7 — comunicação, funcionários, patrimônio, estoque e assembleias

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/v1/comunicados[/{id}]` | `comunicacao.visualizar` | Lista com leituras; detalhe com quem leu/confirmou e pendentes |
| POST/PUT | `/api/v1/comunicados[/{id}]` | `comunicacao.gerenciar` | `{ titulo, texto, publico (Todos/Titulares), exigeConfirmacao, fixado, expiraEm?, publicar }` |
| POST · DELETE | `/api/v1/comunicados/{id}/publicar` · `/{id}` | `comunicacao.gerenciar` | Publica (notifica) · exclui |
| GET | `/api/v1/notificacoes` | autenticado | `{ naoLidas, itens }` do usuário no clube atual |
| POST | `/api/v1/notificacoes/{id}/lida` · `/lidas` | autenticado | Marca uma / todas como lidas |
| GET/POST/PUT/DELETE | `/api/v1/funcionarios[/{id}]` | `funcionarios.gerenciar` | Lista (`situacao`, `departamento`, `busca`), cadastro, edição, exclusão |
| POST · DELETE | `/api/v1/funcionarios/{id}/ausencias` · `/ausencias/{ausenciaId}` | `funcionarios.gerenciar` | Férias/afastamento `{ tipo, inicio, fim }` · cancela |
| POST | `/api/v1/funcionarios/{id}/desligar` · `/readmitir` | `funcionarios.gerenciar` | `{ data, motivo? }` |
| GET | `/api/v1/patrimonio` · `/resumo` · `/{id}` | `patrimonio.gerenciar` | Bens (`situacao`, `localizacao`, `busca`), totais, detalhe com manutenções e histórico |
| POST/PUT | `/api/v1/patrimonio[/{id}]` | `patrimonio.gerenciar` | Código PAT-NNNNNN automático |
| POST | `/api/v1/patrimonio/{id}/movimentar` · `/manutencoes` · `/manutencoes/{mid}/concluir` · `/baixar` | `patrimonio.gerenciar` | Local/estado · abre/conclui manutenção · baixa com motivo |
| GET/POST/PUT | `/api/v1/estoque/produtos[/{id}]` | `estoque.gerenciar` | `abaixoDoMinimo`, `busca`; `quantidadeInicial` na criação |
| POST | `/api/v1/estoque/produtos/{id}/movimentacoes` | `estoque.gerenciar` | `{ tipo (Entrada/Saida/Ajuste), quantidade, motivo }` — saída maior que o saldo → 422 |
| GET | `/api/v1/estoque/movimentacoes` | `estoque.gerenciar` | `produtoId`, `de`, `ate` |
| GET/POST | `/api/v1/estoque/compras` | `estoque.gerenciar` | Pedidos de compra |
| POST | `/api/v1/estoque/compras/abaixo-do-minimo` · `/{id}/atender` · `/{id}/cancelar` | `estoque.gerenciar` | Gera pedidos · recebe (entrada no estoque) · cancela |
| GET | `/api/v1/assembleias[/{id}]` | `assembleias.visualizar` | Detalhe com pautas, apuração (após encerrar a votação), presenças e rascunho da ata |
| POST/PUT | `/api/v1/assembleias[/{id}]` | `assembleias.gerenciar` | `{ titulo, tipo, dataHora, local, convocacao, exigeAdimplencia }` |
| POST · DELETE | `/api/v1/assembleias/{id}/pautas` · `/pautas/{pautaId}` | `assembleias.gerenciar` | `{ titulo, descricao, comVotacao, opcoes? }` (padrão Sim/Não/Abstenção) |
| POST | `/api/v1/assembleias/{id}/convocar` · `/instalar` · `/encerrar` · `/cancelar` | `assembleias.gerenciar` | Ciclo de vida (convocar e cancelar notificam) |
| POST | `/api/v1/assembleias/{id}/presencas` | `assembleias.gerenciar` | `{ codigo }` (QR) ou `{ associadoId }`; responde se está apto |
| POST | `/api/v1/assembleias/{id}/pautas/{pautaId}/abrir-votacao` · `/encerrar-votacao` · `/decisao` | `assembleias.gerenciar` | Votação e decisão `{ texto }` |
| POST | `/api/v1/assembleias/{id}/ata` | `assembleias.gerenciar` | `{ texto }` — uma vez, depois do encerramento |
| GET · POST | `/api/v1/portal/comunicados` · `/{id}/leitura?confirmar=` | app | Mural e leitura/confirmação |
| GET · POST | `/api/v1/portal/assembleias` · `/{id}/pautas/{pautaId}/voto` | app (titular) | Assembleias e voto `{ opcao }` (índice da opção) |

## Pedidos do cliente (DT-032)

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET · POST | `/api/v1/associados/{id}/contratos` | `associados.visualizar` · `associados.cadastrar` | Lista (com parcelas, se puder ver o financeiro) · cria `{ valorMensal?, diaVencimento?, inicio? }` em análise |
| POST | `/api/v1/contratos/{id}/aprovar` | `associados.aprovar` | Aprova e gera as 12 mensalidades |
| POST | `/api/v1/contratos/{id}/reprovar` · `/desligar` | `associados.aprovar` | `{ motivo }` — desligar cancela parcelas futuras e inativa o associado |
| POST | `/api/v1/contratos/{id}/renovar` | `associados.cadastrar` | Contrato do ano seguinte, em análise |
| POST | `/api/v1/dependentes/{id}/tornar-associado` | `associados.cadastrar` | `{ categoriaId }` — dependente maior de idade vira titular |
| GET | `/api/v1/associados?sexo=Feminino` | `associados.visualizar` | Filtro por gênero (`Masculino`, `Feminino`, `Outro`, `NaoInformado`) |
| GET | `/api/v1/espacos/disponiveis?inicio&fim&pessoas&eventoId` | `reservas.visualizar` | Espaços com `livre`, `comporta` e o que ocupa o período |
| POST · DELETE | `/api/v1/eventos/{id}/locais` · `/locais/{espacoId}` | `eventos.gerenciar` | Adiciona `{ espacoId }` / remove um local do evento |
| POST | `/api/v1/eventos/{id}/divulgar` | `eventos.gerenciar` | Aviso no app + e-mails; devolve contagens, texto e link do WhatsApp |
| GET · POST | `/api/v1/diretoria` | `clubes.visualizar` · `clubes.editar` | Diretoria atual/anterior e aviso · adiciona `{ nome, cargo, email?, telefone?, inicioMandato, fimMandato? }` |
| POST | `/api/v1/diretoria/{id}/encerrar` | `clubes.editar` | Encerra o mandato hoje |

| POST | `/api/v1/portal/assembleias/{id}/participar` | app (titular) | Presença pelo app, quando a assembleia permite (`participacaoPeloApp`) |

Carteirinha: `qrCodeConteudo` é um link `…/portaria?codigo=…` e `codigoAcesso` (ex.: `7KQ2-M9XA`) pode ser digitado em
`controle-acesso/validar`, `registrar`, presença em eventos e assembleias. Assembleias aceitam `participacaoPeloApp`.

Espaços aceitam `limiteReservasPorMes` (por família, por tipo de espaço; erro `reserva.limite_tipo`). Pessoas aceitam `profissao`.

## Etapa 8 — LGPD

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/v1/portal/meus-dados` | app | Arquivo JSON com todos os dados da pessoa logada |
| GET | `/api/v1/pessoas/{id}/dados-pessoais` | quem pode ver a pessoa | Mesmo arquivo, para atender pedido do titular |
| POST | `/api/v1/pessoas/{id}/anonimizar` | `associados.excluir` ou `dependentes.excluir` | `{ motivo }` — só sem vínculo vigente; irreversível |

Cabeçalhos em todas as respostas: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy`;
`Cache-Control: no-store` em `/api`. Excesso de requisições → **429**. Violação de regra do banco (ex.: estoque negativo) → **422** `regra_violada`.

Paginação: `?pagina=1&tamanhoPagina=20&busca=&ordenarPor=nome&direcao=asc` →
`{ itens, paginaAtual, tamanhoPagina, totalItens, totalPaginas }`.
