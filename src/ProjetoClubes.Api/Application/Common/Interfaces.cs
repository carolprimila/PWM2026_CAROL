using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProjetoClubes.Domain.Acesso;
using ProjetoClubes.Domain.Arquivos;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Auditoria;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Financeiro;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Application.Common;

/// <summary>Abstração do contexto de dados usada pelos casos de uso (implementada na Infrastructure).</summary>
public interface IAppDbContext
{
    DbSet<Clube> Clubes { get; }
    DbSet<ConfiguracaoClube> ConfiguracoesClube { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<UsuarioClube> UsuarioClubes { get; }
    DbSet<UsuarioClubePerfil> UsuarioClubePerfis { get; }
    DbSet<UsuarioPermissao> UsuarioPermissoes { get; }
    DbSet<Perfil> Perfis { get; }
    DbSet<PerfilPermissao> PerfilPermissoes { get; }
    DbSet<Permissao> Permissoes { get; }
    DbSet<SessaoUsuario> SessoesUsuarios { get; }
    DbSet<TokenRecuperacaoSenha> TokensRecuperacaoSenha { get; }
    DbSet<HistoricoAcesso> HistoricoAcessos { get; }
    DbSet<LogAuditoria> LogsAuditoria { get; }

    DbSet<Pessoa> Pessoas { get; }
    DbSet<CategoriaAssociado> CategoriasAssociados { get; }
    DbSet<Interesse> Interesses { get; }
    DbSet<Associado> Associados { get; }
    DbSet<HistoricoAssociado> HistoricoAssociados { get; }
    DbSet<ContratoAssociado> ContratosAssociados { get; }
    DbSet<MembroDiretoria> MembrosDiretoria { get; }
    DbSet<Dependente> Dependentes { get; }
    DbSet<VinculoDependente> VinculosDependentes { get; }
    DbSet<HistoricoDependente> HistoricoDependentes { get; }
    DbSet<ConfiguracaoDependentes> ConfiguracoesDependentes { get; }
    DbSet<Arquivo> Arquivos { get; }
    DbSet<DocumentoPessoal> DocumentosPessoais { get; }
    DbSet<AcessoClube> AcessosClube { get; }
    DbSet<ConfiguracaoFinanceira> ConfiguracoesFinanceiras { get; }
    DbSet<PlanoMensalidade> PlanosMensalidade { get; }
    DbSet<Mensalidade> Mensalidades { get; }
    DbSet<Pagamento> Pagamentos { get; }
    DbSet<CategoriaFinanceira> CategoriasFinanceiras { get; }
    DbSet<LancamentoFinanceiro> LancamentosFinanceiros { get; }
    DbSet<Domain.Eventos.Evento> Eventos { get; }
    DbSet<Domain.Eventos.InscricaoEvento> InscricoesEventos { get; }
    DbSet<Domain.Reservas.Espaco> Espacos { get; }
    DbSet<Domain.Reservas.Reserva> Reservas { get; }
    DbSet<Domain.Solicitacoes.Solicitacao> Solicitacoes { get; }
    DbSet<Domain.Solicitacoes.RespostaSolicitacao> RespostasSolicitacoes { get; }
    DbSet<Domain.Comunicacao.Comunicado> Comunicados { get; }
    DbSet<Domain.Comunicacao.LeituraComunicado> LeiturasComunicados { get; }
    DbSet<Domain.Comunicacao.Notificacao> Notificacoes { get; }
    DbSet<Domain.Operacional.Funcionario> Funcionarios { get; }
    DbSet<Domain.Operacional.AusenciaFuncionario> AusenciasFuncionarios { get; }
    DbSet<Domain.Operacional.HistoricoFuncionario> HistoricoFuncionarios { get; }
    DbSet<Domain.Operacional.Bem> Bens { get; }
    DbSet<Domain.Operacional.ManutencaoBem> ManutencoesBens { get; }
    DbSet<Domain.Operacional.HistoricoBem> HistoricoBens { get; }
    DbSet<Domain.Operacional.Produto> Produtos { get; }
    DbSet<Domain.Operacional.MovimentacaoEstoque> MovimentacoesEstoque { get; }
    DbSet<Domain.Operacional.SolicitacaoCompra> SolicitacoesCompra { get; }
    DbSet<Domain.Assembleias.Assembleia> Assembleias { get; }
    DbSet<Domain.Assembleias.PautaAssembleia> PautasAssembleias { get; }
    DbSet<Domain.Assembleias.PresencaAssembleia> PresencasAssembleias { get; }
    DbSet<Domain.Assembleias.VotoAssembleia> VotosAssembleias { get; }

    DatabaseFacade Database { get; }

    /// <summary>Usado para recarregar uma entidade dentro de um bloqueio (ver <see cref="ITransacoes"/>).</summary>
    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Identidade da requisição atual. Preenchida pelo servidor a partir do JWT já validado —
/// nunca a partir de dados enviados pelo cliente no corpo ou na query.
/// </summary>
public interface IUsuarioAtual
{
    bool Autenticado { get; }
    Guid? UsuarioId { get; }
    Guid? SessaoId { get; }
    /// <summary>Clube ativo validado pelo servidor.</summary>
    Guid? ClubeId { get; }
    bool SuperAdmin { get; }
    /// <summary>Execução interna (seed, tarefas agendadas). Pode gravar em qualquer clube.</summary>
    bool Sistema { get; }
    string? Ip { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }

    Guid UsuarioIdObrigatorio => UsuarioId ?? throw new NaoAutenticadoException("Sessão inválida.");
    Guid ClubeIdObrigatorio => ClubeId ?? throw new ClubeNaoSelecionadoException();
}

public interface IHashSenha
{
    string Gerar(string senha);
    /// <summary>Retorna (válida, precisaRehash).</summary>
    (bool Valida, bool PrecisaRehash) Verificar(string hash, string senha);
}

public sealed record AccessTokenGerado(string Token, DateTimeOffset ExpiraEm);

public interface ITokenService
{
    AccessTokenGerado GerarAccessToken(Usuario usuario, SessaoUsuario sessao);
    /// <summary>Token opaco aleatório (256 bits, base64url).</summary>
    string GerarTokenOpaco();
    /// <summary>Hash SHA-256 (hex) usado para armazenar tokens opacos.</summary>
    string Hash(string token);
    TimeSpan DuracaoRefreshToken { get; }
}

public sealed record MensagemEmail(string Para, string Assunto, string CorpoTexto);

public interface IEmailSender
{
    Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default);
}

/// <summary>Token de inicialização do sistema (primeiro Superadmin).</summary>
public interface ISetupTokenProvider
{
    bool Disponivel { get; }
    bool Validar(string token);
    void Consumir();
}

/// <summary>Armazenamento privado de arquivos (fora de qualquer pasta pública).</summary>
public interface IArmazenamentoArquivos
{
    /// <summary>Grava o conteúdo e retorna a chave interna de armazenamento.</summary>
    Task<string> SalvarAsync(Guid clubeId, Stream conteudo, string extensao, CancellationToken ct);
    Task<Stream> AbrirAsync(string chave, CancellationToken ct);
    Task RemoverAsync(string chave, CancellationToken ct);
}

/// <summary>Contadores atômicos por clube (ex.: matrícula).</summary>
public interface IGeradorSequencia
{
    Task<long> ProximoAsync(Guid clubeId, string nome, CancellationToken ct);
}

public interface IGeradorQrCode
{
    byte[] GerarPng(string conteudo, int pixelsPorModulo = 10);
}

/// <summary>
/// Executa uma operação em transação com bloqueio por chave (ex.: "evento:{id}"), serializando operações
/// concorrentes sobre o mesmo recurso (vagas de evento, agenda de um espaço).
/// </summary>
public interface ITransacoes
{
    Task<T> ComBloqueioAsync<T>(string chave, Func<Task<T>> acao, CancellationToken ct);
}
