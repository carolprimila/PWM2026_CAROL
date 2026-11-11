using FluentValidation;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.Application.Financeiro;

public sealed record ConfiguracaoFinanceiraDto(decimal MultaPercentual, decimal JurosMensalPercentual, int DiasTolerancia);

public sealed record PlanoDto(Guid Id, string Nome, Guid CategoriaId, string Categoria, decimal ValorMensal, int DiaVencimento,
    bool Ativo, int AssociadosAtivos);

public sealed record SalvarPlanoRequest(string Nome, Guid CategoriaId, decimal ValorMensal, int DiaVencimento, bool Ativo = true);

public sealed record GerarMensalidadesRequest(string Competencia);

public sealed record GerarMensalidadesResultado(string Competencia, int Geradas, int JaExistentes, decimal ValorTotal,
    IReadOnlyList<string> SemPlano);

public sealed record PagamentoResumoDto(Guid Id, long NumeroRecibo, decimal Valor, decimal Encargos, DateOnly Data,
    FormaPagamento Forma);

public sealed record MensalidadeDto(
    Guid Id,
    Guid AssociadoId,
    string AssociadoNome,
    string Matricula,
    string Competencia,
    DateOnly Vencimento,
    decimal ValorPlano,
    decimal ValorDependentes,
    int QuantidadeDependentes,
    decimal Valor,
    // Aberta | Vencida | Paga | Isenta | Cancelada
    string Situacao,
    decimal Encargos,
    decimal ValorAtualizado,
    string? Motivo,
    PagamentoResumoDto? Pagamento);

public sealed class ConsultaMensalidades : ConsultaPaginada
{
    // "yyyy-MM"
    public string? Competencia { get; set; }
    // Aberta | Vencida | Paga | Isenta | Cancelada
    public string? Situacao { get; set; }
    public Guid? AssociadoId { get; set; }
}

public sealed record RegistrarPagamentoRequest(decimal? Valor, DateOnly? Data, FormaPagamento Forma, string? Observacao,
    bool IsentarEncargos = false);

public sealed record MotivoRequest(string Motivo);

public sealed record ReciboDto(long NumeroRecibo, string Clube, string? ClubeCnpj, string Associado, string Matricula,
    string Competencia, decimal ValorMensalidade, decimal Encargos, decimal ValorPago, DateOnly Data, FormaPagamento Forma,
    string? RegistradoPor, bool Estornado, string? MotivoEstorno);

public sealed record CategoriaFinanceiraDto(Guid Id, string Nome, TipoLancamento Tipo);

public sealed record LancamentoDto(Guid Id, TipoLancamento Tipo, Guid CategoriaId, string Categoria, string Descricao,
    decimal Valor, DateOnly Vencimento, DateOnly? PagoEm, string Situacao);

public sealed record SalvarLancamentoRequest(TipoLancamento Tipo, Guid CategoriaId, string Descricao, decimal Valor,
    DateOnly Vencimento, DateOnly? PagoEm);

public sealed class ConsultaLancamentos : ConsultaPaginada
{
    public TipoLancamento? Tipo { get; set; }
    public bool? Pago { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
}

public sealed record InadimplenciaDto(int Associados, int Mensalidades, decimal Valor);

public sealed record ResumoFinanceiroDto(DateOnly De, DateOnly Ate, decimal ReceitasMensalidades, decimal OutrasReceitas,
    decimal Despesas, decimal Saldo, decimal AReceberMensalidades, decimal AReceberOutros, decimal APagar,
    InadimplenciaDto Inadimplencia);

public sealed record MovimentoCaixaDto(DateOnly Data, string Descricao, TipoLancamento Tipo, decimal Valor, decimal SaldoAcumulado);

public sealed record FluxoCaixaDto(DateOnly De, DateOnly Ate, decimal Entradas, decimal Saidas, decimal Saldo,
    IReadOnlyList<MovimentoCaixaDto> Movimentos);

public sealed record InadimplenteDto(Guid AssociadoId, string Nome, string Matricula, int Mensalidades, decimal ValorDevido,
    DateOnly VencimentoMaisAntigo);

public sealed record SituacaoFinanceiraDto(bool Adimplente, int EmAberto, int Vencidas, decimal TotalDevido,
    IReadOnlyList<MensalidadeDto> Mensalidades);

public sealed class SalvarPlanoRequestValidator : AbstractValidator<SalvarPlanoRequest>
{
    public SalvarPlanoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do plano.").MaximumLength(80);
        RuleFor(x => x.CategoriaId).NotEmpty().WithMessage("Selecione a categoria.");
        RuleFor(x => x.ValorMensal).GreaterThanOrEqualTo(0).PrecisionScale(12, 2, true).WithMessage("Valor inválido.");
        RuleFor(x => x.DiaVencimento).InclusiveBetween(1, 28).WithMessage("O dia de vencimento deve estar entre 1 e 28.");
    }
}

public sealed class GerarMensalidadesRequestValidator : AbstractValidator<GerarMensalidadesRequest>
{
    public GerarMensalidadesRequestValidator() =>
        RuleFor(x => x.Competencia).NotEmpty().Matches(@"^\d{4}-(0[1-9]|1[0-2])$").WithMessage("Informe a competência no formato AAAA-MM.");
}

public sealed class RegistrarPagamentoRequestValidator : AbstractValidator<RegistrarPagamentoRequest>
{
    public RegistrarPagamentoRequestValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0).When(x => x.Valor.HasValue).WithMessage("O valor deve ser maior que zero.");
        RuleFor(x => x.Data).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
            .When(x => x.Data.HasValue).WithMessage("A data do pagamento não pode ser futura.");
        RuleFor(x => x.Forma).IsInEnum();
        RuleFor(x => x.Observacao).MaximumLength(300);
    }
}

public sealed class SalvarLancamentoRequestValidator : AbstractValidator<SalvarLancamentoRequest>
{
    public SalvarLancamentoRequestValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.CategoriaId).NotEmpty().WithMessage("Selecione a categoria.");
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("Informe a descrição.").MaximumLength(200);
        RuleFor(x => x.Valor).GreaterThan(0).WithMessage("O valor deve ser maior que zero.").PrecisionScale(12, 2, true);
    }
}

public sealed class ConfiguracaoFinanceiraDtoValidator : AbstractValidator<ConfiguracaoFinanceiraDto>
{
    public ConfiguracaoFinanceiraDtoValidator()
    {
        RuleFor(x => x.MultaPercentual).InclusiveBetween(0, 20).WithMessage("Multa entre 0% e 20%.");
        RuleFor(x => x.JurosMensalPercentual).InclusiveBetween(0, 10).WithMessage("Juros entre 0% e 10% ao mês.");
        RuleFor(x => x.DiasTolerancia).InclusiveBetween(0, 60);
    }
}
