using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clubes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Sigla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Cnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    LogotipoArquivo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    DataFundacao = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Demonstracao = table.Column<bool>(type: "INTEGER", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Cep = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Complemento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Logradouro = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Uf = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubes", x => x.Id);
                    table.CheckConstraint("CK_Clubes_Cnpj", "\"Cnpj\" IS NULL OR \"Cnpj\" GLOB '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_Clubes_Uf", "\"Uf\" IS NULL OR \"Uf\" GLOB '[A-Z][A-Z]'");
                });

            migrationBuilder.CreateTable(
                name: "HistoricoAcessos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Evento = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Sucesso = table.Column<bool>(type: "INTEGER", nullable: false),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EmailInformado = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SessaoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ip = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Detalhe = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAcessos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Entidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Acao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Alteracoes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    Ip = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Modulo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Arquivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeOriginal = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Chave = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arquivos", x => x.Id);
                    table.CheckConstraint("CK_Arquivos_Tamanho", "\"TamanhoBytes\" > 0");
                    table.ForeignKey(
                        name: "FK_Arquivos_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DataHora = table.Column<long>(type: "INTEGER", nullable: false),
                    Local = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Convocacao = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    ExigeAdimplencia = table.Column<bool>(type: "INTEGER", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ConvocadaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    EncerradaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Ata = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    AtaRegistradaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtaRegistradaPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assembleias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assembleias_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Localizacao = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ValorAquisicao = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: true),
                    DataAquisicao = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MotivoBaixa = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bens", x => x.Id);
                    table.CheckConstraint("CK_Bens_Valor", "\"ValorAquisicao\" IS NULL OR \"ValorAquisicao\" >= 0");
                    table.ForeignKey(
                        name: "FK_Bens_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasAssociados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    DireitoVoto = table.Column<bool>(type: "INTEGER", nullable: false),
                    PodeSerVotado = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermiteDependentes = table.Column<bool>(type: "INTEGER", nullable: false),
                    LimiteDependentes = table.Column<int>(type: "INTEGER", nullable: true),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChaveModelo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasAssociados", x => x.Id);
                    table.CheckConstraint("CK_CategoriasAssociados_Limite", "\"LimiteDependentes\" IS NULL OR \"LimiteDependentes\" >= 0");
                    table.ForeignKey(
                        name: "FK_CategoriasAssociados_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasFinanceiras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasFinanceiras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriasFinanceiras_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comunicados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Publico = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ExigeConfirmacao = table.Column<bool>(type: "INTEGER", nullable: false),
                    Fixado = table.Column<bool>(type: "INTEGER", nullable: false),
                    PublicadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExpiraEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunicados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comunicados_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesClube",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CorPrimaria = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    NomeExibicao = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FusoHorario = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ExigeAprovacaoAssociados = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExigeAprovacaoDependentes = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrefixoMatricula = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesClube", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracoesClube_Cor", "\"CorPrimaria\" GLOB '#[0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F]'");
                    table.ForeignKey(
                        name: "FK_ConfiguracoesClube_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesDependentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LimitePadraoDependentes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximoConjuges = table.Column<int>(type: "INTEGER", nullable: false),
                    IdadeMaximaFilhos = table.Column<int>(type: "INTEGER", nullable: true),
                    IdadeMaximaIrmaos = table.Column<int>(type: "INTEGER", nullable: true),
                    IdadeMaioridade = table.Column<int>(type: "INTEGER", nullable: false),
                    IdadeMinimaLoginProprio = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentescosPermitidos = table.Column<string>(type: "TEXT", nullable: false),
                    ExigeDocumentoComprobatorio = table.Column<bool>(type: "INTEGER", nullable: false),
                    MenoresPodemSolicitarReservas = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncluidosNaMensalidadeDoTitular = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValorAdicionalPorDependente = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    PadraoAcessoClube = table.Column<bool>(type: "INTEGER", nullable: false),
                    PadraoInscricaoAtividades = table.Column<bool>(type: "INTEGER", nullable: false),
                    PadraoParticipacaoEventos = table.Column<bool>(type: "INTEGER", nullable: false),
                    PadraoRecebimentoComunicados = table.Column<bool>(type: "INTEGER", nullable: false),
                    PadraoSolicitacaoReservas = table.Column<bool>(type: "INTEGER", nullable: false),
                    PadraoUsoEspacosEsportivos = table.Column<bool>(type: "INTEGER", nullable: false),
                    PadraoUtilizacaoServicos = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesDependentes", x => x.Id);
                    table.CheckConstraint("CK_ConfigDependentes_Limite", "\"LimitePadraoDependentes\" >= 0 AND \"MaximoConjuges\" >= 0");
                    table.CheckConstraint("CK_ConfigDependentes_Valor", "\"ValorAdicionalPorDependente\" >= 0");
                    table.ForeignKey(
                        name: "FK_ConfiguracoesDependentes_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesFinanceiras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MultaPercentual = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    JurosMensalPercentual = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    DiasTolerancia = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesFinanceiras", x => x.Id);
                    table.CheckConstraint("CK_ConfigFinanceira_Valores", "\"MultaPercentual\" >= 0 AND \"JurosMensalPercentual\" >= 0 AND \"DiasTolerancia\" >= 0");
                    table.ForeignKey(
                        name: "FK_ConfiguracoesFinanceiras_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Espacos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Capacidade = table.Column<int>(type: "INTEGER", nullable: true),
                    ValorLocacao = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    TaxaAdicional = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    ExigeAprovacao = table.Column<bool>(type: "INTEGER", nullable: false),
                    Regras = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Espacos", x => x.Id);
                    table.CheckConstraint("CK_Espacos_Valores", "\"ValorLocacao\" >= 0 AND \"TaxaAdicional\" >= 0");
                    table.ForeignKey(
                        name: "FK_Espacos_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Interesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interesses_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Perfis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ChaveModelo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfis_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Unidade = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    EstoqueMinimo = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Quantidade = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.CheckConstraint("CK_Produtos_Minimo", "\"EstoqueMinimo\" >= 0");
                    table.CheckConstraint("CK_Produtos_SaldoNaoNegativo", "\"Quantidade\" >= 0");
                    table.ForeignKey(
                        name: "FK_Produtos_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SequenciasClube",
                columns: table => new
                {
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Valor = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenciasClube", x => new { x.ClubeId, x.Nome });
                    table.ForeignKey(
                        name: "FK_SequenciasClube_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    EmailNormalizado = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SuperAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    TentativasFalhas = table.Column<int>(type: "INTEGER", nullable: false),
                    BloqueadoAte = table.Column<long>(type: "INTEGER", nullable: true),
                    UltimoAcessoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    SenhaAlteradaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    DeveTrocarSenha = table.Column<bool>(type: "INTEGER", nullable: false),
                    CarimboSeguranca = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClubePadraoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Clubes_ClubePadraoId",
                        column: x => x.ClubePadraoId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PautasAssembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssembleiaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ComVotacao = table.Column<bool>(type: "INTEGER", nullable: false),
                    Opcoes = table.Column<string>(type: "TEXT", nullable: false),
                    Votacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Decisao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PautasAssembleias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PautasAssembleias_Assembleias_AssembleiaId",
                        column: x => x.AssembleiaId,
                        principalTable: "Assembleias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoBens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Evento = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoBens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoBens_Bens_BemId",
                        column: x => x.BemId,
                        principalTable: "Bens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManutencoesBens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Fornecedor = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Custo = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutencoesBens", x => x.Id);
                    table.CheckConstraint("CK_Manutencoes_Custo", "\"Custo\" IS NULL OR \"Custo\" >= 0");
                    table.CheckConstraint("CK_Manutencoes_Periodo", "\"Fim\" IS NULL OR \"Fim\" >= \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_ManutencoesBens_Bens_BemId",
                        column: x => x.BemId,
                        principalTable: "Bens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanosMensalidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValorMensal = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    DiaVencimento = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosMensalidade", x => x.Id);
                    table.CheckConstraint("CK_Planos_Dia", "\"DiaVencimento\" BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_Planos_Valor", "\"ValorMensal\" >= 0");
                    table.ForeignKey(
                        name: "FK_PlanosMensalidade_CategoriasAssociados_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriasAssociados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanosMensalidade_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LancamentosFinanceiros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PagoEm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LancamentosFinanceiros", x => x.Id);
                    table.CheckConstraint("CK_Lancamentos_Valor", "\"Valor\" > 0");
                    table.ForeignKey(
                        name: "FK_LancamentosFinanceiros_CategoriasFinanceiras_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriasFinanceiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LancamentosFinanceiros_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Local = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    EspacoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Inicio = table.Column<long>(type: "INTEGER", nullable: false),
                    Fim = table.Column<long>(type: "INTEGER", nullable: false),
                    Capacidade = table.Column<int>(type: "INTEGER", nullable: true),
                    Publico = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    MaximoAcompanhantes = table.Column<int>(type: "INTEGER", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                    table.CheckConstraint("CK_Eventos_Capacidade", "\"Capacidade\" IS NULL OR \"Capacidade\" > 0");
                    table.CheckConstraint("CK_Eventos_Periodo", "\"Fim\" > \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_Eventos_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eventos_Espacos_EspacoId",
                        column: x => x.EspacoId,
                        principalTable: "Espacos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerfilPermissoes",
                columns: table => new
                {
                    PerfilId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissaoId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilPermissoes", x => new { x.PerfilId, x.PermissaoId });
                    table.ForeignKey(
                        name: "FK_PerfilPermissoes_Perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerfilPermissoes_Permissoes_PermissaoId",
                        column: x => x.PermissaoId,
                        principalTable: "Permissoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimentacoesEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Quantidade = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    SaldoApos = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OcorridaEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentacoesEstoque", x => x.Id);
                    table.CheckConstraint("CK_Movimentacoes_Saldo", "\"SaldoApos\" >= 0");
                    table.ForeignKey(
                        name: "FK_MovimentacoesEstoque_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacoesCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantidade = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    QuantidadeRecebida = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: true),
                    ValorTotal = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesCompra", x => x.Id);
                    table.CheckConstraint("CK_Compras_Quantidade", "\"Quantidade\" > 0");
                    table.ForeignKey(
                        name: "FK_SolicitacoesCompra_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Funcionarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Cpf = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Departamento = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Escala = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Admissao = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Desligamento = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcionarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Funcionarios_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Funcionarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadaEm = table.Column<long>(type: "INTEGER", nullable: false),
                    LidaEm = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pessoas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeCompleto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Cpf = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    Rg = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DataNascimento = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Sexo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EstadoCivil = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    FotoArquivoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TokenCarteirinha = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TokenCarteirinhaGeradoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AnonimizadaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Cep = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Complemento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Logradouro = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Uf = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pessoas", x => x.Id);
                    table.CheckConstraint("CK_Pessoas_Cpf", "\"Cpf\" IS NULL OR \"Cpf\" GLOB '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_Pessoas_Uf", "\"Uf\" IS NULL OR \"Uf\" GLOB '[A-Z][A-Z]'");
                    table.ForeignKey(
                        name: "FK_Pessoas_Arquivos_FotoArquivoId",
                        column: x => x.FotoArquivoId,
                        principalTable: "Arquivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pessoas_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pessoas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SessoesUsuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RefreshTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RefreshTokenHashAnterior = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CriadaEm = table.Column<long>(type: "INTEGER", nullable: false),
                    UltimoUsoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiraEm = table.Column<long>(type: "INTEGER", nullable: false),
                    RevogadaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    MotivoRevogacao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Ip = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Dispositivo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesUsuarios_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SessoesUsuarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokensRecuperacaoSenha",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CarimboSeguranca = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiraEm = table.Column<long>(type: "INTEGER", nullable: false),
                    UsadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Ip = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensRecuperacaoSenha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensRecuperacaoSenha_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioClubes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioClubes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioClubes_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioClubes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AusenciasFuncionarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Cancelada = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AusenciasFuncionarios", x => x.Id);
                    table.CheckConstraint("CK_Ausencias_Periodo", "\"Fim\" >= \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_AusenciasFuncionarios_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoFuncionarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Evento = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoFuncionarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoFuncionarios_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcessosClube",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssociadoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DependenteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Autorizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RegistradoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcessosClube", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcessosClube_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcessosClube_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Associados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Matricula = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DataAssociacao = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MotivoSituacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SituacaoAlteradaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Associados_CategoriasAssociados_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriasAssociados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Associados_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Associados_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosPessoais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArquivoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ObservacaoAnalise = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    AnalisadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AnalisadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosPessoais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosPessoais_Arquivos_ArquivoId",
                        column: x => x.ArquivoId,
                        principalTable: "Arquivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosPessoais_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosPessoais_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InscricoesEventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Acompanhantes = table.Column<int>(type: "INTEGER", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PresencaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InscricoesEventos", x => x.Id);
                    table.CheckConstraint("CK_Inscricoes_Acompanhantes", "\"Acompanhantes\" >= 0");
                    table.ForeignKey(
                        name: "FK_InscricoesEventos_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InscricoesEventos_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeiturasComunicados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComunicadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LidoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    ConfirmadoEm = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeiturasComunicados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeiturasComunicados_Comunicados_ComunicadoId",
                        column: x => x.ComunicadoId,
                        principalTable: "Comunicados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeiturasComunicados_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EspacoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Inicio = table.Column<long>(type: "INTEGER", nullable: false),
                    Fim = table.Column<long>(type: "INTEGER", nullable: false),
                    Finalidade = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.CheckConstraint("CK_Reservas_Periodo", "\"Fim\" > \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_Reservas_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Espacos_EspacoId",
                        column: x => x.EspacoId,
                        principalTable: "Espacos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Solicitacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Protocolo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Assunto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ResponsavelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Prazo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioClubePerfis",
                columns: table => new
                {
                    UsuarioClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PerfilId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioClubePerfis", x => new { x.UsuarioClubeId, x.PerfilId });
                    table.ForeignKey(
                        name: "FK_UsuarioClubePerfis_Perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioClubePerfis_UsuarioClubes_UsuarioClubeId",
                        column: x => x.UsuarioClubeId,
                        principalTable: "UsuarioClubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPermissoes",
                columns: table => new
                {
                    UsuarioClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Concedida = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPermissoes", x => new { x.UsuarioClubeId, x.PermissaoId });
                    table.ForeignKey(
                        name: "FK_UsuarioPermissoes_Permissoes_PermissaoId",
                        column: x => x.PermissaoId,
                        principalTable: "Permissoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioPermissoes_UsuarioClubes_UsuarioClubeId",
                        column: x => x.UsuarioClubeId,
                        principalTable: "UsuarioClubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssociadoInteresses",
                columns: table => new
                {
                    AssociadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InteresseId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociadoInteresses", x => new { x.AssociadoId, x.InteresseId });
                    table.ForeignKey(
                        name: "FK_AssociadoInteresses_Associados_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociadoInteresses_Interesses_InteresseId",
                        column: x => x.InteresseId,
                        principalTable: "Interesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dependentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PessoaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociadoTitularId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Parentesco = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ParentescoDescricao = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    NumeroIdentificacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DataInclusao = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExcecaoMultiplosTitulares = table.Column<bool>(type: "INTEGER", nullable: false),
                    JustificativaExcecao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ExcecaoAutorizadaPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    MotivoSituacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SituacaoAlteradaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependentes", x => x.Id);
                    table.CheckConstraint("CK_Dependentes_ParentescoOutro", "\"Parentesco\" <> 'Outro' OR \"ParentescoDescricao\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Dependentes_Associados_AssociadoTitularId",
                        column: x => x.AssociadoTitularId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dependentes_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dependentes_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoAssociados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Evento = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAssociados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoAssociados_Associados_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mensalidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValorPlano = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    ValorDependentes = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    QuantidadeDependentes = table.Column<int>(type: "INTEGER", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensalidades", x => x.Id);
                    table.CheckConstraint("CK_Mensalidades_Valores", "\"ValorPlano\" >= 0 AND \"ValorDependentes\" >= 0 AND substr(\"Competencia\", 9, 2) = '01'");
                    table.ForeignKey(
                        name: "FK_Mensalidades_Associados_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mensalidades_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mensalidades_PlanosMensalidade_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "PlanosMensalidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresencasAssembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssembleiaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AptoVotar = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoInapto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RegistradaPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    RegistradaEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresencasAssembleias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresencasAssembleias_Assembleias_AssembleiaId",
                        column: x => x.AssembleiaId,
                        principalTable: "Assembleias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PresencasAssembleias_Associados_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VotosAssembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PautaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Opcao = table.Column<int>(type: "INTEGER", nullable: false),
                    VotadoEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotosAssembleias", x => x.Id);
                    table.CheckConstraint("CK_Votos_Opcao", "\"Opcao\" >= 0");
                    table.ForeignKey(
                        name: "FK_VotosAssembleias_Associados_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VotosAssembleias_PautasAssembleias_PautaId",
                        column: x => x.PautaId,
                        principalTable: "PautasAssembleias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespostasSolicitacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    AutorUsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DaAdministracao = table.Column<bool>(type: "INTEGER", nullable: false),
                    Interna = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadaEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostasSolicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespostasSolicitacoes_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoDependentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependenteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Evento = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OcorridoEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoDependentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoDependentes_Dependentes_DependenteId",
                        column: x => x.DependenteId,
                        principalTable: "Dependentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissoesDependentes",
                columns: table => new
                {
                    DependenteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AcessoClube = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParticipacaoEventos = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsoEspacosEsportivos = table.Column<bool>(type: "INTEGER", nullable: false),
                    InscricaoAtividades = table.Column<bool>(type: "INTEGER", nullable: false),
                    SolicitacaoReservas = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecebimentoComunicados = table.Column<bool>(type: "INTEGER", nullable: false),
                    UtilizacaoServicos = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissoesDependentes", x => x.DependenteId);
                    table.ForeignKey(
                        name: "FK_PermissoesDependentes_Dependentes_DependenteId",
                        column: x => x.DependenteId,
                        principalTable: "Dependentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VinculosDependentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependenteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociadoTitularId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Inicio = table.Column<long>(type: "INTEGER", nullable: false),
                    Fim = table.Column<long>(type: "INTEGER", nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    RegistradoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosDependentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinculosDependentes_Associados_AssociadoTitularId",
                        column: x => x.AssociadoTitularId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosDependentes_Dependentes_DependenteId",
                        column: x => x.DependenteId,
                        principalTable: "Dependentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MensalidadeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NumeroRecibo = table.Column<long>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Encargos = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Forma = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Estornado = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoEstorno = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                    table.CheckConstraint("CK_Pagamentos_Valor", "\"Valor\" > 0 AND \"Encargos\" >= 0");
                    table.ForeignKey(
                        name: "FK_Pagamentos_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Mensalidades_MensalidadeId",
                        column: x => x.MensalidadeId,
                        principalTable: "Mensalidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcessosClube_ClubeId_OcorridoEm",
                table: "AcessosClube",
                columns: new[] { "ClubeId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_AcessosClube_PessoaId_OcorridoEm",
                table: "AcessosClube",
                columns: new[] { "PessoaId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Arquivos_ClubeId",
                table: "Arquivos",
                column: "ClubeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assembleias_ClubeId_DataHora",
                table: "Assembleias",
                columns: new[] { "ClubeId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_AssociadoInteresses_InteresseId",
                table: "AssociadoInteresses",
                column: "InteresseId");

            migrationBuilder.CreateIndex(
                name: "IX_Associados_CategoriaId",
                table: "Associados",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Associados_ClubeId_Matricula",
                table: "Associados",
                columns: new[] { "ClubeId", "Matricula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associados_ClubeId_Situacao",
                table: "Associados",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Associados_PessoaId",
                table: "Associados",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "UX_Associados_PessoaVigente",
                table: "Associados",
                columns: new[] { "ClubeId", "PessoaId" },
                unique: true,
                filter: "\"Situacao\" IN ('Pendente', 'Ativo', 'Suspenso') AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_AusenciasFuncionarios_FuncionarioId_Inicio",
                table: "AusenciasFuncionarios",
                columns: new[] { "FuncionarioId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Bens_ClubeId_Situacao",
                table: "Bens",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "UX_Bens_ClubeCodigo",
                table: "Bens",
                columns: new[] { "ClubeId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasAssociados_ClubeId_Nome",
                table: "CategoriasAssociados",
                columns: new[] { "ClubeId", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasFinanceiras_ClubeId_Tipo_Nome",
                table: "CategoriasFinanceiras",
                columns: new[] { "ClubeId", "Tipo", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Clubes_Cnpj",
                table: "Clubes",
                column: "Cnpj",
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Clubes_Nome",
                table: "Clubes",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Clubes_Sigla",
                table: "Clubes",
                column: "Sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comunicados_ClubeId_PublicadoEm",
                table: "Comunicados",
                columns: new[] { "ClubeId", "PublicadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesClube_ClubeId",
                table: "ConfiguracoesClube",
                column: "ClubeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesDependentes_ClubeId",
                table: "ConfiguracoesDependentes",
                column: "ClubeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesFinanceiras_ClubeId",
                table: "ConfiguracoesFinanceiras",
                column: "ClubeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dependentes_ClubeId_NumeroIdentificacao",
                table: "Dependentes",
                columns: new[] { "ClubeId", "NumeroIdentificacao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dependentes_ClubeId_Situacao",
                table: "Dependentes",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Dependentes_PessoaId",
                table: "Dependentes",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "UX_Dependentes_PessoaVigente",
                table: "Dependentes",
                columns: new[] { "ClubeId", "PessoaId" },
                unique: true,
                filter: "\"Situacao\" IN ('Pendente', 'Ativo', 'Suspenso') AND NOT \"Excluido\" AND NOT \"ExcecaoMultiplosTitulares\"");

            migrationBuilder.CreateIndex(
                name: "UX_Dependentes_TitularPessoaVigente",
                table: "Dependentes",
                columns: new[] { "AssociadoTitularId", "PessoaId" },
                unique: true,
                filter: "\"Situacao\" IN ('Pendente', 'Ativo', 'Suspenso') AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPessoais_ArquivoId",
                table: "DocumentosPessoais",
                column: "ArquivoId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPessoais_ClubeId",
                table: "DocumentosPessoais",
                column: "ClubeId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPessoais_PessoaId_Situacao",
                table: "DocumentosPessoais",
                columns: new[] { "PessoaId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Espacos_ClubeId_Nome",
                table: "Espacos",
                columns: new[] { "ClubeId", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_ClubeId_Inicio",
                table: "Eventos",
                columns: new[] { "ClubeId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_EspacoId",
                table: "Eventos",
                column: "EspacoId");

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_ClubeId_Nome",
                table: "Funcionarios",
                columns: new[] { "ClubeId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_UsuarioId",
                table: "Funcionarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_Funcionarios_ClubeCpf",
                table: "Funcionarios",
                columns: new[] { "ClubeId", "Cpf" },
                unique: true,
                filter: "\"Cpf\" IS NOT NULL AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAcessos_ClubeId_OcorridoEm",
                table: "HistoricoAcessos",
                columns: new[] { "ClubeId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAcessos_UsuarioId_OcorridoEm",
                table: "HistoricoAcessos",
                columns: new[] { "UsuarioId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAssociados_AssociadoId_OcorridoEm",
                table: "HistoricoAssociados",
                columns: new[] { "AssociadoId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoBens_BemId_OcorridoEm",
                table: "HistoricoBens",
                columns: new[] { "BemId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoDependentes_DependenteId_OcorridoEm",
                table: "HistoricoDependentes",
                columns: new[] { "DependenteId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoFuncionarios_FuncionarioId_OcorridoEm",
                table: "HistoricoFuncionarios",
                columns: new[] { "FuncionarioId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_InscricoesEventos_EventoId_Situacao",
                table: "InscricoesEventos",
                columns: new[] { "EventoId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_InscricoesEventos_PessoaId",
                table: "InscricoesEventos",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "UX_Inscricoes_PessoaEvento",
                table: "InscricoesEventos",
                columns: new[] { "EventoId", "PessoaId" },
                unique: true,
                filter: "\"Situacao\" <> 'Cancelada'");

            migrationBuilder.CreateIndex(
                name: "IX_Interesses_ClubeId_Nome",
                table: "Interesses",
                columns: new[] { "ClubeId", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosFinanceiros_CategoriaId",
                table: "LancamentosFinanceiros",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosFinanceiros_ClubeId_Tipo_PagoEm",
                table: "LancamentosFinanceiros",
                columns: new[] { "ClubeId", "Tipo", "PagoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosFinanceiros_ClubeId_Vencimento",
                table: "LancamentosFinanceiros",
                columns: new[] { "ClubeId", "Vencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_LeiturasComunicados_PessoaId",
                table: "LeiturasComunicados",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "UX_Leituras_ComunicadoPessoa",
                table: "LeiturasComunicados",
                columns: new[] { "ComunicadoId", "PessoaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_ClubeId_OcorridoEm",
                table: "LogsAuditoria",
                columns: new[] { "ClubeId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_Entidade_EntidadeId",
                table: "LogsAuditoria",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_UsuarioId",
                table: "LogsAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_Manutencoes_BemAberta",
                table: "ManutencoesBens",
                column: "BemId",
                unique: true,
                filter: "\"Fim\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidades_ClubeId_Competencia",
                table: "Mensalidades",
                columns: new[] { "ClubeId", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidades_ClubeId_Situacao_Vencimento",
                table: "Mensalidades",
                columns: new[] { "ClubeId", "Situacao", "Vencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidades_PlanoId",
                table: "Mensalidades",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "UX_Mensalidades_AssociadoCompetencia",
                table: "Mensalidades",
                columns: new[] { "AssociadoId", "Competencia" },
                unique: true,
                filter: "\"Situacao\" <> 'Cancelada'");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_ClubeId_OcorridaEm",
                table: "MovimentacoesEstoque",
                columns: new[] { "ClubeId", "OcorridaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_ProdutoId_OcorridaEm",
                table: "MovimentacoesEstoque",
                columns: new[] { "ProdutoId", "OcorridaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_ClubeId",
                table: "Notificacoes",
                column: "ClubeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioId_ClubeId_CriadaEm",
                table: "Notificacoes",
                columns: new[] { "UsuarioId", "ClubeId", "CriadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ClubeId_Data",
                table: "Pagamentos",
                columns: new[] { "ClubeId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ClubeId_NumeroRecibo",
                table: "Pagamentos",
                columns: new[] { "ClubeId", "NumeroRecibo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Pagamentos_MensalidadeValido",
                table: "Pagamentos",
                column: "MensalidadeId",
                unique: true,
                filter: "NOT \"Estornado\"");

            migrationBuilder.CreateIndex(
                name: "IX_PautasAssembleias_AssembleiaId_Ordem",
                table: "PautasAssembleias",
                columns: new[] { "AssembleiaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_PerfilPermissoes_PermissaoId",
                table: "PerfilPermissoes",
                column: "PermissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfis_ClubeId_ChaveModelo",
                table: "Perfis",
                columns: new[] { "ClubeId", "ChaveModelo" },
                unique: true,
                filter: "\"ChaveModelo\" IS NOT NULL AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Perfis_ClubeId_Nome",
                table: "Perfis",
                columns: new[] { "ClubeId", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Permissoes_Codigo",
                table: "Permissoes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_ClubeId_Cpf",
                table: "Pessoas",
                columns: new[] { "ClubeId", "Cpf" },
                unique: true,
                filter: "\"Cpf\" IS NOT NULL AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_ClubeId_NomeCompleto",
                table: "Pessoas",
                columns: new[] { "ClubeId", "NomeCompleto" });

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_ClubeId_UsuarioId",
                table: "Pessoas",
                columns: new[] { "ClubeId", "UsuarioId" },
                unique: true,
                filter: "\"UsuarioId\" IS NOT NULL AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_FotoArquivoId",
                table: "Pessoas",
                column: "FotoArquivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_TokenCarteirinha",
                table: "Pessoas",
                column: "TokenCarteirinha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_UsuarioId",
                table: "Pessoas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosMensalidade_CategoriaId",
                table: "PlanosMensalidade",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "UX_Planos_CategoriaAtiva",
                table: "PlanosMensalidade",
                columns: new[] { "ClubeId", "CategoriaId" },
                unique: true,
                filter: "\"Ativo\" AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_PresencasAssembleias_AssociadoId",
                table: "PresencasAssembleias",
                column: "AssociadoId");

            migrationBuilder.CreateIndex(
                name: "UX_Presencas_AssembleiaAssociado",
                table: "PresencasAssembleias",
                columns: new[] { "AssembleiaId", "AssociadoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Produtos_ClubeNome",
                table: "Produtos",
                columns: new[] { "ClubeId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_ClubeId_Situacao",
                table: "Reservas",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_EspacoId_Inicio",
                table: "Reservas",
                columns: new[] { "EspacoId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_EventoId",
                table: "Reservas",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_PessoaId",
                table: "Reservas",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostasSolicitacoes_SolicitacaoId_CriadaEm",
                table: "RespostasSolicitacoes",
                columns: new[] { "SolicitacaoId", "CriadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_ClubeId",
                table: "SessoesUsuarios",
                column: "ClubeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_RefreshTokenHash",
                table: "SessoesUsuarios",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_RefreshTokenHashAnterior",
                table: "SessoesUsuarios",
                column: "RefreshTokenHashAnterior");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_UsuarioId_RevogadaEm",
                table: "SessoesUsuarios",
                columns: new[] { "UsuarioId", "RevogadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_ClubeId_Protocolo",
                table: "Solicitacoes",
                columns: new[] { "ClubeId", "Protocolo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_ClubeId_Situacao",
                table: "Solicitacoes",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_PessoaId",
                table: "Solicitacoes",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesCompra_ClubeId_Situacao",
                table: "SolicitacoesCompra",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesCompra_ProdutoId",
                table: "SolicitacoesCompra",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensRecuperacaoSenha_TokenHash",
                table: "TokensRecuperacaoSenha",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensRecuperacaoSenha_UsuarioId",
                table: "TokensRecuperacaoSenha",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClubePerfis_PerfilId",
                table: "UsuarioClubePerfis",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClubes_ClubeId_Situacao",
                table: "UsuarioClubes",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClubes_UsuarioId_ClubeId",
                table: "UsuarioClubes",
                columns: new[] { "UsuarioId", "ClubeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPermissoes_PermissaoId",
                table: "UsuarioPermissoes",
                column: "PermissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ClubePadraoId",
                table: "Usuarios",
                column: "ClubePadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmailNormalizado",
                table: "Usuarios",
                column: "EmailNormalizado",
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_SuperAdmin",
                table: "Usuarios",
                column: "SuperAdmin",
                filter: "\"SuperAdmin\"");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosDependentes_AssociadoTitularId",
                table: "VinculosDependentes",
                column: "AssociadoTitularId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosDependentes_DependenteId",
                table: "VinculosDependentes",
                column: "DependenteId");

            migrationBuilder.CreateIndex(
                name: "IX_VotosAssembleias_AssociadoId",
                table: "VotosAssembleias",
                column: "AssociadoId");

            migrationBuilder.CreateIndex(
                name: "UX_Votos_PautaAssociado",
                table: "VotosAssembleias",
                columns: new[] { "PautaId", "AssociadoId" },
                unique: true);

            // Sem conflito de horário na mesma agenda, garantido pelo banco (equivalente à restrição de exclusão do PostgreSQL):
            // duas reservas ativas (Pendente/Aprovada) do mesmo espaço não podem se sobrepor. Intervalo semiaberto [Inicio, Fim).
            foreach (var (nome, evento) in new[] { ("TR_Reservas_SemConflito_Insert", "INSERT"),
                         ("TR_Reservas_SemConflito_Update", "UPDATE OF \"EspacoId\", \"Inicio\", \"Fim\", \"Situacao\"") })
            {
                migrationBuilder.Sql($"""
                    CREATE TRIGGER "{nome}" BEFORE {evento} ON "Reservas"
                    WHEN NEW."Situacao" IN ('Pendente', 'Aprovada')
                    BEGIN
                        SELECT RAISE(ABORT, 'conflito_horario')
                        WHERE EXISTS (
                            SELECT 1 FROM "Reservas" r
                            WHERE r."EspacoId" = NEW."EspacoId" AND r."Id" <> NEW."Id"
                              AND r."Situacao" IN ('Pendente', 'Aprovada')
                              AND r."Inicio" < NEW."Fim" AND NEW."Inicio" < r."Fim");
                    END;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcessosClube");

            migrationBuilder.DropTable(
                name: "AssociadoInteresses");

            migrationBuilder.DropTable(
                name: "AusenciasFuncionarios");

            migrationBuilder.DropTable(
                name: "ConfiguracoesClube");

            migrationBuilder.DropTable(
                name: "ConfiguracoesDependentes");

            migrationBuilder.DropTable(
                name: "ConfiguracoesFinanceiras");

            migrationBuilder.DropTable(
                name: "DocumentosPessoais");

            migrationBuilder.DropTable(
                name: "HistoricoAcessos");

            migrationBuilder.DropTable(
                name: "HistoricoAssociados");

            migrationBuilder.DropTable(
                name: "HistoricoBens");

            migrationBuilder.DropTable(
                name: "HistoricoDependentes");

            migrationBuilder.DropTable(
                name: "HistoricoFuncionarios");

            migrationBuilder.DropTable(
                name: "InscricoesEventos");

            migrationBuilder.DropTable(
                name: "LancamentosFinanceiros");

            migrationBuilder.DropTable(
                name: "LeiturasComunicados");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.DropTable(
                name: "ManutencoesBens");

            migrationBuilder.DropTable(
                name: "MovimentacoesEstoque");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "Pagamentos");

            migrationBuilder.DropTable(
                name: "PerfilPermissoes");

            migrationBuilder.DropTable(
                name: "PermissoesDependentes");

            migrationBuilder.DropTable(
                name: "PresencasAssembleias");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "RespostasSolicitacoes");

            migrationBuilder.DropTable(
                name: "SequenciasClube");

            migrationBuilder.DropTable(
                name: "SessoesUsuarios");

            migrationBuilder.DropTable(
                name: "SolicitacoesCompra");

            migrationBuilder.DropTable(
                name: "TokensRecuperacaoSenha");

            migrationBuilder.DropTable(
                name: "UsuarioClubePerfis");

            migrationBuilder.DropTable(
                name: "UsuarioPermissoes");

            migrationBuilder.DropTable(
                name: "VinculosDependentes");

            migrationBuilder.DropTable(
                name: "VotosAssembleias");

            migrationBuilder.DropTable(
                name: "Interesses");

            migrationBuilder.DropTable(
                name: "Funcionarios");

            migrationBuilder.DropTable(
                name: "CategoriasFinanceiras");

            migrationBuilder.DropTable(
                name: "Comunicados");

            migrationBuilder.DropTable(
                name: "Bens");

            migrationBuilder.DropTable(
                name: "Mensalidades");

            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DropTable(
                name: "Solicitacoes");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Perfis");

            migrationBuilder.DropTable(
                name: "Permissoes");

            migrationBuilder.DropTable(
                name: "UsuarioClubes");

            migrationBuilder.DropTable(
                name: "Dependentes");

            migrationBuilder.DropTable(
                name: "PautasAssembleias");

            migrationBuilder.DropTable(
                name: "PlanosMensalidade");

            migrationBuilder.DropTable(
                name: "Espacos");

            migrationBuilder.DropTable(
                name: "Associados");

            migrationBuilder.DropTable(
                name: "Assembleias");

            migrationBuilder.DropTable(
                name: "CategoriasAssociados");

            migrationBuilder.DropTable(
                name: "Pessoas");

            migrationBuilder.DropTable(
                name: "Arquivos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Clubes");
        }
    }
}
