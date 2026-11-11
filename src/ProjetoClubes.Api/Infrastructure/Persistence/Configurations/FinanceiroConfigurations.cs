using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.Infrastructure.Persistence.Configurations;

internal sealed class ConfiguracaoFinanceiraConfiguration : IEntityTypeConfiguration<ConfiguracaoFinanceira>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoFinanceira> b)
    {
        b.ToTable("ConfiguracoesFinanceiras", t => t.HasCheckConstraint("CK_ConfigFinanceira_Valores",
            "\"MultaPercentual\" >= 0 AND \"JurosMensalPercentual\" >= 0 AND \"DiasTolerancia\" >= 0"));
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ClubeId).IsUnique();
        b.Property(x => x.MultaPercentual).HasPrecision(5, 2);
        b.Property(x => x.JurosMensalPercentual).HasPrecision(5, 2);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PlanoMensalidadeConfiguration : IEntityTypeConfiguration<PlanoMensalidade>
{
    public void Configure(EntityTypeBuilder<PlanoMensalidade> b)
    {
        b.ToTable("PlanosMensalidade", t =>
        {
            t.HasCheckConstraint("CK_Planos_Valor", "\"ValorMensal\" >= 0");
            t.HasCheckConstraint("CK_Planos_Dia", "\"DiaVencimento\" BETWEEN 1 AND 28");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
        b.Property(x => x.ValorMensal).HasPrecision(12, 2);
        // Um plano ativo por categoria.
        b.HasIndex(x => new { x.ClubeId, x.CategoriaId }).IsUnique().HasFilter("\"Ativo\" AND NOT \"Excluido\"")
            .HasDatabaseName("UX_Planos_CategoriaAtiva");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CategoriaAssociado>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContratoAssociadoConfiguration : IEntityTypeConfiguration<Domain.Associados.ContratoAssociado>
{
    public void Configure(EntityTypeBuilder<Domain.Associados.ContratoAssociado> b)
    {
        b.ToTable("ContratosAssociados", t =>
        {
            t.HasCheckConstraint("CK_Contratos_Dia", "\"DiaVencimento\" BETWEEN 1 AND 28");
            t.HasCheckConstraint("CK_Contratos_Periodo", "\"Fim\" > \"Inicio\"");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Numero).HasMaxLength(20).IsRequired();
        b.Property(x => x.ValorMensal).HasPrecision(12, 2);
        b.Property(x => x.Motivo).HasMaxLength(300);
        b.HasIndex(x => new { x.ClubeId, x.Numero }).IsUnique().HasDatabaseName("UX_Contratos_ClubeNumero");
        // No máximo um contrato em análise por associado.
        b.HasIndex(x => x.AssociadoId).IsUnique().HasFilter("\"Situacao\" = 'EmAnalise'").HasDatabaseName("UX_Contratos_UmEmAnalise");
        b.HasOne<Domain.Associados.Associado>().WithMany().HasForeignKey(x => x.AssociadoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MensalidadeConfiguration : IEntityTypeConfiguration<Mensalidade>
{
    public void Configure(EntityTypeBuilder<Mensalidade> b)
    {
        b.ToTable("Mensalidades", t => t.HasCheckConstraint("CK_Mensalidades_Valores",
            "\"ValorPlano\" >= 0 AND \"ValorDependentes\" >= 0 AND substr(\"Competencia\", 9, 2) = '01'"));
        b.HasKey(x => x.Id);
        b.Ignore(x => x.Valor);
        b.Property(x => x.ValorPlano).HasPrecision(12, 2);
        b.Property(x => x.ValorDependentes).HasPrecision(12, 2);
        b.Property(x => x.Motivo).HasMaxLength(300);
        // Uma cobrança por associado e competência (canceladas não contam).
        b.HasIndex(x => new { x.AssociadoId, x.Competencia }).IsUnique().HasFilter("\"Situacao\" <> 'Cancelada'")
            .HasDatabaseName("UX_Mensalidades_AssociadoCompetencia");
        b.HasIndex(x => new { x.ClubeId, x.Competencia });
        b.HasIndex(x => x.ContratoId);
        b.HasOne<Domain.Associados.ContratoAssociado>().WithMany().HasForeignKey(x => x.ContratoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClubeId, x.Situacao, x.Vencimento });
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Associado>().WithMany().HasForeignKey(x => x.AssociadoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlanoMensalidade>().WithMany().HasForeignKey(x => x.PlanoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> b)
    {
        b.ToTable("Pagamentos", t => t.HasCheckConstraint("CK_Pagamentos_Valor", "\"Valor\" > 0 AND \"Encargos\" >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Valor).HasPrecision(12, 2);
        b.Property(x => x.Encargos).HasPrecision(12, 2);
        b.Property(x => x.Observacao).HasMaxLength(300);
        b.Property(x => x.MotivoEstorno).HasMaxLength(300);
        b.HasIndex(x => new { x.ClubeId, x.NumeroRecibo }).IsUnique();
        b.HasIndex(x => new { x.ClubeId, x.Data });
        // Só um pagamento válido (não estornado) por mensalidade.
        b.HasIndex(x => x.MensalidadeId).IsUnique().HasFilter("NOT \"Estornado\"").HasDatabaseName("UX_Pagamentos_MensalidadeValido");
        b.HasOne(x => x.Mensalidade).WithMany().HasForeignKey(x => x.MensalidadeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CategoriaFinanceiraConfiguration : IEntityTypeConfiguration<CategoriaFinanceira>
{
    public void Configure(EntityTypeBuilder<CategoriaFinanceira> b)
    {
        b.ToTable("CategoriasFinanceiras");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
        b.HasIndex(x => new { x.ClubeId, x.Tipo, x.Nome }).IsUnique().HasFilter("NOT \"Excluido\"");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LancamentoFinanceiroConfiguration : IEntityTypeConfiguration<LancamentoFinanceiro>
{
    public void Configure(EntityTypeBuilder<LancamentoFinanceiro> b)
    {
        b.ToTable("LancamentosFinanceiros", t => t.HasCheckConstraint("CK_Lancamentos_Valor", "\"Valor\" > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
        b.Property(x => x.Valor).HasPrecision(12, 2);
        b.HasIndex(x => new { x.ClubeId, x.Tipo, x.PagoEm });
        b.HasIndex(x => new { x.ClubeId, x.Vencimento });
        b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}
