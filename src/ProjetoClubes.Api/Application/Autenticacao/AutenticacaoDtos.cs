namespace ProjetoClubes.Application.Autenticacao;

public sealed record LoginRequest(string Email, string Senha, Guid? ClubeId = null, string? Dispositivo = null);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record TrocarClubeRequest(Guid ClubeId);

public sealed record EsqueciSenhaRequest(string Email);

public sealed record RedefinirSenhaRequest(string Token, string NovaSenha);

public sealed record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);

public sealed record ClubeResumoDto(Guid Id, string Nome, string Sigla, string CorPrimaria, string? NomeExibicao,
    bool Demonstracao);

public sealed record UsuarioLogadoDto(
    Guid Id,
    string Nome,
    string Email,
    bool SuperAdmin,
    bool DeveTrocarSenha,
    ClubeResumoDto? ClubeAtual,
    IReadOnlyList<ClubeResumoDto> Clubes,
    IReadOnlyList<string> Permissoes);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiraEm,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiraEm,
    UsuarioLogadoDto Usuario);

public sealed record SessaoDto(Guid Id, DateTimeOffset CriadaEm, DateTimeOffset UltimoUsoEm, DateTimeOffset ExpiraEm,
    string? Ip, string? Dispositivo, string? UserAgent, bool Atual);

public sealed record HistoricoAcessoDto(Guid Id, string Evento, bool Sucesso, DateTimeOffset OcorridoEm,
    string? Ip, string? UserAgent, string? Detalhe);
