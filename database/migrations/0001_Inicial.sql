CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE EXTENSION IF NOT EXISTS btree_gist;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "Clubes" (
        "Id" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Sigla" character varying(20) NOT NULL,
        "Cnpj" character varying(14),
        "Telefone" character varying(11),
        "Email" character varying(254),
        "LogotipoArquivo" character varying(300),
        "DataFundacao" date,
        "Situacao" character varying(40) NOT NULL,
        "Demonstracao" boolean NOT NULL,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "Bairro" character varying(100),
        "Cep" character varying(8),
        "Cidade" character varying(100),
        "Complemento" character varying(100),
        "Logradouro" character varying(200),
        "Numero" character varying(20),
        "Uf" character varying(2),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Clubes" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Clubes_Cnpj" CHECK ("Cnpj" IS NULL OR "Cnpj" ~ '^[0-9]{14}$'),
        CONSTRAINT "CK_Clubes_Uf" CHECK ("Uf" IS NULL OR "Uf" ~ '^[A-Z]{2}$')
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "HistoricoAcessos" (
        "Id" uuid NOT NULL,
        "Evento" character varying(40) NOT NULL,
        "Sucesso" boolean NOT NULL,
        "OcorridoEm" timestamp with time zone NOT NULL,
        "UsuarioId" uuid,
        "EmailInformado" character varying(254),
        "ClubeId" uuid,
        "SessaoId" uuid,
        "Ip" character varying(64),
        "UserAgent" character varying(500),
        "Detalhe" character varying(300),
        CONSTRAINT "PK_HistoricoAcessos" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "LogsAuditoria" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid,
        "UsuarioId" uuid,
        "Entidade" character varying(100) NOT NULL,
        "EntidadeId" character varying(100) NOT NULL,
        "Acao" character varying(40) NOT NULL,
        "Alteracoes" jsonb,
        "OcorridoEm" timestamp with time zone NOT NULL,
        "Ip" character varying(64),
        "CorrelationId" character varying(64),
        CONSTRAINT "PK_LogsAuditoria" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "Permissoes" (
        "Id" uuid NOT NULL,
        "Codigo" character varying(80) NOT NULL,
        "Modulo" character varying(80) NOT NULL,
        "Descricao" character varying(200) NOT NULL,
        CONSTRAINT "PK_Permissoes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "ConfiguracoesClube" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "CorPrimaria" character varying(7) NOT NULL,
        "NomeExibicao" character varying(100),
        "FusoHorario" character varying(60) NOT NULL,
        "ExigeAprovacaoAssociados" boolean NOT NULL,
        "ExigeAprovacaoDependentes" boolean NOT NULL,
        "PrefixoMatricula" character varying(10),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_ConfiguracoesClube" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_ConfiguracoesClube_Cor" CHECK ("CorPrimaria" ~ '^#[0-9A-F]{6}$'),
        CONSTRAINT "FK_ConfiguracoesClube_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "Perfis" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(80) NOT NULL,
        "Descricao" character varying(300),
        "ChaveModelo" character varying(40),
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Perfis" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Perfis_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "Usuarios" (
        "Id" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Email" character varying(254) NOT NULL,
        "EmailNormalizado" character varying(254) NOT NULL,
        "SenhaHash" character varying(300),
        "SuperAdmin" boolean NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "TentativasFalhas" integer NOT NULL,
        "BloqueadoAte" timestamp with time zone,
        "UltimoAcessoEm" timestamp with time zone,
        "SenhaAlteradaEm" timestamp with time zone,
        "DeveTrocarSenha" boolean NOT NULL,
        "CarimboSeguranca" character varying(64) NOT NULL,
        "ClubePadraoId" uuid,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Usuarios" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Usuarios_Clubes_ClubePadraoId" FOREIGN KEY ("ClubePadraoId") REFERENCES "Clubes" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "PerfilPermissoes" (
        "PerfilId" uuid NOT NULL,
        "PermissaoId" uuid NOT NULL,
        CONSTRAINT "PK_PerfilPermissoes" PRIMARY KEY ("PerfilId", "PermissaoId"),
        CONSTRAINT "FK_PerfilPermissoes_Perfis_PerfilId" FOREIGN KEY ("PerfilId") REFERENCES "Perfis" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PerfilPermissoes_Permissoes_PermissaoId" FOREIGN KEY ("PermissaoId") REFERENCES "Permissoes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "SessoesUsuarios" (
        "Id" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "ClubeId" uuid,
        "RefreshTokenHash" character varying(64) NOT NULL,
        "RefreshTokenHashAnterior" character varying(64),
        "CriadaEm" timestamp with time zone NOT NULL,
        "UltimoUsoEm" timestamp with time zone NOT NULL,
        "ExpiraEm" timestamp with time zone NOT NULL,
        "RevogadaEm" timestamp with time zone,
        "MotivoRevogacao" character varying(200),
        "Ip" character varying(64),
        "UserAgent" character varying(500),
        "Dispositivo" character varying(100),
        CONSTRAINT "PK_SessoesUsuarios" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SessoesUsuarios_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_SessoesUsuarios_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES "Usuarios" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "TokensRecuperacaoSenha" (
        "Id" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "CarimboSeguranca" character varying(64) NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "ExpiraEm" timestamp with time zone NOT NULL,
        "UsadoEm" timestamp with time zone,
        "Ip" character varying(64),
        CONSTRAINT "PK_TokensRecuperacaoSenha" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TokensRecuperacaoSenha_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES "Usuarios" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "UsuarioClubes" (
        "Id" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_UsuarioClubes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UsuarioClubes_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_UsuarioClubes_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "UsuarioClubePerfis" (
        "UsuarioClubeId" uuid NOT NULL,
        "PerfilId" uuid NOT NULL,
        CONSTRAINT "PK_UsuarioClubePerfis" PRIMARY KEY ("UsuarioClubeId", "PerfilId"),
        CONSTRAINT "FK_UsuarioClubePerfis_Perfis_PerfilId" FOREIGN KEY ("PerfilId") REFERENCES "Perfis" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_UsuarioClubePerfis_UsuarioClubes_UsuarioClubeId" FOREIGN KEY ("UsuarioClubeId") REFERENCES "UsuarioClubes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE TABLE "UsuarioPermissoes" (
        "UsuarioClubeId" uuid NOT NULL,
        "PermissaoId" uuid NOT NULL,
        "Concedida" boolean NOT NULL,
        CONSTRAINT "PK_UsuarioPermissoes" PRIMARY KEY ("UsuarioClubeId", "PermissaoId"),
        CONSTRAINT "FK_UsuarioPermissoes_Permissoes_PermissaoId" FOREIGN KEY ("PermissaoId") REFERENCES "Permissoes" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UsuarioPermissoes_UsuarioClubes_UsuarioClubeId" FOREIGN KEY ("UsuarioClubeId") REFERENCES "UsuarioClubes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Clubes_Cnpj" ON "Clubes" ("Cnpj") WHERE "Cnpj" IS NOT NULL AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_Clubes_Nome" ON "Clubes" ("Nome");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Clubes_Sigla" ON "Clubes" ("Sigla");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_ConfiguracoesClube_ClubeId" ON "ConfiguracoesClube" ("ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_HistoricoAcessos_ClubeId_OcorridoEm" ON "HistoricoAcessos" ("ClubeId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_HistoricoAcessos_UsuarioId_OcorridoEm" ON "HistoricoAcessos" ("UsuarioId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_LogsAuditoria_ClubeId_OcorridoEm" ON "LogsAuditoria" ("ClubeId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_LogsAuditoria_Entidade_EntidadeId" ON "LogsAuditoria" ("Entidade", "EntidadeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_LogsAuditoria_UsuarioId" ON "LogsAuditoria" ("UsuarioId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_PerfilPermissoes_PermissaoId" ON "PerfilPermissoes" ("PermissaoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Perfis_ClubeId_ChaveModelo" ON "Perfis" ("ClubeId", "ChaveModelo") WHERE "ChaveModelo" IS NOT NULL AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Perfis_ClubeId_Nome" ON "Perfis" ("ClubeId", "Nome") WHERE NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Permissoes_Codigo" ON "Permissoes" ("Codigo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_SessoesUsuarios_ClubeId" ON "SessoesUsuarios" ("ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_SessoesUsuarios_RefreshTokenHash" ON "SessoesUsuarios" ("RefreshTokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_SessoesUsuarios_RefreshTokenHashAnterior" ON "SessoesUsuarios" ("RefreshTokenHashAnterior");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_SessoesUsuarios_UsuarioId_RevogadaEm" ON "SessoesUsuarios" ("UsuarioId", "RevogadaEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_TokensRecuperacaoSenha_TokenHash" ON "TokensRecuperacaoSenha" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_TokensRecuperacaoSenha_UsuarioId" ON "TokensRecuperacaoSenha" ("UsuarioId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_UsuarioClubePerfis_PerfilId" ON "UsuarioClubePerfis" ("PerfilId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_UsuarioClubes_ClubeId_Situacao" ON "UsuarioClubes" ("ClubeId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_UsuarioClubes_UsuarioId_ClubeId" ON "UsuarioClubes" ("UsuarioId", "ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_UsuarioPermissoes_PermissaoId" ON "UsuarioPermissoes" ("PermissaoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_Usuarios_ClubePadraoId" ON "Usuarios" ("ClubePadraoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Usuarios_EmailNormalizado" ON "Usuarios" ("EmailNormalizado") WHERE NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    CREATE INDEX "IX_Usuarios_SuperAdmin" ON "Usuarios" ("SuperAdmin") WHERE "SuperAdmin";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111125111_Inicial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111125111_Inicial', '10.0.15');
    END IF;
END $EF$;
COMMIT;

