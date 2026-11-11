using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Autenticacao;

public interface IAutenticacaoService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<TokenResponse> RenovarAsync(RefreshRequest request, CancellationToken ct);
    Task LogoutAsync(CancellationToken ct);
    Task<TokenResponse> TrocarClubeAsync(TrocarClubeRequest request, CancellationToken ct);
    Task<UsuarioLogadoDto> ObterUsuarioLogadoAsync(CancellationToken ct);
    Task SolicitarRecuperacaoSenhaAsync(EsqueciSenhaRequest request, CancellationToken ct);
    Task RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken ct);
    Task AlterarSenhaAsync(AlterarSenhaRequest request, CancellationToken ct);
    Task<IReadOnlyList<SessaoDto>> ListarSessoesAsync(CancellationToken ct);
    Task RevogarSessaoAsync(Guid sessaoId, CancellationToken ct);
    Task<IReadOnlyList<HistoricoAcessoDto>> ListarMeuHistoricoAsync(int limite, CancellationToken ct);
}

public sealed class AutenticacaoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IHashSenha hashSenha,
    ITokenService tokens,
    IEmailSender email,
    IPermissaoResolver permissoes,
    IOptions<AutenticacaoOptions> opcoes,
    TimeProvider relogio,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<EsqueciSenhaRequest> esqueciValidator,
    IValidator<RedefinirSenhaRequest> redefinirValidator,
    IValidator<AlterarSenhaRequest> alterarValidator,
    ILogger<AutenticacaoService> logger) : IAutenticacaoService
{
    private const string MensagemCredenciaisInvalidas = "E-mail ou senha incorretos.";
    private readonly AutenticacaoOptions _opcoes = opcoes.Value;

    // Hash fictício para equalizar o tempo de resposta quando o e-mail não existe.
    private static string? _hashFicticio;

    private DateTimeOffset Agora => relogio.GetUtcNow();

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidarAsync(request, ct);
        var emailNormalizado = Usuario.NormalizarEmail(request.Email);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == emailNormalizado, ct);

        if (usuario is null || usuario.SenhaHash is null)
        {
            _hashFicticio ??= hashSenha.Gerar(Guid.NewGuid().ToString());
            hashSenha.Verificar(_hashFicticio, request.Senha);
            await RegistrarAsync(EventoAcesso.LoginFalha, false, usuario?.Id, request.Email, null, null,
                "Usuário inexistente ou sem senha definida", ct);
            throw new NaoAutenticadoException(MensagemCredenciaisInvalidas, "credenciais_invalidas");
        }

        if (usuario.EstaBloqueadoTemporariamente(Agora))
        {
            await RegistrarAsync(EventoAcesso.LoginFalha, false, usuario.Id, request.Email, null, null,
                "Tentativa durante bloqueio temporário", ct);
            throw new ContaBloqueadaException(
                $"Muitas tentativas sem sucesso. Tente novamente após {usuario.BloqueadoAte:HH:mm} (UTC) ou recupere sua senha.");
        }

        var (valida, precisaRehash) = hashSenha.Verificar(usuario.SenhaHash, request.Senha);
        if (!valida)
        {
            var bloqueou = usuario.RegistrarFalhaLogin(Agora, _opcoes.MaxTentativasFalhas,
                TimeSpan.FromMinutes(_opcoes.MinutosBloqueio));
            Historico(EventoAcesso.LoginFalha, false, usuario.Id, request.Email, null, null, "Senha incorreta");
            if (bloqueou)
                Historico(EventoAcesso.BloqueioTemporario, true, usuario.Id, request.Email, null, null,
                    $"Bloqueado por {_opcoes.MinutosBloqueio} minutos");
            await db.SaveChangesAsync(ct);
            throw new NaoAutenticadoException(MensagemCredenciaisInvalidas, "credenciais_invalidas");
        }

        if (usuario.Situacao != SituacaoUsuario.Ativo)
        {
            await RegistrarAsync(EventoAcesso.LoginFalha, false, usuario.Id, request.Email, null, null,
                $"Usuário {usuario.Situacao}", ct);
            throw new NaoAutenticadoException(
                "Seu acesso está bloqueado ou inativo. Procure a administração do clube.", "usuario_bloqueado");
        }

        var clubeId = await EscolherClubeAsync(usuario, request.ClubeId, ct);

        if (precisaRehash) usuario.DefinirSenhaHash(hashSenha.Gerar(request.Senha), usuario.SenhaAlteradaEm ?? Agora);
        usuario.RegistrarLoginSucesso(Agora);

        var refresh = tokens.GerarTokenOpaco();
        var sessao = new SessaoUsuario(usuario.Id, clubeId, tokens.Hash(refresh), Agora,
            Agora.Add(tokens.DuracaoRefreshToken), atual.Ip, Truncar(atual.UserAgent, 500), Truncar(request.Dispositivo, 100));
        db.SessoesUsuarios.Add(sessao);
        Historico(EventoAcesso.LoginSucesso, true, usuario.Id, request.Email, clubeId, sessao.Id, null);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Login do usuário {UsuarioId} na sessão {SessaoId} (clube {ClubeId})",
            usuario.Id, sessao.Id, clubeId);
        return await MontarRespostaAsync(usuario, sessao, refresh, ct);
    }

    public async Task<TokenResponse> RenovarAsync(RefreshRequest request, CancellationToken ct)
    {
        await refreshValidator.ValidarAsync(request, ct);
        var hash = tokens.Hash(request.RefreshToken);

        var sessao = await db.SessoesUsuarios.Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);

        if (sessao is null)
        {
            // Reutilização de um refresh token já rotacionado: possível roubo. Revoga a sessão inteira.
            var reutilizada = await db.SessoesUsuarios.FirstOrDefaultAsync(s => s.RefreshTokenHashAnterior == hash, ct);
            if (reutilizada is not null && reutilizada.RevogadaEm is null)
            {
                reutilizada.Revogar(Agora, "Reutilização de refresh token detectada");
                Historico(EventoAcesso.ReusoRefreshToken, false, reutilizada.UsuarioId, null, reutilizada.ClubeId,
                    reutilizada.Id, "Sessão revogada por segurança");
                await db.SaveChangesAsync(ct);
                logger.LogWarning("Reutilização de refresh token na sessão {SessaoId}; sessão revogada", reutilizada.Id);
            }
            throw new NaoAutenticadoException("Sessão expirada. Entre novamente.", "sessao_invalida");
        }

        if (!sessao.Ativa(Agora) || !sessao.Usuario.PodeAutenticar(Agora))
            throw new NaoAutenticadoException("Sessão expirada. Entre novamente.", "sessao_invalida");

        if (sessao.ClubeId is { } clubeId && !await PodeAcessarClubeAsync(sessao.Usuario, clubeId, ct))
        {
            sessao.Revogar(Agora, "Acesso ao clube removido");
            await db.SaveChangesAsync(ct);
            throw new NaoAutenticadoException("Seu acesso a este clube foi removido. Entre novamente.", "sessao_invalida");
        }

        var novoRefresh = tokens.GerarTokenOpaco();
        sessao.Rotacionar(tokens.Hash(novoRefresh), Agora, Agora.Add(tokens.DuracaoRefreshToken));
        Historico(EventoAcesso.TokenRenovado, true, sessao.UsuarioId, null, sessao.ClubeId, sessao.Id, null);
        await db.SaveChangesAsync(ct);
        return await MontarRespostaAsync(sessao.Usuario, sessao, novoRefresh, ct);
    }

    public async Task LogoutAsync(CancellationToken ct)
    {
        var sessao = await ObterSessaoAtualAsync(ct);
        sessao.Revogar(Agora, "Logout");
        Historico(EventoAcesso.Logout, true, sessao.UsuarioId, null, sessao.ClubeId, sessao.Id, null);
        await db.SaveChangesAsync(ct);
    }

    public async Task<TokenResponse> TrocarClubeAsync(TrocarClubeRequest request, CancellationToken ct)
    {
        var sessao = await ObterSessaoAtualAsync(ct);
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == sessao.UsuarioId, ct);

        if (!await PodeAcessarClubeAsync(usuario, request.ClubeId, ct))
            throw new AcessoNegadoException("Você não tem acesso a este clube.");

        sessao.TrocarClube(request.ClubeId);
        var novoRefresh = tokens.GerarTokenOpaco();
        sessao.Rotacionar(tokens.Hash(novoRefresh), Agora, Agora.Add(tokens.DuracaoRefreshToken));
        usuario.ClubePadraoId = request.ClubeId;
        Historico(EventoAcesso.TrocaClube, true, usuario.Id, null, request.ClubeId, sessao.Id, null);
        await db.SaveChangesAsync(ct);
        return await MontarRespostaAsync(usuario, sessao, novoRefresh, ct);
    }

    public async Task<UsuarioLogadoDto> ObterUsuarioLogadoAsync(CancellationToken ct)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == atual.UsuarioIdObrigatorio, ct)
                      ?? throw new NaoAutenticadoException("Sessão inválida.");
        return await MontarUsuarioAsync(usuario, atual.ClubeId, ct);
    }

    public async Task SolicitarRecuperacaoSenhaAsync(EsqueciSenhaRequest request, CancellationToken ct)
    {
        await esqueciValidator.ValidarAsync(request, ct);
        var emailNormalizado = Usuario.NormalizarEmail(request.Email);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == emailNormalizado, ct);

        // Resposta idêntica exista ou não o e-mail (não revela contas cadastradas).
        if (usuario is null || usuario.Situacao != SituacaoUsuario.Ativo)
        {
            await RegistrarAsync(EventoAcesso.RecuperacaoSolicitada, false, usuario?.Id, request.Email, null, null,
                "E-mail inexistente ou usuário não ativo", ct);
            return;
        }

        var pendentes = await db.TokensRecuperacaoSenha
            .Where(t => t.UsuarioId == usuario.Id && t.UsadoEm == null && t.ExpiraEm > Agora)
            .ToListAsync(ct);
        foreach (var p in pendentes) p.MarcarUsado(Agora); // só o link mais recente vale

        var token = tokens.GerarTokenOpaco();
        db.TokensRecuperacaoSenha.Add(new TokenRecuperacaoSenha(usuario.Id, tokens.Hash(token),
            usuario.CarimboSeguranca, Agora, Agora.AddMinutes(_opcoes.MinutosValidadeTokenRecuperacao), atual.Ip));
        Historico(EventoAcesso.RecuperacaoSolicitada, true, usuario.Id, request.Email, null, null, null);
        await db.SaveChangesAsync(ct);

        var link = $"{_opcoes.UrlRedefinicaoSenha}?token={Uri.EscapeDataString(token)}";
        await email.EnviarAsync(new MensagemEmail(usuario.Email, "Projeto Clubes — redefinição de senha",
            $"Olá, {usuario.Nome}.\n\nRecebemos um pedido para redefinir sua senha. Acesse o link abaixo " +
            $"(válido por {_opcoes.MinutosValidadeTokenRecuperacao} minutos):\n\n{link}\n\n" +
            "Se você não fez este pedido, ignore esta mensagem."), ct);
    }

    public async Task RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken ct)
    {
        await redefinirValidator.ValidarAsync(request, ct);
        var hash = tokens.Hash(request.Token);
        var registro = await db.TokensRecuperacaoSenha.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        var usuario = registro is null ? null : await db.Usuarios.FirstOrDefaultAsync(u => u.Id == registro.UsuarioId, ct);

        if (registro is null || usuario is null || !registro.Valido(Agora, usuario.CarimboSeguranca)
            || usuario.Situacao != SituacaoUsuario.Ativo)
            throw new ConflitoException("token_invalido", "Este link de redefinição é inválido ou expirou. Solicite um novo.");

        registro.MarcarUsado(Agora);
        usuario.DefinirSenhaHash(hashSenha.Gerar(request.NovaSenha), Agora);
        usuario.DeveTrocarSenha = false;
        usuario.Desbloquear();
        await RevogarSessoesAsync(usuario.Id, exceto: null, "Senha redefinida", ct);
        Historico(EventoAcesso.RecuperacaoConcluida, true, usuario.Id, usuario.Email, null, null, null);
        await db.SaveChangesAsync(ct);
    }

    public async Task AlterarSenhaAsync(AlterarSenhaRequest request, CancellationToken ct)
    {
        await alterarValidator.ValidarAsync(request, ct);
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == atual.UsuarioIdObrigatorio, ct);
        if (usuario.SenhaHash is null || !hashSenha.Verificar(usuario.SenhaHash, request.SenhaAtual).Valida)
        {
            var erro = new FluentValidation.Results.ValidationFailure(nameof(request.SenhaAtual), "Senha atual incorreta.");
            throw new ValidationException([erro]);
        }

        usuario.DefinirSenhaHash(hashSenha.Gerar(request.NovaSenha), Agora);
        usuario.DeveTrocarSenha = false;
        await RevogarSessoesAsync(usuario.Id, exceto: atual.SessaoId, "Senha alterada", ct);
        Historico(EventoAcesso.SenhaAlterada, true, usuario.Id, null, atual.ClubeId, atual.SessaoId, null);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SessaoDto>> ListarSessoesAsync(CancellationToken ct)
    {
        var usuarioId = atual.UsuarioIdObrigatorio;
        var agora = Agora;
        return await db.SessoesUsuarios.AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId && s.RevogadaEm == null && s.ExpiraEm > agora)
            .OrderByDescending(s => s.UltimoUsoEm)
            .Select(s => new SessaoDto(s.Id, s.CriadaEm, s.UltimoUsoEm, s.ExpiraEm, s.Ip, s.Dispositivo, s.UserAgent,
                s.Id == atual.SessaoId))
            .ToListAsync(ct);
    }

    public async Task RevogarSessaoAsync(Guid sessaoId, CancellationToken ct)
    {
        var usuarioId = atual.UsuarioIdObrigatorio;
        var sessao = await db.SessoesUsuarios.FirstOrDefaultAsync(s => s.Id == sessaoId && s.UsuarioId == usuarioId, ct)
                     ?? throw new NaoEncontradoException("Sessão");
        sessao.Revogar(Agora, "Revogada pelo usuário");
        Historico(EventoAcesso.SessaoRevogada, true, usuarioId, null, sessao.ClubeId, sessao.Id, "Pelo próprio usuário");
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<HistoricoAcessoDto>> ListarMeuHistoricoAsync(int limite, CancellationToken ct)
    {
        var usuarioId = atual.UsuarioIdObrigatorio;
        var lista = await db.HistoricoAcessos.AsNoTracking()
            .Where(h => h.UsuarioId == usuarioId)
            .OrderByDescending(h => h.OcorridoEm)
            .Take(Math.Clamp(limite, 1, 200))
            .ToListAsync(ct);
        return lista.Select(h => new HistoricoAcessoDto(h.Id, h.Evento.ToString(), h.Sucesso, h.OcorridoEm, h.Ip,
            h.UserAgent, h.Detalhe)).ToList();
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<Guid?> EscolherClubeAsync(Usuario usuario, Guid? solicitado, CancellationToken ct)
    {
        if (solicitado is { } id)
        {
            if (!await PodeAcessarClubeAsync(usuario, id, ct))
                throw new AcessoNegadoException("Você não tem acesso ao clube selecionado.");
            return id;
        }

        var disponiveis = await ClubesAcessiveisQuery(usuario).Select(c => c.Id).ToListAsync(ct);
        if (usuario.ClubePadraoId is { } padrao && disponiveis.Contains(padrao)) return padrao;
        if (disponiveis.Count > 0) return disponiveis[0];
        if (usuario.SuperAdmin) return null; // Superadmin sem clube: acessa a área da plataforma.

        await RegistrarAsync(EventoAcesso.LoginFalha, false, usuario.Id, usuario.Email, null, null,
            "Sem vínculo com clube ativo", ct);
        throw new NaoAutenticadoException("Seu usuário não está vinculado a nenhum clube ativo.", "sem_clube");
    }

    /// <summary>Clubes que o usuário pode selecionar: vínculos ativos com clubes ativos (Superadmin: todos os ativos).</summary>
    private IQueryable<Clube> ClubesAcessiveisQuery(Usuario usuario)
    {
        var ativos = db.Clubes.Where(c => c.Situacao == SituacaoClube.Ativo);
        if (usuario.SuperAdmin) return ativos.OrderBy(c => c.Nome);
        var usuarioId = usuario.Id;
        return ativos
            .Where(c => db.UsuarioClubes.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
                .Any(v => v.ClubeId == c.Id && v.UsuarioId == usuarioId && v.Situacao == SituacaoVinculo.Ativo))
            .OrderBy(c => c.Nome);
    }

    private Task<bool> PodeAcessarClubeAsync(Usuario usuario, Guid clubeId, CancellationToken ct) =>
        ClubesAcessiveisQuery(usuario).AnyAsync(c => c.Id == clubeId, ct);

    private async Task<SessaoUsuario> ObterSessaoAtualAsync(CancellationToken ct)
    {
        var sessaoId = atual.SessaoId ?? throw new NaoAutenticadoException("Sessão inválida.");
        return await db.SessoesUsuarios.FirstOrDefaultAsync(s => s.Id == sessaoId && s.RevogadaEm == null, ct)
               ?? throw new NaoAutenticadoException("Sessão expirada. Entre novamente.", "sessao_invalida");
    }

    private async Task RevogarSessoesAsync(Guid usuarioId, Guid? exceto, string motivo, CancellationToken ct)
    {
        var sessoes = await db.SessoesUsuarios
            .Where(s => s.UsuarioId == usuarioId && s.RevogadaEm == null && s.Id != exceto)
            .ToListAsync(ct);
        foreach (var s in sessoes) s.Revogar(Agora, motivo);
    }

    private async Task<TokenResponse> MontarRespostaAsync(Usuario usuario, SessaoUsuario sessao, string refresh,
        CancellationToken ct)
    {
        var access = tokens.GerarAccessToken(usuario, sessao);
        var dto = await MontarUsuarioAsync(usuario, sessao.ClubeId, ct);
        return new TokenResponse(access.Token, access.ExpiraEm, refresh, sessao.ExpiraEm, dto);
    }

    private async Task<UsuarioLogadoDto> MontarUsuarioAsync(Usuario usuario, Guid? clubeId, CancellationToken ct)
    {
        var clubes = await ClubesAcessiveisQuery(usuario)
            .Select(c => new ClubeResumoDto(c.Id, c.Nome, c.Sigla, c.Configuracao.CorPrimaria,
                c.Configuracao.NomeExibicao, c.Demonstracao))
            .ToListAsync(ct);

        var clubeAtual = clubeId is null ? null : clubes.FirstOrDefault(c => c.Id == clubeId);
        IReadOnlyCollection<string> lista = clubeAtual is null
            ? []
            : usuario.SuperAdmin
                ? Permissoes.Codigos
                : await permissoes.ObterAsync(usuario.Id, clubeAtual.Id, ct);

        return new UsuarioLogadoDto(usuario.Id, usuario.Nome, usuario.Email, usuario.SuperAdmin,
            usuario.DeveTrocarSenha, clubeAtual, clubes, lista.Order().ToList());
    }

    private void Historico(EventoAcesso evento, bool sucesso, Guid? usuarioId, string? emailInformado, Guid? clubeId,
        Guid? sessaoId, string? detalhe) =>
        db.HistoricoAcessos.Add(new HistoricoAcesso(evento, sucesso, Agora, usuarioId,
            Truncar(emailInformado, 254), clubeId, sessaoId, atual.Ip, Truncar(atual.UserAgent, 500), detalhe));

    private async Task RegistrarAsync(EventoAcesso evento, bool sucesso, Guid? usuarioId, string? emailInformado,
        Guid? clubeId, Guid? sessaoId, string? detalhe, CancellationToken ct)
    {
        Historico(evento, sucesso, usuarioId, emailInformado, clubeId, sessaoId, detalhe);
        await db.SaveChangesAsync(ct);
    }

    private static string? Truncar(string? valor, int max) =>
        valor is null ? null : valor.Length <= max ? valor : valor[..max];
}
