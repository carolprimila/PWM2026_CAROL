START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "Arquivos" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "NomeOriginal" character varying(150) NOT NULL,
        "ContentType" character varying(80) NOT NULL,
        "TamanhoBytes" bigint NOT NULL,
        "HashSha256" character varying(64) NOT NULL,
        "Chave" character varying(300) NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Arquivos" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Arquivos_Tamanho" CHECK ("TamanhoBytes" > 0),
        CONSTRAINT "FK_Arquivos_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "CategoriasAssociados" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(80) NOT NULL,
        "Descricao" character varying(300),
        "DireitoVoto" boolean NOT NULL,
        "PodeSerVotado" boolean NOT NULL,
        "PermiteDependentes" boolean NOT NULL,
        "LimiteDependentes" integer,
        "Ordem" integer NOT NULL,
        "Ativa" boolean NOT NULL,
        "ChaveModelo" character varying(40),
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_CategoriasAssociados" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_CategoriasAssociados_Limite" CHECK ("LimiteDependentes" IS NULL OR "LimiteDependentes" >= 0),
        CONSTRAINT "FK_CategoriasAssociados_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "ConfiguracoesDependentes" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "LimitePadraoDependentes" integer NOT NULL,
        "MaximoConjuges" integer NOT NULL,
        "IdadeMaximaFilhos" integer,
        "IdadeMaximaIrmaos" integer,
        "IdadeMaioridade" integer NOT NULL,
        "IdadeMinimaLoginProprio" integer NOT NULL,
        "ParentescosPermitidos" text[] NOT NULL,
        "ExigeDocumentoComprobatorio" boolean NOT NULL,
        "MenoresPodemSolicitarReservas" boolean NOT NULL,
        "IncluidosNaMensalidadeDoTitular" boolean NOT NULL,
        "ValorAdicionalPorDependente" numeric(12,2) NOT NULL,
        "PadraoAcessoClube" boolean NOT NULL,
        "PadraoInscricaoAtividades" boolean NOT NULL,
        "PadraoParticipacaoEventos" boolean NOT NULL,
        "PadraoRecebimentoComunicados" boolean NOT NULL,
        "PadraoSolicitacaoReservas" boolean NOT NULL,
        "PadraoUsoEspacosEsportivos" boolean NOT NULL,
        "PadraoUtilizacaoServicos" boolean NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_ConfiguracoesDependentes" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_ConfigDependentes_Limite" CHECK ("LimitePadraoDependentes" >= 0 AND "MaximoConjuges" >= 0),
        CONSTRAINT "CK_ConfigDependentes_Valor" CHECK ("ValorAdicionalPorDependente" >= 0),
        CONSTRAINT "FK_ConfiguracoesDependentes_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "Interesses" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(60) NOT NULL,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Interesses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Interesses_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "SequenciasClube" (
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(40) NOT NULL,
        "Valor" bigint NOT NULL,
        CONSTRAINT "PK_SequenciasClube" PRIMARY KEY ("ClubeId", "Nome"),
        CONSTRAINT "FK_SequenciasClube_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "Pessoas" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "NomeCompleto" character varying(150) NOT NULL,
        "Cpf" character varying(11),
        "Rg" character varying(20),
        "DataNascimento" date,
        "Sexo" character varying(40) NOT NULL,
        "EstadoCivil" character varying(40),
        "Telefone" character varying(11),
        "Email" character varying(254),
        "FotoArquivoId" uuid,
        "UsuarioId" uuid,
        "TokenCarteirinha" character varying(100) NOT NULL,
        "TokenCarteirinhaGeradoEm" timestamp with time zone NOT NULL,
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
        CONSTRAINT "PK_Pessoas" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Pessoas_Cpf" CHECK ("Cpf" IS NULL OR "Cpf" ~ '^[0-9]{11}$'),
        CONSTRAINT "CK_Pessoas_Uf" CHECK ("Uf" IS NULL OR "Uf" ~ '^[A-Z]{2}$'),
        CONSTRAINT "FK_Pessoas_Arquivos_FotoArquivoId" FOREIGN KEY ("FotoArquivoId") REFERENCES "Arquivos" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Pessoas_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Pessoas_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES "Usuarios" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "AcessosClube" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "PessoaId" uuid,
        "AssociadoId" uuid,
        "DependenteId" uuid,
        "Tipo" character varying(40) NOT NULL,
        "Autorizado" boolean NOT NULL,
        "Motivo" character varying(200),
        "RegistradoPor" uuid,
        "OcorridoEm" timestamp with time zone NOT NULL,
        "Observacao" character varying(300),
        CONSTRAINT "PK_AcessosClube" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AcessosClube_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AcessosClube_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "Associados" (
        "Id" uuid NOT NULL,
        "PessoaId" uuid NOT NULL,
        "Matricula" character varying(30) NOT NULL,
        "CategoriaId" uuid NOT NULL,
        "DataAssociacao" date NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        "ClubeId" uuid NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "MotivoSituacao" character varying(300),
        "SituacaoAlteradaEm" timestamp with time zone,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        CONSTRAINT "PK_Associados" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Associados_CategoriasAssociados_CategoriaId" FOREIGN KEY ("CategoriaId") REFERENCES "CategoriasAssociados" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Associados_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Associados_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "DocumentosPessoais" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "PessoaId" uuid NOT NULL,
        "ArquivoId" uuid NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Descricao" character varying(200),
        "Situacao" character varying(40) NOT NULL,
        "ObservacaoAnalise" character varying(300),
        "AnalisadoPor" uuid,
        "AnalisadoEm" timestamp with time zone,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_DocumentosPessoais" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DocumentosPessoais_Arquivos_ArquivoId" FOREIGN KEY ("ArquivoId") REFERENCES "Arquivos" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DocumentosPessoais_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DocumentosPessoais_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "AssociadoInteresses" (
        "AssociadoId" uuid NOT NULL,
        "InteresseId" uuid NOT NULL,
        CONSTRAINT "PK_AssociadoInteresses" PRIMARY KEY ("AssociadoId", "InteresseId"),
        CONSTRAINT "FK_AssociadoInteresses_Associados_AssociadoId" FOREIGN KEY ("AssociadoId") REFERENCES "Associados" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AssociadoInteresses_Interesses_InteresseId" FOREIGN KEY ("InteresseId") REFERENCES "Interesses" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "Dependentes" (
        "Id" uuid NOT NULL,
        "PessoaId" uuid NOT NULL,
        "AssociadoTitularId" uuid NOT NULL,
        "Parentesco" character varying(40) NOT NULL,
        "ParentescoDescricao" character varying(60),
        "NumeroIdentificacao" character varying(40) NOT NULL,
        "DataInclusao" date NOT NULL,
        "ExcecaoMultiplosTitulares" boolean NOT NULL,
        "JustificativaExcecao" character varying(300),
        "ExcecaoAutorizadaPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        "ClubeId" uuid NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "MotivoSituacao" character varying(300),
        "SituacaoAlteradaEm" timestamp with time zone,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        CONSTRAINT "PK_Dependentes" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Dependentes_ParentescoOutro" CHECK ("Parentesco" <> 'Outro' OR "ParentescoDescricao" IS NOT NULL),
        CONSTRAINT "FK_Dependentes_Associados_AssociadoTitularId" FOREIGN KEY ("AssociadoTitularId") REFERENCES "Associados" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Dependentes_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Dependentes_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "HistoricoAssociados" (
        "Id" uuid NOT NULL,
        "AssociadoId" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Evento" character varying(40) NOT NULL,
        "Descricao" character varying(500) NOT NULL,
        "UsuarioId" uuid,
        "OcorridoEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_HistoricoAssociados" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_HistoricoAssociados_Associados_AssociadoId" FOREIGN KEY ("AssociadoId") REFERENCES "Associados" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "HistoricoDependentes" (
        "Id" uuid NOT NULL,
        "DependenteId" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Evento" character varying(40) NOT NULL,
        "Descricao" character varying(500) NOT NULL,
        "UsuarioId" uuid,
        "OcorridoEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_HistoricoDependentes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_HistoricoDependentes_Dependentes_DependenteId" FOREIGN KEY ("DependenteId") REFERENCES "Dependentes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "PermissoesDependentes" (
        "DependenteId" uuid NOT NULL,
        "AcessoClube" boolean NOT NULL,
        "ParticipacaoEventos" boolean NOT NULL,
        "UsoEspacosEsportivos" boolean NOT NULL,
        "InscricaoAtividades" boolean NOT NULL,
        "SolicitacaoReservas" boolean NOT NULL,
        "RecebimentoComunicados" boolean NOT NULL,
        "UtilizacaoServicos" boolean NOT NULL,
        CONSTRAINT "PK_PermissoesDependentes" PRIMARY KEY ("DependenteId"),
        CONSTRAINT "FK_PermissoesDependentes_Dependentes_DependenteId" FOREIGN KEY ("DependenteId") REFERENCES "Dependentes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE TABLE "VinculosDependentes" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "DependenteId" uuid NOT NULL,
        "AssociadoTitularId" uuid NOT NULL,
        "Inicio" timestamp with time zone NOT NULL,
        "Fim" timestamp with time zone,
        "Motivo" character varying(300) NOT NULL,
        "RegistradoPor" uuid,
        CONSTRAINT "PK_VinculosDependentes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VinculosDependentes_Associados_AssociadoTitularId" FOREIGN KEY ("AssociadoTitularId") REFERENCES "Associados" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VinculosDependentes_Dependentes_DependenteId" FOREIGN KEY ("DependenteId") REFERENCES "Dependentes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_AcessosClube_ClubeId_OcorridoEm" ON "AcessosClube" ("ClubeId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_AcessosClube_PessoaId_OcorridoEm" ON "AcessosClube" ("PessoaId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Arquivos_ClubeId" ON "Arquivos" ("ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_AssociadoInteresses_InteresseId" ON "AssociadoInteresses" ("InteresseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Associados_CategoriaId" ON "Associados" ("CategoriaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_Associados_ClubeId_Matricula" ON "Associados" ("ClubeId", "Matricula");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Associados_ClubeId_Situacao" ON "Associados" ("ClubeId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Associados_PessoaId" ON "Associados" ("PessoaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "UX_Associados_PessoaVigente" ON "Associados" ("ClubeId", "PessoaId") WHERE "Situacao" IN ('Pendente', 'Ativo', 'Suspenso') AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_CategoriasAssociados_ClubeId_Nome" ON "CategoriasAssociados" ("ClubeId", "Nome") WHERE NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_ConfiguracoesDependentes_ClubeId" ON "ConfiguracoesDependentes" ("ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_Dependentes_ClubeId_NumeroIdentificacao" ON "Dependentes" ("ClubeId", "NumeroIdentificacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Dependentes_ClubeId_Situacao" ON "Dependentes" ("ClubeId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Dependentes_PessoaId" ON "Dependentes" ("PessoaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "UX_Dependentes_PessoaVigente" ON "Dependentes" ("ClubeId", "PessoaId") WHERE "Situacao" IN ('Pendente', 'Ativo', 'Suspenso') AND NOT "Excluido" AND NOT "ExcecaoMultiplosTitulares";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "UX_Dependentes_TitularPessoaVigente" ON "Dependentes" ("AssociadoTitularId", "PessoaId") WHERE "Situacao" IN ('Pendente', 'Ativo', 'Suspenso') AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_DocumentosPessoais_ArquivoId" ON "DocumentosPessoais" ("ArquivoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_DocumentosPessoais_ClubeId" ON "DocumentosPessoais" ("ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_DocumentosPessoais_PessoaId_Situacao" ON "DocumentosPessoais" ("PessoaId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_HistoricoAssociados_AssociadoId_OcorridoEm" ON "HistoricoAssociados" ("AssociadoId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_HistoricoDependentes_DependenteId_OcorridoEm" ON "HistoricoDependentes" ("DependenteId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_Interesses_ClubeId_Nome" ON "Interesses" ("ClubeId", "Nome") WHERE NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_Pessoas_ClubeId_Cpf" ON "Pessoas" ("ClubeId", "Cpf") WHERE "Cpf" IS NOT NULL AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Pessoas_ClubeId_NomeCompleto" ON "Pessoas" ("ClubeId", "NomeCompleto");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_Pessoas_ClubeId_UsuarioId" ON "Pessoas" ("ClubeId", "UsuarioId") WHERE "UsuarioId" IS NOT NULL AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Pessoas_FotoArquivoId" ON "Pessoas" ("FotoArquivoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE UNIQUE INDEX "IX_Pessoas_TokenCarteirinha" ON "Pessoas" ("TokenCarteirinha");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_Pessoas_UsuarioId" ON "Pessoas" ("UsuarioId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_VinculosDependentes_AssociadoTitularId" ON "VinculosDependentes" ("AssociadoTitularId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    CREATE INDEX "IX_VinculosDependentes_DependenteId" ON "VinculosDependentes" ("DependenteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111131709_Cadastros') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111131709_Cadastros', '10.0.15');
    END IF;
END $EF$;
COMMIT;

