using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoClubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "Clubes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Sigla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    LogotipoArquivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DataFundacao = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Demonstracao = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Clubes", x => x.Id);
                    table.CheckConstraint("CK_Clubes_Cnpj", "\"Cnpj\" IS NULL OR \"Cnpj\" ~ '^[0-9]{14}$'");
                    table.CheckConstraint("CK_Clubes_Uf", "\"Uf\" IS NULL OR \"Uf\" ~ '^[A-Z]{2}$'");
                });

            migrationBuilder.CreateTable(
                name: "HistoricoAcessos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailInformado = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Detalhe = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAcessos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Entidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Acao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Alteracoes = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Modulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesClube",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorPrimaria = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    NomeExibicao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FusoHorario = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ExigeAprovacaoAssociados = table.Column<bool>(type: "boolean", nullable: false),
                    ExigeAprovacaoDependentes = table.Column<bool>(type: "boolean", nullable: false),
                    PrefixoMatricula = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesClube", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracoesClube_Cor", "\"CorPrimaria\" ~ '^#[0-9A-F]{6}$'");
                    table.ForeignKey(
                        name: "FK_ConfiguracoesClube_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Perfis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_Perfis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfis_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    EmailNormalizado = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SuperAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TentativasFalhas = table.Column<int>(type: "integer", nullable: false),
                    BloqueadoAte = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UltimoAcessoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SenhaAlteradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeveTrocarSenha = table.Column<bool>(type: "boolean", nullable: false),
                    CarimboSeguranca = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClubePadraoId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Clubes_ClubePadraoId",
                        column: x => x.ClubePadraoId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PerfilPermissoes",
                columns: table => new
                {
                    PerfilId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissaoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilPermissoes", x => new { x.PerfilId, x.PermissaoId });
                    table.ForeignKey(
                        name: "FK_PerfilPermissoes_Perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerfilPermissoes_Permissoes_PermissaoId",
                        column: x => x.PermissaoId,
                        principalTable: "Permissoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessoesUsuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefreshTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RefreshTokenHashAnterior = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CriadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UltimoUsoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevogadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MotivoRevogacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Dispositivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesUsuarios_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SessoesUsuarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokensRecuperacaoSenha",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CarimboSeguranca = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensRecuperacaoSenha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensRecuperacaoSenha_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioClubes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioClubes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioClubes_Clubes_ClubeId",
                        column: x => x.ClubeId,
                        principalTable: "Clubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioClubes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioClubePerfis",
                columns: table => new
                {
                    UsuarioClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioClubePerfis", x => new { x.UsuarioClubeId, x.PerfilId });
                    table.ForeignKey(
                        name: "FK_UsuarioClubePerfis_Perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioClubePerfis_UsuarioClubes_UsuarioClubeId",
                        column: x => x.UsuarioClubeId,
                        principalTable: "UsuarioClubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPermissoes",
                columns: table => new
                {
                    UsuarioClubeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Concedida = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPermissoes", x => new { x.UsuarioClubeId, x.PermissaoId });
                    table.ForeignKey(
                        name: "FK_UsuarioPermissoes_Permissoes_PermissaoId",
                        column: x => x.PermissaoId,
                        principalTable: "Permissoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioPermissoes_UsuarioClubes_UsuarioClubeId",
                        column: x => x.UsuarioClubeId,
                        principalTable: "UsuarioClubes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clubes_Cnpj",
                table: "Clubes",
                column: "Cnpj",
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Clubes_Nome",
                table: "Clubes",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Clubes_Sigla",
                table: "Clubes",
                column: "Sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesClube_ClubeId",
                table: "ConfiguracoesClube",
                column: "ClubeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAcessos_ClubeId_OcorridoEm",
                table: "HistoricoAcessos",
                columns: new[] { "ClubeId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAcessos_UsuarioId_OcorridoEm",
                table: "HistoricoAcessos",
                columns: new[] { "UsuarioId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_ClubeId_OcorridoEm",
                table: "LogsAuditoria",
                columns: new[] { "ClubeId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_Entidade_EntidadeId",
                table: "LogsAuditoria",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_UsuarioId",
                table: "LogsAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilPermissoes_PermissaoId",
                table: "PerfilPermissoes",
                column: "PermissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfis_ClubeId_ChaveModelo",
                table: "Perfis",
                columns: new[] { "ClubeId", "ChaveModelo" },
                unique: true,
                filter: "\"ChaveModelo\" IS NOT NULL AND NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Perfis_ClubeId_Nome",
                table: "Perfis",
                columns: new[] { "ClubeId", "Nome" },
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Permissoes_Codigo",
                table: "Permissoes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_ClubeId",
                table: "SessoesUsuarios",
                column: "ClubeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_RefreshTokenHash",
                table: "SessoesUsuarios",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_RefreshTokenHashAnterior",
                table: "SessoesUsuarios",
                column: "RefreshTokenHashAnterior");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesUsuarios_UsuarioId_RevogadaEm",
                table: "SessoesUsuarios",
                columns: new[] { "UsuarioId", "RevogadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_TokensRecuperacaoSenha_TokenHash",
                table: "TokensRecuperacaoSenha",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensRecuperacaoSenha_UsuarioId",
                table: "TokensRecuperacaoSenha",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClubePerfis_PerfilId",
                table: "UsuarioClubePerfis",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClubes_ClubeId_Situacao",
                table: "UsuarioClubes",
                columns: new[] { "ClubeId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClubes_UsuarioId_ClubeId",
                table: "UsuarioClubes",
                columns: new[] { "UsuarioId", "ClubeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPermissoes_PermissaoId",
                table: "UsuarioPermissoes",
                column: "PermissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ClubePadraoId",
                table: "Usuarios",
                column: "ClubePadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmailNormalizado",
                table: "Usuarios",
                column: "EmailNormalizado",
                unique: true,
                filter: "NOT \"Excluido\"");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_SuperAdmin",
                table: "Usuarios",
                column: "SuperAdmin",
                filter: "\"SuperAdmin\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesClube");

            migrationBuilder.DropTable(
                name: "HistoricoAcessos");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.DropTable(
                name: "PerfilPermissoes");

            migrationBuilder.DropTable(
                name: "SessoesUsuarios");

            migrationBuilder.DropTable(
                name: "TokensRecuperacaoSenha");

            migrationBuilder.DropTable(
                name: "UsuarioClubePerfis");

            migrationBuilder.DropTable(
                name: "UsuarioPermissoes");

            migrationBuilder.DropTable(
                name: "Perfis");

            migrationBuilder.DropTable(
                name: "Permissoes");

            migrationBuilder.DropTable(
                name: "UsuarioClubes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Clubes");
        }
    }
}
