using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Identidade;

public enum SituacaoUsuario
{
    Ativo = 1,
    /// <summary>Bloqueado por um administrador. Só um administrador desbloqueia.</summary>
    Bloqueado = 2,
    Inativo = 3,
}

/// <summary>
/// Conta de acesso global da plataforma. O acesso a cada clube é dado por <see cref="UsuarioClube"/>.
/// </summary>
public class Usuario : AuditableEntity, ISoftDelete
{
    private readonly List<UsuarioClube> _clubes = [];

    private Usuario() { }

    public Usuario(string nome, string email, bool superAdmin = false)
    {
        Nome = nome.Trim();
        DefinirEmail(email);
        SuperAdmin = superAdmin;
        Situacao = SituacaoUsuario.Ativo;
        RenovarCarimboSeguranca();
    }

    public string Nome { get; set; } = null!;
    public string Email { get; private set; } = null!;
    /// <summary>E-mail em minúsculas, usado para unicidade e login.</summary>
    public string EmailNormalizado { get; private set; } = null!;
    public string? SenhaHash { get; private set; }
    public bool SuperAdmin { get; private set; }
    public SituacaoUsuario Situacao { get; private set; }
    public int TentativasFalhas { get; private set; }
    public DateTimeOffset? BloqueadoAte { get; private set; }
    public DateTimeOffset? UltimoAcessoEm { get; private set; }
    public DateTimeOffset? SenhaAlteradaEm { get; private set; }
    public bool DeveTrocarSenha { get; set; }
    /// <summary>Muda sempre que credenciais mudam; invalida tokens de recuperação antigos.</summary>
    public string CarimboSeguranca { get; private set; } = null!;
    public Guid? ClubePadraoId { get; set; }

    public IReadOnlyCollection<UsuarioClube> Clubes => _clubes;

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    public void DefinirEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("usuario.email_invalido", "E-mail inválido.");
        Email = email.Trim();
        EmailNormalizado = NormalizarEmail(email);
    }

    public void DefinirSenhaHash(string hash, DateTimeOffset agora)
    {
        SenhaHash = hash;
        SenhaAlteradaEm = agora;
        RenovarCarimboSeguranca();
    }

    public bool EstaBloqueadoTemporariamente(DateTimeOffset agora) => BloqueadoAte.HasValue && BloqueadoAte > agora;

    public bool PodeAutenticar(DateTimeOffset agora) =>
        !Excluido && Situacao == SituacaoUsuario.Ativo && !EstaBloqueadoTemporariamente(agora);

    /// <summary>Registra falha de senha; retorna true se a conta foi bloqueada temporariamente agora.</summary>
    public bool RegistrarFalhaLogin(DateTimeOffset agora, int maxTentativas, TimeSpan duracaoBloqueio)
    {
        TentativasFalhas++;
        if (TentativasFalhas < maxTentativas) return false;
        BloqueadoAte = agora.Add(duracaoBloqueio);
        TentativasFalhas = 0;
        return true;
    }

    public void RegistrarLoginSucesso(DateTimeOffset agora)
    {
        TentativasFalhas = 0;
        BloqueadoAte = null;
        UltimoAcessoEm = agora;
    }

    public void Bloquear()
    {
        Situacao = SituacaoUsuario.Bloqueado;
        RenovarCarimboSeguranca();
    }

    public void Desbloquear()
    {
        Situacao = SituacaoUsuario.Ativo;
        TentativasFalhas = 0;
        BloqueadoAte = null;
    }

    public void DefinirSuperAdmin(bool valor) => SuperAdmin = valor;

    public void RenovarCarimboSeguranca() => CarimboSeguranca = Convert.ToHexString(Guid.NewGuid().ToByteArray());

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
        Situacao = SituacaoUsuario.Inativo;
        RenovarCarimboSeguranca();
    }
}
