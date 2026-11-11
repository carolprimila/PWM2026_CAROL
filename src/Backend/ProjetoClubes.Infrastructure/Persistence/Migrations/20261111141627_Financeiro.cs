using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Financeiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasFinanceiras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_CategoriasFinanceiras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriasFinanceiras_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesFinanceiras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MultaPercentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    JurosMensalPercentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DiasTolerancia = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "PlanosMensalidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValorMensal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DiaVencimento = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    PagoEm = table.Column<DateOnly>(type: "date", nullable: true),
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
                name: "Mensalidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorPlano = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ValorDependentes = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    QuantidadeDependentes = table.Column<int>(type: "integer", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensalidades", x => x.Id);
                    table.CheckConstraint("CK_Mensalidades_Valores", "\"ValorPlano\" >= 0 AND \"ValorDependentes\" >= 0 AND EXTRACT(DAY FROM \"Competencia\") = 1");
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
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MensalidadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroRecibo = table.Column<long>(type: "bigint", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Encargos = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Forma = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Estornado = table.Column<bool>(type: "boolean", nullable: false),
                    MotivoEstorno = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_CategoriasFinanceiras_ClubeId_Tipo_Nome",
                table: "CategoriasFinanceiras",
                columns: new[] { "ClubeId", "Tipo", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesFinanceiras_ClubeId",
                table: "ConfiguracoesFinanceiras",
                column: "ClubeId",
                unique: true);

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
                name: "IX_PlanosMensalidade_CategoriaId",
                table: "PlanosMensalidade",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "UX_Planos_CategoriaAtiva",
                table: "PlanosMensalidade",
                columns: new[] { "ClubeId", "CategoriaId" },
                unique: true,
                filter: "\"Ativo\" AND NOT \"Excluido\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesFinanceiras");

            migrationBuilder.DropTable(
                name: "LancamentosFinanceiros");

            migrationBuilder.DropTable(
                name: "Pagamentos");

            migrationBuilder.DropTable(
                name: "CategoriasFinanceiras");

            migrationBuilder.DropTable(
                name: "Mensalidades");

            migrationBuilder.DropTable(
                name: "PlanosMensalidade");
        }
    }
}
