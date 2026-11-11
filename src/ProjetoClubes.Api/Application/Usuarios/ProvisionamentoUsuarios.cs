using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjetoClubes.Application.Autenticacao;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Usuarios;

public sealed record ResultadoProvisionamento(Usuario Usuario, UsuarioClube Vinculo, bool UsuarioCriado, bool ConviteEnviado);

public interface IProvisionamentoUsuarios
{
    /// <summary>
    /// Cria o usuário (ou reaproveita a conta existente com o mesmo e-mail) e o vincula ao clube com os perfis
    /// informados. Sem senha inicial, envia um convite para o próprio usuário definir a senha.
    /// Não grava: o chamador chama SaveChanges.
    /// </summary>
    Task<ResultadoProvisionamento> CriarOuVincularAsync(string nome, string email, Guid clubeId,
        IReadOnlyCollection<Guid> perfilIds, string? senhaInicial, CancellationToken ct);
}

public sealed class ProvisionamentoUsuarios(
    IAppDbContext db,
    IHashSenha hashSenha,
    ITokenService tokens,
    IEmailSender emailSender,
    IUsuarioAtual atual,
    IOptions<AutenticacaoOptions> opcoes,
    TimeProvider relogio) : IProvisionamentoUsuarios
{
    private const int HorasValidadeConvite = 72;

    public async Task<ResultadoProvisionamento> CriarOuVincularAsync(string nome, string email, Guid clubeId,
        IReadOnlyCollection<Guid> perfilIds, string? senhaInicial, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var emailNormalizado = Usuario.NormalizarEmail(email);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == emailNormalizado, ct);
        var criado = false;

        if (usuario is null)
        {
            usuario = new Usuario(nome, email);
            if (senhaInicial is not null)
            {
                usuario.DefinirSenhaHash(hashSenha.Gerar(senhaInicial), agora);
                usuario.DeveTrocarSenha = true;
            }
            db.Usuarios.Add(usuario);
            criado = true;
        }

        var vinculo = await db.UsuarioClubes.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
            .Include(v => v.Perfis)
            .FirstOrDefaultAsync(v => v.UsuarioId == usuario.Id && v.ClubeId == clubeId, ct);
        if (vinculo is not null && vinculo.Ativo)
            throw new ConflitoException("usuario.ja_vinculado", "Este usuário já tem acesso a este clube.");

        if (vinculo is null)
        {
            vinculo = new UsuarioClube(usuario.Id, clubeId);
            db.UsuarioClubes.Add(vinculo);
        }
        else
        {
            vinculo.Ativar();
        }
        vinculo.DefinirPerfis(perfilIds);

        var conviteEnviado = false;
        if (usuario.SenhaHash is null)
        {
            var token = tokens.GerarTokenOpaco();
            db.TokensRecuperacaoSenha.Add(new TokenRecuperacaoSenha(usuario.Id, tokens.Hash(token),
                usuario.CarimboSeguranca, agora, agora.AddHours(HorasValidadeConvite), atual.Ip));
            var link = $"{opcoes.Value.UrlRedefinicaoSenha}?token={Uri.EscapeDataString(token)}&convite=1";
            await emailSender.EnviarAsync(new MensagemEmail(usuario.Email, "Projeto Clubes — convite de acesso",
                $"Olá, {usuario.Nome}.\n\nVocê recebeu acesso ao Projeto Clubes. Defina sua senha pelo link abaixo " +
                $"(válido por {HorasValidadeConvite} horas):\n\n{link}"), ct);
            conviteEnviado = true;
        }

        return new ResultadoProvisionamento(usuario, vinculo, criado, conviteEnviado);
    }
}
