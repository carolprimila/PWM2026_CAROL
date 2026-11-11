using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cadastros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Arquivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Chave = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "CategoriasAssociados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DireitoVoto = table.Column<bool>(type: "boolean", nullable: false),
                    PodeSerVotado = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteDependentes = table.Column<bool>(type: "boolean", nullable: false),
                    LimiteDependentes = table.Column<int>(type: "integer", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    ChaveModelo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "ConfiguracoesDependentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LimitePadraoDependentes = table.Column<int>(type: "integer", nullable: false),
                    MaximoConjuges = table.Column<int>(type: "integer", nullable: false),
                    IdadeMaximaFilhos = table.Column<int>(type: "integer", nullable: true),
                    IdadeMaximaIrmaos = table.Column<int>(type: "integer", nullable: true),
                    IdadeMaioridade = table.Column<int>(type: "integer", nullable: false),
                    IdadeMinimaLoginProprio = table.Column<int>(type: "integer", nullable: false),
                    ParentescosPermitidos = table.Column<string[]>(type: "text[]", nullable: false),
                    ExigeDocumentoComprobatorio = table.Column<bool>(type: "boolean", nullable: false),
                    MenoresPodemSolicitarReservas = table.Column<bool>(type: "boolean", nullable: false),
                    IncluidosNaMensalidadeDoTitular = table.Column<bool>(type: "boolean", nullable: false),
                    ValorAdicionalPorDependente = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PadraoAcessoClube = table.Column<bool>(type: "boolean", nullable: false),
                    PadraoInscricaoAtividades = table.Column<bool>(type: "boolean", nullable: false),
                    PadraoParticipacaoEventos = table.Column<bool>(type: "boolean", nullable: false),
                    PadraoRecebimentoComunicados = table.Column<bool>(type: "boolean", nullable: false),
                    PadraoSolicitacaoReservas = table.Column<bool>(type: "boolean", nullable: false),
                    PadraoUsoEspacosEsportivos = table.Column<bool>(type: "boolean", nullable: false),
                    PadraoUtilizacaoServicos = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "Interesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "SequenciasClube",
                columns: table => new
                {
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Valor = table.Column<long>(type: "bigint", nullable: false)
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
                name: "Pessoas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    Rg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    Sexo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EstadoCivil = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    FotoArquivoId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    TokenCarteirinha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TokenCarteirinhaGeradoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pessoas", x => x.Id);
                    table.CheckConstraint("CK_Pessoas_Cpf", "\"Cpf\" IS NULL OR \"Cpf\" ~ '^[0-9]{11}$'");
                    table.CheckConstraint("CK_Pessoas_Uf", "\"Uf\" IS NULL OR \"Uf\" ~ '^[A-Z]{2}$'");
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
                name: "AcessosClube",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssociadoId = table.Column<Guid>(type: "uuid", nullable: true),
                    DependenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Autorizado = table.Column<bool>(type: "boolean", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RegistradoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Matricula = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataAssociacao = table.Column<DateOnly>(type: "date", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MotivoSituacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SituacaoAlteradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArquivoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ObservacaoAnalise = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AnalisadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AnalisadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "AssociadoInteresses",
                columns: table => new
                {
                    AssociadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    InteresseId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociadoTitularId = table.Column<Guid>(type: "uuid", nullable: false),
                    Parentesco = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ParentescoDescricao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NumeroIdentificacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DataInclusao = table.Column<DateOnly>(type: "date", nullable: false),
                    ExcecaoMultiplosTitulares = table.Column<bool>(type: "boolean", nullable: false),
                    JustificativaExcecao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ExcecaoAutorizadaPor = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MotivoSituacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SituacaoAlteradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "HistoricoDependentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DependenteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    DependenteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcessoClube = table.Column<bool>(type: "boolean", nullable: false),
                    ParticipacaoEventos = table.Column<bool>(type: "boolean", nullable: false),
                    UsoEspacosEsportivos = table.Column<bool>(type: "boolean", nullable: false),
                    InscricaoAtividades = table.Column<bool>(type: "boolean", nullable: false),
                    SolicitacaoReservas = table.Column<bool>(type: "boolean", nullable: false),
                    RecebimentoComunicados = table.Column<bool>(type: "boolean", nullable: false),
                    UtilizacaoServicos = table.Column<bool>(type: "boolean", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependenteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociadoTitularId = table.Column<Guid>(type: "uuid", nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RegistradoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_CategoriasAssociados_ClubeId_Nome",
                table: "CategoriasAssociados",
                columns: new[] { "ClubeId", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesDependentes_ClubeId",
                table: "ConfiguracoesDependentes",
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
                name: "IX_HistoricoAssociados_AssociadoId_OcorridoEm",
                table: "HistoricoAssociados",
                columns: new[] { "AssociadoId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoDependentes_DependenteId_OcorridoEm",
                table: "HistoricoDependentes",
                columns: new[] { "DependenteId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Interesses_ClubeId_Nome",
                table: "Interesses",
                columns: new[] { "ClubeId", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

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
                name: "IX_VinculosDependentes_AssociadoTitularId",
                table: "VinculosDependentes",
                column: "AssociadoTitularId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosDependentes_DependenteId",
                table: "VinculosDependentes",
                column: "DependenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcessosClube");

            migrationBuilder.DropTable(
                name: "AssociadoInteresses");

            migrationBuilder.DropTable(
                name: "ConfiguracoesDependentes");

            migrationBuilder.DropTable(
                name: "DocumentosPessoais");

            migrationBuilder.DropTable(
                name: "HistoricoAssociados");

            migrationBuilder.DropTable(
                name: "HistoricoDependentes");

            migrationBuilder.DropTable(
                name: "PermissoesDependentes");

            migrationBuilder.DropTable(
                name: "SequenciasClube");

            migrationBuilder.DropTable(
                name: "VinculosDependentes");

            migrationBuilder.DropTable(
                name: "Interesses");

            migrationBuilder.DropTable(
                name: "Dependentes");

            migrationBuilder.DropTable(
                name: "Associados");

            migrationBuilder.DropTable(
                name: "CategoriasAssociados");

            migrationBuilder.DropTable(
                name: "Pessoas");

            migrationBuilder.DropTable(
                name: "Arquivos");
        }
    }
}
