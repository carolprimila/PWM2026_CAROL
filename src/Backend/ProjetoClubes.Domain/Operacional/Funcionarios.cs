using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Operacional;

public enum SituacaoFuncionario
{
    Ativo = 1,
    Ferias = 2,
    Afastado = 3,
    Desligado = 4,
}

public enum TipoAusencia
{
    Ferias = 1,
    Afastamento = 2,
}

/// <summary>
/// Funcionário do clube. Cargo e departamento são texto livre (o clube usa os nomes que quiser); o acesso ao sistema,
/// quando houver, é um usuário comum do clube com perfil próprio (ex.: Portaria) ligado por <see cref="UsuarioId"/>.
/// </summary>
public class Funcionario : AuditableEntity, ITenantScoped, ISoftDelete
{
    private readonly List<AusenciaFuncionario> _ausencias = [];

    private Funcionario() { }

    public Funcionario(Guid clubeId, string nome, string? cpf, string cargo, string? departamento, DateOnly admissao)
    {
        ClubeId = clubeId;
        Situacao = SituacaoFuncionario.Ativo;
        AlterarDados(nome, cpf, cargo, departamento, admissao);
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; private set; } = null!;
    /// <summary>CPF só com dígitos (opcional).</summary>
    public string? Cpf { get; private set; }
    public string Cargo { get; private set; } = null!;
    public string? Departamento { get; private set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    /// <summary>Escala de trabalho em texto (ex.: "Seg a Sex, 8h às 17h").</summary>
    public string? Escala { get; set; }
    public string? Observacoes { get; set; }
    public DateOnly Admissao { get; private set; }
    public DateOnly? Desligamento { get; private set; }
    public SituacaoFuncionario Situacao { get; private set; }
    public Guid? UsuarioId { get; set; }
    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public IReadOnlyCollection<AusenciaFuncionario> Ausencias => _ausencias;

    public void AlterarDados(string nome, string? cpf, string cargo, string? departamento, DateOnly admissao)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("funcionario.nome", "Informe o nome.");
        if (string.IsNullOrWhiteSpace(cargo)) throw new DomainException("funcionario.cargo", "Informe o cargo.");
        var digitos = string.IsNullOrWhiteSpace(cpf) ? null : Documentos.SomenteDigitos(cpf);
        if (digitos is not null && !Documentos.CpfValido(digitos)) throw new DomainException("funcionario.cpf", "CPF inválido.");
        Nome = nome.Trim();
        Cpf = digitos;
        Cargo = cargo.Trim();
        Departamento = string.IsNullOrWhiteSpace(departamento) ? null : departamento.Trim();
        Admissao = admissao;
    }

    /// <summary>Registra férias ou afastamento. Não pode sobrepor outro período do mesmo funcionário.</summary>
    public AusenciaFuncionario RegistrarAusencia(TipoAusencia tipo, DateOnly inicio, DateOnly fim, string? observacao)
    {
        if (Situacao == SituacaoFuncionario.Desligado) throw new DomainException("funcionario.desligado", "Funcionário desligado.");
        if (fim < inicio) throw new DomainException("funcionario.periodo", "O fim deve ser igual ou posterior ao início.");
        if (_ausencias.Any(a => !a.Cancelada && a.Inicio <= fim && inicio <= a.Fim))
            throw new DomainException("funcionario.ausencia_sobreposta", "Já existe férias ou afastamento neste período.");
        var a = new AusenciaFuncionario(ClubeId, Id, tipo, inicio, fim, observacao);
        _ausencias.Add(a);
        return a;
    }

    /// <summary>Situação do dia: desligado prevalece; senão férias/afastamento em curso; senão ativo.</summary>
    public void AtualizarSituacao(DateOnly hoje)
    {
        if (Situacao == SituacaoFuncionario.Desligado) return;
        var atual = _ausencias.FirstOrDefault(a => !a.Cancelada && a.Inicio <= hoje && hoje <= a.Fim);
        Situacao = atual is null ? SituacaoFuncionario.Ativo
            : atual.Tipo == TipoAusencia.Ferias ? SituacaoFuncionario.Ferias : SituacaoFuncionario.Afastado;
    }

    public void Desligar(DateOnly data)
    {
        if (Situacao == SituacaoFuncionario.Desligado) throw new DomainException("funcionario.desligado", "Funcionário já desligado.");
        if (data < Admissao) throw new DomainException("funcionario.periodo", "O desligamento não pode ser antes da admissão.");
        Situacao = SituacaoFuncionario.Desligado;
        Desligamento = data;
    }

    public void Readmitir(DateOnly data)
    {
        if (Situacao != SituacaoFuncionario.Desligado) throw new DomainException("funcionario.ativo", "Funcionário não está desligado.");
        Situacao = SituacaoFuncionario.Ativo;
        Desligamento = null;
        Admissao = data;
    }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

public class AusenciaFuncionario : Entity, ITenantScoped
{
    private AusenciaFuncionario() { }

    internal AusenciaFuncionario(Guid clubeId, Guid funcionarioId, TipoAusencia tipo, DateOnly inicio, DateOnly fim, string? observacao)
    {
        ClubeId = clubeId;
        FuncionarioId = funcionarioId;
        Tipo = tipo;
        Inicio = inicio;
        Fim = fim;
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
    }

    public Guid ClubeId { get; private set; }
    public Guid FuncionarioId { get; private set; }
    public TipoAusencia Tipo { get; private set; }
    public DateOnly Inicio { get; private set; }
    public DateOnly Fim { get; private set; }
    public string? Observacao { get; private set; }
    public bool Cancelada { get; private set; }

    public int Dias => Fim.DayNumber - Inicio.DayNumber + 1;

    public void Cancelar() => Cancelada = true;
}

public class HistoricoFuncionario : RegistroHistorico
{
    private HistoricoFuncionario() { }

    public HistoricoFuncionario(Guid clubeId, Guid funcionarioId, string evento, string descricao, Guid? usuarioId, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        FuncionarioId = funcionarioId;
        Evento = evento;
        Descricao = descricao;
        UsuarioId = usuarioId;
        OcorridoEm = agora;
    }

    public Guid FuncionarioId { get; private set; }
}
