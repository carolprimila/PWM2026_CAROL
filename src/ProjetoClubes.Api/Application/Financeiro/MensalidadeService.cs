using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.Application.Financeiro;

public interface IMensalidadeService
{
    Task<ConfiguracaoFinanceiraDto> ObterConfiguracaoAsync(CancellationToken ct);
    Task<ConfiguracaoFinanceiraDto> SalvarConfiguracaoAsync(ConfiguracaoFinanceiraDto r, CancellationToken ct);
    Task<IReadOnlyList<PlanoDto>> ListarPlanosAsync(CancellationToken ct);
    Task<PlanoDto> SalvarPlanoAsync(Guid? id, SalvarPlanoRequest r, CancellationToken ct);
    Task ExcluirPlanoAsync(Guid id, CancellationToken ct);
    Task<GerarMensalidadesResultado> GerarAsync(GerarMensalidadesRequest r, CancellationToken ct);
    Task<Pagina<MensalidadeDto>> ListarAsync(ConsultaMensalidades c, CancellationToken ct);
    Task<MensalidadeDto> PagarAsync(Guid id, RegistrarPagamentoRequest r, CancellationToken ct);
    Task<MensalidadeDto> EstornarPagamentoAsync(Guid pagamentoId, MotivoRequest r, CancellationToken ct);
    Task<MensalidadeDto> IsentarAsync(Guid id, MotivoRequest r, CancellationToken ct);
    Task<MensalidadeDto> CancelarAsync(Guid id, MotivoRequest r, CancellationToken ct);
    Task<ReciboDto> ReciboAsync(Guid pagamentoId, CancellationToken ct);
    Task<SituacaoFinanceiraDto> SituacaoDoAssociadoAsync(Guid associadoId, CancellationToken ct);
    Task<SituacaoFinanceiraDto> SituacaoNoPortalAsync(CancellationToken ct);
}

/// <summary>Planos, geração de mensalidades, pagamentos (com recibo), estornos, isenções e situação financeira.</summary>
public sealed class MensalidadeService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IAcessoPessoas acessoPessoas,
    IGeradorSequencia sequencias,
    TimeProvider relogio,
    IValidator<SalvarPlanoRequest> planoValidator,
    IValidator<GerarMensalidadesRequest> gerarValidator,
    IValidator<RegistrarPagamentoRequest> pagamentoValidator,
    IValidator<ConfiguracaoFinanceiraDto> configValidator) : IMensalidadeService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);

    // ------------------------------------------------------------------ configuração e planos

    public async Task<ConfiguracaoFinanceiraDto> ObterConfiguracaoAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        var c = await ConfigAsync(ct);
        return new(c.MultaPercentual, c.JurosMensalPercentual, c.DiasTolerancia);
    }

    public async Task<ConfiguracaoFinanceiraDto> SalvarConfiguracaoAsync(ConfiguracaoFinanceiraDto r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        await configValidator.ValidarAsync(r, ct);
        var c = await ConfigAsync(ct);
        c.MultaPercentual = r.MultaPercentual;
        c.JurosMensalPercentual = r.JurosMensalPercentual;
        c.DiasTolerancia = r.DiasTolerancia;
        await db.SaveChangesAsync(ct);
        return r;
    }

    public async Task<IReadOnlyList<PlanoDto>> ListarPlanosAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        return await db.PlanosMensalidade.AsNoTracking().Where(p => p.ClubeId == ClubeId).OrderBy(p => p.Nome)
            .Select(p => new PlanoDto(p.Id, p.Nome, p.CategoriaId,
                db.CategoriasAssociados.Where(c => c.Id == p.CategoriaId).Select(c => c.Nome).FirstOrDefault() ?? "—",
                p.ValorMensal, p.DiaVencimento, p.Ativo,
                db.Associados.Count(a => a.CategoriaId == p.CategoriaId && a.Situacao == SituacaoCadastral.Ativo)))
            .ToListAsync(ct);
    }

    public async Task<PlanoDto> SalvarPlanoAsync(Guid? id, SalvarPlanoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        await planoValidator.ValidarAsync(r, ct);
        if (!await db.CategoriasAssociados.AnyAsync(c => c.Id == r.CategoriaId && c.ClubeId == ClubeId, ct))
            throw Invalido("CategoriaId", "Categoria inexistente.");
        if (r.Ativo && await db.PlanosMensalidade.AnyAsync(p => p.ClubeId == ClubeId && p.CategoriaId == r.CategoriaId && p.Ativo && p.Id != id, ct))
            throw new ConflitoException("plano.categoria_duplicada", "Já existe um plano ativo para esta categoria. Desative-o antes.");

        PlanoMensalidade plano;
        if (id is { } existente)
        {
            plano = await db.PlanosMensalidade.FirstOrDefaultAsync(p => p.Id == existente && p.ClubeId == ClubeId, ct)
                    ?? throw new NaoEncontradoException("Plano");
            plano.Nome = r.Nome.Trim();
            plano.CategoriaId = r.CategoriaId;
            plano.DefinirValores(r.ValorMensal, r.DiaVencimento);
            plano.Ativo = r.Ativo;
        }
        else
        {
            plano = new PlanoMensalidade(ClubeId, r.Nome, r.CategoriaId, r.ValorMensal, r.DiaVencimento) { Ativo = r.Ativo };
            db.PlanosMensalidade.Add(plano);
        }
        await db.SaveChangesAsync(ct);
        return (await ListarPlanosAsync(ct)).Single(p => p.Id == plano.Id);
    }

    public async Task ExcluirPlanoAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        var plano = await db.PlanosMensalidade.FirstOrDefaultAsync(p => p.Id == id && p.ClubeId == ClubeId, ct)
                    ?? throw new NaoEncontradoException("Plano");
        db.PlanosMensalidade.Remove(plano);
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------ geração

    /// <summary>Gera as mensalidades da competência para associados ativos. Idempotente: não duplica.</summary>
    public async Task<GerarMensalidadesResultado> GerarAsync(GerarMensalidadesRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        await gerarValidator.ValidarAsync(r, ct);
        var competencia = CalculoFinanceiro.Competencia(int.Parse(r.Competencia[..4]), int.Parse(r.Competencia[5..]));

        var planos = await db.PlanosMensalidade.Where(p => p.ClubeId == ClubeId && p.Ativo).ToDictionaryAsync(p => p.CategoriaId, ct);
        var cfgDep = await db.ConfiguracoesDependentes.FirstAsync(c => c.ClubeId == ClubeId, ct);
        var associados = await db.Associados.Where(a => a.ClubeId == ClubeId && a.Situacao == SituacaoCadastral.Ativo)
            .Select(a => new
            {
                a.Id, a.CategoriaId, a.Pessoa.NomeCompleto,
                Dependentes = a.Dependentes.Count(d => d.Situacao == SituacaoCadastral.Ativo),
            })
            .ToListAsync(ct);
        var jaGeradas = (await db.Mensalidades
                .Where(m => m.ClubeId == ClubeId && m.Competencia == competencia && m.Situacao != SituacaoMensalidade.Cancelada)
                .Select(m => m.AssociadoId).ToListAsync(ct))
            .ToHashSet();

        int geradas = 0, existentes = 0;
        decimal total = 0;
        var semPlano = new List<string>();
        foreach (var a in associados)
        {
            if (jaGeradas.Contains(a.Id)) { existentes++; continue; }
            if (!planos.TryGetValue(a.CategoriaId, out var plano)) { semPlano.Add(a.NomeCompleto); continue; }

            var adicional = cfgDep.IncluidosNaMensalidadeDoTitular ? 0m : a.Dependentes * cfgDep.ValorAdicionalPorDependente;
            var m = new Mensalidade(ClubeId, a.Id, plano.Id, competencia, CalculoFinanceiro.Vencimento(competencia, plano.DiaVencimento),
                plano.ValorMensal, CalculoFinanceiro.Arredondar(adicional), cfgDep.IncluidosNaMensalidadeDoTitular ? 0 : a.Dependentes);
            db.Mensalidades.Add(m);
            geradas++;
            total += m.Valor;
        }
        await db.SaveChangesAsync(ct);
        return new GerarMensalidadesResultado(r.Competencia, geradas, existentes, total, semPlano.Order().ToList());
    }

    // ------------------------------------------------------------------ consulta

    public async Task<Pagina<MensalidadeDto>> ListarAsync(ConsultaMensalidades c, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        var q = db.Mensalidades.AsNoTracking().Where(m => m.ClubeId == ClubeId);
        if (!string.IsNullOrWhiteSpace(c.Competencia) && c.Competencia.Length == 7
            && int.TryParse(c.Competencia[..4], out var ano) && int.TryParse(c.Competencia[5..], out var mes) && mes is >= 1 and <= 12)
        {
            var comp = CalculoFinanceiro.Competencia(ano, mes);
            q = q.Where(m => m.Competencia == comp);
        }
        if (c.AssociadoId is { } aid) q = q.Where(m => m.AssociadoId == aid);
        if (c.ContratoId is { } cid) q = q.Where(m => m.ContratoId == cid);
        var hoje = Hoje;
        q = c.Situacao switch
        {
            "Vencida" => q.Where(m => m.Situacao == SituacaoMensalidade.Aberta && m.Vencimento < hoje),
            "Aberta" => q.Where(m => m.Situacao == SituacaoMensalidade.Aberta && m.Vencimento >= hoje),
            _ when Enum.TryParse<SituacaoMensalidade>(c.Situacao, true, out var s) => q.Where(m => m.Situacao == s),
            _ => q,
        };
        if (!string.IsNullOrWhiteSpace(c.Busca))
        {
            var termo = c.Busca.Trim().ToLower();
            q = q.Where(m => db.Associados.Any(a => a.Id == m.AssociadoId
                && (a.Pessoa.NomeCompleto.ToLower().Contains(termo) || a.Matricula.ToLower().Contains(termo))));
        }

        var total = await q.CountAsync(ct);
        var ids = await q.OrderBy(m => m.Vencimento).ThenBy(m => m.Id)
            .Skip((c.Pagina - 1) * c.TamanhoPagina).Take(c.TamanhoPagina).Select(m => m.Id).ToListAsync(ct);
        var itens = await MontarAsync(ids, ct);
        return new Pagina<MensalidadeDto>(itens, c.Pagina, c.TamanhoPagina, total);
    }

    // ------------------------------------------------------------------ pagamentos

    public async Task<MensalidadeDto> PagarAsync(Guid id, RegistrarPagamentoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.RegistrarPagamentos, ct);
        await pagamentoValidator.ValidarAsync(r, ct);
        var m = await db.Mensalidades.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Mensalidade");

        var data = r.Data ?? Hoje;
        var encargos = r.IsentarEncargos ? 0m : CalculoFinanceiro.Encargos(m.Valor, m.Vencimento, data, await ConfigAsync(ct));
        var devido = m.Valor + encargos;
        var valor = r.Valor ?? devido;
        if (valor < devido)
            throw new DomainException("pagamento.valor_insuficiente",
                $"O valor informado é menor que o devido (R$ {devido:N2}{(encargos > 0 ? $", com R$ {encargos:N2} de multa e juros" : "")}).");

        m.MarcarPaga();
        var recibo = await sequencias.ProximoAsync(ClubeId, "recibo", ct);
        db.Pagamentos.Add(new Pagamento(ClubeId, m.Id, recibo, valor, valor - m.Valor, data, r.Forma, r.Observacao?.Trim()));
        await db.SaveChangesAsync(ct);
        return (await MontarAsync([m.Id], ct))[0];
    }

    public async Task<MensalidadeDto> EstornarPagamentoAsync(Guid pagamentoId, MotivoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        var p = await db.Pagamentos.Include(x => x.Mensalidade).FirstOrDefaultAsync(x => x.Id == pagamentoId && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Pagamento");
        p.Estornar(r.Motivo);
        p.Mensalidade.Estornar();
        await db.SaveChangesAsync(ct);
        return (await MontarAsync([p.MensalidadeId], ct))[0];
    }

    public Task<MensalidadeDto> IsentarAsync(Guid id, MotivoRequest r, CancellationToken ct) =>
        AlterarAsync(id, m => m.Isentar(r.Motivo), ct);

    public Task<MensalidadeDto> CancelarAsync(Guid id, MotivoRequest r, CancellationToken ct) =>
        AlterarAsync(id, m => m.Cancelar(r.Motivo), ct);

    private async Task<MensalidadeDto> AlterarAsync(Guid id, Action<Mensalidade> acao, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        var m = await db.Mensalidades.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Mensalidade");
        acao(m);
        await db.SaveChangesAsync(ct);
        return (await MontarAsync([id], ct))[0];
    }

    public async Task<ReciboDto> ReciboAsync(Guid pagamentoId, CancellationToken ct)
    {
        var p = await db.Pagamentos.AsNoTracking().Include(x => x.Mensalidade)
                    .FirstOrDefaultAsync(x => x.Id == pagamentoId && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Recibo");
        var associado = await db.Associados.AsNoTracking().Include(a => a.Pessoa).FirstAsync(a => a.Id == p.Mensalidade.AssociadoId, ct);

        // Tesouraria ou o próprio associado (portal).
        if (!await permissoes.PossuiAsync(Permissoes.Financeiro.Visualizar, ct))
            await acessoPessoas.ExigirAsync(associado.PessoaId, AcaoPessoa.VerDados, ct);

        var clube = await db.Clubes.AsNoTracking().Where(c => c.Id == ClubeId).Select(c => new { c.Nome, c.Cnpj }).FirstAsync(ct);
        var quem = await db.Usuarios.IgnoreQueryFilters().Where(u => u.Id == p.CriadoPor).Select(u => u.Nome).FirstOrDefaultAsync(ct);
        return new ReciboDto(p.NumeroRecibo, clube.Nome, clube.Cnpj, associado.Pessoa.NomeCompleto, associado.Matricula,
            p.Mensalidade.Competencia.ToString("yyyy-MM"), p.Mensalidade.Valor, p.Encargos, p.Valor, p.Data, p.Forma, quem,
            p.Estornado, p.MotivoEstorno);
    }

    // ------------------------------------------------------------------ situação financeira

    public async Task<SituacaoFinanceiraDto> SituacaoDoAssociadoAsync(Guid associadoId, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        if (!await db.Associados.AnyAsync(a => a.Id == associadoId && a.ClubeId == ClubeId, ct)) throw new NaoEncontradoException("Associado");
        return await SituacaoAsync(associadoId, ct);
    }

    /// <summary>Portal: o titular vê as próprias mensalidades (que já incluem a cobrança dos dependentes).</summary>
    public async Task<SituacaoFinanceiraDto> SituacaoNoPortalAsync(CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        var titular = portal.Associado ?? throw new AcessoNegadoException("A situação financeira fica disponível para o associado titular.");
        return await SituacaoAsync(titular.Id, ct);
    }

    private async Task<SituacaoFinanceiraDto> SituacaoAsync(Guid associadoId, CancellationToken ct)
    {
        var ids = await db.Mensalidades.Where(m => m.AssociadoId == associadoId && m.ClubeId == ClubeId)
            .OrderByDescending(m => m.Competencia).Take(12).Select(m => m.Id).ToListAsync(ct);
        var lista = await MontarAsync(ids, ct);
        var abertas = await db.Mensalidades.Where(m => m.AssociadoId == associadoId && m.ClubeId == ClubeId && m.Situacao == SituacaoMensalidade.Aberta)
            .Select(m => m.Id).ToListAsync(ct);
        var abertasDto = await MontarAsync(abertas, ct);
        var vencidas = abertasDto.Count(m => m.Situacao == "Vencida");
        return new SituacaoFinanceiraDto(vencidas == 0, abertasDto.Count, vencidas, abertasDto.Sum(m => m.ValorAtualizado),
            lista.OrderByDescending(m => m.Competencia).ToList());
    }

    // ------------------------------------------------------------------ auxiliares

    internal async Task<ConfiguracaoFinanceira> ConfigAsync(CancellationToken ct) =>
        await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync(c => c.ClubeId == ClubeId, ct)
        ?? throw new NaoEncontradoException("Configuração financeira");

    /// <summary>Monta os DTOs preservando a ordem dos ids, com encargos calculados para hoje.</summary>
    private async Task<List<MensalidadeDto>> MontarAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var cfg = await ConfigAsync(ct);
        var hoje = Hoje;
        var dados = await db.Mensalidades.AsNoTracking().Where(m => ids.Contains(m.Id))
            .Select(m => new
            {
                M = m,
                Nome = db.Associados.Where(a => a.Id == m.AssociadoId).Select(a => a.Pessoa.NomeCompleto).FirstOrDefault(),
                Matricula = db.Associados.Where(a => a.Id == m.AssociadoId).Select(a => a.Matricula).FirstOrDefault(),
                Contrato = db.ContratosAssociados.Where(c => c.Id == m.ContratoId).Select(c => c.Numero).FirstOrDefault(),
                Pagamento = db.Pagamentos.Where(p => p.MensalidadeId == m.Id && !p.Estornado)
                    .Select(p => new PagamentoResumoDto(p.Id, p.NumeroRecibo, p.Valor, p.Encargos, p.Data, p.Forma)).FirstOrDefault(),
            })
            .ToDictionaryAsync(x => x.M.Id, ct);

        return ids.Where(dados.ContainsKey).Select(id =>
        {
            var d = dados[id];
            var m = d.M;
            var vencida = m.Vencida(hoje);
            var (multa, juros) = vencida ? CalculoFinanceiro.Detalhar(m.Valor, m.Vencimento, hoje, cfg) : (0m, 0m);
            var encargos = multa + juros;
            return new MensalidadeDto(m.Id, m.AssociadoId, d.Nome ?? "—", d.Matricula ?? "—", m.Competencia.ToString("yyyy-MM"),
                m.Vencimento, m.ValorPlano, m.ValorDependentes, m.QuantidadeDependentes, m.Valor,
                vencida ? "Vencida" : m.Situacao.ToString(), encargos, m.Valor + encargos, m.Motivo, d.Pagamento,
                m.CriadoEm, multa, juros, d.Contrato);
        }).ToList();
    }

    private static ValidationException Invalido(string campo, string msg) => new([new ValidationFailure(campo, msg)]);
}
