using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContratosEventosDiretoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Profissao",
                table: "Pessoas",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContratoId",
                table: "Mensalidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DivulgadoEm",
                table: "Eventos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LimiteReservasPorMes",
                table: "Espacos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContratosAssociados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValorMensal = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    DiaVencimento = table.Column<int>(type: "INTEGER", nullable: false),
                    Dependentes = table.Column<string>(type: "TEXT", nullable: false),
                    ContratoAnteriorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AprovadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosAssociados", x => x.Id);
                    table.CheckConstraint("CK_Contratos_Dia", "\"DiaVencimento\" BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_Contratos_Periodo", "\"Fim\" > \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_ContratosAssociados_Associados_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembrosDiretoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClubeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    InicioMandato = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FimMandato = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EncerradoEm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembrosDiretoria", x => x.Id);
                    table.CheckConstraint("CK_Diretoria_Mandato", "\"FimMandato\" >= \"InicioMandato\"");
                    table.ForeignKey(
                        name: "FK_MembrosDiretoria_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidades_ContratoId",
                table: "Mensalidades",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "UX_Contratos_ClubeNumero",
                table: "ContratosAssociados",
                columns: new[] { "ClubeId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Contratos_UmEmAnalise",
                table: "ContratosAssociados",
                column: "AssociadoId",
                unique: true,
                filter: "\"Situacao\" = 'EmAnalise'");

            migrationBuilder.CreateIndex(
                name: "IX_MembrosDiretoria_ClubeId_FimMandato",
                table: "MembrosDiretoria",
                columns: new[] { "ClubeId", "FimMandato" });

            migrationBuilder.AddForeignKey(
                name: "FK_Mensalidades_ContratosAssociados_ContratoId",
                table: "Mensalidades",
                column: "ContratoId",
                principalTable: "ContratosAssociados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mensalidades_ContratosAssociados_ContratoId",
                table: "Mensalidades");

            migrationBuilder.DropTable(
                name: "ContratosAssociados");

            migrationBuilder.DropTable(
                name: "MembrosDiretoria");

            migrationBuilder.DropIndex(
                name: "IX_Mensalidades_ContratoId",
                table: "Mensalidades");

            migrationBuilder.DropColumn(
                name: "Profissao",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "ContratoId",
                table: "Mensalidades");

            migrationBuilder.DropColumn(
                name: "DivulgadoEm",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "LimiteReservasPorMes",
                table: "Espacos");
        }
    }
}
