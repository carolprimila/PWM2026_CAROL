START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "Assembleias" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Titulo" character varying(150) NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "DataHora" timestamp with time zone NOT NULL,
        "Local" character varying(150),
        "Convocacao" character varying(8000),
        "ExigeAdimplencia" boolean NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "ConvocadaEm" timestamp with time zone,
        "EncerradaEm" timestamp with time zone,
        "Ata" character varying(20000),
        "AtaRegistradaEm" timestamp with time zone,
        "AtaRegistradaPor" uuid,
        "MotivoCancelamento" character varying(300),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Assembleias" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Assembleias_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "Bens" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Codigo" character varying(20) NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Descricao" character varying(1000),
        "Localizacao" character varying(120),
        "ValorAquisicao" numeric(14,2),
        "DataAquisicao" date,
        "Estado" character varying(40) NOT NULL,
        "Situacao" character varying(40) NOT NULL,
        "MotivoBaixa" character varying(300),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Bens" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Bens_Valor" CHECK ("ValorAquisicao" IS NULL OR "ValorAquisicao" >= 0),
        CONSTRAINT "FK_Bens_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "Comunicados" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Titulo" character varying(150) NOT NULL,
        "Texto" character varying(8000) NOT NULL,
        "Publico" character varying(40) NOT NULL,
        "ExigeConfirmacao" boolean NOT NULL,
        "Fixado" boolean NOT NULL,
        "PublicadoEm" timestamp with time zone,
        "ExpiraEm" timestamp with time zone,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Comunicados" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Comunicados_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "Funcionarios" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Cpf" character varying(11),
        "Cargo" character varying(80) NOT NULL,
        "Departamento" character varying(80),
        "Telefone" character varying(30),
        "Email" character varying(150),
        "Escala" character varying(200),
        "Observacoes" character varying(2000),
        "Admissao" date NOT NULL,
        "Desligamento" date,
        "Situacao" character varying(40) NOT NULL,
        "UsuarioId" uuid,
        "Excluido" boolean NOT NULL,
        "ExcluidoEm" timestamp with time zone,
        "ExcluidoPor" uuid,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Funcionarios" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Funcionarios_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Funcionarios_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES "Usuarios" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "Notificacoes" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Titulo" character varying(150) NOT NULL,
        "Texto" character varying(1000) NOT NULL,
        "ReferenciaId" uuid,
        "CriadaEm" timestamp with time zone NOT NULL,
        "LidaEm" timestamp with time zone,
        CONSTRAINT "PK_Notificacoes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Notificacoes_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Notificacoes_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES "Usuarios" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "Produtos" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Nome" character varying(120) NOT NULL,
        "Unidade" character varying(10) NOT NULL,
        "Categoria" character varying(60),
        "EstoqueMinimo" numeric(14,3) NOT NULL,
        "Quantidade" numeric(14,3) NOT NULL,
        "Ativo" boolean NOT NULL,
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_Produtos" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Produtos_Minimo" CHECK ("EstoqueMinimo" >= 0),
        CONSTRAINT "CK_Produtos_SaldoNaoNegativo" CHECK ("Quantidade" >= 0),
        CONSTRAINT "FK_Produtos_Clubes_ClubeId" FOREIGN KEY ("ClubeId") REFERENCES "Clubes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "PautasAssembleias" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "AssembleiaId" uuid NOT NULL,
        "Ordem" integer NOT NULL,
        "Titulo" character varying(200) NOT NULL,
        "Descricao" character varying(4000),
        "ComVotacao" boolean NOT NULL,
        "Opcoes" text[] NOT NULL,
        "Votacao" character varying(40),
        "Decisao" character varying(2000),
        CONSTRAINT "PK_PautasAssembleias" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PautasAssembleias_Assembleias_AssembleiaId" FOREIGN KEY ("AssembleiaId") REFERENCES "Assembleias" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "PresencasAssembleias" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "AssembleiaId" uuid NOT NULL,
        "AssociadoId" uuid NOT NULL,
        "AptoVotar" boolean NOT NULL,
        "MotivoInapto" character varying(200),
        "RegistradaPor" uuid,
        "RegistradaEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PresencasAssembleias" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PresencasAssembleias_Assembleias_AssembleiaId" FOREIGN KEY ("AssembleiaId") REFERENCES "Assembleias" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PresencasAssembleias_Associados_AssociadoId" FOREIGN KEY ("AssociadoId") REFERENCES "Associados" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "HistoricoBens" (
        "Id" uuid NOT NULL,
        "BemId" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Evento" character varying(40) NOT NULL,
        "Descricao" character varying(500) NOT NULL,
        "UsuarioId" uuid,
        "OcorridoEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_HistoricoBens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_HistoricoBens_Bens_BemId" FOREIGN KEY ("BemId") REFERENCES "Bens" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "ManutencoesBens" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "BemId" uuid NOT NULL,
        "Descricao" character varying(500) NOT NULL,
        "Fornecedor" character varying(120),
        "Inicio" date NOT NULL,
        "Fim" date,
        "Custo" numeric(12,2),
        CONSTRAINT "PK_ManutencoesBens" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Manutencoes_Custo" CHECK ("Custo" IS NULL OR "Custo" >= 0),
        CONSTRAINT "CK_Manutencoes_Periodo" CHECK ("Fim" IS NULL OR "Fim" >= "Inicio"),
        CONSTRAINT "FK_ManutencoesBens_Bens_BemId" FOREIGN KEY ("BemId") REFERENCES "Bens" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "LeiturasComunicados" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "ComunicadoId" uuid NOT NULL,
        "PessoaId" uuid NOT NULL,
        "LidoEm" timestamp with time zone NOT NULL,
        "ConfirmadoEm" timestamp with time zone,
        CONSTRAINT "PK_LeiturasComunicados" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LeiturasComunicados_Comunicados_ComunicadoId" FOREIGN KEY ("ComunicadoId") REFERENCES "Comunicados" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_LeiturasComunicados_Pessoas_PessoaId" FOREIGN KEY ("PessoaId") REFERENCES "Pessoas" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "AusenciasFuncionarios" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "FuncionarioId" uuid NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Inicio" date NOT NULL,
        "Fim" date NOT NULL,
        "Observacao" character varying(300),
        "Cancelada" boolean NOT NULL,
        CONSTRAINT "PK_AusenciasFuncionarios" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Ausencias_Periodo" CHECK ("Fim" >= "Inicio"),
        CONSTRAINT "FK_AusenciasFuncionarios_Funcionarios_FuncionarioId" FOREIGN KEY ("FuncionarioId") REFERENCES "Funcionarios" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "HistoricoFuncionarios" (
        "Id" uuid NOT NULL,
        "FuncionarioId" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "Evento" character varying(40) NOT NULL,
        "Descricao" character varying(500) NOT NULL,
        "UsuarioId" uuid,
        "OcorridoEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_HistoricoFuncionarios" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_HistoricoFuncionarios_Funcionarios_FuncionarioId" FOREIGN KEY ("FuncionarioId") REFERENCES "Funcionarios" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "MovimentacoesEstoque" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "ProdutoId" uuid NOT NULL,
        "Tipo" character varying(40) NOT NULL,
        "Quantidade" numeric(14,3) NOT NULL,
        "SaldoApos" numeric(14,3) NOT NULL,
        "Motivo" character varying(200),
        "UsuarioId" uuid,
        "OcorridaEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MovimentacoesEstoque" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Movimentacoes_Saldo" CHECK ("SaldoApos" >= 0),
        CONSTRAINT "FK_MovimentacoesEstoque_Produtos_ProdutoId" FOREIGN KEY ("ProdutoId") REFERENCES "Produtos" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "SolicitacoesCompra" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "ProdutoId" uuid NOT NULL,
        "Quantidade" numeric(14,3) NOT NULL,
        "Observacao" character varying(300),
        "Situacao" character varying(40) NOT NULL,
        "QuantidadeRecebida" numeric(14,3),
        "ValorTotal" numeric(12,2),
        "CriadoEm" timestamp with time zone NOT NULL,
        "CriadoPor" uuid,
        "AtualizadoEm" timestamp with time zone,
        "AtualizadoPor" uuid,
        CONSTRAINT "PK_SolicitacoesCompra" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Compras_Quantidade" CHECK ("Quantidade" > 0),
        CONSTRAINT "FK_SolicitacoesCompra_Produtos_ProdutoId" FOREIGN KEY ("ProdutoId") REFERENCES "Produtos" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE TABLE "VotosAssembleias" (
        "Id" uuid NOT NULL,
        "ClubeId" uuid NOT NULL,
        "PautaId" uuid NOT NULL,
        "AssociadoId" uuid NOT NULL,
        "Opcao" integer NOT NULL,
        "VotadoEm" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VotosAssembleias" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Votos_Opcao" CHECK ("Opcao" >= 0),
        CONSTRAINT "FK_VotosAssembleias_Associados_AssociadoId" FOREIGN KEY ("AssociadoId") REFERENCES "Associados" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VotosAssembleias_PautasAssembleias_PautaId" FOREIGN KEY ("PautaId") REFERENCES "PautasAssembleias" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_Assembleias_ClubeId_DataHora" ON "Assembleias" ("ClubeId", "DataHora");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_AusenciasFuncionarios_FuncionarioId_Inicio" ON "AusenciasFuncionarios" ("FuncionarioId", "Inicio");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_Bens_ClubeId_Situacao" ON "Bens" ("ClubeId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE UNIQUE INDEX "UX_Bens_ClubeCodigo" ON "Bens" ("ClubeId", "Codigo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_Comunicados_ClubeId_PublicadoEm" ON "Comunicados" ("ClubeId", "PublicadoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_Funcionarios_ClubeId_Nome" ON "Funcionarios" ("ClubeId", "Nome");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_Funcionarios_UsuarioId" ON "Funcionarios" ("UsuarioId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE UNIQUE INDEX "UX_Funcionarios_ClubeCpf" ON "Funcionarios" ("ClubeId", "Cpf") WHERE "Cpf" IS NOT NULL AND NOT "Excluido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_HistoricoBens_BemId_OcorridoEm" ON "HistoricoBens" ("BemId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_HistoricoFuncionarios_FuncionarioId_OcorridoEm" ON "HistoricoFuncionarios" ("FuncionarioId", "OcorridoEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_LeiturasComunicados_PessoaId" ON "LeiturasComunicados" ("PessoaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE UNIQUE INDEX "UX_Leituras_ComunicadoPessoa" ON "LeiturasComunicados" ("ComunicadoId", "PessoaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE UNIQUE INDEX "UX_Manutencoes_BemAberta" ON "ManutencoesBens" ("BemId") WHERE "Fim" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_MovimentacoesEstoque_ClubeId_OcorridaEm" ON "MovimentacoesEstoque" ("ClubeId", "OcorridaEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_MovimentacoesEstoque_ProdutoId_OcorridaEm" ON "MovimentacoesEstoque" ("ProdutoId", "OcorridaEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_Notificacoes_ClubeId" ON "Notificacoes" ("ClubeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_Notificacoes_UsuarioId_ClubeId_CriadaEm" ON "Notificacoes" ("UsuarioId", "ClubeId", "CriadaEm");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_PautasAssembleias_AssembleiaId_Ordem" ON "PautasAssembleias" ("AssembleiaId", "Ordem");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_PresencasAssembleias_AssociadoId" ON "PresencasAssembleias" ("AssociadoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE UNIQUE INDEX "UX_Presencas_AssembleiaAssociado" ON "PresencasAssembleias" ("AssembleiaId", "AssociadoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE UNIQUE INDEX "UX_Produtos_ClubeNome" ON "Produtos" ("ClubeId", "Nome");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_SolicitacoesCompra_ClubeId_Situacao" ON "SolicitacoesCompra" ("ClubeId", "Situacao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_SolicitacoesCompra_ProdutoId" ON "SolicitacoesCompra" ("ProdutoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE INDEX "IX_VotosAssembleias_AssociadoId" ON "VotosAssembleias" ("AssociadoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    CREATE UNIQUE INDEX "UX_Votos_PautaAssociado" ON "VotosAssembleias" ("PautaId", "AssociadoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111163101_Etapa7') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111163101_Etapa7', '10.0.15');
    END IF;
END $EF$;
COMMIT;

