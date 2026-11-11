# Colocando o Projeto Clubes em produção

Roteiro para subir **PostgreSQL + API + Web administrativo** num servidor Linux com Docker, e publicar o app MAUI.
O `docker-compose.yml` e os Dockerfiles estão na raiz e em `src/*/Dockerfile`.

> Situação: os Dockerfiles usam exatamente o `dotnet publish -c Release` que foi testado na máquina de desenvolvimento,
> mas **os containers ainda não foram executados** (Docker não está instalado nesta máquina). Faça o primeiro `docker
> compose up` num ambiente de homologação antes da produção.

## 1. Servidor
- Linux com Docker e Docker Compose; 2 vCPU / 4 GB RAM atendem vários clubes.
- Um domínio com dois nomes, por exemplo `admin.seuclube.com.br` (Web) e `api.seuclube.com.br` (API do app).
- Um **proxy reverso com HTTPS** na frente (Caddy é o mais simples: certificado automático). Exemplo de `Caddyfile`:

```
admin.seuclube.com.br {
    reverse_proxy localhost:5278
}
api.seuclube.com.br {
    reverse_proxy localhost:5080
}
```

O Web usa conexão permanente (SignalR/WebSocket); Caddy e Nginx recentes já repassam WebSocket.

## 2. Configuração (`.env`)
```bash
cp .env.example .env
```
Preencha, no mínimo:

| Variável | O que é |
|---|---|
| `POSTGRES_PASSWORD` | Senha forte do banco (`openssl rand -base64 24`) |
| `JWT_CHAVE` | Chave de assinatura dos tokens, 32+ caracteres (`openssl rand -base64 48`) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `WEB_URL` | Endereço público do Web (`https://admin.seuclube.com.br`) — usado nos links de e-mail |
| `EMAIL_PROVEDOR=Smtp` + `SMTP_*` | Envio real de e-mail (recuperação de senha e convites). Sem isso os e-mails ficam só em arquivo. |
| `SEED_DEMO` | **false**. A API se recusa a iniciar em `Production` com dados de demonstração. |
| `SWAGGER_HABILITADO` | `false` (ou proteja `/swagger` no proxy) |

O `.env` nunca vai para o repositório. Segredos não ficam em `appsettings.json`.

## 3. Banco e migrations
Opção simples (recomendada no primeiro deploy): `APLICAR_MIGRATIONS=true` — a API aplica as migrations ao iniciar.

Opção controlada: `APLICAR_MIGRATIONS=false` e aplique os scripts idempotentes em ordem:
```bash
for f in database/migrations/*.sql; do docker exec -i projetoclubes-postgres psql -U projetoclubes -d projetoclubes < "$f"; done
```

## 4. Subir
```bash
docker compose up -d --build
docker compose logs api | grep "token de inicialização"
```
Use o token uma única vez para criar o Superadmin (README → "Primeiro acesso"). Depois, pelo Web: cadastre os clubes,
convide os presidentes e configure perfis, categorias, planos e regras de cada clube.

## 5. Segurança — conferir antes de abrir
- [ ] HTTPS no proxy; HTTP redirecionando para HTTPS.
- [ ] PostgreSQL só em `127.0.0.1` (o compose já publica assim) e senha forte.
- [ ] `SEED_DEMO=false`, `SWAGGER_HABILITADO=false`.
- [ ] SMTP configurado e testado (peça "Esqueci minha senha" com um e-mail seu).
- [ ] O proxy repassa `X-Forwarded-For`. A API só confia nesse cabeçalho vindo da rede do Docker (`172.28.0.0/16`,
      `Proxy__RedesConfiaveis`); se o proxy rodar fora do Docker, inclua o endereço dele.
- [ ] Firewall: só 80/443 abertos para a internet.

O que já vem ligado: cabeçalhos de segurança (nosniff, frame-deny, CSP na API, no-store), limite de tentativas de login
por IP, limite geral de requisições por usuário, bloqueio de conta após falhas, sessões revogáveis, auditoria, isolamento
por clube em várias camadas e anonimização LGPD (ver `docs/DECISOES_TECNICAS.md`, DT-029).

## 6. Backup
O que precisa de backup: o banco e o volume `api-dados` (documentos, fotos, e-mails em arquivo).
```bash
# diário, com retenção — agende no cron
docker exec projetoclubes-postgres pg_dump -U projetoclubes -Fc projetoclubes > backup-$(date +%F).dump
docker run --rm -v projetoclubes_api-dados:/dados -v "$PWD":/destino alpine tar czf /destino/arquivos-$(date +%F).tgz -C /dados .
```
Restauração: `pg_restore -U projetoclubes -d projetoclubes --clean backup-AAAA-MM-DD.dump`. Teste a restauração uma vez
por mês num banco separado.

## 7. Monitoramento
- `GET https://api.seuclube.com.br/health` responde 200 com o PostgreSQL acessível (use num monitor externo).
- Logs: `docker compose logs -f api` (JSON compacto via Serilog; não registram senhas nem tokens).

## 8. Atualizar versão
```bash
git pull && docker compose up -d --build
```
Com `APLICAR_MIGRATIONS=false`, aplique antes o novo script de `database/migrations/`. Faça backup antes de cada atualização.

## 9. Aplicativo (.NET MAUI)
1. Em `src/Mobile/ProjetoClubes.Mobile/Configuracao.cs`, troque `UrlApiProducao` pelo endereço HTTPS da API.
2. Android: `dotnet publish src/Mobile/ProjetoClubes.Mobile -f net10.0-android -c Release` com a chave de assinatura
   da loja (`AndroidSigningKeyStore` etc. via variáveis de ambiente, nunca no repositório). Gera o `.aab` para a Play Store.
3. iOS: precisa de um Mac com Xcode e conta Apple Developer (`-f net10.0-ios -c Release`).
4. Push (Firebase/APNs) ainda não está ligado: as notificações aparecem na caixa "Avisos" do app (DT-028).
