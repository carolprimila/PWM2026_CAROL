using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Eventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Espacos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Capacidade = table.Column<int>(type: "integer", nullable: true),
                    ValorLocacao = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    TaxaAdicional = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ExigeAprovacao = table.Column<bool>(type: "boolean", nullable: false),
                    Regras = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                name: "Solicitacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Assunto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Prazo = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Local = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EspacoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Capacidade = table.Column<int>(type: "integer", nullable: true),
                    Publico = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MaximoAcompanhantes = table.Column<int>(type: "integer", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                name: "RespostasSolicitacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texto = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AutorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    DaAdministracao = table.Column<bool>(type: "boolean", nullable: false),
                    Interna = table.Column<bool>(type: "boolean", nullable: false),
                    CriadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "InscricoesEventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Acompanhantes = table.Column<int>(type: "integer", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PresencaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EspacoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Finalidade = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
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

            // Garantia no banco contra reservas sobrepostas no mesmo espaço (inclusive sob concorrência).
            // Intervalo semiaberto [Inicio, Fim): uma reserva pode começar exatamente quando a anterior termina.
            migrationBuilder.Sql("""
                ALTER TABLE "Reservas" ADD CONSTRAINT "EX_Reservas_SemConflito"
                EXCLUDE USING gist ("EspacoId" WITH =, tstzrange("Inicio", "Fim", '[)') WITH &&)
                WHERE ("Situacao" IN ('Pendente', 'Aprovada'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Reservas\" DROP CONSTRAINT IF EXISTS \"EX_Reservas_SemConflito\";");

            migrationBuilder.DropTable(
                name: "InscricoesEventos");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "RespostasSolicitacoes");

            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DropTable(
                name: "Solicitacoes");

            migrationBuilder.DropTable(
                name: "Espacos");
        }
    }
}
