namespace ProjetoClubes.Application.Seguranca;

public sealed record DefinicaoPermissao(string Codigo, string Modulo, string Descricao);

/// <summary>
/// Catálogo de permissões da plataforma. É a fonte da verdade: na inicialização a API sincroniza
/// esta lista com a tabela Permissoes. Os perfis de cada clube combinam estas permissões livremente.
/// </summary>
public static class Permissoes
{
    public static class Clubes
    {
        public const string Visualizar = "clubes.visualizar";
        public const string Editar = "clubes.editar";
        public const string Configurar = "clubes.configurar";
    }

    public static class Usuarios
    {
        public const string Visualizar = "usuarios.visualizar";
        public const string Gerenciar = "usuarios.gerenciar";
        public const string GerenciarPerfis = "perfis.gerenciar";
    }

    public static class Associados
    {
        public const string Visualizar = "associados.visualizar";
        public const string Cadastrar = "associados.cadastrar";
        public const string Editar = "associados.editar";
        public const string Aprovar = "associados.aprovar";
        public const string Excluir = "associados.excluir";
    }

    public static class Dependentes
    {
        public const string Visualizar = "dependentes.visualizar";
        public const string Cadastrar = "dependentes.cadastrar";
        public const string Editar = "dependentes.editar";
        public const string Aprovar = "dependentes.aprovar";
        public const string Excluir = "dependentes.excluir";
    }

    public static class Documentos
    {
        public const string Visualizar = "documentos.visualizar";
        public const string Gerenciar = "documentos.gerenciar";
    }

    public static class Financeiro
    {
        public const string Visualizar = "financeiro.visualizar";
        public const string RegistrarPagamentos = "financeiro.registrar_pagamentos";
        public const string Gerenciar = "financeiro.gerenciar";
    }

    public static class Eventos
    {
        public const string Visualizar = "eventos.visualizar";
        public const string Gerenciar = "eventos.gerenciar";
    }

    public static class Reservas
    {
        public const string Visualizar = "reservas.visualizar";
        public const string Aprovar = "reservas.aprovar";
        public const string GerenciarEspacos = "espacos.gerenciar";
    }

    public static class Solicitacoes
    {
        public const string Visualizar = "solicitacoes.visualizar";
        public const string Atender = "solicitacoes.atender";
    }

    public static class Comunicacao
    {
        public const string Visualizar = "comunicacao.visualizar";
        public const string Gerenciar = "comunicacao.gerenciar";
    }

    public static class Operacional
    {
        public const string Funcionarios = "funcionarios.gerenciar";
        public const string Patrimonio = "patrimonio.gerenciar";
        public const string Estoque = "estoque.gerenciar";
    }

    public static class Assembleias
    {
        public const string Visualizar = "assembleias.visualizar";
        public const string Gerenciar = "assembleias.gerenciar";
    }

    public static class Relatorios
    {
        public const string Visualizar = "relatorios.visualizar";
    }

    public static class ControleAcesso
    {
        public const string Validar = "acesso.validar";
    }

    public static class Auditoria
    {
        public const string Visualizar = "auditoria.visualizar";
    }

    public static class Portal
    {
        /// <summary>Acesso do associado/dependente às próprias informações (app).</summary>
        public const string Acessar = "portal.acessar";
    }

    public static readonly IReadOnlyList<DefinicaoPermissao> Todas =
    [
        new(Clubes.Visualizar, "Clube", "Visualizar dados do clube"),
        new(Clubes.Editar, "Clube", "Editar dados cadastrais do clube"),
        new(Clubes.Configurar, "Clube", "Alterar configurações e identidade visual do clube"),

        new(Usuarios.Visualizar, "Usuários e permissões", "Visualizar usuários do clube"),
        new(Usuarios.Gerenciar, "Usuários e permissões", "Cadastrar usuários e definir seus acessos"),
        new(Usuarios.GerenciarPerfis, "Usuários e permissões", "Criar e editar perfis de acesso"),

        new(Associados.Visualizar, "Associados", "Visualizar associados"),
        new(Associados.Cadastrar, "Associados", "Cadastrar associados"),
        new(Associados.Editar, "Associados", "Editar associados"),
        new(Associados.Aprovar, "Associados", "Aprovar, suspender e inativar associados"),
        new(Associados.Excluir, "Associados", "Excluir associados"),

        new(Dependentes.Visualizar, "Dependentes", "Visualizar dependentes"),
        new(Dependentes.Cadastrar, "Dependentes", "Cadastrar dependentes"),
        new(Dependentes.Editar, "Dependentes", "Editar dependentes"),
        new(Dependentes.Aprovar, "Dependentes", "Aprovar ou rejeitar dependentes"),
        new(Dependentes.Excluir, "Dependentes", "Excluir dependentes"),

        new(Documentos.Visualizar, "Documentos", "Visualizar documentos pessoais"),
        new(Documentos.Gerenciar, "Documentos", "Enviar e remover documentos"),

        new(Financeiro.Visualizar, "Financeiro", "Visualizar o financeiro"),
        new(Financeiro.RegistrarPagamentos, "Financeiro", "Registrar pagamentos"),
        new(Financeiro.Gerenciar, "Financeiro", "Gerenciar planos, cobranças, receitas e despesas"),

        new(Eventos.Visualizar, "Eventos", "Visualizar eventos e inscrições"),
        new(Eventos.Gerenciar, "Eventos", "Gerenciar eventos e participantes"),

        new(Reservas.Visualizar, "Espaços e reservas", "Visualizar reservas"),
        new(Reservas.Aprovar, "Espaços e reservas", "Aprovar, recusar e alterar reservas"),
        new(Reservas.GerenciarEspacos, "Espaços e reservas", "Gerenciar espaços físicos"),

        new(Solicitacoes.Visualizar, "Solicitações", "Visualizar solicitações e reclamações"),
        new(Solicitacoes.Atender, "Solicitações", "Responder e alterar o status de solicitações"),

        new(Comunicacao.Visualizar, "Comunicação", "Visualizar comunicados e enquetes"),
        new(Comunicacao.Gerenciar, "Comunicação", "Publicar comunicados, notificações e enquetes"),

        new(Operacional.Funcionarios, "Operacional", "Gerenciar funcionários"),
        new(Operacional.Patrimonio, "Operacional", "Gerenciar patrimônio e manutenções"),
        new(Operacional.Estoque, "Operacional", "Gerenciar estoque"),

        new(Assembleias.Visualizar, "Assembleias", "Visualizar assembleias e atas"),
        new(Assembleias.Gerenciar, "Assembleias", "Gerenciar assembleias, votações e atas"),

        new(Relatorios.Visualizar, "Relatórios", "Consultar relatórios"),
        new(ControleAcesso.Validar, "Controle de acesso", "Validar carteirinhas e registrar entradas e saídas"),
        new(Auditoria.Visualizar, "Auditoria", "Consultar logs de auditoria e histórico de acessos"),
        new(Portal.Acessar, "Portal do associado", "Acessar o aplicativo do associado"),
    ];

    public static readonly IReadOnlySet<string> Codigos = Todas.Select(p => p.Codigo).ToHashSet();
}
