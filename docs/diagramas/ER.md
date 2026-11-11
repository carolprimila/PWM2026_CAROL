# Diagrama entidade-relacionamento

## Etapa 1 — implementado

```mermaid
erDiagram
    Clubes ||--|| ConfiguracoesClube : "possui"
    Clubes ||--o{ Perfis : "define"
    Clubes ||--o{ UsuarioClubes : "concede acesso"
    Usuarios ||--o{ UsuarioClubes : "participa"
    UsuarioClubes ||--o{ UsuarioClubePerfis : "tem"
    Perfis ||--o{ UsuarioClubePerfis : "atribuído"
    Perfis ||--o{ PerfilPermissoes : "contém"
    Permissoes ||--o{ PerfilPermissoes : ""
    UsuarioClubes ||--o{ UsuarioPermissoes : "exceções"
    Permissoes ||--o{ UsuarioPermissoes : ""
    Usuarios ||--o{ SessoesUsuarios : "abre"
    Usuarios ||--o{ TokensRecuperacaoSenha : "solicita"

    Clubes {
        uuid Id PK
        varchar Nome
        varchar Sigla UK
        varchar Cnpj UK "parcial: não excluído"
        varchar Situacao
        bool Demonstracao
        bool Excluido
    }
    ConfiguracoesClube {
        uuid Id PK
        uuid ClubeId FK,UK
        varchar CorPrimaria
        varchar FusoHorario
        bool ExigeAprovacaoAssociados
        bool ExigeAprovacaoDependentes
    }
    Usuarios {
        uuid Id PK
        varchar EmailNormalizado UK "parcial"
        varchar SenhaHash
        bool SuperAdmin
        varchar Situacao
        int TentativasFalhas
        timestamptz BloqueadoAte
        varchar CarimboSeguranca
    }
    UsuarioClubes {
        uuid Id PK
        uuid UsuarioId FK
        uuid ClubeId FK
        varchar Situacao
    }
    Perfis {
        uuid Id PK
        uuid ClubeId FK
        varchar Nome "UK por clube"
        varchar ChaveModelo
    }
    Permissoes {
        uuid Id PK
        varchar Codigo UK
        varchar Modulo
    }
    SessoesUsuarios {
        uuid Id PK
        uuid UsuarioId FK
        uuid ClubeId FK
        varchar RefreshTokenHash UK
        varchar RefreshTokenHashAnterior
        timestamptz ExpiraEm
        timestamptz RevogadaEm
    }
    TokensRecuperacaoSenha {
        uuid Id PK
        uuid UsuarioId FK
        varchar TokenHash UK
        timestamptz ExpiraEm
        timestamptz UsadoEm
    }
    HistoricoAcessos {
        uuid Id PK
        varchar Evento
        uuid UsuarioId
        uuid ClubeId
        timestamptz OcorridoEm
    }
    LogsAuditoria {
        uuid Id PK
        uuid ClubeId
        varchar Entidade
        varchar EntidadeId
        varchar Acao
        jsonb Alteracoes
    }
```

## Etapa 2 — implementado

```mermaid
erDiagram
    Clubes ||--o{ Pessoas : ""
    Clubes ||--|| ConfiguracoesDependentes : "regras"
    Clubes ||--o{ CategoriasAssociados : ""
    Clubes ||--o{ Interesses : ""
    Pessoas ||--o{ Associados : "titular"
    Pessoas ||--o{ Dependentes : "dependente"
    Pessoas |o--o| Usuarios : "login do app"
    Pessoas |o--o| Arquivos : "foto"
    CategoriasAssociados ||--o{ Associados : ""
    Associados ||--o{ AssociadoInteresses : ""
    Interesses ||--o{ AssociadoInteresses : ""
    Associados ||--o{ Dependentes : "titular atual"
    Dependentes ||--|| PermissoesDependentes : ""
    Dependentes ||--o{ VinculosDependentes : "histórico de titularidade"
    Associados ||--o{ VinculosDependentes : ""
    Associados ||--o{ HistoricoAssociados : ""
    Dependentes ||--o{ HistoricoDependentes : ""
    Pessoas ||--o{ DocumentosPessoais : ""
    Arquivos ||--o{ DocumentosPessoais : ""
    Pessoas ||--o{ AcessosClube : "entrada/saída"
    Clubes ||--o{ SequenciasClube : "matrícula"

    Pessoas {
        uuid Id PK
        uuid ClubeId FK
        varchar Cpf "UK (ClubeId,Cpf) parcial"
        varchar TokenCarteirinha UK
        uuid UsuarioId FK
    }
    Associados {
        uuid Id PK
        varchar Matricula "UK (ClubeId,Matricula)"
        varchar Situacao
        uuid CategoriaId FK
    }
    Dependentes {
        uuid Id PK
        uuid AssociadoTitularId FK
        varchar Parentesco
        varchar NumeroIdentificacao "UK por clube"
        varchar Situacao
        bool ExcecaoMultiplosTitulares
    }
    ConfiguracoesDependentes {
        int LimitePadraoDependentes
        int MaximoConjuges
        int IdadeMaximaFilhos
        text_array ParentescosPermitidos
        decimal ValorAdicionalPorDependente
    }
    AcessosClube {
        uuid Id PK
        varchar Tipo
        bool Autorizado
        uuid RegistradoPor
    }
```

Índices de regra de negócio:
- `UX_Associados_PessoaVigente (ClubeId, PessoaId) WHERE vigente` — um cadastro vigente por pessoa.
- `UX_Dependentes_PessoaVigente (ClubeId, PessoaId) WHERE vigente AND NOT ExcecaoMultiplosTitulares`.
- `UX_Dependentes_TitularPessoaVigente (AssociadoTitularId, PessoaId) WHERE vigente`.

## Etapa 3 — financeiro

```mermaid
erDiagram
    CategoriasAssociados ||--o{ PlanosMensalidade : "um ativo por categoria"
    Associados ||--o{ Mensalidades : "uma por competência"
    Mensalidades ||--o{ Pagamentos : "um válido (estornos ficam)"
    CategoriasFinanceiras ||--o{ LancamentosFinanceiros : ""
    Clubes ||--|| ConfiguracoesFinanceiras : "multa, juros, tolerância"
```

## Etapa 4 — eventos, espaços, reservas e solicitações

```mermaid
erDiagram
    Eventos ||--o{ InscricoesEventos : ""
    Pessoas ||--o{ InscricoesEventos : ""
    Espacos ||--o{ Reservas : "EX_Reservas_SemConflito"
    Eventos |o--o| Reservas : "evento ocupa o espaço"
    Pessoas ||--o{ Reservas : "solicitante"
    Pessoas ||--o{ Solicitacoes : "solicitante"
    Solicitacoes ||--o{ RespostasSolicitacoes : ""
    Eventos {
        uuid Id PK
        varchar Nome
        timestamptz Inicio
        timestamptz Fim
        varchar Publico
        varchar Situacao
        int Capacidade
        decimal Valor
        int MaximoAcompanhantes
        uuid EspacoId FK
    }
    InscricoesEventos {
        uuid Id PK
        uuid EventoId FK
        uuid PessoaId FK
        int Acompanhantes
        varchar Situacao
        timestamptz PresencaEm
    }
    Espacos {
        uuid Id PK
        varchar Nome
        varchar Tipo
        int Capacidade
        decimal ValorLocacao
        decimal TaxaAdicional
        bool ExigeAprovacao
    }
    Reservas {
        uuid Id PK
        uuid EspacoId FK
        timestamptz Inicio
        timestamptz Fim
        varchar Situacao
        uuid EventoId FK
        uuid PessoaId FK
    }
    Solicitacoes {
        uuid Id PK
        varchar Protocolo
        varchar Tipo
        varchar Situacao
        uuid PessoaId FK
    }
    RespostasSolicitacoes {
        uuid Id PK
        text Texto
        bool Interna
    }
```

- `EX_Reservas_SemConflito`: `EXCLUDE USING gist ("EspacoId" WITH =, tstzrange("Inicio","Fim",'[)') WITH &&) WHERE Situacao IN ('Pendente','Aprovada')`.
- `UX_Inscricoes_PessoaEvento (EventoId, PessoaId) WHERE inscrição ativa`.

## Etapa 7 — comunicação, operacional e assembleias

```mermaid
erDiagram
    Comunicados ||--o{ LeiturasComunicados : "UX (ComunicadoId, PessoaId)"
    Pessoas ||--o{ LeiturasComunicados : ""
    Usuarios ||--o{ Notificacoes : "caixa de entrada por clube"
    Funcionarios ||--o{ AusenciasFuncionarios : "férias/afastamentos"
    Funcionarios ||--o{ HistoricoFuncionarios : ""
    Usuarios |o--o{ Funcionarios : "acesso ao sistema (opcional)"
    Bens ||--o{ ManutencoesBens : "uma aberta por bem"
    Bens ||--o{ HistoricoBens : ""
    Produtos ||--o{ MovimentacoesEstoque : "saldo após cada movimento"
    Produtos ||--o{ SolicitacoesCompra : ""
    Assembleias ||--o{ PautasAssembleias : "opções em text[]"
    Assembleias ||--o{ PresencasAssembleias : "UX (AssembleiaId, AssociadoId)"
    Associados ||--o{ PresencasAssembleias : "só titulares"
    PautasAssembleias ||--o{ VotosAssembleias : "UX (PautaId, AssociadoId)"
    Associados ||--o{ VotosAssembleias : ""
```

- `CK_Produtos_SaldoNaoNegativo ("Quantidade" >= 0)`, `CK_Movimentacoes_Saldo`, `CK_Votos_Opcao`, `CK_Ausencias_Periodo`, `CK_Manutencoes_*`.
- `UX_Funcionarios_ClubeCpf` (parcial: CPF informado e não excluído), `UX_Bens_ClubeCodigo`, `UX_Produtos_ClubeNome`.
