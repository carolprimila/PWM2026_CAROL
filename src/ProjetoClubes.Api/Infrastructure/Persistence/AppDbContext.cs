using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Acesso;
using ProjetoClubes.Domain.Arquivos;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Auditoria;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Financeiro;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IUsuarioAtual usuarioAtual)
    : DbContext(options), IAppDbContext
{
    public DbSet<Clube> Clubes => Set<Clube>();
    public DbSet<ConfiguracaoClube> ConfiguracoesClube => Set<ConfiguracaoClube>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioClube> UsuarioClubes => Set<UsuarioClube>();
    public DbSet<UsuarioClubePerfil> UsuarioClubePerfis => Set<UsuarioClubePerfil>();
    public DbSet<UsuarioPermissao> UsuarioPermissoes => Set<UsuarioPermissao>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<PerfilPermissao> PerfilPermissoes => Set<PerfilPermissao>();
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<SessaoUsuario> SessoesUsuarios => Set<SessaoUsuario>();
    public DbSet<TokenRecuperacaoSenha> TokensRecuperacaoSenha => Set<TokenRecuperacaoSenha>();
    public DbSet<HistoricoAcesso> HistoricoAcessos => Set<HistoricoAcesso>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<CategoriaAssociado> CategoriasAssociados => Set<CategoriaAssociado>();
    public DbSet<Interesse> Interesses => Set<Interesse>();
    public DbSet<Associado> Associados => Set<Associado>();
    public DbSet<HistoricoAssociado> HistoricoAssociados => Set<HistoricoAssociado>();
    public DbSet<ContratoAssociado> ContratosAssociados => Set<ContratoAssociado>();
    public DbSet<MembroDiretoria> MembrosDiretoria => Set<MembroDiretoria>();
    public DbSet<Dependente> Dependentes => Set<Dependente>();
    public DbSet<VinculoDependente> VinculosDependentes => Set<VinculoDependente>();
    public DbSet<HistoricoDependente> HistoricoDependentes => Set<HistoricoDependente>();
    public DbSet<ConfiguracaoDependentes> ConfiguracoesDependentes => Set<ConfiguracaoDependentes>();
    public DbSet<Arquivo> Arquivos => Set<Arquivo>();
    public DbSet<DocumentoPessoal> DocumentosPessoais => Set<DocumentoPessoal>();
    public DbSet<AcessoClube> AcessosClube => Set<AcessoClube>();
    public DbSet<SequenciaClube> SequenciasClube => Set<SequenciaClube>();

    public DbSet<ConfiguracaoFinanceira> ConfiguracoesFinanceiras => Set<ConfiguracaoFinanceira>();
    public DbSet<PlanoMensalidade> PlanosMensalidade => Set<PlanoMensalidade>();
    public DbSet<Mensalidade> Mensalidades => Set<Mensalidade>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<CategoriaFinanceira> CategoriasFinanceiras => Set<CategoriaFinanceira>();
    public DbSet<LancamentoFinanceiro> LancamentosFinanceiros => Set<LancamentoFinanceiro>();

    public DbSet<Domain.Eventos.Evento> Eventos => Set<Domain.Eventos.Evento>();
    public DbSet<Domain.Eventos.InscricaoEvento> InscricoesEventos => Set<Domain.Eventos.InscricaoEvento>();
    public DbSet<Domain.Reservas.Espaco> Espacos => Set<Domain.Reservas.Espaco>();
    public DbSet<Domain.Reservas.Reserva> Reservas => Set<Domain.Reservas.Reserva>();
    public DbSet<Domain.Solicitacoes.Solicitacao> Solicitacoes => Set<Domain.Solicitacoes.Solicitacao>();
    public DbSet<Domain.Solicitacoes.RespostaSolicitacao> RespostasSolicitacoes => Set<Domain.Solicitacoes.RespostaSolicitacao>();

    public DbSet<Domain.Comunicacao.Comunicado> Comunicados => Set<Domain.Comunicacao.Comunicado>();
    public DbSet<Domain.Comunicacao.LeituraComunicado> LeiturasComunicados => Set<Domain.Comunicacao.LeituraComunicado>();
    public DbSet<Domain.Comunicacao.Notificacao> Notificacoes => Set<Domain.Comunicacao.Notificacao>();
    public DbSet<Domain.Operacional.Funcionario> Funcionarios => Set<Domain.Operacional.Funcionario>();
    public DbSet<Domain.Operacional.AusenciaFuncionario> AusenciasFuncionarios => Set<Domain.Operacional.AusenciaFuncionario>();
    public DbSet<Domain.Operacional.HistoricoFuncionario> HistoricoFuncionarios => Set<Domain.Operacional.HistoricoFuncionario>();
    public DbSet<Domain.Operacional.Bem> Bens => Set<Domain.Operacional.Bem>();
    public DbSet<Domain.Operacional.ManutencaoBem> ManutencoesBens => Set<Domain.Operacional.ManutencaoBem>();
    public DbSet<Domain.Operacional.HistoricoBem> HistoricoBens => Set<Domain.Operacional.HistoricoBem>();
    public DbSet<Domain.Operacional.Produto> Produtos => Set<Domain.Operacional.Produto>();
    public DbSet<Domain.Operacional.MovimentacaoEstoque> MovimentacoesEstoque => Set<Domain.Operacional.MovimentacaoEstoque>();
    public DbSet<Domain.Operacional.SolicitacaoCompra> SolicitacoesCompra => Set<Domain.Operacional.SolicitacaoCompra>();
    public DbSet<Domain.Assembleias.Assembleia> Assembleias => Set<Domain.Assembleias.Assembleia>();
    public DbSet<Domain.Assembleias.PautaAssembleia> PautasAssembleias => Set<Domain.Assembleias.PautaAssembleia>();
    public DbSet<Domain.Assembleias.PresencaAssembleia> PresencasAssembleias => Set<Domain.Assembleias.PresencaAssembleia>();
    public DbSet<Domain.Assembleias.VotoAssembleia> VotosAssembleias => Set<Domain.Assembleias.VotoAssembleia>();

    /// <summary>
    /// Clube usado pelo filtro global. Avaliado a cada consulta. Sem clube selecionado, usa Guid.Empty
    /// e o filtro não retorna nenhum registro de tenant (falha fechada).
    /// </summary>
    internal Guid ClubeIdFiltro => usuarioAtual.ClubeId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        AplicarChavesGeradasNaAplicacao(modelBuilder);
        AplicarFiltrosGlobais(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(500);
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
        // SQLite não tem tipo de data com fuso: datas/horas são gravadas como instantes UTC em ticks (INTEGER).
        // Assim comparações e ordenações funcionam no banco, e filtros com horário local (-03:00) viram o mesmo instante.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetUtcTicks>();
    }

    private sealed class DateTimeOffsetUtcTicks()
        : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

    /// <summary>
    /// Os Ids (UUIDv7) são gerados pela aplicação. Sem isto, o EF trata um filho novo adicionado a uma coleção
    /// de entidade já rastreada (ex.: novo vínculo de dependente) como existente e emite UPDATE em vez de INSERT.
    /// </summary>
    private static void AplicarChavesGeradasNaAplicacao(ModelBuilder modelBuilder)
    {
        foreach (var tipo in modelBuilder.Model.GetEntityTypes()
                     .Where(t => !t.IsOwned() && typeof(Entity).IsAssignableFrom(t.ClrType) && t.BaseType is null))
            modelBuilder.Entity(tipo.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
    }

    private void AplicarFiltrosGlobais(ModelBuilder modelBuilder)
    {
        foreach (var tipo in modelBuilder.Model.GetEntityTypes().Where(t => t.BaseType is null && !t.IsOwned()))
        {
            var clr = tipo.ClrType;
            if (typeof(ITenantScoped).IsAssignableFrom(clr))
                modelBuilder.Entity(clr).HasQueryFilter(FiltrosConsulta.Tenant, FiltroTenant(clr));
            if (typeof(ISoftDelete).IsAssignableFrom(clr))
                modelBuilder.Entity(clr).HasQueryFilter(FiltrosConsulta.ExclusaoLogica, FiltroExclusao(clr));
        }
    }

    private LambdaExpression FiltroTenant(Type tipo)
    {
        // e => e.ClubeId == this.ClubeIdFiltro
        var e = Expression.Parameter(tipo, "e");
        var corpo = Expression.Equal(
            Expression.Property(e, nameof(ITenantScoped.ClubeId)),
            Expression.Property(Expression.Constant(this), nameof(ClubeIdFiltro)));
        return Expression.Lambda(corpo, e);
    }

    private static LambdaExpression FiltroExclusao(Type tipo)
    {
        // e => !e.Excluido
        var e = Expression.Parameter(tipo, "e");
        return Expression.Lambda(Expression.Not(Expression.Property(e, nameof(ISoftDelete.Excluido))), e);
    }
}
