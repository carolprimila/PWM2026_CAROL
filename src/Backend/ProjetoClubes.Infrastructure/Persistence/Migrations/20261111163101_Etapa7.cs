using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Etapa7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Local = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Convocacao = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ExigeAdimplencia = table.Column<bool>(type: "boolean", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConvocadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EncerradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Ata = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    AtaRegistradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtaRegistradaPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Localizacao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ValorAquisicao = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    DataAquisicao = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MotivoBaixa = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "Comunicados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Texto = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Publico = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExigeConfirmacao = table.Column<bool>(type: "boolean", nullable: false),
                    Fixado = table.Column<bool>(type: "boolean", nullable: false),
                    PublicadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Comunicados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comunicados_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Funcionarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    Cargo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Departamento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Escala = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Admissao = table.Column<DateOnly>(type: "date", nullable: false),
                    Desligamento = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LidaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    EstoqueMinimo = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "PautasAssembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssembleiaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ComVotacao = table.Column<bool>(type: "boolean", nullable: false),
                    Opcoes = table.Column<List<string>>(type: "text[]", nullable: false),
                    Votacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Decisao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
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
                name: "PresencasAssembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssembleiaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AptoVotar = table.Column<bool>(type: "boolean", nullable: false),
                    MotivoInapto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RegistradaPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RegistradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "HistoricoBens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Fornecedor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fim = table.Column<DateOnly>(type: "date", nullable: true),
                    Custo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true)
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
                name: "LeiturasComunicados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComunicadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    LidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                name: "AusenciasFuncionarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fim = table.Column<DateOnly>(type: "date", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Cancelada = table.Column<bool>(type: "boolean", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "MovimentacoesEstoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SaldoApos = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    OcorridaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    QuantidadeRecebida = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    ValorTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "VotosAssembleias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PautaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Opcao = table.Column<int>(type: "integer", nullable: false),
                    VotadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Assembleias_ClubeId_DataHora",
                table: "Assembleias",
                columns: new[] { "ClubeId", "DataHora" });

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
                name: "IX_Comunicados_ClubeId_PublicadoEm",
                table: "Comunicados",
                columns: new[] { "ClubeId", "PublicadoEm" });

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
                name: "IX_HistoricoBens_BemId_OcorridoEm",
                table: "HistoricoBens",
                columns: new[] { "BemId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoFuncionarios_FuncionarioId_OcorridoEm",
                table: "HistoricoFuncionarios",
                columns: new[] { "FuncionarioId", "OcorridoEm" });

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
                name: "UX_Manutencoes_BemAberta",
                table: "ManutencoesBens",
                column: "BemId",
                unique: true,
                filter: "\"Fim\" IS NULL");

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
                name: "IX_PautasAssembleias_AssembleiaId_Ordem",
                table: "PautasAssembleias",
                columns: new[] { "AssembleiaId", "Ordem" });

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
                name: "IX_SolicitacoesCompra_ClubeId_Situacao",
                table: "SolicitacoesCompra",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesCompra_ProdutoId",
                table: "SolicitacoesCompra",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_VotosAssembleias_AssociadoId",
                table: "VotosAssembleias",
                column: "AssociadoId");

            migrationBuilder.CreateIndex(
                name: "UX_Votos_PautaAssociado",
                table: "VotosAssembleias",
                columns: new[] { "PautaId", "AssociadoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AusenciasFuncionarios");

            migrationBuilder.DropTable(
                name: "HistoricoBens");

            migrationBuilder.DropTable(
                name: "HistoricoFuncionarios");

            migrationBuilder.DropTable(
                name: "LeiturasComunicados");

            migrationBuilder.DropTable(
                name: "ManutencoesBens");

            migrationBuilder.DropTable(
                name: "MovimentacoesEstoque");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "PresencasAssembleias");

            migrationBuilder.DropTable(
                name: "SolicitacoesCompra");

            migrationBuilder.DropTable(
                name: "VotosAssembleias");

            migrationBuilder.DropTable(
                name: "Funcionarios");

            migrationBuilder.DropTable(
                name: "Comunicados");

            migrationBuilder.DropTable(
                name: "Bens");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "PautasAssembleias");

            migrationBuilder.DropTable(
                name: "Assembleias");
        }
    }
}
