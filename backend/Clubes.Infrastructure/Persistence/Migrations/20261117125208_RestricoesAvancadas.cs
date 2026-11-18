using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clubes.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Regras garantidas pelo próprio PostgreSQL, independentemente da aplicação:
    /// 1. Reservas: dois intervalos ativos do mesmo espaço nunca se sobrepõem (restrição de exclusão).
    /// 2. Auditoria: registros não podem ser alterados nem apagados.
    /// 3. Valores financeiros e limites básicos coerentes (CHECK).
    /// </summary>
    public partial class RestricoesAvancadas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "ReservationSlot"
                    ADD CONSTRAINT "CK_ReservationSlot_Periodo" CHECK ("EndAt" > "StartAt");

                ALTER TABLE "ReservationSlot"
                    ADD CONSTRAINT "EX_ReservationSlot_NoOverlap"
                    EXCLUDE USING gist ("SpaceId" WITH =, tstzrange("StartAt", "EndAt", '[)') WITH &&)
                    WHERE ("IsActive");
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION auditlog_somente_insercao() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Registros de auditoria são imutáveis (operação % não permitida).', TG_OP
                        USING ERRCODE = 'integrity_constraint_violation';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_AuditLog_Imutavel"
                    BEFORE UPDATE OR DELETE ON "AuditLog"
                    FOR EACH ROW EXECUTE FUNCTION auditlog_somente_insercao();

                CREATE TRIGGER "TR_AuditLog_SemTruncate"
                    BEFORE TRUNCATE ON "AuditLog"
                    FOR EACH STATEMENT EXECUTE FUNCTION auditlog_somente_insercao();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Charge" ADD CONSTRAINT "CK_Charge_Valores" CHECK (
                    "OriginalAmount" >= 0 AND "Additions" >= 0 AND "Discounts" >= 0 AND "FineAmount" >= 0
                    AND "InterestAmount" >= 0 AND "FeesAmount" >= 0 AND "PaidAmount" >= 0
                    AND "PaidAmount" <= "OriginalAmount" + "Additions" + "FineAmount" + "InterestAmount" + "FeesAmount" - "Discounts");
                ALTER TABLE "Payment" ADD CONSTRAINT "CK_Payment_Valor" CHECK ("Amount" > 0);
                ALTER TABLE "FinancialEntry" ADD CONSTRAINT "CK_FinancialEntry_Valor" CHECK ("Amount" > 0);
                ALTER TABLE "StockItem" ADD CONSTRAINT "CK_StockItem_Saldo" CHECK ("Quantity" >= 0);
                ALTER TABLE "SpaceBlock" ADD CONSTRAINT "CK_SpaceBlock_Periodo" CHECK ("EndAt" > "StartAt");
                ALTER TABLE "Reservation" ADD CONSTRAINT "CK_Reservation_Pessoas" CHECK ("PeopleCount" >= 1);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Reservation" DROP CONSTRAINT IF EXISTS "CK_Reservation_Pessoas";
                ALTER TABLE "SpaceBlock" DROP CONSTRAINT IF EXISTS "CK_SpaceBlock_Periodo";
                ALTER TABLE "StockItem" DROP CONSTRAINT IF EXISTS "CK_StockItem_Saldo";
                ALTER TABLE "FinancialEntry" DROP CONSTRAINT IF EXISTS "CK_FinancialEntry_Valor";
                ALTER TABLE "Payment" DROP CONSTRAINT IF EXISTS "CK_Payment_Valor";
                ALTER TABLE "Charge" DROP CONSTRAINT IF EXISTS "CK_Charge_Valores";
                DROP TRIGGER IF EXISTS "TR_AuditLog_SemTruncate" ON "AuditLog";
                DROP TRIGGER IF EXISTS "TR_AuditLog_Imutavel" ON "AuditLog";
                DROP FUNCTION IF EXISTS auditlog_somente_insercao();
                ALTER TABLE "ReservationSlot" DROP CONSTRAINT IF EXISTS "EX_ReservationSlot_NoOverlap";
                ALTER TABLE "ReservationSlot" DROP CONSTRAINT IF EXISTS "CK_ReservationSlot_Periodo";
                """);
        }
    }
}
