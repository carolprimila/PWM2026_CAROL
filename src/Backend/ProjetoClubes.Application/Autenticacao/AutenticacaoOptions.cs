namespace ProjetoClubes.Application.Autenticacao;

public sealed class AutenticacaoOptions
{
    public const string Secao = "Autenticacao";

    /// <summary>Falhas consecutivas até o bloqueio temporário.</summary>
    public int MaxTentativasFalhas { get; set; } = 5;
    public int MinutosBloqueio { get; set; } = 15;
    public int MinutosValidadeTokenRecuperacao { get; set; } = 60;
    /// <summary>URL do Web usada no link de recuperação de senha.</summary>
    public string UrlRedefinicaoSenha { get; set; } = "http://localhost:5278/redefinir-senha";
}
