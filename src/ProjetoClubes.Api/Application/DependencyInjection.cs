using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjetoClubes.Application.Arquivos;
using ProjetoClubes.Application.Auditoria;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Carteirinhas;
using ProjetoClubes.Application.ControleAcesso;
using ProjetoClubes.Application.Financeiro;
using ProjetoClubes.Application.Portal;
using ProjetoClubes.Application.Autenticacao;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Perfis;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Application.Setup;
using ProjetoClubes.Application.Usuarios;

namespace ProjetoClubes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // DadosPessoaisValidator é composto manualmente (recebe parâmetro), não registrado no contêiner.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true,
            filter: r => r.ValidatorType != typeof(DadosPessoaisValidator));
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IPermissaoResolver, PermissaoResolver>();
        services.AddScoped<IAutenticacaoService, AutenticacaoService>();
        services.AddScoped<ISetupService, SetupService>();
        services.AddScoped<IProvisionamentoUsuarios, ProvisionamentoUsuarios>();
        services.AddScoped<IClubeService, ClubeService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();

        // Etapa 2 — cadastros
        services.AddScoped<IInicializadorClube, InicializadorClube>();
        services.AddScoped<IAcessoPessoas, AcessoPessoas>();
        services.AddScoped<GestaoDependentes>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IAssociadoService, AssociadoService>();
        services.AddScoped<DependenteService>();
        services.AddScoped<IDependenteService>(sp => sp.GetRequiredService<DependenteService>());
        services.AddScoped<IDocumentoService, DocumentoService>();
        services.AddScoped<ICarteirinhaService, CarteirinhaService>();
        services.AddScoped<IControleAcessoService, ControleAcessoService>();
        services.AddScoped<IPortalService, PortalService>();

        // Etapa 3 — financeiro
        services.AddScoped<IMensalidadeService, MensalidadeService>();
        services.AddScoped<ICaixaService, CaixaService>();

        // Etapa 4 — eventos, reservas e solicitações
        services.AddScoped<Eventos.IEventoService, Eventos.EventoService>();
        services.AddScoped<Reservas.IReservaService, Reservas.ReservaService>();
        services.AddScoped<Solicitacoes.ISolicitacaoService, Solicitacoes.SolicitacaoService>();

        // Etapa 7 — comunicação, operacional e assembleias
        services.AddScoped<Comunicacao.INotificador, Comunicacao.Notificador>();
        services.AddScoped<Comunicacao.IComunicacaoService, Comunicacao.ComunicacaoService>();
        services.AddScoped<Operacional.IFuncionarioService, Operacional.FuncionarioService>();
        services.AddScoped<Operacional.IPatrimonioService, Operacional.PatrimonioService>();
        services.AddScoped<Operacional.IEstoqueService, Operacional.EstoqueService>();
        services.AddScoped<Assembleias.IAssembleiaService, Assembleias.AssembleiaService>();

        // Etapa 8 — LGPD
        services.AddScoped<Privacidade.IPrivacidadeService, Privacidade.PrivacidadeService>();
        return services;
    }
}
