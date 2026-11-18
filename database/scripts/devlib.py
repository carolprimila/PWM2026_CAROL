"""Apoio aos scripts de desenvolvimento (contas-dev.py e dados-exemplo-dev.py). SOMENTE ambiente local."""
import json, os, re, secrets, sys, time, urllib.error, urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
os.chdir(ROOT)
sys.stdout.reconfigure(encoding='utf-8')
BASE = os.environ.get('CLUBES_API', 'http://localhost:5080')
if not re.match(r'^http://(localhost|127\.0\.0\.1)(:\d+)?$', BASE):
    sys.exit('Estes scripts só podem ser usados contra a API local de desenvolvimento.')

LOCAL_ENV = 'frontend/clubes-web/.env.local'
SEDES = [('guaira', 'Daros Guaíra', 'Guaíra', 'PR'), ('foz', 'Daros Foz', 'Foz do Iguaçu', 'PR')]


def read_env():
    text = open('.env', encoding='utf-8').read()
    return text, dict(l.strip().split('=', 1) for l in text.splitlines() if '=' in l and not l.startswith('#'))


def call(method, path, body=None, token=None, club=None, ok=(200, 204)):
    headers = {'Content-Type': 'application/json'}
    if token: headers['Authorization'] = 'Bearer ' + token
    if club: headers['X-Club-Id'] = club
    data = json.dumps(body).encode() if body is not None else (b'{}' if method in ('POST', 'PUT') else None)
    try:
        with urllib.request.urlopen(urllib.request.Request(BASE + path, method=method, data=data, headers=headers)) as r:
            text = r.read().decode()
            return r.status, (json.loads(text) if text else None)
    except urllib.error.HTTPError as e:
        text = e.read().decode()
        try: payload = json.loads(text)
        except Exception: payload = {'message': text}
        if e.code not in ok: raise SystemExit(f'{method} {path} -> {e.code}: {payload.get("message")} {payload.get("errors") or ""}')
        return e.code, payload


def login(email, password):
    for _ in range(10):
        status, data = call('POST', '/api/auth/login', {'email': email, 'password': password, 'device': 'scripts-dev'}, ok=(200, 401, 403, 429))
        # A API limita as tentativas de autenticação por minuto: aguarda a janela e repete.
        if status == 429 and data.get('code') == 'muitas_requisicoes':
            time.sleep(15)
            continue
        return data if status == 200 else None
    return None


def dev_password():
    """Senha única de teste, reaproveitada entre execuções (fica apenas em arquivos locais não versionados)."""
    if os.path.exists(LOCAL_ENV):
        m = re.search(r'^DEV_LOGIN_PASSWORD=(.+)$', open(LOCAL_ENV, encoding='utf-8').read(), re.M)
        if m: return m.group(1).strip()
    return 'Dev' + secrets.token_urlsafe(8) + '9a'


def cpf(n):
    d = [int(c) for c in f'{n:09d}']
    for j in (9, 10): d.append(sum(d[i] * (j + 1 - i) for i in range(j)) * 10 % 11 % 10)
    return ''.join(map(str, d))
