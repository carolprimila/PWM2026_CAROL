"""Popula as duas sedes do ambiente de DESENVOLVIMENTO com dados de EXEMPLO (associados, dependentes, cobranças,
reservas de quiosques/churrasqueiras, convidados, eventos, comunicados, manifestações, assembleia, manutenção, estoque...).

Uso (depois de `python database/scripts/contas-dev.py`):
    python database/scripts/dados-exemplo-dev.py

Tudo é criado pelos mesmos endpoints usados pelo Web e pelo aplicativo, então as regras reais se aplicam.
Os valores de política gravados aqui (mensalidade, vencimento, taxas, preços) são APENAS EXEMPLOS para demonstração —
não representam decisões do clube. Nunca execute em produção (o script só aceita a API local).
"""
import datetime, uuid
from devlib import SEDES, call, cpf, dev_password, login

password = dev_password()
today = datetime.date.today()
now = datetime.datetime.now(datetime.timezone.utc)
competence = today.strftime('%Y-%m')
day = lambda n: str(today + datetime.timedelta(days=n))

PEOPLE = {
    'guaira': ['Marina Albuquerque Teles', 'Rafael Siqueira Prado', 'Helena Duarte Nogueira', 'Otávio Brandão Luz', 'Beatriz Campos Vidal',
               'Caio Monteiro Sá', 'Larissa Pacheco Reis', 'Gustavo Fontes Arruda', 'Priscila Amaral Bento', 'Diego Varela Coutinho'],
    'foz': ['Renata Figueiredo Lemos', 'Thiago Barros Menezes', 'Isabela Cardoso Pinho', 'Marcelo Antunes Rocha', 'Juliana Vasques Moura',
            'Eduardo Salles Brito', 'Camila Teixeira Dantas', 'Rodrigo Paiva Lacerda', 'Aline Guimarães Torres', 'Vinícius Mattos Queiroz'],
}
KIDS = [('Lucas', 'Filho(a)', '2012-05-20'), ('Sofia', 'Filho(a)', '2015-09-02'), ('Cônjuge', 'Cônjuge', '1988-01-11')]


def next_weekday(weekday, min_days=1):
    d = today + datetime.timedelta(days=min_days)
    while d.weekday() != weekday: d += datetime.timedelta(days=1)
    return str(d)


def populate(slug, city, admin_email):
    auth = login(admin_email, password)
    if auth is None: raise SystemExit(f'Execute antes: python database/scripts/contas-dev.py (não foi possível entrar como {admin_email}).')
    t = auth['accessToken']
    get = lambda path: call('GET', path, token=t)[1]
    post = lambda path, body=None, ok=(200, 204): call('POST', path, body, t, ok=ok)[1]

    if get('/api/associados?pageSize=1')['total'] >= 8:
        print(f'  {city}: já possui dados de exemplo — nada a fazer.')
        return

    # ---- Políticas de exemplo (somente para demonstração) ----
    s = get('/api/configuracoes')
    s.pop('pending', None)
    s.update(monthlyBaseFee=320, dueDay=25, graceDays=5, lateFinePercent=2, dailyInterestPercent=0.033, dependentPricingMode='Progressive',
             dependentPricingRules=[{'fromCount': i, 'toCount': i, 'amount': a} for i, a in enumerate([90, 70, 50, 40, 40], 1)],
             blockAccessWhenDelinquent=True, blockReservationsWhenDelinquent=True, delinquentReservationPolicy='Suspend', allowAdvancePayment=True,
             maxAdvanceDays=60, maxConcurrentReservations=4, freeCancellationHours=24, lateCancellationFee=30, noShowFee=50,
             requireAdvancePayment=False, reservationApprovalMode='Manual', guestFee=25, guestAdvanceRegistrationRequired=False,
             guestDocumentRequired=True, guestEntryWithoutMemberAllowed=False,
             poolPolicy='Uso de traje de banho obrigatório. Crianças menores de 10 anos somente acompanhadas de um responsável.',
             minorsRules='Menores de 12 anos devem estar acompanhados de um responsável nas dependências do clube.')
    call('PUT', '/api/configuracoes', {'settings': s, 'justification': 'Valores de exemplo para demonstração'}, t)

    spaces = {x['name']: x for x in get('/api/espacos')}
    for name, sp in spaces.items():
        extra = {'hourlyPrice': 35, 'chargeMode': 'NextMonthlyBill'} if sp['type'] == 'Kiosk' else \
                {'periodPrice': 1800, 'cleaningFee': 350, 'chargeMode': 'NextMonthlyBill'} if sp['type'] == 'PartyHall' else None
        if extra: call('PUT', f"/api/espacos/{sp['id']}", {**{k: sp[k] for k in ('name', 'type', 'subtype', 'description', 'capacity', 'isReservable', 'bookingMode', 'slotMinutes',
                       'fixedDays', 'approvalMode', 'usageRules', 'imageFileId', 'isActive', 'sortOrder', 'schedules')}, **extra}, t)

    # ---- Associados e dependentes ----
    seed = 300_000_000 if slug == 'guaira' else 600_000_000
    members = []
    for i, name in enumerate(PEOPLE[slug]):
        first = name.split()[0].lower().replace('á', 'a').replace('í', 'i').replace('ú', 'u')
        m = post('/api/associados', {'fullName': name, 'cpf': cpf(seed + i * 104_729), 'email': f'{first}.{slug}@exemplo.local', 'phone': f'(45) 9{8100 + i:04d}-{2200 + i * 7:04d}',
                                     'birthDate': f'{1975 + i * 2}-0{1 + i % 9}-1{i % 9}', 'city': city, 'state': 'PR', 'street': 'Rua das Palmeiras', 'addressNumber': str(100 + i * 12)})
        if i < 9: m = post(f"/api/associados/{m['id']}/aprovar")  # o último fica aguardando aprovação
        members.append(m)
    for i, count in enumerate([3, 2, 2, 1, 1, 1, 0, 2]):
        last = members[i]['fullName'].split()[-1]
        for k in range(count):
            first, rel, birth = KIDS[k]
            post(f"/api/dependentes?memberId={members[i]['id']}", {'fullName': f'{first} {last}' if rel != 'Cônjuge' else f'Cônjuge de {members[i]["fullName"].split()[0]} {last}',
                                                                  'relationship': rel, 'birthDate': birth})

    # ---- Financeiro ----
    post('/api/mensalidades/gerar', {'competence': competence})
    charges = get(f'/api/mensalidades?competence={competence}&pageSize=100')['items']
    by_member = {c['memberId']: c for c in charges}
    for i in range(6):  # seis associados já pagaram; um pagou parte
        c = by_member[members[i]['id']]
        post('/api/pagamentos', {'chargeId': c['id'], 'amount': c['balance'], 'method': ['Pix', 'Cash', 'CreditCard'][i % 3], 'idempotencyKey': str(uuid.uuid4())})
    c = by_member[members[6]['id']]
    post('/api/pagamentos', {'chargeId': c['id'], 'amount': 150, 'method': 'Pix', 'notes': 'Pagamento parcial', 'idempotencyKey': str(uuid.uuid4())})
    # um associado inadimplente (cobrança antiga em atraso): exemplo de bloqueio de acesso e de reservas
    post('/api/financeiro/cobrancas', {'memberId': members[7]['id'], 'type': 'Fee', 'description': 'Taxa de manutenção extraordinária', 'amount': 180, 'dueDate': day(-40)})
    post('/api/mensalidades/atualizar-encargos')
    for kind, category, description, who, amount, due, paid in [
        ('Expense', 'Energia', 'Conta de energia elétrica', 'Copel', 2140.80, day(5), False), ('Expense', 'Água', 'Conta de água', 'Sanepar', 890.35, day(-3), True),
        ('Expense', 'Folha de pagamento', 'Salários da equipe', None, 14800, day(-10), True), ('Expense', 'Piscina', 'Produtos de tratamento', 'Aqua Química', 640, day(8), False),
        ('Revenue', 'Patrocínio', 'Placa publicitária na quadra', 'Mercado Central', 1200, day(-6), True), ('Revenue', 'Lanchonete', 'Aluguel do espaço da lanchonete', 'Sabor da Sede', 950, day(10), False)]:
        e = post('/api/financeiro/lancamentos', {'kind': kind, 'category': category, 'description': description, 'counterparty': who, 'amount': amount, 'dueDate': due})
        if paid: post(f"/api/financeiro/lancamentos/{e['id']}/pagar", {'paidOn': due})

    # ---- Reservas (quiosques/churrasqueiras, quadras, salão) ----
    def reserve(space, date, hours, people, member=None, guests=(), **extra):
        body = {'spaceId': spaces[space]['id'], 'date': date, 'startMinutes': [h * 60 for h in hours], 'peopleCount': people, 'kind': 'Member',
                'guests': [{'fullName': g, 'document': f'RG {10 + n}.{345 + n}.678-{n}'} for n, g in enumerate(guests)], **extra}
        if member is not None: body['memberId'] = members[member]['id']
        return post('/api/reservas', body)

    saturday, sunday = next_weekday(5, 2), next_weekday(6, 2)
    reserve('Quiosque Fechado 1', saturday, [12, 13, 14, 15, 16], 14, 0, ['Pedro Antunes', 'Lívia Sarmento'], notes='Churrasco de aniversário')
    reserve('Quiosque Aberto 2', saturday, [18, 19, 20, 21, 22, 23], 10, 1, ['Jonas Peçanha'])
    reserve('Quiosque Fechado 3', sunday, [12, 13, 14], 8, 2)
    reserve('Quadra de Tênis', day(1), [9, 10], 4, 3)
    reserve('Quadra de Futsal', day(2), [19, 20], 12, 4)
    reserve('Salão de Festas', day(24), [], 160, 5, notes='Festa de 15 anos')
    post('/api/reservas', {'spaceId': spaces['Quadra de Futcampo']['id'], 'date': next_weekday(2), 'startMinutes': [17 * 60, 18 * 60], 'peopleCount': 20,
                           'kind': 'InternalEvent', 'title': 'Escolinha de futebol'})
    post('/api/espacos/bloqueios', {'spaceId': spaces['Quadra de Vôlei']['id'], 'startAt': f'{day(6)}T11:00:00Z', 'endAt': f'{day(6)}T21:00:00Z',
                                    'reason': 'Pintura das linhas da quadra', 'kind': 'Maintenance'})
    post('/api/espacos/feriados', {'date': f'{today.year}-12-25', 'name': 'Natal', 'scope': 'National', 'isClosed': True})

    # ---- Solicitações feitas pelo próprio associado (ficam pendentes para a administração) ----
    access = post(f"/api/associados/{members[0]['id']}/acesso", {})
    mt = login(access['email'], access['temporaryPassword'])['accessToken']
    mpost = lambda path, body=None: call('POST', path, body, mt)[1]
    mpost('/api/reservas', {'spaceId': spaces['Quiosque Aberto 4']['id'], 'date': next_weekday(4, 3), 'startMinutes': [19 * 60, 20 * 60, 21 * 60], 'peopleCount': 9,
                            'guests': [{'fullName': 'Tatiane Ribas', 'document': 'RG 22.111.333-4'}], 'notes': 'Confraternização da família'})
    mpost('/api/reservas', {'spaceId': spaces['Quadra de Vôlei']['id'], 'date': day(3), 'startMinutes': [16 * 60], 'peopleCount': 6, 'guests': []})
    for guest, doc in [('Bruno Castilho', 'RG 33.222.111-0'), ('Vanessa Lobo', 'CPF 111.444.777-35')]:
        mpost('/api/convidados', {'fullName': guest, 'document': doc, 'phone': '(45) 99911-2233', 'visitDate': day(1)})
    types = {x['name']: x for x in call('GET', '/api/manifestacoes/tipos', token=mt)[1]}
    mpost('/api/manifestacoes', {'typeId': types['Sugestão']['id'], 'description': 'Sugiro instalar iluminação de LED na quadra de futcampo para jogos à noite.'})
    complaint = mpost('/api/manifestacoes', {'typeId': types['Reclamação']['id'], 'description': 'O chuveiro do vestiário masculino está sem água quente desde a semana passada.'})
    post(f"/api/manifestacoes/{complaint['manifestation']['id']}/andamento", {'status': 'UnderReview', 'classification': 'Manutenção'})

    # ---- Comunicação ----
    party = post('/api/eventos', {'title': 'Festa de confraternização', 'description': 'Jantar dançante com música ao vivo para associados e familiares.',
                                  'startAt': (now + datetime.timedelta(days=12)).isoformat(), 'endAt': (now + datetime.timedelta(days=12, hours=5)).isoformat(), 'location': 'Salão de Festas',
                                  'capacity': 220, 'audience': 'Public', 'allowMembers': True, 'allowDependents': True, 'allowStaff': False, 'registrationRequired': True})
    post('/api/eventos', {'title': 'Torneio interno de tênis', 'description': 'Inscrições abertas para duplas. Categorias adulto e juvenil.',
                          'startAt': (now + datetime.timedelta(days=20)).isoformat(), 'endAt': (now + datetime.timedelta(days=21)).isoformat(), 'location': 'Quadra de Tênis',
                          'capacity': 32, 'audience': 'MembersOnly', 'allowMembers': True, 'allowDependents': False, 'allowStaff': False, 'registrationRequired': True})
    mpost(f"/api/eventos/{party['id']}/inscricao")
    for title, body, category, priority, dependents in [
        ('Manutenção da piscina no sábado', 'A piscina principal ficará fechada das 8h às 12h para tratamento da água.', 'Operation', 'High', True),
        ('Novo horário da secretaria', 'A partir do próximo mês a secretaria atenderá de terça a sábado, das 9h às 18h.', 'Notice', 'Normal', True),
        ('Convocação: assembleia geral ordinária', 'Convocamos os associados para a assembleia geral. A pauta está disponível no aplicativo.', 'Important', 'Urgent', False)]:
        post('/api/comunicados', {'title': title, 'body': body, 'category': category, 'priority': priority, 'audienceMembers': True, 'audienceDependents': dependents,
                                  'audienceStaff': True, 'isSpecific': False, 'targetUserIds': [], 'publish': True})
    assembly = post('/api/assembleias', {'title': 'Assembleia geral ordinária', 'scheduledAt': (now + datetime.timedelta(days=15)).isoformat(), 'location': 'Salão de Festas',
                                         'agenda': '1. Prestação de contas\n2. Reforma do parque aquático\n3. Assuntos gerais'})
    poll = post('/api/votacoes', {'assemblyId': assembly['id'], 'title': 'Reforma do parque aquático', 'description': 'Aprovação do orçamento para a reforma das piscinas.',
                                  'criterion': 'ActiveMembers', 'options': ['Aprovar', 'Rejeitar', 'Abstenção']})
    post(f"/api/votacoes/{poll['id']}/abrir")
    mpost(f"/api/votacoes/{poll['id']}/votos", {'optionId': poll['options'][0]['id']})

    # ---- Operação ----
    post('/api/funcionarios', {'fullName': 'Jorge Zelador (exemplo)', 'cpf': cpf(seed + 77_777_777), 'position': 'Zelador', 'department': 'Manutenção', 'hiredAt': '2021-03-01'})
    post('/api/manutencoes', {'title': 'Troca dos refletores da quadra de tênis', 'kind': 'Corrective', 'spaceId': spaces['Quadra de Tênis']['id'], 'responsibleName': 'Zeladoria',
                              'dueDate': day(6), 'estimatedCost': 480})
    post('/api/manutencoes', {'title': 'Revisão preventiva das churrasqueiras', 'kind': 'Preventive', 'spaceId': spaces['Quiosque Fechado 2']['id'], 'responsibleName': 'Zeladoria', 'dueDate': day(12)})
    supplier = post('/api/estoque/fornecedores', {'name': 'Aqua Química', 'phone': '(45) 3333-1010'})
    for name, category, unit, minimum, qty in [('Cloro granulado', 'Piscina', 'kg', 20, 12), ('Carvão vegetal', 'Churrasqueiras', 'saco', 10, 35), ('Papel toalha', 'Limpeza', 'pct', 15, 40)]:
        item = post('/api/estoque/produtos', {'name': name, 'category': category, 'unit': unit, 'minQuantity': minimum, 'supplierId': supplier['id'] if category == 'Piscina' else None})
        post('/api/estoque/movimentacoes', {'stockItemId': item['id'], 'kind': 'In', 'quantity': qty, 'unitCost': 14.9, 'reason': 'Compra mensal'})
    for code, name, category, location, value in [('PAT-001', 'Cortador de grama', 'Jardinagem', 'Depósito', 3200), ('PAT-002', 'Freezer horizontal', 'Cozinha', 'Salão de Festas', 2890),
                                                  ('PAT-003', 'Mesa de pingue-pongue', 'Esportes', 'Área de lazer', 1450)]:
        post('/api/patrimonio', {'code': code, 'name': name, 'category': category, 'location': location, 'condition': 'Good', 'value': value, 'acquiredOn': '2023-06-10'})

    # ---- Portaria: uma entrada registrada com QR Code de uso único ----
    qr = mpost('/api/qr-code')
    post('/api/portaria/validar', {'code': qr['token'], 'device': 'dados de exemplo'})

    d = get('/api/relatorios/dashboard')
    print(f"  {city}: {d['members']} associados, {d['dependents']} dependentes, {d['pendingReservations']} reservas pendentes, {d['pendingGuests']} convidados aguardando, "
          f"{d['openManifestations']} manifestações, {d['finance']['delinquentMembers']} inadimplente(s).")


print('Dados de exemplo:')
populate('guaira', 'Guaíra', 'admin@daros.local')
populate('foz', 'Foz do Iguaçu', 'admin.foz@daros.local')

# A associada de teste do aplicativo (sede Guaíra) também recebe movimentação própria.
ana = login('associado@daros.local', password)
if ana:
    t = ana['accessToken']
    if call('GET', '/api/reservas?pageSize=1', token=t)[1]['total'] == 0:
        spaces = {x['name']: x for x in call('GET', '/api/espacos', token=t)[1]}
        call('POST', '/api/reservas', {'spaceId': spaces['Quiosque Aberto 1']['id'], 'date': next_weekday(5, 8), 'startMinutes': [12 * 60, 13 * 60, 14 * 60, 15 * 60], 'peopleCount': 10,
                                       'guests': [{'fullName': 'Clara Mendonça', 'document': 'RG 44.555.666-7'}], 'notes': 'Churrasco com amigos'}, t)
        call('POST', '/api/convidados', {'fullName': 'Rogério Faria', 'document': 'RG 55.666.777-8', 'visitDate': day(2)}, t)
        print('  Associada de teste (aplicativo): reserva de quiosque e convidado solicitados.')
