using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Lgpd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnonimizadaEm",
                table: "Pessoas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonimizadaEm",
                table: "Pessoas");
        }
    }
}
