using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Pessoas;

public enum Sexo
{
    NaoInformado = 0,
    Masculino = 1,
    Feminino = 2,
    Outro = 3,
}

public enum EstadoCivil
{
    Solteiro = 1,
    Casado = 2,
    UniaoEstavel = 3,
    Divorciado = 4,
    Separado = 5,
    Viuvo = 6,
}

/// <summary>
/// Dados pessoais de quem participa do clube (associado ou dependente). Uma pessoa por CPF em cada clube:
/// o mesmo ser humano em dois clubes são dois registros, para que um clube nunca altere dados vistos por outro.
/// </summary>
public class Pessoa : AuditableEntity, ITenantScoped, ISoftDelete
{
    private Pessoa() { }

    public Pessoa(Guid clubeId, string nomeCompleto, string tokenCarteirinha, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        NomeCompleto = nomeCompleto.Trim();
        RegenerarTokenCarteirinha(tokenCarteirinha, agora);
    }

    public Guid ClubeId { get; private set; }
    public string NomeCompleto { get; set; } = null!;
    /// <summary>Somente dígitos.</summary>
    public string? Cpf { get; private set; }
    public string? Rg { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public Sexo Sexo { get; set; }
    public EstadoCivil? EstadoCivil { get; set; }
    public Endereco Endereco { get; set; } = new();
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    /// <summary>Qualificação/profissão (texto livre).</summary>
    public string? Profissao { get; set; }
    public Guid? FotoArquivoId { get; set; }
    /// <summary>Conta de acesso ao aplicativo, quando houver.</summary>
    public Guid? UsuarioId { get; set; }
    /// <summary>Identificador opaco impresso no QR Code. Não contém dados pessoais; validado no servidor.</summary>
    public string TokenCarteirinha { get; private set; } = null!;
    public DateTimeOffset TokenCarteirinhaGeradoEm { get; private set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }
    /// <summary>LGPD: dados pessoais apagados (o registro fica só para manter o histórico financeiro e de participação).</summary>
    public DateTimeOffset? AnonimizadaEm { get; private set; }

    /// <summary>
    /// Apaga os dados pessoais de forma irreversível. O token da carteirinha é trocado (o QR antigo deixa de valer)
    /// e o vínculo com o login é desfeito.
    /// </summary>
    public void Anonimizar(string novoTokenCarteirinha, DateTimeOffset agora)
    {
        if (AnonimizadaEm is not null) throw new DomainException("pessoa.anonimizada", "Os dados desta pessoa já foram anonimizados.");
        NomeCompleto = $"Pessoa anonimizada {Id.ToString("N")[..8]}";
        Cpf = null;
        Rg = null;
        DataNascimento = null;
        EstadoCivil = null;
        Endereco = new Endereco();
        Telefone = null;
        Email = null;
        Profissao = null;
        FotoArquivoId = null;
        UsuarioId = null;
        RegenerarTokenCarteirinha(novoTokenCarteirinha, agora);
        AnonimizadaEm = agora;
    }

    public void DefinirCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            Cpf = null;
            return;
        }
        if (!Documentos.CpfValido(cpf)) throw new DomainException("pessoa.cpf_invalido", "CPF inválido.");
        Cpf = Documentos.SomenteDigitos(cpf);
    }

    public void RegenerarTokenCarteirinha(string token, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 20)
            throw new DomainException("pessoa.token_invalido", "Token de carteirinha inválido.");
        TokenCarteirinha = token;
        TokenCarteirinhaGeradoEm = agora;
    }

    public int? Idade(DateOnly hoje) => DataNascimento is { } n ? CalcularIdade(n, hoje) : null;

    public bool MenorDeIdade(DateOnly hoje, int maioridade) => Idade(hoje) is { } i && i < maioridade;

    public static int CalcularIdade(DateOnly nascimento, DateOnly hoje)
    {
        var idade = hoje.Year - nascimento.Year;
        if (hoje < nascimento.AddYears(idade)) idade--;
        return idade;
    }

    /// <summary>CPF parcialmente oculto para listagens (LGPD): ***.982.247-**</summary>
    public static string? MascararCpf(string? cpf) =>
        cpf is { Length: 11 } ? $"***.{cpf[3..6]}.{cpf[6..9]}-**" : null;

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}
