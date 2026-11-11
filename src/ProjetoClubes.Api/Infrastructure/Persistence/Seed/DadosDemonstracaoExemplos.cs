using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Eventos;
using ProjetoClubes.Domain.Financeiro;
using ProjetoClubes.Domain.Pessoas;
using ProjetoClubes.Domain.Reservas;

namespace ProjetoClubes.Infrastructure.Persistence.Seed;

/// <summary>
/// Exemplos prontos para as telas não ficarem vazias: mais associados (com profissão e endereço), contratos em todas as
/// situações com parcelas pagas, em aberto e atrasadas, reservas com o nome de quem reservou, eventos com mais de um local
/// e gestão anterior da diretoria. Roda uma vez por clube de demonstração (enquanto o clube não tiver contratos),
/// inclusive em bancos que já existiam.
/// </summary>
internal static partial class DadosDemonstracao
{
    private sealed record PerfilDemo(string Profissao, string Rua, string Numero, string Bairro, string Cep, string Telefone);

    private static readonly Dictionary<string, PerfilDemo> PerfisExistentes = new()
    {
        ["Carlos Alberto Ribeiro"] = new("Engenheiro civil", "Rua Almirante Barroso", "1520", "Centro", "85851010", "45999120034"),
        ["Mariana Costa Ribeiro"] = new("Arquiteta", "Rua Almirante Barroso", "1520", "Centro", "85851010", "45999120035"),
        ["Ana Paula Souza"] = new("Professora", "Rua Jorge Sanways", "430", "Centro", "85851190", "45998874410"),
        ["João Batista Lima"] = new("Comerciante", "Avenida Paraná", "2210", "Jardim Central", "85864000", "45991032287"),
        ["Roberto Fernandes"] = new("Agricultor", "Rua Coronel Otávio Tosta", "815", "Centro", "85980000", "44998127730"),
        ["Cláudia Fernandes"] = new("Enfermeira", "Rua Coronel Otávio Tosta", "815", "Centro", "85980000", "44998127731"),
    };

    private sealed record NovoAssociadoDemo(string Nome, string Categoria, Sexo Sexo, int Idade, int MesesDeClube, PerfilDemo Perfil);

    private static readonly NovoAssociadoDemo[] NovosAssociadosFoz =
    [
        new("Fernanda Oliveira Costa", "associado", Sexo.Feminino, 33, 2,
            new("Advogada", "Rua Marechal Deodoro", "980", "Centro", "85851030", "45999341122")),
        new("Lucas Martins Pereira", "associado", Sexo.Masculino, 29, 1,
            new("Analista de sistemas", "Rua Tarobá", "1200", "Centro", "85851220", "45998456120")),
        new("Beatriz Santos Almeida", "fundador", Sexo.Feminino, 64, 60,
            new("Aposentada", "Avenida Brasil", "455", "Centro", "85851000", "45999870055")),
        new("Rafael Gomes Teixeira", "convidado", Sexo.Masculino, 38, 1,
            new("Médico", "Rua Rui Barbosa", "77", "Vila Yolanda", "85853000", "45991205566")),
    ];

    private static async Task CriarExemplosAsync(IServiceProvider sp, AppDbContext db, DateTimeOffset agora, ILogger logger, CancellationToken ct)
    {
        var tokens = sp.GetRequiredService<ITokenService>();
        var seq = sp.GetRequiredService<IGeradorSequencia>();
        var semTenant = FiltrosConsulta.SomenteTenant;
        var brasilia = TimeSpan.FromHours(-3);
        var hoje = DateOnly.FromDateTime(agora.ToOffset(brasilia).DateTime);
        var mesAtual = new DateOnly(hoje.Year, hoje.Month, 1);
        var indiceCpf = 40;

        foreach (var sigla in Clubes.Select(c => c.Sigla))
        {
            var clube = await db.Clubes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Sigla == sigla && c.Demonstracao, ct);
            if (clube is null || await db.ContratosAssociados.IgnoreQueryFilters().AnyAsync(c => c.ClubeId == clube.Id, ct)) continue;
            var id = clube.Id;
            var associados = await db.Associados.IgnoreQueryFilters(semTenant).Include(a => a.Pessoa)
                .Where(a => a.ClubeId == id).ToListAsync(ct);
            if (associados.Count == 0) continue;

            var emailPresidente = sigla == "FOZ2" ? $"presidente.foz@{Dominio}" : $"presidente.guaira@{Dominio}";
            var emailSecretario = $"secretario.foz@{Dominio}";
            var presidenteId = await db.Usuarios.Where(u => u.EmailNormalizado == emailPresidente).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);
            var secretarioId = await db.Usuarios.Where(u => u.EmailNormalizado == emailSecretario).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
                               ?? presidenteId;
            var categorias = await db.CategoriasAssociados.IgnoreQueryFilters(semTenant).Where(c => c.ClubeId == id)
                .ToDictionaryAsync(c => c.ChaveModelo ?? c.Nome, ct);
            var planos = (await db.PlanosMensalidade.IgnoreQueryFilters(semTenant).Where(p => p.ClubeId == id && p.Ativo).ToListAsync(ct))
                .GroupBy(p => p.CategoriaId).ToDictionary(g => g.Key, g => g.First());

            // ---- cadastro completo: profissão, endereço e telefone dos que já existem
            var dependentes = await db.Dependentes.IgnoreQueryFilters(semTenant).Include(d => d.Pessoa).Where(d => d.ClubeId == id).ToListAsync(ct);
            foreach (var pessoa in associados.Select(a => a.Pessoa).Concat(dependentes.Select(d => d.Pessoa)))
                if (PerfisExistentes.TryGetValue(pessoa.NomeCompleto, out var perfil)) Completar(pessoa, perfil);
            if (associados.FirstOrDefault(a => a.Pessoa.NomeCompleto == "Carlos Alberto Ribeiro") is { } carlosHist)
                db.HistoricoAssociados.Add(new HistoricoAssociado(id, carlosHist.Id, "Alteracao",
                    "Alterado: telefone, endereço, profissão.", secretarioId, agora.AddDays(-3)));

            // ---- mais associados no clube principal
            if (sigla == "FOZ2")
            {
                foreach (var n in NovosAssociadosFoz)
                {
                    if (associados.Any(a => a.Pessoa.NomeCompleto == n.Nome) || !categorias.TryGetValue(n.Categoria, out var categoria)) continue;
                    var pessoa = NovaPessoa(id, n.Nome, n.Sexo, n.Idade, hoje, ++indiceCpf, tokens, agora);
                    Completar(pessoa, n.Perfil);
                    var matricula = $"{sigla}{await seq.ProximoAsync(id, "matricula", ct):000000}";
                    var a = new Associado(id, pessoa, matricula, categoria.Id, hoje.AddMonths(-n.MesesDeClube), false, agora);
                    db.Pessoas.Add(pessoa);
                    db.Associados.Add(a);
                    db.HistoricoAssociados.Add(new HistoricoAssociado(id, a.Id, "Cadastro", "Cadastro de demonstração.", secretarioId, agora));
                    associados.Add(a);
                }
            }

            Associado? Por(string nome) => associados.FirstOrDefault(a => a.Pessoa.NomeCompleto == nome);

            // ---- contratos e mensalidades
            async Task<ContratoAssociado?> ContratoAsync(Associado? a, DateOnly inicio, Guid? anterior = null)
            {
                if (a is null || !planos.TryGetValue(a.CategoriaId, out var plano)) return null;
                var deps = dependentes.Where(d => d.AssociadoTitularId == a.Id && d.Situacao == SituacaoCadastral.Ativo)
                    .Select(d => $"{d.Pessoa.NomeCompleto} ({NomeParentescoDemo(d.Parentesco)})");
                var numero = $"{inicio.Year}-{await seq.ProximoAsync(id, $"contrato-{inicio.Year}", ct):0000}";
                var c = new ContratoAssociado(id, a.Id, numero, inicio, plano.ValorMensal, plano.DiaVencimento, deps, anterior);
                db.ContratosAssociados.Add(c);
                db.HistoricoAssociados.Add(new HistoricoAssociado(id, a.Id, "Contrato",
                    $"Contrato {numero} criado (em análise): {c.ValorMensal:C} por mês, {c.Inicio:MM/yyyy} a {c.Fim:MM/yyyy}.", secretarioId, agora));
                return c;
            }

            async Task<List<Mensalidade>> AprovarAsync(ContratoAssociado c, Func<Mensalidade, bool> pagar)
            {
                c.Aprovar(agora);
                var a = associados.First(x => x.Id == c.AssociadoId);
                var plano = planos[a.CategoriaId];
                var parcelas = new List<Mensalidade>();
                foreach (var comp in c.Competencias())
                {
                    var m = new Mensalidade(id, a.Id, plano.Id, comp, CalculoFinanceiro.Vencimento(comp, c.DiaVencimento), c.ValorMensal, 0m,
                        c.Dependentes.Count);
                    m.VincularContrato(c.Id);
                    db.Mensalidades.Add(m);
                    parcelas.Add(m);
                    if (m.Vencimento < hoje && pagar(m))
                    {
                        m.MarcarPaga();
                        var recibo = await seq.ProximoAsync(id, "recibo", ct);
                        var forma = comp.Month % 3 == 0 ? FormaPagamento.Dinheiro : FormaPagamento.Pix;
                        db.Pagamentos.Add(new Pagamento(id, m.Id, recibo, m.Valor, 0m, m.Vencimento.AddDays(-2), forma, null));
                    }
                }
                db.HistoricoAssociados.Add(new HistoricoAssociado(id, a.Id, "Contrato",
                    $"Contrato {c.Numero} aprovado: 12 parcela(s) de {c.ValorMensal:C} geradas ({c.Inicio:MM/yyyy} a {c.Fim:MM/yyyy}).",
                    presidenteId, agora));
                return parcelas;
            }

            // Carlos: contrato do ano quase no fim, duas parcelas atrasadas (com multa e juros) e a renovação em análise.
            if (await ContratoAsync(Por("Carlos Alberto Ribeiro"), mesAtual.AddMonths(-10)) is { } cCarlos)
            {
                await AprovarAsync(cCarlos, m => m.Vencimento < hoje.AddDays(-45));
                await ContratoAsync(Por("Carlos Alberto Ribeiro"), cCarlos.Fim.AddDays(1), cCarlos.Id);
            }
            // Ana Paula: em dia.
            if (await ContratoAsync(Por("Ana Paula Souza"), mesAtual.AddMonths(-3)) is { } cAna) await AprovarAsync(cAna, _ => true);
            // Rafael: começou este mês.
            if (await ContratoAsync(Por("Rafael Gomes Teixeira"), mesAtual) is { } cRafael) await AprovarAsync(cRafael, _ => true);
            // Fernanda: aguardando análise.
            await ContratoAsync(Por("Fernanda Oliveira Costa"), mesAtual.AddMonths(1));
            // Lucas: reprovado.
            if (await ContratoAsync(Por("Lucas Martins Pereira"), mesAtual.AddMonths(1)) is { } cLucas)
                cLucas.Reprovar("Documentação incompleta: falta o comprovante de residência.");
            // Beatriz: desligada depois de 4 meses (parcelas seguintes canceladas, cadastro inativo).
            if (await ContratoAsync(Por("Beatriz Santos Almeida"), mesAtual.AddMonths(-8)) is { } cBia)
            {
                var desligamento = cBia.Inicio.AddMonths(4);
                var parcelas = await AprovarAsync(cBia, m => m.Competencia < desligamento);
                cBia.Desligar("Mudou-se para Curitiba.");
                foreach (var m in parcelas.Where(m => m.Competencia >= desligamento && m.Situacao == SituacaoMensalidade.Aberta))
                    m.Cancelar($"Contrato {cBia.Numero} desligado.");
                var bia = Por("Beatriz Santos Almeida")!;
                bia.Inativar("Desligado do clube: Mudou-se para Curitiba.", agora);
                db.HistoricoAssociados.Add(new HistoricoAssociado(id, bia.Id, "Contrato",
                    $"Contrato {cBia.Numero} desligado (parcelas futuras canceladas): Mudou-se para Curitiba.", presidenteId, agora));
            }
            // Guaíra: Roberto em dia.
            if (await ContratoAsync(Por("Roberto Fernandes"), mesAtual.AddMonths(-10)) is { } cRoberto) await AprovarAsync(cRoberto, _ => true);

            // ---- espaços, reservas com nome e eventos com mais de um local
            var espacos = await db.Espacos.IgnoreQueryFilters(semTenant).Where(e => e.ClubeId == id).ToListAsync(ct);
            var salao = espacos.FirstOrDefault(e => e.Tipo == TipoEspaco.SalaoSocial);
            var churrasqueira = espacos.FirstOrDefault(e => e.Tipo == TipoEspaco.Churrasqueira);
            var quadra = espacos.FirstOrDefault(e => e.Tipo == TipoEspaco.Quadra);
            var quiosque = espacos.FirstOrDefault(e => e.Tipo == TipoEspaco.Quiosque);
            if (churrasqueira is not null) churrasqueira.LimiteReservasPorMes ??= 2;
            if (quiosque is null && salao is not null)
            {
                quiosque = new Espaco(id, "Quiosque 1", TipoEspaco.Quiosque)
                { Capacidade = 15, ValorLocacao = 40m, ExigeAprovacao = false, LimiteReservasPorMes = 2, Descricao = "Quiosque com pia e mesa para 15 pessoas." };
                db.Espacos.Add(quiosque);
            }

            var ocupadas = await db.Reservas.IgnoreQueryFilters(semTenant)
                .Where(r => r.ClubeId == id && (r.Situacao == SituacaoReserva.Aprovada || r.Situacao == SituacaoReserva.Pendente))
                .Select(r => new { r.EspacoId, r.Inicio, r.Fim }).ToListAsync(ct);
            var novas = new List<Reserva>();
            Reserva? Reservar(Espaco? e, Associado? quem, DateOnly dia, int hora, int horas, string finalidade, Guid? eventoId = null)
            {
                if (e is null || (quem is null && eventoId is null)) return null;
                var inicio = new DateTimeOffset(dia.Year, dia.Month, dia.Day, hora, 0, 0, brasilia);
                var fim = inicio.AddHours(horas);
                if (ocupadas.Any(o => o.EspacoId == e.Id && Reserva.Conflita(o.Inicio, o.Fim, inicio, fim))
                    || novas.Any(o => o.EspacoId == e.Id && Reserva.Conflita(o.Inicio, o.Fim, inicio, fim))) return null;
                var r = new Reserva(id, e.Id, quem?.PessoaId, eventoId, inicio, fim, finalidade,
                    eventoId is null ? e.ValorLocacao + e.TaxaAdicional : 0m, exigeAprovacao: eventoId is null && e.ExigeAprovacao);
                db.Reservas.Add(r);
                novas.Add(r);
                return r;
            }

            var sabado = hoje.AddDays(((int)DayOfWeek.Saturday - (int)hoje.DayOfWeek + 7) % 7 is var d && d == 0 ? 7 : d);
            var terca = hoje.AddDays(((int)DayOfWeek.Tuesday - (int)hoje.DayOfWeek + 7) % 7 is var t && t == 0 ? 7 : t);
            if (sigla == "FOZ2")
            {
                Reservar(churrasqueira, Por("Carlos Alberto Ribeiro"), hoje.AddDays(-35), 12, 4, "Churrasco com os amigos");
                Reservar(churrasqueira, Por("Carlos Alberto Ribeiro"), sabado, 11, 6, "Aniversário do Pedro");
                Reservar(quadra, Por("Fernanda Oliveira Costa"), terca, 19, 2, "Futebol com as amigas");
                Reservar(quiosque, Por("Rafael Gomes Teixeira"), sabado.AddDays(7), 12, 4, "Confraternização da equipe");
                Reservar(salao, Por("Ana Paula Souza"), sabado.AddDays(14), 18, 5, "Chá de bebê"); // salão: fica pendente
            }
            else
            {
                Reservar(churrasqueira, Por("Roberto Fernandes"), sabado, 11, 5, "Almoço de família");
            }

            var feijoada = await db.Eventos.IgnoreQueryFilters(semTenant).FirstOrDefaultAsync(e => e.ClubeId == id && e.Nome == "Feijoada beneficente", ct);
            if (feijoada is not null)
            {
                // Segundo local do evento: a quadra vira espaço kids durante a feijoada.
                var inicioLocal = feijoada.Inicio.ToOffset(brasilia);
                Reservar(quadra, null, DateOnly.FromDateTime(inicioLocal.DateTime), inicioLocal.Hour, 6, $"Evento: {feijoada.Nome} (espaço kids)", feijoada.Id);
                feijoada.DivulgadoEm ??= agora.AddDays(-1);
                // Quem já se inscreveu (ex.: testando o app) fica como está.
                var jaInscritos = await db.InscricoesEventos.IgnoreQueryFilters(semTenant).Where(i => i.EventoId == feijoada.Id)
                    .Select(i => i.PessoaId).ToListAsync(ct);
                foreach (var nome in new[] { "Carlos Alberto Ribeiro", "Ana Paula Souza", "Rafael Gomes Teixeira" })
                    if (Por(nome) is { Situacao: SituacaoCadastral.Ativo } a && !jaInscritos.Contains(a.PessoaId))
                        db.InscricoesEventos.Add(new InscricaoEvento(id, feijoada.Id, a.PessoaId, nome == "Carlos Alberto Ribeiro" ? 2 : 1, SituacaoInscricao.Confirmada));
            }
            if (quiosque is not null && sigla == "FOZ2")
            {
                var dia = sabado.AddDays(7);
                var inicio = new DateTimeOffset(dia.Year, dia.Month, dia.Day, 19, 0, 0, brasilia);
                var truco = new Evento(id, "Torneio de truco", inicio, inicio.AddHours(4), PublicoEvento.Geral)
                {
                    Descricao = "Torneio em duplas, aberto ao público. Premiação para as três primeiras duplas.",
                    Local = quiosque.Nome, EspacoId = quiosque.Id, Capacidade = 12, Valor = 20m, MaximoAcompanhantes = 0,
                };
                if (Reservar(quiosque, null, dia, 19, 4, $"Evento: {truco.Nome}", truco.Id) is not null) db.Eventos.Add(truco);
            }

            // ---- gestão anterior da diretoria
            var primeiroMandato = await db.MembrosDiretoria.IgnoreQueryFilters(semTenant).Where(m => m.ClubeId == id)
                .OrderBy(m => m.InicioMandato).Select(m => (DateOnly?)m.InicioMandato).FirstOrDefaultAsync(ct);
            if (primeiroMandato is { } inicioAtual && inicioAtual.Year > 2000)
            {
                var anterior = inicioAtual.AddYears(-2);
                db.MembrosDiretoria.Add(new Domain.Clubes.MembroDiretoria(id, "Antônio Carlos Mendes", Domain.Clubes.MembroDiretoria.CargoPresidente, anterior, inicioAtual.AddDays(-1)));
                db.MembrosDiretoria.Add(new Domain.Clubes.MembroDiretoria(id, "Helena Duarte", "Vice-presidente", anterior, inicioAtual.AddDays(-1)));
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Exemplos de contratos, reservas e eventos criados no clube {Clube}", clube.Nome);
        }
    }

    /// <summary>
    /// Assembleia em andamento com participação pelo app e votação aberta, para testar o voto no celular.
    /// Roda uma vez por clube (também em bancos que já existiam).
    /// </summary>
    private static async Task CriarAssembleiaPeloAppAsync(AppDbContext db, DateTimeOffset agora, ILogger logger, CancellationToken ct)
    {
        foreach (var sigla in Clubes.Select(c => c.Sigla))
        {
            var clube = await db.Clubes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Sigla == sigla && c.Demonstracao, ct);
            if (clube is null || await db.Assembleias.IgnoreQueryFilters().AnyAsync(a => a.ClubeId == clube.Id && a.ParticipacaoPeloApp, ct)) continue;
            var a = new Domain.Assembleias.Assembleia(clube.Id, "Assembleia Extraordinária — cobertura da piscina",
                Domain.Assembleias.TipoAssembleia.Extraordinaria, agora.AddMinutes(-30), "Salão social e pelo aplicativo",
                "Ficam convocados os associados para deliberar sobre a cobertura da piscina. Participação presencial ou pelo aplicativo.",
                exigeAdimplencia: false);
            a.DefinirParticipacaoPeloApp(true);
            var cobertura = a.AdicionarPauta("Aprovar a cobertura da piscina", "Investimento de R$ 180 mil, pago em 24 parcelas com o fundo de obras.", true, null);
            a.AdicionarPauta("Horário da piscina no inverno", null, true, ["Das 9h às 18h", "Das 10h às 20h", "Abstenção"]);
            a.AdicionarPauta("Informes da diretoria", null, false, null);
            a.Convocar(agora.AddDays(-10));
            a.Instalar();
            a.AbrirVotacao(cobertura.Id);
            db.Assembleias.Add(a);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Assembleia com votação pelo app criada no clube {Clube}", clube.Nome);
        }
    }

    private static void Completar(Pessoa p, PerfilDemo perfil)
    {
        p.Profissao ??= perfil.Profissao;
        p.Telefone ??= perfil.Telefone;
        p.Endereco.Logradouro ??= perfil.Rua;
        p.Endereco.Numero ??= perfil.Numero;
        p.Endereco.Bairro ??= perfil.Bairro;
        p.Endereco.Cep ??= perfil.Cep;
        if (perfil.Cep.StartsWith("8598")) p.Endereco.Cidade = "Guaíra";
    }

    private static string NomeParentescoDemo(Parentesco p) => p switch
    {
        Parentesco.Conjuge => "Cônjuge",
        Parentesco.Filho => "Filho(a)",
        Parentesco.Mae => "Mãe",
        Parentesco.Irmao => "Irmão(ã)",
        _ => p.ToString(),
    };
}
