using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChargeGuestSemChaveEstrangeira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charge_Guest_ClubId_GuestId",
                table: "Charge");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Guest_ClubId_Id",
                table: "Guest");

            migrationBuilder.DropIndex(
                name: "IX_Charge_ClubId_GuestId",
                table: "Charge");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Guest_ClubId_Id",
                table: "Guest",
                columns: new[] { "ClubId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Charge_ClubId_GuestId",
                table: "Charge",
                columns: new[] { "ClubId", "GuestId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_Guest_ClubId_GuestId",
                table: "Charge",
                columns: new[] { "ClubId", "GuestId" },
                principalTable: "Guest",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
