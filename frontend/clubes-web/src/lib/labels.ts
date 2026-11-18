import type { Schema } from './api'

export type Tone = 'neutral' | 'brand' | 'success' | 'warning' | 'danger' | 'info'
type Entry = { label: string; tone: Tone }
type Dict<K extends string> = Record<K, Entry>

const e = (label: string, tone: Tone = 'neutral'): Entry => ({ label, tone })

/** Rótulos e cores de todas as enumerações da API — fonte única para Badges e filtros. */
export const labels = {
  userKind: { SuperAdmin: e('Superadministrador', 'brand'), ClubAdmin: e('Administrador', 'brand'), Staff: e('Funcionário', 'info'), Member: e('Associado'), Dependent: e('Dependente') } satisfies Dict<Schema<'UserKind'>>,
  memberStatus: { PendingApproval: e('Aguardando aprovação', 'warning'), Active: e('Ativo', 'success'), Suspended: e('Suspenso', 'danger'), Cancelled: e('Cancelado') } satisfies Dict<Schema<'MemberStatus'>>,
  dependentStatus: { PendingApproval: e('Aguardando aprovação', 'warning'), Active: e('Ativo', 'success'), Suspended: e('Suspenso', 'danger'), Rejected: e('Não aprovado'), Removed: e('Removido') } satisfies Dict<Schema<'DependentStatus'>>,
  employeeStatus: { Active: e('Ativo', 'success'), Inactive: e('Inativo') } satisfies Dict<Schema<'EmployeeStatus'>>,
  personType: { Member: e('Associado', 'brand'), Dependent: e('Dependente', 'info'), Guest: e('Convidado', 'warning') } satisfies Dict<Schema<'PersonType'>>,
  accessOperation: { Entry: e('Entrada', 'success'), Exit: e('Saída', 'info') } satisfies Dict<Schema<'AccessOperation'>>,
  accessResult: { Authorized: e('Autorizado', 'success'), Denied: e('Negado', 'danger') } satisfies Dict<Schema<'AccessResult'>>,
  chargeType: { Monthly: e('Mensalidade', 'brand'), Fine: e('Multa', 'danger'), Fee: e('Taxa'), Reservation: e('Reserva', 'info'), Guest: e('Convidado'), Damage: e('Danos', 'danger'), Penalty: e('Penalidade', 'warning'), Other: e('Outra') } satisfies Dict<Schema<'ChargeType'>>,
  chargeStatus: { Open: e('Em aberto', 'warning'), Paid: e('Paga', 'success'), Cancelled: e('Cancelada') } satisfies Dict<Schema<'ChargeStatus'>>,
  adjustmentKind: { Discount: e('Desconto', 'success'), Addition: e('Acréscimo', 'warning'), Fine: e('Multa', 'danger'), Interest: e('Juros', 'danger'), Fee: e('Taxa') } satisfies Dict<Schema<'AdjustmentKind'>>,
  paymentMethod: { Cash: e('Dinheiro'), Pix: e('Pix'), CreditCard: e('Cartão de crédito'), DebitCard: e('Cartão de débito'), BankSlip: e('Boleto'), Transfer: e('Transferência') } satisfies Dict<Schema<'PaymentMethod'>>,
  paymentChannel: { App: e('Aplicativo', 'info'), FrontDesk: e('Secretaria') } satisfies Dict<Schema<'PaymentChannel'>>,
  paymentStatus: { Pending: e('Pendente', 'warning'), Confirmed: e('Confirmado', 'success'), Failed: e('Falhou', 'danger'), Refunded: e('Estornado') } satisfies Dict<Schema<'PaymentStatus'>>,
  entryKind: { Revenue: e('Receita', 'success'), Expense: e('Despesa', 'danger') } satisfies Dict<Schema<'FinancialEntryKind'>>,
  entryStatus: { Pending: e('Pendente', 'warning'), Paid: e('Pago', 'success'), Cancelled: e('Cancelado') } satisfies Dict<Schema<'FinancialEntryStatus'>>,
  pricingMode: { NotConfigured: e('Não definida', 'warning'), None: e('Sem cobrança por dependente'), FixedPerDependent: e('Valor fixo por dependente'), Progressive: e('Progressiva (valor por posição)'), Tiers: e('Por faixas de quantidade') } satisfies Dict<Schema<'DependentPricingMode'>>,
  delinquentPolicy: { Keep: e('Manter reservas'), Suspend: e('Suspender reservas', 'warning'), Cancel: e('Cancelar reservas', 'danger') } satisfies Dict<Schema<'DelinquentReservationPolicy'>>,
  spaceType: { Court: e('Quadra', 'brand'), Kiosk: e('Quiosque', 'info'), PartyHall: e('Salão de festas', 'warning'), Pool: e('Piscina'), Other: e('Outro') } satisfies Dict<Schema<'SpaceType'>>,
  bookingMode: { Hourly: e('Por horário'), MultiDay: e('Por período de dias') } satisfies Dict<Schema<'BookingMode'>>,
  chargeMode: { NextMonthlyBill: e('Na mensalidade seguinte'), Advance: e('Pagamento antecipado') } satisfies Dict<Schema<'SpaceChargeMode'>>,
  approvalMode: { Manual: e('Aprovação manual'), Automatic: e('Aprovação automática') } satisfies Dict<Schema<'ApprovalMode'>>,
  reservationKind: { Member: e('Associado'), Administrative: e('Administrativa', 'info'), InternalEvent: e('Evento interno', 'brand') } satisfies Dict<Schema<'ReservationKind'>>,
  reservationStatus: { Pending: e('Pendente', 'warning'), Approved: e('Aprovada', 'success'), Rejected: e('Recusada', 'danger'), Cancelled: e('Cancelada'), Suspended: e('Suspensa', 'danger'), Completed: e('Concluída', 'info') } satisfies Dict<Schema<'ReservationStatus'>>,
  attendance: { Unknown: e('Não registrado'), Attended: e('Compareceu', 'success'), NoShow: e('Não compareceu', 'danger') } satisfies Dict<Schema<'AttendanceStatus'>>,
  blockKind: { Administrative: e('Administrativo'), Maintenance: e('Manutenção', 'warning'), Holiday: e('Feriado', 'info') } satisfies Dict<Schema<'BlockKind'>>,
  holidayScope: { National: e('Feriado nacional'), State: e('Feriado estadual'), Municipal: e('Feriado municipal'), Commemorative: e('Data comemorativa', 'info'), ExceptionalClosure: e('Fechamento excepcional', 'danger'), SpecialHours: e('Horário especial', 'warning') } satisfies Dict<Schema<'HolidayScope'>>,
  slotState: { Available: e('Disponível', 'success'), Reserved: e('Reservado', 'info'), Blocked: e('Bloqueado', 'danger'), Past: e('Encerrado'), Closed: e('Fechado') } satisfies Dict<Schema<'SlotState'>>,
  guestAuthorization: { Pending: e('Aguardando autorização', 'warning'), Authorized: e('Autorizado', 'success'), Denied: e('Não autorizado', 'danger') } satisfies Dict<Schema<'GuestAuthorization'>>,
  eventAudience: { Public: e('Público (todos)'), MembersOnly: e('Somente associados', 'brand'), Specific: e('Públicos específicos', 'info') } satisfies Dict<Schema<'EventAudience'>>,
  eventStatus: { Scheduled: e('Agendado', 'success'), Cancelled: e('Cancelado', 'danger'), Finished: e('Encerrado') } satisfies Dict<Schema<'EventStatus'>>,
  registrationStatus: { Registered: e('Inscrito', 'success'), Cancelled: e('Cancelada') } satisfies Dict<Schema<'RegistrationStatus'>>,
  announcementCategory: { Communication: e('Comunicado', 'brand'), Notice: e('Aviso', 'warning'), News: e('Notícia', 'info'), Important: e('Importante', 'danger'), Operation: e('Funcionamento'), Event: e('Evento', 'info'), Financial: e('Financeiro', 'success') } satisfies Dict<Schema<'AnnouncementCategory'>>,
  priority: { Normal: e('Normal'), High: e('Alta', 'warning'), Urgent: e('Urgente', 'danger') } satisfies Dict<Schema<'Priority'>>,
  notificationCategory: { General: e('Geral'), Finance: e('Financeiro', 'success'), Reservation: e('Reservas', 'info'), Dependent: e('Dependentes'), Support: e('Atendimento', 'warning'), Assembly: e('Assembleias', 'brand'), Event: e('Eventos', 'info') } satisfies Dict<Schema<'NotificationCategory'>>,
  deliveryStatus: { NotRequested: e('Não solicitado'), Pending: e('Pendente', 'warning'), Sent: e('Enviado', 'success'), Failed: e('Falhou', 'danger'), NoDestination: e('Sem destino') } satisfies Dict<Schema<'DeliveryStatus'>>,
  manifestationStatus: { Received: e('Recebida', 'warning'), UnderReview: e('Em análise', 'info'), InProgress: e('Em andamento', 'info'), Accepted: e('Aceita', 'success'), ForwardedToAssembly: e('Encaminhada à assembleia', 'brand'), Resolved: e('Resolvida', 'success'), Refused: e('Recusada', 'danger') } satisfies Dict<Schema<'ManifestationStatus'>>,
  assemblyStatus: { Scheduled: e('Agendada', 'info'), InProgress: e('Em andamento', 'success'), Finished: e('Encerrada'), Cancelled: e('Cancelada', 'danger') } satisfies Dict<Schema<'AssemblyStatus'>>,
  pollStatus: { Draft: e('Rascunho'), Open: e('Aberta', 'success'), Closed: e('Encerrada') } satisfies Dict<Schema<'PollStatus'>>,
  pollCriterion: { ActiveMembers: e('Todos os associados ativos'), NonDelinquentMembers: e('Associados em dia'), PresentAtAssembly: e('Presentes na assembleia') } satisfies Dict<Schema<'PollCriterion'>>,
  assetCondition: { New: e('Novo', 'success'), Good: e('Bom', 'success'), Regular: e('Regular', 'warning'), Bad: e('Ruim', 'danger'), Unusable: e('Inservível', 'danger') } satisfies Dict<Schema<'AssetCondition'>>,
  stockMovement: { In: e('Entrada', 'success'), Out: e('Saída', 'danger'), Adjustment: e('Ajuste', 'info') } satisfies Dict<Schema<'StockMovementKind'>>,
  maintenanceKind: { Preventive: e('Preventiva', 'info'), Corrective: e('Corretiva', 'warning') } satisfies Dict<Schema<'MaintenanceKind'>>,
  maintenanceStatus: { Requested: e('Solicitada', 'warning'), UnderReview: e('Em análise', 'info'), Approved: e('Aprovada', 'brand'), InProgress: e('Em andamento', 'info'), Completed: e('Concluída', 'success'), Cancelled: e('Cancelada') } satisfies Dict<Schema<'MaintenanceStatus'>>,
  inspectionMoment: { Before: e('Antes do uso', 'info'), After: e('Depois do uso', 'brand') } satisfies Dict<Schema<'InspectionMoment'>>,
  inspectionCategory: { Conservation: e('Conservação'), Cleaning: e('Limpeza'), Equipment: e('Equipamentos'), Furniture: e('Móveis'), Utensils: e('Utensílios') } satisfies Dict<Schema<'InspectionCategory'>>,
  occurrenceKind: { Damage: e('Dano', 'danger'), Gate: e('Portaria', 'info'), Other: e('Outra') } satisfies Dict<Schema<'OccurrenceKind'>>,
  occurrenceStatus: { Open: e('Aberta', 'warning'), UnderReview: e('Em análise', 'info'), Resolved: e('Resolvida', 'success'), Dismissed: e('Arquivada') } satisfies Dict<Schema<'OccurrenceStatus'>>,
  chargeDecision: { None: e('Sem cobrança'), Pending: e('Aguardando aprovação', 'warning'), Approved: e('Cobrança aprovada', 'success'), Rejected: e('Cobrança rejeitada', 'danger') } satisfies Dict<Schema<'ChargeDecision'>>,
  calendarType: { Reservation: e('Reserva', 'info'), HallRental: e('Locação do salão', 'warning'), Sports: e('Atividade esportiva', 'brand'), Event: e('Evento', 'success'), Assembly: e('Assembleia', 'brand'), Maintenance: e('Manutenção', 'warning'), Block: e('Bloqueio', 'danger'), Holiday: e('Feriado') } satisfies Dict<Schema<'CalendarItemType'>>,
}

export const weekDays = ['Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado'] as const
/** A API serializa DayOfWeek como texto em inglês. */
export const dayOfWeek: Record<Schema<'DayOfWeek'>, { label: string; short: string; index: number }> = {
  Sunday: { label: 'Domingo', short: 'Dom', index: 0 }, Monday: { label: 'Segunda-feira', short: 'Seg', index: 1 }, Tuesday: { label: 'Terça-feira', short: 'Ter', index: 2 },
  Wednesday: { label: 'Quarta-feira', short: 'Qua', index: 3 }, Thursday: { label: 'Quinta-feira', short: 'Qui', index: 4 }, Friday: { label: 'Sexta-feira', short: 'Sex', index: 5 },
  Saturday: { label: 'Sábado', short: 'Sáb', index: 6 },
}
export const daysOfWeek = Object.keys(dayOfWeek) as Schema<'DayOfWeek'>[]

/** Opções para <Select> a partir de um dicionário de rótulos. */
export function options<K extends string>(dict: Record<K, Entry>): { value: K; label: string }[] {
  return (Object.keys(dict) as K[]).map((value) => ({ value, label: dict[value].label }))
}
