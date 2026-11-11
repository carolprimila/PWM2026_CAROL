START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE TABLE "Espacos" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(100) NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Descricao" character varying(1000),
        "Capacidade" integer,
        "ValorLocacao" numeric(12,2) NOT NULL,
        "TaxaAdicional" numeric(12,2) NOT NULL,
        "ExigeAprovacao" boolean NOT NULL,
        "Regras" character varying(2000),
        "Ativo" boolean NOT NULL,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Espacos" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Espacos_Valores" CHECK ("ValorLocacao" >= 0 AND "TaxaAdicional" >= 0),
        CONSTRAINT "FK_Espacos_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE TABLE "Solicitacoes" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Protocolo" character varying(20) NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Assunto" character varying(150) NOT NULL,
        "Descricao" character varying(4000) NOT NULL,
        "PessoaId" uuid,
        "ResponsavelId" uuid,
        "Prazo" date,
        "Situacao" character varying(40) NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Solicitacoes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Solicitacoes_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Solicitacoes_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE TABLE "Eventos" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Descricao" character varying(2000),
        "Local" character varying(150),
        "EspacoId" uuid,
        "Inicio" timestamp with time zone NOT NULL,
        "Fim" timestamp with time zone NOT NULL,
        "Capacidade" integer,
        "Publico" character varying(40) NOT NULL,
        "Valor" numeric(12,2) NOT NULL,
        "MaximoAcompanhantes" integer NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "MotivoCancelamento" character varying(300),
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Eventos" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Eventos_Capacidade" CHECK ("Capacidade" IS NULL OR "Capacidade" > 0),
        CONSTRAINT "CK_Eventos_Periodo" CHECK ("Fim" > "Inicio"),
        CONSTRAINT "FK_Eventos_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Eventos_Espacos_EspacoId" FOREIGN KEY ("EspacoId") REFERENCES "Espacos" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE TABLE "RespostasSolicitacoes" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "SolicitacaoId" uuid NOT NULL,
        "Texto" character varying(4000) NOT NULL,
        "AutorUsuarioId" uuid,
        "DaAdministracao" boolean NOT NULL,
        "Interna" boolean NOT NULL,
        "CriadaEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RespostasSolicitacoes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RespostasSolicitacoes_Solicitacoes_SolicitacaoId" FOREIGN KEY ("SolicitacaoId") REFERENCES "Solicitacoes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE TABLE "InscricoesEventos" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "EventoId" uuid NOT NULL,
        "PessoaId" uuid NOT NULL,
        "Acompanhantes" integer NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "PresencaEm" timestamp with time zone,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_InscricoesEventos" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Inscricoes_Acompanhantes" CHECK ("Acompanhantes" >= 0),
        CONSTRAINT "FK_InscricoesEventos_Eventos_EventoId" FOREIGN KEY ("EventoId") REFERENCES "Eventos" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_InscricoesEventos_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE TABLE "Reservas" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "EspacoId" uuid NOT NULL,
        "PessoaId" uuid,
        "EventoId" uuid,
        "Inicio" timestamp with time zone NOT NULL,
        "Fim" timestamp with time zone NOT NULL,
        "Finalidade" character varying(200),
        "Valor" numeric(12,2) NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "Motivo" character varying(300),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Reservas" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Reservas_Periodo" CHECK ("Fim" > "Inicio"),
        CONSTRAINT "FK_Reservas_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservas_Espacos_EspacoId" FOREIGN KEY ("EspacoId") REFERENCES "Espacos" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservas_Eventos_EventoId" FOREIGN KEY ("EventoId") REFERENCES "Eventos" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservas_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE UNIQUE INDEX "IX_Espacos_ClubeId_Nome" ON "Espacos" ("ClubeId", "Nome") WHERE NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Eventos_ClubeId_Inicio" ON "Eventos" ("ClubeId", "Inicio");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Eventos_EspacoId" ON "Eventos" ("EspacoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_InscricoesEventos_EventoId_Situacao" ON "InscricoesEventos" ("EventoId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_InscricoesEventos_PessoaId" ON "InscricoesEventos" ("PessoaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE UNIQUE INDEX "UX_Inscricoes_PessoaEvento" ON "InscricoesEventos" ("EventoId", "PessoaId") WHERE "Situacao" <> 'Cancelada';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Reservas_ClubeId_Situacao" ON "Reservas" ("ClubeId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Reservas_EspacoId_Inicio" ON "Reservas" ("EspacoId", "Inicio");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Reservas_EventoId" ON "Reservas" ("EventoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Reservas_PessoaId" ON "Reservas" ("PessoaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_RespostasSolicitacoes_SolicitacaoId_CriadaEm" ON "RespostasSolicitacoes" ("SolicitacaoId", "CriadaEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE UNIQUE INDEX "IX_Solicitacoes_ClubeId_Protocolo" ON "Solicitacoes" ("ClubeId", "Protocolo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Solicitacoes_ClubeId_Situacao" ON "Solicitacoes" ("ClubeId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    CREATE INDEX "IX_Solicitacoes_PessoaId" ON "Solicitacoes" ("PessoaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    ALTER TABLE "Reservas" ADD CONSTRAINT "EX_Reservas_SemConflito"
    EXCLUDE USING gist ("EspacoId" WITH =, tstzrange("Inicio", "Fim", '[)') WITH &&)
    WHERE ("Situacao" IN ('Pendente', 'Aprovada'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111143528_Eventos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111143528_Eventos', '10.0.15');
    END IF;
END $EF$;
COMMIT;

