START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE TABLE "CategoriasFinanceiras" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(80) NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_CategoriasFinanceiras" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CategoriasFinanceiras_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE TABLE "ConfiguracoesFinanceiras" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "MultaPercentual" numeric(5,2) NOT NULL,
        "JurosMensalPercentual" numeric(5,2) NOT NULL,
        "DiasTolerancia" integer NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_ConfiguracoesFinanceiras" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_ConfigFinanceira_Valores" CHECK ("MultaPercentual" >= 0 AND "JurosMensalPercentual" >= 0 AND "DiasTolerancia" >= 0),
        CONSTRAINT "FK_ConfiguracoesFinanceiras_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE TABLE "PlanosMensalidade" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(80) NOT NULL,
        "CategoriaId" uuid NOT NULL,
        "ValorMensal" numeric(12,2) NOT NULL,
        "DiaVencimento" integer NOT NULL,
        "Ativo" boolean NOT NULL,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_PlanosMensalidade" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Planos_Dia" CHECK ("DiaVencimento" BETWEEN 1 AND 28),
        CONSTRAINT "CK_Planos_Valor" CHECK ("ValorMensal" >= 0),
        CONSTRAINT "FK_PlanosMensalidade_CategoriasAssociados_CategoriaId" FOREIGN KEY ("CategoriaId") REFERENCES "CategoriasAssociados" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PlanosMensalidade_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE TABLE "LancamentosFinanceiros" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "CategoriaId" uuid NOT NULL,
        "Descricao" character varying(200) NOT NULL,
        "Valor" numeric(12,2) NOT NULL,
        "Vencimento" date NOT NULL,
        "PagoEm" date,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_LancamentosFinanceiros" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Lancamentos_Valor" CHECK ("Valor" > 0),
        CONSTRAINT "FK_LancamentosFinanceiros_CategoriasFinanceiras_CategoriaId" FOREIGN KEY ("CategoriaId") REFERENCES "CategoriasFinanceiras" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_LancamentosFinanceiros_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE TABLE "Mensalidades" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "AssociadoId" uuid NOT NULL,
        "PlanoId" uuid NOT NULL,
        "Competencia" date NOT NULL,
        "Vencimento" date NOT NULL,
        "ValorPlano" numeric(12,2) NOT NULL,
        "ValorDependentes" numeric(12,2) NOT NULL,
        "QuantidadeDependentes" integer NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "Motivo" character varying(300),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Mensalidades" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Mensalidades_Valores" CHECK ("ValorPlano" >= 0 AND "ValorDependentes" >= 0 AND EXTRACT(DAY FROM "Competencia") = 1),
        CONSTRAINT "FK_Mensalidades_Associados_AssociadoId" FOREIGN KEY ("AssociadoId") REFERENCES "Associados" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Mensalidades_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Mensalidades_PlanosMensalidade_PlanoId" FOREIGN KEY ("PlanoId") REFERENCES "PlanosMensalidade" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE TABLE "Pagamentos" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "MensalidadeId" uuid NOT NULL,
        "NumeroRecibo" bigint NOT NULL,
        "Valor" numeric(12,2) NOT NULL,
        "Encargos" numeric(12,2) NOT NULL,
        "Data" date NOT NULL,
        "Forma" character varying(40) NOT NULL,
        "Observacao" character varying(300),
        "Estornado" boolean NOT NULL,
        "MotivoEstorno" character varying(300),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Pagamentos" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Pagamentos_Valor" CHECK ("Valor" > 0 AND "Encargos" >= 0),
        CONSTRAINT "FK_Pagamentos_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Pagamentos_Mensalidades_MensalidadeId" FOREIGN KEY ("MensalidadeId") REFERENCES "Mensalidades" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE UNIQUE INDEX "IX_CategoriasFinanceiras_ClubeId_Tipo_Nome" ON "CategoriasFinanceiras" ("ClubeId", "Tipo", "Nome") WHERE NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE UNIQUE INDEX "IX_ConfiguracoesFinanceiras_ClubeId" ON "ConfiguracoesFinanceiras" ("ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_LancamentosFinanceiros_CategoriaId" ON "LancamentosFinanceiros" ("CategoriaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_LancamentosFinanceiros_ClubeId_Tipo_PagoEm" ON "LancamentosFinanceiros" ("ClubeId", "Tipo", "PagoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_LancamentosFinanceiros_ClubeId_Vencimento" ON "LancamentosFinanceiros" ("ClubeId", "Vencimento");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_Mensalidades_ClubeId_Competencia" ON "Mensalidades" ("ClubeId", "Competencia");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_Mensalidades_ClubeId_Situacao_Vencimento" ON "Mensalidades" ("ClubeId", "Situacao", "Vencimento");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_Mensalidades_PlanoId" ON "Mensalidades" ("PlanoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE UNIQUE INDEX "UX_Mensalidades_AssociadoCompetencia" ON "Mensalidades" ("AssociadoId", "Competencia") WHERE "Situacao" <> 'Cancelada';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_Pagamentos_ClubeId_Data" ON "Pagamentos" ("ClubeId", "Data");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE UNIQUE INDEX "IX_Pagamentos_ClubeId_NumeroRecibo" ON "Pagamentos" ("ClubeId", "NumeroRecibo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE UNIQUE INDEX "UX_Pagamentos_MensalidadeValido" ON "Pagamentos" ("MensalidadeId") WHERE NOT "Estornado";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE INDEX "IX_PlanosMensalidade_CategoriaId" ON "PlanosMensalidade" ("CategoriaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    CREATE UNIQUE INDEX "UX_Planos_CategoriaAtiva" ON "PlanosMensalidade" ("ClubeId", "CategoriaId") WHERE "Ativo" AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111141627_Financeiro') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111141627_Financeiro', '10.0.15');
    END IF;
END $EF$;
COMMIT;

