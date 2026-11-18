# Segurança e LGPD

## Medidas de segurança implementadas

| Tema | Implementação |
|---|---|
| Senhas | Hash PBKDF2 (ASP.NET Identity `PasswordHasher`); mínimo de 8 caracteres com letras e números; senhas temporárias aleatórias com troca obrigatória no primeiro acesso |
| Tokens | JWT de acesso (15 min) assinado com HMAC-SHA256; token de renovação opaco, armazenado como hash SHA-256, com rotação a cada uso e detecção de reuso |
| Sessões | Lista e encerramento de sessões pelo próprio usuário e pela administração; troca de senha, bloqueio, cancelamento e desligamento encerram as sessões |
| Força bruta | Bloqueio temporário após tentativas inválidas (configurável em `Auth`), limite de requisições por IP (geral e mais restrito nos endpoints de autenticação), histórico de acessos |
| Autorização | Validada sempre no servidor: permissão por endpoint, restrição por tipo de usuário e escopo "somente os próprios dados" para associados e dependentes |
| Isolamento entre clubes | Clube vindo do token, filtro global, guarda de gravação e chaves estrangeiras compostas no banco (ver `arquitetura.md`) |
| Injeção de SQL | Acesso por EF Core (consultas parametrizadas); o SQL manual usa interpolação parametrizada; curingas de `ILIKE` escapados |
| XSS | A API devolve apenas JSON; o Web (React) escapa todo conteúdo e não usa HTML bruto; CSP restritiva no nginx; `X-Content-Type-Options: nosniff` |
| CSRF | Não se aplica: a autenticação é por cabeçalho `Authorization` (sem cookies) |
| CORS | Somente as origens configuradas em `Cors:Origins` |
| Arquivos | Tipos permitidos (JPG, PNG, WEBP, PDF) e limite de 10 MB; nome físico gerado pelo servidor; armazenados fora da raiz pública; download somente autenticado, com verificação de clube, escopo e titularidade; resposta 404 para quem não pode ver |
| QR Code | Token aleatório de uso único, validade curta, somente o hash é armazenado, consumo atômico no servidor |
| Pagamentos | Nenhum dado de cartão trafega ou é armazenado no sistema; confirmação somente por notificação assinada do gateway; idempotência |
| Auditoria | Registro automático de toda criação/alteração/exclusão, imutável no banco |
| Segredos | Apenas em variáveis de ambiente (`.env` fora do repositório); a API não inicia sem `Jwt:Key` de 32+ caracteres |
| Erros | Tratamento global: respostas padronizadas, sem detalhes internos; erros inesperados retornam apenas um identificador de rastreamento |
| Credenciais no Mobile | Tokens somente em `SecureStorage` (Keystore/Keychain) |
| Credenciais no Web | Token de acesso em memória; token de renovação em `sessionStorage` (descartado ao fechar a aba) |
| Contêiner | A API roda como usuário sem privilégios; o banco é publicado apenas em `127.0.0.1` |

Recomendações de implantação: HTTPS obrigatório no proxy reverso (com HSTS), firewall liberando somente 80/443, backups copiados
para fora do servidor e rotação periódica da senha do banco.

## LGPD — tratamento de dados pessoais

### Dados tratados e finalidade

| Dado | Titular | Finalidade |
|---|---|---|
| Nome, CPF, RG, nascimento, gênero, estado civil, contato, endereço, fotografia | Associado | Cadastro, identificação na portaria, cobrança |
| Nome, CPF/RG (quando aplicável), nascimento, parentesco, fotografia, contato | Dependente | Vínculo com o associado, identificação, acesso |
| Nome, documento, telefone | Convidado | Controle de acesso e segurança |
| Nome, CPF, contato, cargo | Funcionário | Gestão da equipe e controle de acesso ao sistema |
| Cobranças e pagamentos | Associado | Gestão financeira e obrigações contábeis |
| Registros de entrada e saída | Todos | Segurança e controle de ocupação |

### Princípios aplicados

- **Necessidade / minimização**: listas exibem CPF mascarado; a portaria recebe apenas nome, foto, situação e documento mascarado;
  dependentes não veem dados financeiros; observações internas não são devolvidas ao associado; campos opcionais são realmente opcionais.
- **Controle de acesso**: documentos pessoais exigem a permissão `documents.view`; cada perfil acessa somente o necessário à função
  (ex.: portaria não acessa financeiro nem cadastro completo).
- **Transparência e consentimento**: o texto da política de privacidade é configurado pelo clube; cada usuário precisa aceitá-lo no
  primeiro acesso e novamente a cada nova versão. Data e versão aceitas ficam registradas por usuário.
- **Acesso do titular**: `GET /api/associados/eu/meus-dados` devolve ao associado a cópia dos dados mantidos sobre ele
  (cadastro, dependentes, convidados, cobranças, pagamentos, reservas, acessos e consentimento).
- **Correção**: o associado atualiza contato, endereço e foto pelo aplicativo; demais dados pela secretaria.
- **Eliminação / anonimização**: `POST /api/associados/{id}/anonimizar` (permissão `members.approve`, com justificativa auditada)
  remove os dados pessoais do associado, de seus dependentes e convidados e encerra os logins. Exige associação cancelada e sem
  cobranças em aberto. O histórico financeiro e de acessos é preservado **sem identificação**, por obrigação legal e contábil.
- **Rastreabilidade**: toda alteração de dados pessoais fica na auditoria (quem, quando, valores anteriores e novos).
- **Segurança**: medidas da tabela acima.

### Política de retenção (a definir pelo clube)

O sistema oferece os mecanismos (exclusão lógica, anonimização, limpeza automática de tokens e sessões expiradas). Os **prazos** são
decisão do controlador (o clube) e devem constar na política de privacidade. Sugestão de ponto de partida para deliberação:

| Dado | Retenção sugerida |
|---|---|
| Cadastro de associado cancelado | Anonimizar após o prazo definido em assembleia/estatuto, respeitando obrigações legais |
| Registros financeiros | Prazo legal contábil/fiscal aplicável |
| Registros de acesso (portaria) | Prazo definido pelo clube para fins de segurança |
| Convidados | Anonimizados junto com o associado responsável ou conforme prazo do clube |

Enquanto os prazos não forem definidos, nada é apagado automaticamente (apenas tokens de QR não utilizados e sessões expiradas).

### Pendências que dependem do clube

- Publicar o texto da política de privacidade (Configurações → LGPD).
- Indicar o encarregado (DPO) e o canal de atendimento ao titular no texto da política.
- Definir os prazos de retenção.
