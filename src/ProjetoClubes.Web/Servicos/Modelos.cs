namespace ProjetoClubes.Web.Servicos;

// Modelos simples espelhando as respostas da API (somente os campos usados nas telas).

public sealed record Pagina<T>(List<T> Itens, int PaginaAtual, int TamanhoPagina, int TotalItens, int TotalPaginas);

public sealed record ClubeResumo(Guid Id, string Nome, string Sigla, string CorPrimaria, string? NomeExibicao, bool Demonstracao);

public sealed record UsuarioLogado(Guid Id, string Nome, string Email, bool SuperAdmin, bool DeveTrocarSenha,
    ClubeResumo? ClubeAtual, List<ClubeResumo> Clubes, List<string> Permissoes);

public sealed record TokenResponse(string AccessToken, string RefreshToken, UsuarioLogado Usuario);

public sealed record ResumoCadastros(int TotalAssociados, Dictionary<string, int> AssociadosPorSituacao,
    Dictionary<string, int> AssociadosPorCategoria, int TotalDependentes, Dictionary<string, int> DependentesPorSituacao);

public sealed record Categoria(Guid Id, string Nome, bool DireitoVoto, bool PermiteDependentes, bool Ativa);

public sealed record AssociadoLista(Guid Id, Guid PessoaId, string Matricula, string Nome, string? CpfMascarado,
    string Categoria, string Situacao, DateOnly DataAssociacao, int TotalDependentes);

public sealed record Endereco(string? Logradouro, string? Numero, string? Complemento, string? Bairro, string? Cidade,
    string? Uf, string? Cep);

public sealed record Pessoa(Guid Id, string NomeCompleto, string? Cpf, DateOnly? DataNascimento, int? Idade, string Sexo,
    Endereco Endereco, string? Telefone, string? Email, bool TemFoto, bool TemAcessoApp)
{
    public string? Rg { get; init; }
    public string? EstadoCivil { get; init; }
    public string? Profissao { get; init; }
    public DateTimeOffset CadastradoEm { get; init; }
}

public sealed record DependenteResumo(Guid Id, Guid PessoaId, string Nome, string Parentesco, string? ParentescoDescricao,
    string NumeroIdentificacao, string Situacao, int? Idade);

public sealed record CategoriaResumo(Guid Id, string Nome, bool DireitoVoto);

public sealed record AssociadoDetalhe(Guid Id, Pessoa Pessoa, string Matricula, CategoriaResumo Categoria, DateOnly DataAssociacao,
    string Situacao, string? MotivoSituacao, bool AptoAVotar, List<DependenteResumo> Dependentes);

public sealed record DependenteLista(Guid Id, Guid PessoaId, string Nome, string? CpfMascarado, int? Idade, string Parentesco,
    string? ParentescoDescricao, string NumeroIdentificacao, string Situacao, Guid TitularId, string TitularNome,
    string TitularMatricula);

public sealed record Carteirinha(Guid PessoaId, string Tipo, string Nome, string NumeroIdentificacao, string Situacao,
    string? Categoria, string? TitularNome, string QrCodeConteudo)
{
    public string? CodigoAcesso { get; init; }
}

public sealed record ResultadoValidacao(bool Autorizado, string Mensagem, Guid? PessoaId, string? Nome, string? Tipo,
    string? NumeroIdentificacao, string? Situacao, string? Categoria, string? TitularNome);

public sealed record Acesso(Guid Id, string? Nome, string Tipo, bool Autorizado, string? Motivo, DateTimeOffset OcorridoEm,
    string? RegistradoPorNome);

public sealed record Presente(Guid PessoaId, string Nome, DateTimeOffset EntradaEm);

public sealed record PerfilResumo(Guid Id, string Nome);

public sealed record UsuarioLista(Guid Id, string Nome, string Email, string SituacaoConta, string SituacaoNoClube,
    List<PerfilResumo> Perfis, DateTimeOffset? UltimoAcessoEm);

public sealed record Perfil(Guid Id, string Nome, string? Descricao, bool Padrao, int TotalUsuarios);

public sealed record ClubeLista(Guid Id, string Nome, string Sigla, string? Cidade, string? Uf, string Situacao,
    bool Demonstracao, int TotalUsuarios);

public sealed record PagamentoResumo(Guid Id, long NumeroRecibo, decimal Valor, decimal Encargos, DateOnly Data, string Forma);

public sealed record Mensalidade(Guid Id, Guid AssociadoId, string AssociadoNome, string Matricula, string Competencia,
    DateOnly Vencimento, decimal Valor, int QuantidadeDependentes, string Situacao, decimal Encargos, decimal ValorAtualizado,
    string? Motivo, PagamentoResumo? Pagamento)
{
    public DateTimeOffset EmitidaEm { get; init; }
    public decimal Multa { get; init; }
    public decimal Juros { get; init; }
    public string? Contrato { get; init; }
}

public sealed record ContratoInfo(Guid Id, string Numero, string Situacao, DateOnly Inicio, DateOnly Fim, decimal ValorMensal,
    int DiaVencimento, List<string> Dependentes, DateTimeOffset DataContrato, DateTimeOffset? AprovadoEm, string? Motivo, bool Vigente,
    List<Mensalidade> Parcelas);

public sealed record HistoricoCadastro(string Evento, string Descricao, string? UsuarioNome, DateTimeOffset OcorridoEm);

public sealed record Inadimplencia(int Associados, int Mensalidades, decimal Valor);

public sealed record ResumoFinanceiro(DateOnly De, DateOnly Ate, decimal ReceitasMensalidades, decimal OutrasReceitas,
    decimal Despesas, decimal Saldo, decimal AReceberMensalidades, decimal AReceberOutros, decimal APagar, Inadimplencia Inadimplencia);

public sealed record GerarResultado(string Competencia, int Geradas, int JaExistentes, decimal ValorTotal, List<string> SemPlano);

public sealed record Plano(Guid Id, string Nome, Guid CategoriaId, string Categoria, decimal ValorMensal, int DiaVencimento,
    bool Ativo, int AssociadosAtivos);

public sealed record CategoriaFinanceira(Guid Id, string Nome, string Tipo);

public sealed record Lancamento(Guid Id, string Tipo, Guid CategoriaId, string Categoria, string Descricao, decimal Valor,
    DateOnly Vencimento, DateOnly? PagoEm, string Situacao);

public sealed record MovimentoCaixa(DateOnly Data, string Descricao, string Tipo, decimal Valor, decimal SaldoAcumulado);

public sealed record FluxoCaixa(DateOnly De, DateOnly Ate, decimal Entradas, decimal Saidas, decimal Saldo, List<MovimentoCaixa> Movimentos);

public sealed record EventoInfo(Guid Id, string Nome, string? Descricao, string? Local, Guid? EspacoId, string? Espaco,
    DateTimeOffset Inicio, DateTimeOffset Fim, string Publico, int? Capacidade, int LugaresOcupados, int NaListaEspera,
    decimal Valor, int MaximoAcompanhantes, string Situacao, string? MotivoCancelamento)
{
    public List<LocalEvento> Locais { get; init; } = [];
    public DateTimeOffset? DivulgadoEm { get; init; }
}

public sealed record LocalEvento(Guid EspacoId, string Nome, int? Capacidade);
public sealed record LocalDisponivel(Guid Id, string Nome, string Tipo, int? Capacidade, bool Livre, bool Comporta, string? Ocupacao);
public sealed record Divulgacao(int AvisosNoApp, int Emails, string Texto, string LinkWhatsApp);

public sealed record InscricaoInfo(Guid Id, Guid EventoId, Guid PessoaId, string Nome, int Acompanhantes, string Situacao,
    DateTimeOffset InscritoEm, DateTimeOffset? PresencaEm);

public sealed record EventoDetalheInfo(EventoInfo Evento, List<InscricaoInfo> Inscricoes);

public sealed record EspacoInfo(Guid Id, string Nome, string Tipo, string? Descricao, int? Capacidade, decimal ValorLocacao,
    decimal TaxaAdicional, bool ExigeAprovacao, string? Regras, bool Ativo)
{
    public int? LimiteReservasPorMes { get; init; }
}

public sealed record ReservaInfo(Guid Id, Guid EspacoId, string Espaco, Guid? PessoaId, string? Solicitante, Guid? EventoId,
    DateTimeOffset Inicio, DateTimeOffset Fim, string? Finalidade, decimal Valor, string Situacao, string? Motivo);

public sealed record ItemCalendario(string Tipo, Guid Id, string Titulo, Guid? EspacoId, string? Espaco, DateTimeOffset Inicio,
    DateTimeOffset Fim, string Situacao);

public sealed record SolicitacaoResumo(Guid Id, string Protocolo, string Tipo, string Assunto, string Situacao, string? Solicitante,
    DateOnly? Prazo, bool Atrasada, DateTimeOffset AbertaEm, int Respostas);

public sealed record RespostaInfo(Guid Id, string Texto, bool DaAdministracao, bool Interna, string? Autor, DateTimeOffset CriadaEm);

public sealed record SolicitacaoInfo(Guid Id, string Protocolo, string Tipo, string Assunto, string Descricao, string Situacao,
    string? Solicitante, string? Responsavel, DateOnly? Prazo, DateTimeOffset AbertaEm, List<RespostaInfo> Respostas);

/// <summary>Rótulos amigáveis para os códigos da API.</summary>
// Etapa 7 — comunicação, operacional e assembleias
public sealed record ComunicadoInfo(Guid Id, string Titulo, string Texto, string Publico, bool ExigeConfirmacao, bool Fixado,
    DateTimeOffset? PublicadoEm, DateTimeOffset? ExpiraEm, DateTimeOffset CriadoEm, int Destinatarios, int Leituras, int Confirmacoes);
public sealed record LeituraInfo(Guid PessoaId, string Nome, DateTimeOffset LidoEm, DateTimeOffset? ConfirmadoEm);
public sealed record ComunicadoLeituras(ComunicadoInfo Comunicado, List<LeituraInfo> Leituras, List<string> Pendentes);
public sealed record HistoricoInfo(string Evento, string Descricao, string? Usuario, DateTimeOffset OcorridoEm);
public sealed record FuncionarioResumo(Guid Id, string Nome, string Cargo, string? Departamento, string? Telefone, string Situacao, DateOnly Admissao);
public sealed record AusenciaInfo(Guid Id, string Tipo, DateOnly Inicio, DateOnly Fim, int Dias, string? Observacao, bool Cancelada);
public sealed record FuncionarioInfo(Guid Id, string Nome, string? Cpf, string Cargo, string? Departamento, string? Telefone, string? Email,
    string? Escala, string? Observacoes, DateOnly Admissao, DateOnly? Desligamento, string Situacao, Guid? UsuarioId, string? UsuarioEmail,
    List<AusenciaInfo> Ausencias, List<HistoricoInfo> Historico);
public sealed record BemResumo(Guid Id, string Codigo, string Nome, string? Localizacao, string Estado, string Situacao, decimal? ValorAquisicao,
    DateOnly? DataAquisicao);
public sealed record ManutencaoInfo(Guid Id, string Descricao, string? Fornecedor, DateOnly Inicio, DateOnly? Fim, decimal? Custo);
public sealed record BemInfo(Guid Id, string Codigo, string Nome, string? Descricao, string? Localizacao, string Estado, string Situacao,
    decimal? ValorAquisicao, DateOnly? DataAquisicao, string? MotivoBaixa, decimal CustoManutencoes, List<ManutencaoInfo> Manutencoes,
    List<HistoricoInfo> Historico);
public sealed record ResumoPatrimonio(int Bens, int EmManutencao, int Baixados, decimal ValorEmUso);
public sealed record ProdutoInfo(Guid Id, string Nome, string Unidade, string? Categoria, decimal EstoqueMinimo, decimal Quantidade,
    bool AbaixoDoMinimo, bool Ativo, decimal? CompraEmAberto);
public sealed record MovimentacaoInfo(Guid Id, Guid ProdutoId, string Produto, string Unidade, string Tipo, decimal Quantidade, decimal SaldoApos,
    string? Motivo, string? Usuario, DateTimeOffset OcorridaEm);
public sealed record CompraInfo(Guid Id, Guid ProdutoId, string Produto, string Unidade, decimal Quantidade, string? Observacao, string Situacao,
    decimal? QuantidadeRecebida, decimal? ValorTotal, DateTimeOffset CriadaEm);
public sealed record AssembleiaResumo(Guid Id, string Titulo, string Tipo, DateTimeOffset DataHora, string? Local, string Situacao, int Pautas, int Presentes);
public sealed record ApuracaoInfo(string Opcao, int Votos);
public sealed record PautaInfo(Guid Id, int Ordem, string Titulo, string? Descricao, bool ComVotacao, List<string> Opcoes, string? Votacao,
    int TotalVotos, List<ApuracaoInfo>? Apuracao, string? Decisao);
public sealed record PresencaInfo(Guid AssociadoId, string Nome, string Matricula, bool AptoVotar, string? MotivoInapto, DateTimeOffset RegistradaEm)
{
    public bool PeloApp { get; init; }
}
public sealed record AssembleiaInfo(Guid Id, string Titulo, string Tipo, DateTimeOffset DataHora, string? Local, string? Convocacao,
    bool ExigeAdimplencia, string Situacao, DateTimeOffset? ConvocadaEm, DateTimeOffset? EncerradaEm, string? MotivoCancelamento, string? Ata,
    DateTimeOffset? AtaRegistradaEm, string? AtaSugerida, int Presentes, int Aptos, List<PautaInfo> Pautas, List<PresencaInfo> Presencas)
{
    public bool ParticipacaoPeloApp { get; init; }
}

// Etapa 8 — perfis, auditoria, configurações e relatórios
public sealed record PermissaoItem(string Codigo, string Modulo, string Descricao);
public sealed record ModuloPermissoes(string Modulo, List<PermissaoItem> Permissoes);
public sealed record PerfilDetalhe(Guid Id, string Nome, string? Descricao, bool Padrao, int TotalUsuarios, List<string> Permissoes);
public sealed record PermissaoIndividual(string Codigo, bool Concedida);
public sealed record UsuarioDetalhe(Guid Id, string Nome, string Email, string SituacaoConta, string SituacaoNoClube, bool SuperAdmin,
    bool DeveTrocarSenha, DateTimeOffset? UltimoAcessoEm, DateTimeOffset CriadoEm, List<PerfilResumo> Perfis,
    List<PermissaoIndividual> PermissoesIndividuais, List<string> PermissoesEfetivas);
public sealed record LogAuditoria(Guid Id, string Entidade, string EntidadeId, string Acao, string? Alteracoes, Guid? UsuarioId,
    string? UsuarioNome, DateTimeOffset OcorridoEm);

public static class Rotulos
{
    public static string Parentesco(string p, string? descricao = null) => p switch
    {
        "Conjuge" => "Cônjuge",
        "Filho" => "Filho(a)",
        "Mae" => "Mãe",
        "Irmao" => "Irmão(ã)",
        "Outro" => descricao ?? "Outro",
        _ => p,
    };

    public static string CorSituacao(string s) => s switch
    {
        "Ativo" or "Aprovado" or "EmUso" or "Atendida" => "success",
        "EmManutencao" or "Rascunho" or "NaoIniciada" => "warning",
        "Ferias" or "Convocada" => "info",
        "Afastado" => "orange",
        "Baixado" or "Desligado" or "Encerrada" => "secondary",
        "Pendente" or "Aberta" or "Em aberto" or "ListaEspera" => "warning",
        "Aprovada" or "Confirmada" or "InscricoesAbertas" or "Concluida" => "success",
        "EmAndamento" => "info",
        "Recusada" or "Cancelado" => "danger",
        "Paga" or "Pago" => "success",
        "Vencida" or "Atrasado" => "danger",
        "Isenta" or "Cancelada" => "secondary",
        "Suspenso" => "orange",
        "Rejeitado" or "Bloqueado" or "Reprovado" => "danger",
        "EmAnalise" => "warning",
        _ => "secondary",
    };

    public static string Cpf(string? cpf) =>
        cpf is { Length: 11 } ? $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}" : cpf ?? "—";

    public static string Telefone(string? t) => t switch
    {
        { Length: 11 } => $"({t[..2]}) {t[2..7]}-{t[7..]}",
        { Length: 10 } => $"({t[..2]}) {t[2..6]}-{t[6..]}",
        _ => t ?? "—",
    };

    public static string Moeda(decimal v) => v.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    public static readonly string[] FormasPagamento = ["Pix", "Dinheiro", "CartaoDebito", "CartaoCredito", "Boleto", "Transferencia"];

    public static string Forma(string f) => f switch
    {
        "CartaoDebito" => "Cartão de débito",
        "CartaoCredito" => "Cartão de crédito",
        "Transferencia" => "Transferência",
        _ => f,
    };

    public static string Texto(string codigo) => codigo switch
    {
        "ListaEspera" => "Lista de espera",
        "InscricoesAbertas" => "Inscrições abertas",
        "InscricoesEncerradas" => "Inscrições encerradas",
        "EmAndamento" => "Em andamento",
        "Concluida" => "Concluída",
        "AssociadosEDependentes" => "Associados e dependentes",
        "Associados" => "Só associados",
        "Geral" => "Todos",
        "ProblemaEstrutural" => "Problema estrutural",
        "AlteracaoCadastral" => "Alteração cadastral",
        "Reclamacao" => "Reclamação",
        "Solicitacao" => "Solicitação",
        "Sugestao" => "Sugestão",
        "SalaoSocial" => "Salão social",
        "CampoFutebol" => "Campo de futebol",
        "SalaReuniao" => "Sala de reunião",
        "EmUso" => "Em uso",
        "EmManutencao" => "Em manutenção",
        "Ferias" => "Férias",
        "NaoIniciada" => "Votação não iniciada",
        "Ordinaria" => "Ordinária",
        "Extraordinaria" => "Extraordinária",
        "Inservivel" => "Inservível",
        "Titulares" => "Só titulares",
        "Todos" => "Associados e dependentes",
        "Saida" => "Saída",
        "Afastamento" => "Afastamento",
        "EmAnalise" => "Em análise",
        "NaoInformado" => "Não informado",
        _ => codigo,
    };

    public static string Periodo(DateTimeOffset inicio, DateTimeOffset fim)
    {
        var i = inicio.ToLocalTime();
        var f = fim.ToLocalTime();
        return i.Date == f.Date ? $"{i:dd/MM/yyyy HH:mm}–{f:HH:mm}" : $"{i:dd/MM HH:mm} até {f:dd/MM HH:mm}";
    }

    public static string Qtd(decimal v) => v.ToString("0.###", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    public static readonly string[] Estados = ["Novo", "Bom", "Regular", "Ruim", "Inservivel"];

    public static readonly string[] Parentescos = ["Conjuge", "Filho", "Pai", "Mae", "Irmao", "Outro"];
    public static readonly string[] Situacoes = ["Pendente", "Ativo", "Suspenso", "Inativo", "Rejeitado"];
}
