using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Setup;

public sealed record SetupStatusDto(bool Inicializado);

public sealed record InicializarSistemaRequest(string TokenInicializacao, string Nome, string Email, string Senha);

public sealed class InicializarSistemaRequestValidator : AbstractValidator<InicializarSistemaRequest>
{
    public InicializarSistemaRequestValidator()
    {
        RuleFor(x => x.TokenInicializacao).NotEmpty().WithMessage("Informe o token de inicialização.");
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("E-mail inválido.").MaximumLength(254);
        RuleFor(x => x.Senha).SenhaForte();
    }
}

public interface ISetupService
{
    Task<SetupStatusDto> ObterStatusAsync(CancellationToken ct);
    Task InicializarAsync(InicializarSistemaRequest request, CancellationToken ct);
}

/// <summary>
/// Criação do primeiro Superadmin. Só funciona enquanto não houver nenhum Superadmin e exige o token
/// de inicialização exibido no log da API (ou definido em Setup__Token). Não existem credenciais fixas.
/// </summary>
public sealed class SetupService(
    IAppDbContext db,
    IHashSenha hashSenha,
    ISetupTokenProvider setupToken,
    IUsuarioAtual atual,
    TimeProvider relogio,
    IValidator<InicializarSistemaRequest> validator,
    ILogger<SetupService> logger) : ISetupService
{
    private static readonly SemaphoreSlim Trava = new(1, 1);

    public async Task<SetupStatusDto> ObterStatusAsync(CancellationToken ct) =>
        new(await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.SuperAdmin, ct));

    public async Task InicializarAsync(InicializarSistemaRequest request, CancellationToken ct)
    {
        await validator.ValidarAsync(request, ct);

        await Trava.WaitAsync(ct);
        try
        {
            if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.SuperAdmin, ct) || !setupToken.Disponivel)
                throw new ConflitoException("sistema_inicializado", "O sistema já foi inicializado.");

            if (!setupToken.Validar(request.TokenInicializacao))
                throw new AcessoNegadoException("Token de inicialização inválido.");

            var emailNormalizado = Usuario.NormalizarEmail(request.Email);
            var usuario = await db.Usuarios.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.EmailNormalizado == emailNormalizado, ct);
            if (usuario is null)
            {
                usuario = new Usuario(request.Nome, request.Email, superAdmin: true);
                db.Usuarios.Add(usuario);
            }
            else
            {
                usuario.DefinirSuperAdmin(true);
            }

            var agora = relogio.GetUtcNow();
            usuario.DefinirSenhaHash(hashSenha.Gerar(request.Senha), agora);
            db.HistoricoAcessos.Add(new HistoricoAcesso(EventoAcesso.InicializacaoSistema, true, agora, usuario.Id,
                usuario.Email, null, null, atual.Ip, atual.UserAgent, "Primeiro Superadmin criado"));
            await db.SaveChangesAsync(ct);
            setupToken.Consumir();
            logger.LogWarning("Sistema inicializado. Superadmin {UsuarioId} criado; token de inicialização descartado.",
                usuario.Id);
        }
        finally
        {
            Trava.Release();
        }
    }
}
