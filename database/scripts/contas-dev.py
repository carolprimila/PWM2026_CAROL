"""Prepara o ambiente de DESENVOLVIMENTO com as duas sedes (Guaíra e Foz) e as contas de teste de cada perfil.

Uso (na raiz do projeto, com a API de desenvolvimento em http://localhost:5080):
    python database/scripts/contas-dev.py

Cada sede é um clube isolado na plataforma: a equipe de uma sede (inclusive o presidente) só acessa a própria sede;
o superadministrador alterna entre as sedes e enxerga tudo de cada uma.

Arquivos gerados (contêm a senha de teste e NÃO são versionados):
    frontend/clubes-web/.env.local                      -> botões de entrada rápida do Web (apenas em `npm run dev`)
    mobile/Clubes.Mobile/Services/DevLogins.local.cs    -> botões de entrada rápida do aplicativo (apenas em build Debug)
"""
import json, os, re
from devlib import LOCAL_ENV, SEDES, call, cpf, dev_password, login, read_env

env_text, env = read_env()
password = dev_password()


def finish_first_access(email, current):
    """Conclui o primeiro acesso (troca da senha temporária e aceite da política) e devolve a sessão."""
    auth = login(email, current)
    if auth is None: raise SystemExit(f'Não foi possível entrar como {email}.')
    if auth['user']['mustChangePassword'] or current != password:
        call('POST', '/api/auth/alterar-senha', {'currentPassword': current, 'newPassword': password}, auth['accessToken'])
        auth = login(email, password)
    if not auth['user']['privacyAccepted']:
        call('POST', '/api/auth/aceitar-privacidade', None, auth['accessToken'])
    return auth


def bootstrap_account(email_key, password_key):
    """Contas criadas na inicialização (.env): entra com a senha de teste ou com a do .env e a padroniza."""
    global env_text
    auth = login(env[email_key], password) or finish_first_access(env[email_key], env[password_key])
    if not auth['user']['privacyAccepted']: call('POST', '/api/auth/aceitar-privacidade', None, auth['accessToken'])
    env_text = re.sub(rf'(?m)^{password_key}=.*$', f'{password_key}={password}', env_text)
    return auth


guaira_admin = bootstrap_account('CLUB_ADMIN_EMAIL', 'CLUB_ADMIN_PASSWORD')
superadmin = bootstrap_account('SUPERADMIN_EMAIL', 'SUPERADMIN_PASSWORD')
open('.env', 'w', encoding='utf-8').write(env_text)
super_token = superadmin['accessToken']

# ---------- Sedes ----------
clubs = call('GET', '/api/clubes', token=super_token)[1]
first = next(c for c in clubs if c['id'] == guaira_admin['user']['clubId'])
if first['name'] != 'Daros Guaíra':
    body = {k: first.get(k) for k in ('legalName', 'cnpj', 'email', 'phone', 'zipCode', 'street', 'number', 'complement', 'district', 'logoFileId', 'about', 'timeZoneId')}
    call('PUT', f"/api/clubes/{first['id']}", {**body, 'name': 'Daros Guaíra', 'city': 'Guaíra', 'state': 'PR'}, super_token)

FOZ_ADMIN = 'admin.foz@daros.local'
foz_admin = login(FOZ_ADMIN, password)
if foz_admin is None:
    foz = next((c for c in clubs if c['name'] == 'Daros Foz'), None)
    if foz:
        # A sede já existe: o superadministrador, operando nela, redefine a senha do administrador de teste.
        user = next(u for u in call('GET', f'/api/usuarios?search={FOZ_ADMIN}', token=super_token, club=foz['id'])[1]['items'] if u['email'] == FOZ_ADMIN)
        temp = call('POST', f"/api/usuarios/{user['id']}/redefinir-senha", None, super_token, club=foz['id'])[1]['temporaryPassword']
    else:
        temp = call('POST', '/api/clubes', {'club': {'name': 'Daros Foz', 'city': 'Foz do Iguaçu', 'state': 'PR'}, 'adminName': 'Administrador Foz (teste)',
                                            'adminEmail': FOZ_ADMIN, 'applyStandardStructure': True}, super_token)[1]['admin']['temporaryPassword']
    foz_admin = finish_first_access(FOZ_ADMIN, temp)
admins = {'guaira': guaira_admin, 'foz': foz_admin}


def ensure_login(token, email, create):
    """Garante que a conta exista na sede e esteja com a senha de teste. `create` devolve o TemporaryPasswordDto."""
    if login(email, password): return finish_first_access(email, password)
    items = call('GET', f'/api/usuarios?search={email}&pageSize=5', token=token)[1]['items']
    user = next((u for u in items if u['email'] == email), None)
    temp = call('POST', f"/api/usuarios/{user['id']}/redefinir-senha", None, token)[1] if user else create()
    return finish_first_access(email, temp['temporaryPassword'])


def setup_staff(token, staff):
    catalog = [p['code'] for p in call('GET', '/api/funcoes/permissoes', token=token)[1]]
    roles = {r['name']: r for r in call('GET', '/api/funcoes', token=token)[1]}
    if 'Presidência' not in roles:
        # Presidência: acesso completo à própria sede, como função configurável (sem alterar código).
        roles['Presidência'] = call('POST', '/api/funcoes', {'name': 'Presidência', 'description': 'Diretoria da sede: acesso completo', 'permissions': catalog}, token)[1]
    for _, email, name, role in staff:
        ensure_login(token, email, lambda: call('POST', '/api/usuarios', {'name': name, 'email': email, 'kind': 'Staff', 'roleIds': [roles[role]['id']]}, token)[1])


staff_guaira = [('Presidente · Guaíra', 'presidente@daros.local', 'Paulo Presidente (teste)', 'Presidência'),
                ('Tesouraria · Guaíra', 'financeiro@daros.local', 'Tereza Tesoureira (teste)', 'Financeiro'),
                ('Secretaria · Guaíra', 'secretaria@daros.local', 'Sara Secretaria (teste)', 'Secretaria'),
                ('Portaria · Guaíra', 'portaria@daros.local', 'Pedro Portaria (teste)', 'Portaria')]
staff_foz = [('Presidente · Foz', 'presidente.foz@daros.local', 'Fábio Presidente (teste)', 'Presidência'),
             ('Tesouraria · Foz', 'financeiro.foz@daros.local', 'Flávia Tesoureira (teste)', 'Financeiro'),
             ('Portaria · Foz', 'portaria.foz@daros.local', 'Felipe Portaria (teste)', 'Portaria')]
setup_staff(admins['guaira']['accessToken'], staff_guaira)
setup_staff(admins['foz']['accessToken'], staff_foz)

# ---------- Associado e dependente de teste (aplicativo) — sede Guaíra ----------
token = admins['guaira']['accessToken']
MEMBER_EMAIL, DEPENDENT_EMAIL = 'associado@daros.local', 'dependente@daros.local'


def create_member():
    found = call('GET', '/api/associados?search=Ana%20Associada&pageSize=5', token=token)[1]['items']
    member = found[0] if found else call('POST', '/api/associados', {'fullName': 'Ana Associada (teste)', 'cpf': cpf(417263980), 'email': MEMBER_EMAIL, 'phone': '(44) 90000-0001',
                                                                     'city': 'Guaíra', 'state': 'PR'}, token)[1]
    if member['status'] == 'PendingApproval': call('POST', f"/api/associados/{member['id']}/aprovar", None, token)
    return call('POST', f"/api/associados/{member['id']}/acesso", {'email': MEMBER_EMAIL}, token)[1]


member_auth = ensure_login(token, MEMBER_EMAIL, create_member)
member_id = member_auth['user']['memberId']


def create_dependent():
    found = [d for d in call('GET', f'/api/dependentes?memberId={member_id}&pageSize=10', token=token)[1]['items'] if d['status'] == 'Active']
    dependent = found[0] if found else call('POST', f'/api/dependentes?memberId={member_id}', {'fullName': 'Davi Dependente (teste)', 'relationship': 'Filho(a)', 'birthDate': '2010-03-15', 'email': DEPENDENT_EMAIL}, token)[1]
    return call('POST', f"/api/dependentes/{dependent['id']}/acesso", {'email': DEPENDENT_EMAIL}, token)[1]


dependent_auth = ensure_login(token, DEPENDENT_EMAIL, create_dependent)
# Permissões do dependente: liberadas pela administração e habilitadas pelo associado (as duas etapas reais).
for code in ('reservations.request', 'events.register', 'guests.register'):
    call('PUT', f"/api/dependentes/{dependent_auth['user']['dependentId']}/permissoes", {'code': code, 'value': True}, token)
    call('PUT', f"/api/dependentes/{dependent_auth['user']['dependentId']}/permissoes", {'code': code, 'value': True}, member_auth['accessToken'], ok=(200, 403))

# ---------- Arquivos locais de entrada rápida ----------
web = [{'label': 'Superadmin · todas as sedes', 'email': env['SUPERADMIN_EMAIL']},
       {'label': 'Administrador · Guaíra', 'email': env['CLUB_ADMIN_EMAIL']}, {'label': 'Administrador · Foz', 'email': FOZ_ADMIN}] \
    + [{'label': l, 'email': e} for l, e, _, _ in staff_guaira + staff_foz]
mobile = [('Associado', MEMBER_EMAIL), ('Dependente', DEPENDENT_EMAIL), ('Portaria', 'portaria@daros.local')]

existing = open(LOCAL_ENV, encoding='utf-8').read() if os.path.exists(LOCAL_ENV) else ''
kept = [l for l in existing.splitlines() if l.strip() and not l.startswith(('DEV_LOGIN_PASSWORD=', 'VITE_DEV_LOGINS=', '# Contas de teste'))]
with open(LOCAL_ENV, 'w', encoding='utf-8') as f:
    f.write('\n'.join(kept + [
        '# Contas de teste geradas por database/scripts/contas-dev.py — somente desenvolvimento; arquivo não versionado.',
        f'DEV_LOGIN_PASSWORD={password}',
        'VITE_DEV_LOGINS=' + json.dumps([{**w, 'password': password} for w in web], ensure_ascii=False),
    ]) + '\n')

with open('mobile/Clubes.Mobile/Services/DevLogins.local.cs', 'w', encoding='utf-8') as f:
    entries = '\n'.join(f'        All.Add(new DevLogin("{label}", "{email}", "{password}"));' for label, email in mobile)
    f.write(f'''// Gerado por database/scripts/contas-dev.py — contas de TESTE do ambiente de desenvolvimento. Arquivo não versionado.
#if DEBUG
namespace Clubes.Mobile.Services;

public static partial class DevLogins
{{
    static partial void Fill()
    {{
{entries}
    }}
}}
#endif
''')

print('Sedes e contas de teste prontas (senha única nos arquivos locais não versionados):')
for w in web: print('  Web    ·', w['label'].ljust(30), w['email'])
for label, email in mobile: print('  Mobile ·', label.ljust(30), email)
