using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoClubes.Domain.Assembleias;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Comunicacao;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Domain.Operacional;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Infrastructure.Persistence.Configurations;

// ============================================================================================ comunicação

internal sealed class ComunicadoConfiguration : IEntityTypeConfiguration<Comunicado>
{
    public void Configure(EntityTypeBuilder<Comunicado> b)
    {
        b.ToTable("Comunicados");
        b.HasKey(x => x.Id);
        b.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
        b.Property(x => x.Texto).HasMaxLength(8000).IsRequired();
        b.HasIndex(x => new { x.ClubeId, x.PublicadoEm });
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LeituraComunicadoConfiguration : IEntityTypeConfiguration<LeituraComunicado>
{
    public void Configure(EntityTypeBuilder<LeituraComunicado> b)
    {
        b.ToTable("LeiturasComunicados");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ComunicadoId, x.PessoaId }).IsUnique().HasDatabaseName("UX_Leituras_ComunicadoPessoa");
        b.HasOne<Comunicado>().WithMany().HasForeignKey(x => x.ComunicadoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Pessoa>().WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> b)
    {
        b.ToTable("Notificacoes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
        b.Property(x => x.Texto).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => new { x.UsuarioId, x.ClubeId, x.CriadaEm });
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ============================================================================================ funcionários

internal sealed class FuncionarioConfiguration : IEntityTypeConfiguration<Funcionario>
{
    public void Configure(EntityTypeBuilder<Funcionario> b)
    {
        b.ToTable("Funcionarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Cpf).HasMaxLength(11);
        b.Property(x => x.Cargo).HasMaxLength(80).IsRequired();
        b.Property(x => x.Departamento).HasMaxLength(80);
        b.Property(x => x.Telefone).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Escala).HasMaxLength(200);
        b.Property(x => x.Observacoes).HasMaxLength(2000);
        b.HasIndex(x => new { x.ClubeId, x.Cpf }).IsUnique().HasFilter("\"Cpf\" IS NOT NULL AND NOT \"Excluido\"")
            .HasDatabaseName("UX_Funcionarios_ClubeCpf");
        b.HasIndex(x => new { x.ClubeId, x.Nome });
        b.HasMany(x => x.Ausencias).WithOne().HasForeignKey(x => x.FuncionarioId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Ausencias).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class AusenciaFuncionarioConfiguration : IEntityTypeConfiguration<AusenciaFuncionario>
{
    public void Configure(EntityTypeBuilder<AusenciaFuncionario> b)
    {
        b.ToTable("AusenciasFuncionarios", t => t.HasCheckConstraint("CK_Ausencias_Periodo", "\"Fim\" >= \"Inicio\""));
        b.HasKey(x => x.Id);
        b.Ignore(x => x.Dias);
        b.Property(x => x.Observacao).HasMaxLength(300);
        b.HasIndex(x => new { x.FuncionarioId, x.Inicio });
    }
}

internal sealed class HistoricoFuncionarioConfiguration : IEntityTypeConfiguration<HistoricoFuncionario>
{
    public void Configure(EntityTypeBuilder<HistoricoFuncionario> b)
    {
        b.ToTable("HistoricoFuncionarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Evento).HasMaxLength(40).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
        b.HasIndex(x => new { x.FuncionarioId, x.OcorridoEm });
        b.HasOne<Funcionario>().WithMany().HasForeignKey(x => x.FuncionarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================================================ patrimônio

internal sealed class BemConfiguration : IEntityTypeConfiguration<Bem>
{
    public void Configure(EntityTypeBuilder<Bem> b)
    {
        b.ToTable("Bens", t => t.HasCheckConstraint("CK_Bens_Valor", "\"ValorAquisicao\" IS NULL OR \"ValorAquisicao\" >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(1000);
        b.Property(x => x.Localizacao).HasMaxLength(120);
        b.Property(x => x.MotivoBaixa).HasMaxLength(300);
        b.Property(x => x.ValorAquisicao).HasPrecision(14, 2);
        b.HasIndex(x => new { x.ClubeId, x.Codigo }).IsUnique().HasDatabaseName("UX_Bens_ClubeCodigo");
        b.HasIndex(x => new { x.ClubeId, x.Situacao });
        b.HasMany(x => x.Manutencoes).WithOne().HasForeignKey(x => x.BemId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Manutencoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ManutencaoBemConfiguration : IEntityTypeConfiguration<ManutencaoBem>
{
    public void Configure(EntityTypeBuilder<ManutencaoBem> b)
    {
        b.ToTable("ManutencoesBens", t =>
        {
            t.HasCheckConstraint("CK_Manutencoes_Periodo", "\"Fim\" IS NULL OR \"Fim\" >= \"Inicio\"");
            t.HasCheckConstraint("CK_Manutencoes_Custo", "\"Custo\" IS NULL OR \"Custo\" >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
        b.Property(x => x.Fornecedor).HasMaxLength(120);
        b.Property(x => x.Custo).HasPrecision(12, 2);
        // Uma manutenção em aberto por bem.
        b.HasIndex(x => x.BemId).IsUnique().HasFilter("\"Fim\" IS NULL").HasDatabaseName("UX_Manutencoes_BemAberta");
    }
}

internal sealed class HistoricoBemConfiguration : IEntityTypeConfiguration<HistoricoBem>
{
    public void Configure(EntityTypeBuilder<HistoricoBem> b)
    {
        b.ToTable("HistoricoBens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Evento).HasMaxLength(40).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
        b.HasIndex(x => new { x.BemId, x.OcorridoEm });
        b.HasOne<Bem>().WithMany().HasForeignKey(x => x.BemId).OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================================================ estoque

internal sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.ToTable("Produtos", t =>
        {
            // Última barreira: o saldo nunca fica negativo, mesmo fora da API.
            t.HasCheckConstraint("CK_Produtos_SaldoNaoNegativo", "\"Quantidade\" >= 0");
            t.HasCheckConstraint("CK_Produtos_Minimo", "\"EstoqueMinimo\" >= 0");
        });
        b.HasKey(x => x.Id);
        b.Ignore(x => x.AbaixoDoMinimo);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Unidade).HasMaxLength(10).IsRequired();
        b.Property(x => x.Categoria).HasMaxLength(60);
        b.Property(x => x.Quantidade).HasPrecision(14, 3);
        b.Property(x => x.EstoqueMinimo).HasPrecision(14, 3);
        b.HasIndex(x => new { x.ClubeId, x.Nome }).IsUnique().HasDatabaseName("UX_Produtos_ClubeNome");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MovimentacaoEstoqueConfiguration : IEntityTypeConfiguration<MovimentacaoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentacaoEstoque> b)
    {
        b.ToTable("MovimentacoesEstoque", t => t.HasCheckConstraint("CK_Movimentacoes_Saldo", "\"SaldoApos\" >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Quantidade).HasPrecision(14, 3);
        b.Property(x => x.SaldoApos).HasPrecision(14, 3);
        b.Property(x => x.Motivo).HasMaxLength(200);
        b.HasIndex(x => new { x.ProdutoId, x.OcorridaEm });
        b.HasIndex(x => new { x.ClubeId, x.OcorridaEm });
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SolicitacaoCompraConfiguration : IEntityTypeConfiguration<SolicitacaoCompra>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCompra> b)
    {
        b.ToTable("SolicitacoesCompra", t => t.HasCheckConstraint("CK_Compras_Quantidade", "\"Quantidade\" > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Quantidade).HasPrecision(14, 3);
        b.Property(x => x.QuantidadeRecebida).HasPrecision(14, 3);
        b.Property(x => x.ValorTotal).HasPrecision(12, 2);
        b.Property(x => x.Observacao).HasMaxLength(300);
        b.HasIndex(x => new { x.ClubeId, x.Situacao });
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ============================================================================================ assembleias

internal sealed class AssembleiaConfiguration : IEntityTypeConfiguration<Assembleia>
{
    public void Configure(EntityTypeBuilder<Assembleia> b)
    {
        b.ToTable("Assembleias");
        b.HasKey(x => x.Id);
        b.Ignore(x => x.RecebePresencas);
        b.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
        b.Property(x => x.Local).HasMaxLength(150);
        b.Property(x => x.Convocacao).HasMaxLength(8000);
        b.Property(x => x.Ata).HasMaxLength(20000);
        b.Property(x => x.MotivoCancelamento).HasMaxLength(300);
        b.HasIndex(x => new { x.ClubeId, x.DataHora });
        b.HasMany(x => x.Pautas).WithOne().HasForeignKey(x => x.AssembleiaId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Pautas).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PautaAssembleiaConfiguration : IEntityTypeConfiguration<PautaAssembleia>
{
    public void Configure(EntityTypeBuilder<PautaAssembleia> b)
    {
        b.ToTable("PautasAssembleias");
        b.HasKey(x => x.Id);
        b.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(4000);
        b.Property(x => x.Decisao).HasMaxLength(2000);
        // Lista de opções guardada como JSON (coleção primitiva do EF Core).
        b.HasIndex(x => new { x.AssembleiaId, x.Ordem });
    }
}

internal sealed class PresencaAssembleiaConfiguration : IEntityTypeConfiguration<PresencaAssembleia>
{
    public void Configure(EntityTypeBuilder<PresencaAssembleia> b)
    {
        b.ToTable("PresencasAssembleias");
        b.HasKey(x => x.Id);
        b.Property(x => x.MotivoInapto).HasMaxLength(200);
        b.HasIndex(x => new { x.AssembleiaId, x.AssociadoId }).IsUnique().HasDatabaseName("UX_Presencas_AssembleiaAssociado");
        b.HasOne<Assembleia>().WithMany().HasForeignKey(x => x.AssembleiaId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Associado>().WithMany().HasForeignKey(x => x.AssociadoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VotoAssembleiaConfiguration : IEntityTypeConfiguration<VotoAssembleia>
{
    public void Configure(EntityTypeBuilder<VotoAssembleia> b)
    {
        b.ToTable("VotosAssembleias", t => t.HasCheckConstraint("CK_Votos_Opcao", "\"Opcao\" >= 0"));
        b.HasKey(x => x.Id);
        // Um voto por associado e pauta, garantido pelo banco.
        b.HasIndex(x => new { x.PautaId, x.AssociadoId }).IsUnique().HasDatabaseName("UX_Votos_PautaAssociado");
        b.HasOne<PautaAssembleia>().WithMany().HasForeignKey(x => x.PautaId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Associado>().WithMany().HasForeignKey(x => x.AssociadoId).OnDelete(DeleteBehavior.Restrict);
    }
}
