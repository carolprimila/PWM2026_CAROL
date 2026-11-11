using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChavesGeradasNaAplicacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sem alteração de esquema: registra no modelo que os Ids (UUIDv7) são gerados pela aplicação
            // (ValueGeneratedNever). Ver DT-016 em docs/DECISOES_TECNICAS.md.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
