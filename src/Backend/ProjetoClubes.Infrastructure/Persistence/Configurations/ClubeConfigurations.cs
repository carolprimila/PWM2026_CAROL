using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoClubes.Domain.Clubes;

namespace ProjetoClubes.Infrastructure.Persistence.Configurations;

internal sealed class ClubeConfiguration : IEntityTypeConfiguration<Clube>
{
    public void Configure(EntityTypeBuilder<Clube> b)
    {
        b.ToTable("Clubes", t =>
        {
            t.HasCheckConstraint("CK_Clubes_Cnpj", "\"Cnpj\" IS NULL OR \"Cnpj\" ~ '^[0-9]{14}$'");
            t.HasCheckConstraint("CK_Clubes_Uf", "\"Uf\" IS NULL OR \"Uf\" ~ '^[A-Z]{2}$'");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Sigla).HasMaxLength(20).IsRequired();
        b.Property(x => x.Cnpj).HasMaxLength(14);
        b.Property(x => x.Telefone).HasMaxLength(11);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.LogotipoArquivo).HasMaxLength(300);
        b.ComplexProperty(x => x.Endereco, e =>
        {
            e.Property(p => p.Logradouro).HasColumnName("Logradouro").HasMaxLength(200);
            e.Property(p => p.Numero).HasColumnName("Numero").HasMaxLength(20);
            e.Property(p => p.Complemento).HasColumnName("Complemento").HasMaxLength(100);
            e.Property(p => p.Bairro).HasColumnName("Bairro").HasMaxLength(100);
            e.Property(p => p.Cidade).HasColumnName("Cidade").HasMaxLength(100);
            e.Property(p => p.Uf).HasColumnName("Uf").HasMaxLength(2);
            e.Property(p => p.Cep).HasColumnName("Cep").HasMaxLength(8);
        });

        b.HasIndex(x => x.Sigla).IsUnique();
        b.HasIndex(x => x.Cnpj).IsUnique().HasFilter("\"Cnpj\" IS NOT NULL AND NOT \"Excluido\"");
        b.HasIndex(x => x.Nome);

        b.HasOne(x => x.Configuracao).WithOne()
            .HasForeignKey<ConfiguracaoClube>(c => c.ClubeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Configuracao).AutoInclude();
    }
}

internal sealed class ConfiguracaoClubeConfiguration : IEntityTypeConfiguration<ConfiguracaoClube>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoClube> b)
    {
        b.ToTable("ConfiguracoesClube", t =>
            t.HasCheckConstraint("CK_ConfiguracoesClube_Cor", "\"CorPrimaria\" ~ '^#[0-9A-F]{6}$'"));
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ClubeId).IsUnique();
        b.Property(x => x.CorPrimaria).HasMaxLength(7).IsRequired();
        b.Property(x => x.NomeExibicao).HasMaxLength(100);
        b.Property(x => x.FusoHorario).HasMaxLength(60).IsRequired();
        b.Property(x => x.PrefixoMatricula).HasMaxLength(10);
    }
}
