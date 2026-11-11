using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CarteirinhaEVotoPeloApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PeloApp",
                table: "PresencasAssembleias",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CodigoAcesso",
                table: "Pessoas",
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ParticipacaoPeloApp",
                table: "Assembleias",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_ClubeId_CodigoAcesso",
                table: "Pessoas",
                columns: new[] { "ClubeId", "CodigoAcesso" },
                unique: true,
                filter: "\"CodigoAcesso\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pessoas_ClubeId_CodigoAcesso",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "PeloApp",
                table: "PresencasAssembleias");

            migrationBuilder.DropColumn(
                name: "CodigoAcesso",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "ParticipacaoPeloApp",
                table: "Assembleias");
        }
    }
}
