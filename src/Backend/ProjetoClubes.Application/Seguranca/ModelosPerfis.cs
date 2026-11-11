using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Seguranca;

public sealed record ModeloPerfil(string Chave, string Nome, string Descricao, IReadOnlyCollection<string> Permissoes);

/// <summary>
/// Perfis iniciais copiados para cada clube no momento do cadastro. Depois da cópia, cada clube
/// edita livremente as permissões dos seus perfis — os modelos são apenas o ponto de partida.
/// </summary>
public static class ModelosPerfis
{
    public const string Presidente = "presidente";
    public const string VicePresidente = "vice_presidente";
    public const string Secretario = "secretario";
    public const string Tesoureiro = "tesoureiro";
    public const string Administrador = "administrador";
    public const string Funcionario = "funcionario";
    public const string Associado = "associado";
    public const string Dependente = "dependente";

    private static string[] TodasExceto(params string[] excecoes) =>
        Seguranca.Permissoes.Codigos.Except(excecoes).ToArray();

    private static string[] Visualizacao() =>
        Seguranca.Permissoes.Codigos.Where(c => c.EndsWith(".visualizar", StringComparison.Ordinal)).ToArray();

    public static readonly IReadOnlyList<ModeloPerfil> Todos =
    [
        new(Presidente, "Presidente", "Acesso completo à gestão do clube.", TodasExceto()),
        new(VicePresidente, "Vice-presidente", "Consulta todos os módulos e gerencia eventos e assembleias.",
        [
            .. Visualizacao(),
            Seguranca.Permissoes.Eventos.Gerenciar,
            Seguranca.Permissoes.Assembleias.Gerenciar,
            Seguranca.Permissoes.Comunicacao.Gerenciar,
            Seguranca.Permissoes.Associados.Aprovar,
            Seguranca.Permissoes.Dependentes.Aprovar,
        ]),
        new(Secretario, "Secretário", "Cadastros, documentos, comunicação, solicitações e assembleias.",
        [
            Seguranca.Permissoes.Clubes.Visualizar,
            Seguranca.Permissoes.Associados.Visualizar, Seguranca.Permissoes.Associados.Cadastrar,
            Seguranca.Permissoes.Associados.Editar, Seguranca.Permissoes.Associados.Aprovar,
            Seguranca.Permissoes.Dependentes.Visualizar, Seguranca.Permissoes.Dependentes.Cadastrar,
            Seguranca.Permissoes.Dependentes.Editar, Seguranca.Permissoes.Dependentes.Aprovar,
            Seguranca.Permissoes.Documentos.Visualizar, Seguranca.Permissoes.Documentos.Gerenciar,
            Seguranca.Permissoes.Comunicacao.Visualizar, Seguranca.Permissoes.Comunicacao.Gerenciar,
            Seguranca.Permissoes.Solicitacoes.Visualizar, Seguranca.Permissoes.Solicitacoes.Atender,
            Seguranca.Permissoes.Eventos.Visualizar, Seguranca.Permissoes.Eventos.Gerenciar,
            Seguranca.Permissoes.Reservas.Visualizar,
            Seguranca.Permissoes.Assembleias.Visualizar, Seguranca.Permissoes.Assembleias.Gerenciar,
            Seguranca.Permissoes.Relatorios.Visualizar,
        ]),
        new(Tesoureiro, "Tesoureiro", "Gestão financeira do clube.",
        [
            Seguranca.Permissoes.Clubes.Visualizar,
            Seguranca.Permissoes.Financeiro.Visualizar, Seguranca.Permissoes.Financeiro.RegistrarPagamentos,
            Seguranca.Permissoes.Financeiro.Gerenciar,
            Seguranca.Permissoes.Associados.Visualizar, Seguranca.Permissoes.Dependentes.Visualizar,
            Seguranca.Permissoes.Reservas.Visualizar, Seguranca.Permissoes.Eventos.Visualizar,
            Seguranca.Permissoes.Relatorios.Visualizar,
        ]),
        new(Administrador, "Administrador", "Administração operacional completa do clube.", TodasExceto()),
        new(Funcionario, "Funcionário", "Portaria e consultas operacionais.",
        [
            Seguranca.Permissoes.Clubes.Visualizar,
            Seguranca.Permissoes.ControleAcesso.Validar,
            Seguranca.Permissoes.Associados.Visualizar, Seguranca.Permissoes.Dependentes.Visualizar,
            Seguranca.Permissoes.Eventos.Visualizar, Seguranca.Permissoes.Reservas.Visualizar,
            Seguranca.Permissoes.Solicitacoes.Visualizar,
        ]),
        new(Associado, "Associado", "Acesso do associado ao aplicativo.", [Seguranca.Permissoes.Portal.Acessar]),
        new(Dependente, "Dependente", "Acesso do dependente ao aplicativo.", [Seguranca.Permissoes.Portal.Acessar]),
    ];

    /// <summary>Instancia os perfis-modelo para um clube recém-criado.</summary>
    /// <param name="clubeId">Clube dono dos perfis.</param>
    /// <param name="catalogo">Código da permissão → Id na tabela Permissoes.</param>
    public static List<Perfil> InstanciarPara(Guid clubeId, IReadOnlyDictionary<string, Guid> catalogo) =>
        Todos.Select(m =>
        {
            var perfil = new Perfil(clubeId, m.Nome, m.Descricao, m.Chave);
            perfil.DefinirPermissoes(m.Permissoes.Where(catalogo.ContainsKey).Select(c => catalogo[c]));
            return perfil;
        }).ToList();
}
