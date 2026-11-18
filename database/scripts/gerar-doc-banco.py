"""Gera docs/banco-de-dados/diagrama-er.md e dicionario-de-dados.md a partir do esquema real do PostgreSQL.
Uso (na raiz do projeto, com o contêiner db em execução): python database/scripts/gerar-doc-banco.py"""
import subprocess, collections, os

def q(sql):
    out = subprocess.run(['docker', 'compose', 'exec', '-T', 'db', 'sh', '-c', 'psql -At -F "|" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "$0"', sql],
                         capture_output=True, text=True, encoding='utf-8', check=True).stdout
    return [l.split('|') for l in out.splitlines() if l]

cols = q(r"select table_name, column_name, data_type, is_nullable, coalesce(character_maximum_length::text,'') from information_schema.columns where table_schema='public' and table_name not like '\_\_%' order by table_name, ordinal_position")
fks = q("""select c.conrelid::regclass::text, c.confrelid::regclass::text, string_agg(a.attname, ',' order by x.n) from pg_constraint c
 cross join lateral unnest(c.conkey) with ordinality as x(attnum,n) join pg_attribute a on a.attrelid=c.conrelid and a.attnum=x.attnum
 where c.contype='f' and c.connamespace='public'::regnamespace group by c.oid, c.conrelid, c.confrelid order by 1,2""")
idx = q(r"select tablename, indexname, indexdef from pg_indexes where schemaname='public' and tablename not like '\_\_%' order by 1,2")
checks = q("select conrelid::regclass::text, conname, pg_get_constraintdef(oid) from pg_constraint where contype in ('c','x') and connamespace='public'::regnamespace order by 1,2")
clean = lambda s: s.strip('"')

tables = collections.OrderedDict()
for t, c, ty, nul, ln in cols: tables.setdefault(t, []).append((c, ty, nul, ln))
groups = collections.OrderedDict([
 ('Plataforma e acesso', ['Club','ClubSettings','ClubOpeningHour','DependentPricingRule','User','Role','RolePermission','UserRole','UserPermission','RefreshToken','PasswordResetToken','LoginHistory','ClubSequence']),
 ('Pessoas e portaria', ['Member','Dependent','DependentPermission','Employee','DigitalCard','QrToken','AccessLog','Presence','Guest']),
 ('Financeiro', ['Charge','ChargeAdjustment','Payment','FinancialEntry','MonthlyClosing','FinancialRelease']),
 ('Espaços e reservas', ['Space','SpaceSchedule','SpaceBlock','Holiday','Reservation','ReservationSlot','KeyHandover','Inspection','InspectionItem','Occurrence']),
 ('Comunicação', ['ClubEvent','EventRegistration','Announcement','AnnouncementTarget','AnnouncementRead','Notification','DeviceToken','NotificationPreference','ManifestationType','Manifestation','ManifestationUpdate','Assembly','AssemblyAttendance','Poll','PollOption','Vote']),
 ('Operação e auditoria', ['MaintenanceOrder','Asset','Supplier','StockItem','StockMovement','StoredFile','Attachment','AuditLog','StatusHistory']),
])
known = {t for g in groups.values() for t in g}
extra = [t for t in tables if t not in known]
if extra: groups['Outras'] = extra

os.makedirs('docs/banco-de-dados', exist_ok=True)
with open('docs/banco-de-dados/diagrama-er.md', 'w', encoding='utf-8') as f:
    f.write('# Diagrama entidade-relacionamento\n\nGerado a partir do esquema real (`python database/scripts/gerar-doc-banco.py`). ')
    f.write('Os relacionamentos com `Club` (todas as tabelas de clube possuem `ClubId`) e com `User`/`StoredFile` foram omitidos dos diagramas para manter a leitura; constam no dicionário de dados.\n')
    for name, members in groups.items():
        present = [t for t in members if t in tables]
        f.write(f'\n## {name}\n\n```mermaid\nerDiagram\n')
        seen = set()
        for child, parent, columns in fks:
            child, parent = clean(child), clean(parent)
            if child not in present or parent in ('Club', 'User', 'StoredFile') or (child, parent) in seen: continue
            seen.add((child, parent))
            col = [c for c in columns.split(',') if c != 'ClubId'] or ['ClubId']
            f.write(f'    {parent} ||--o{{ {child} : "{col[-1]}"\n')
        for t in present:
            f.write(f'    {t} {{\n')
            for c, ty, nul, ln in tables[t][:14]:
                f.write(f'        {ty.replace(" ", "_").replace("timestamp_with_time_zone","timestamptz").replace("character_varying","varchar")} {c}\n')
            if len(tables[t]) > 14: f.write(f'        etc mais_{len(tables[t]) - 14}_colunas\n')
            f.write('    }\n')
        f.write('```\n')

with open('docs/banco-de-dados/dicionario-de-dados.md', 'w', encoding='utf-8') as f:
    f.write(f'# Dicionário de dados\n\nGerado a partir do esquema real. {len(tables)} tabelas.\n')
    for name, members in groups.items():
        f.write(f'\n## {name}\n')
        for t in [t for t in members if t in tables]:
            f.write(f'\n### {t}\n\n| Coluna | Tipo | Nulo |\n|---|---|---|\n')
            for c, ty, nul, ln in tables[t]:
                f.write(f'| {c} | {ty}{f"({ln})" if ln else ""} | {"sim" if nul == "YES" else "não"} |\n')
            rel = [(clean(p), cs) for ch, p, cs in fks if clean(ch) == t]
            if rel: f.write('\nChaves estrangeiras: ' + '; '.join(f'({cs}) → {p}' for p, cs in rel) + '\n')
            ix = [d.split(' USING ', 1)[1] if ' USING ' in d else d for tt, n, d in idx if tt == t and ('UNIQUE' in d) and not n.startswith('PK_')]
            un = [f'`{n}`' + (' (filtrado)' if 'WHERE' in d else '') for tt, n, d in idx if tt == t and 'UNIQUE' in d and not n.startswith('PK_')]
            if un: f.write('\nUnicidade: ' + ', '.join(un) + '\n')
            ck = [f'`{n}`: {d}' for tt, n, d in checks if clean(tt) == t]
            if ck: f.write('\nRestrições: ' + '; '.join(ck) + '\n')
print(len(tables), 'tabelas,', len(fks), 'chaves estrangeiras')
