using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoClubes.Domain.Acesso;
using ProjetoClubes.Domain.Arquivos;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Infrastructure.Persistence.Configurations;

internal static class Sql
{
    /// <summary>Situações que contam para unicidade e limites (enums gravados como texto).</summary>
    public const string Vigente = "\"Situacao\" IN ('Pendente', 'Ativo', 'Suspenso') AND NOT \"Excluido\"";
}

internal sealed class PessoaConfiguration : IEntityTypeConfiguration<Pessoa>
{
    public void Configure(EntityTypeBuilder<Pessoa> b)
    {
        b.ToTable("Pessoas", t =>
        {
            t.HasCheckConstraint("CK_Pessoas_Cpf", "\"Cpf\" IS NULL OR \"Cpf\" GLOB '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
            t.HasCheckConstraint("CK_Pessoas_Uf", "\"Uf\" IS NULL OR \"Uf\" GLOB '[A-Z][A-Z]'");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.NomeCompleto).HasMaxLength(150).IsRequired();
        b.Property(x => x.Cpf).HasMaxLength(11);
        b.Property(x => x.Rg).HasMaxLength(20);
        b.Property(x => x.Telefone).HasMaxLength(11);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Profissao).HasMaxLength(100);
        b.Property(x => x.TokenCarteirinha).HasMaxLength(100).IsRequired();
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

        b.HasIndex(x => new { x.ClubeId, x.Cpf }).IsUnique().HasFilter("\"Cpf\" IS NOT NULL AND NOT \"Excluido\"");
        b.HasIndex(x => new { x.ClubeId, x.UsuarioId }).IsUnique().HasFilter("\"UsuarioId\" IS NOT NULL AND NOT \"Excluido\"");
        b.HasIndex(x => x.TokenCarteirinha).IsUnique();
        b.HasIndex(x => new { x.ClubeId, x.NomeCompleto });

        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Arquivo>().WithMany().HasForeignKey(x => x.FotoArquivoId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class CategoriaAssociadoConfiguration : IEntityTypeConfiguration<CategoriaAssociado>
{
    public void Configure(EntityTypeBuilder<CategoriaAssociado> b)
    {
        b.ToTable("CategoriasAssociados", t =>
            t.HasCheckConstraint("CK_CategoriasAssociados_Limite", "\"LimiteDependentes\" IS NULL OR \"LimiteDependentes\" >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(300);
        b.Property(x => x.ChaveModelo).HasMaxLength(40);
        b.HasIndex(x => new { x.ClubeId, x.Nome }).IsUnique().HasFilter("NOT \"Excluido\"");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InteresseConfiguration : IEntityTypeConfiguration<Interesse>
{
    public void Configure(EntityTypeBuilder<Interesse> b)
    {
        b.ToTable("Interesses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(60).IsRequired();
        b.HasIndex(x => new { x.ClubeId, x.Nome }).IsUnique().HasFilter("NOT \"Excluido\"");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AssociadoConfiguration : IEntityTypeConfiguration<Associado>
{
    public void Configure(EntityTypeBuilder<Associado> b)
    {
        b.ToTable("Associados");
        b.HasKey(x => x.Id);
        b.Property(x => x.Matricula).HasMaxLength(30).IsRequired();
        b.Property(x => x.MotivoSituacao).HasMaxLength(300);
        // Matrícula nunca é reutilizada, nem após exclusão lógica.
        b.HasIndex(x => new { x.ClubeId, x.Matricula }).IsUnique();
        // Uma pessoa só pode ter um cadastro de associado vigente por clube.
        b.HasIndex(x => new { x.ClubeId, x.PessoaId }).IsUnique().HasFilter(Sql.Vigente).HasDatabaseName("UX_Associados_PessoaVigente");
        b.HasIndex(x => new { x.ClubeId, x.Situacao });
        b.HasIndex(x => x.CategoriaId);

        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Pessoa).WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Interesses).WithOne().HasForeignKey(x => x.AssociadoId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Dependentes).WithOne(x => x.AssociadoTitular).HasForeignKey(x => x.AssociadoTitularId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Interesses).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Dependentes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AssociadoInteresseConfiguration : IEntityTypeConfiguration<AssociadoInteresse>
{
    public void Configure(EntityTypeBuilder<AssociadoInteresse> b)
    {
        b.ToTable("AssociadoInteresses");
        b.HasKey(x => new { x.AssociadoId, x.InteresseId });
        b.HasOne(x => x.Interesse).WithMany().HasForeignKey(x => x.InteresseId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class HistoricoAssociadoConfiguration : IEntityTypeConfiguration<HistoricoAssociado>
{
    public void Configure(EntityTypeBuilder<HistoricoAssociado> b)
    {
        b.ToTable("HistoricoAssociados");
        b.HasKey(x => x.Id);
        b.Property(x => x.Evento).HasMaxLength(40).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
        b.HasIndex(x => new { x.AssociadoId, x.OcorridoEm });
        b.HasOne<Associado>().WithMany().HasForeignKey(x => x.AssociadoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DependenteConfiguration : IEntityTypeConfiguration<Dependente>
{
    public void Configure(EntityTypeBuilder<Dependente> b)
    {
        b.ToTable("Dependentes", t => t.HasCheckConstraint("CK_Dependentes_ParentescoOutro",
            "\"Parentesco\" <> 'Outro' OR \"ParentescoDescricao\" IS NOT NULL"));
        b.HasKey(x => x.Id);
        b.Property(x => x.NumeroIdentificacao).HasMaxLength(40).IsRequired();
        b.Property(x => x.ParentescoDescricao).HasMaxLength(60);
        b.Property(x => x.MotivoSituacao).HasMaxLength(300);
        b.Property(x => x.JustificativaExcecao).HasMaxLength(300);

        b.HasIndex(x => new { x.ClubeId, x.NumeroIdentificacao }).IsUnique();
        // Regra: a pessoa não pode ser dependente vigente de dois titulares no mesmo clube, salvo exceção registrada.
        b.HasIndex(x => new { x.ClubeId, x.PessoaId }).IsUnique()
            .HasFilter($"{Sql.Vigente} AND NOT \"ExcecaoMultiplosTitulares\"").HasDatabaseName("UX_Dependentes_PessoaVigente");
        // Nem com exceção a mesma pessoa pode ser dependente vigente duas vezes do mesmo titular.
        b.HasIndex(x => new { x.AssociadoTitularId, x.PessoaId }).IsUnique().HasFilter(Sql.Vigente)
            .HasDatabaseName("UX_Dependentes_TitularPessoaVigente");
        b.HasIndex(x => new { x.ClubeId, x.Situacao });
        b.HasIndex(x => x.PessoaId);

        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Pessoa).WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Vinculos).WithOne().HasForeignKey(x => x.DependenteId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Vinculos).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsOne(x => x.Permissoes, p =>
        {
            p.ToTable("PermissoesDependentes");
            p.WithOwner().HasForeignKey("DependenteId");
            p.HasKey("DependenteId");
        });
        b.Navigation(x => x.Permissoes).IsRequired();
    }
}

internal sealed class VinculoDependenteConfiguration : IEntityTypeConfiguration<VinculoDependente>
{
    public void Configure(EntityTypeBuilder<VinculoDependente> b)
    {
        b.ToTable("VinculosDependentes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Motivo).HasMaxLength(300).IsRequired();
        b.HasIndex(x => x.DependenteId);
        b.HasIndex(x => x.AssociadoTitularId);
        b.HasOne<Associado>().WithMany().HasForeignKey(x => x.AssociadoTitularId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class HistoricoDependenteConfiguration : IEntityTypeConfiguration<HistoricoDependente>
{
    public void Configure(EntityTypeBuilder<HistoricoDependente> b)
    {
        b.ToTable("HistoricoDependentes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Evento).HasMaxLength(40).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
        b.HasIndex(x => new { x.DependenteId, x.OcorridoEm });
        b.HasOne<Dependente>().WithMany().HasForeignKey(x => x.DependenteId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ConfiguracaoDependentesConfiguration : IEntityTypeConfiguration<ConfiguracaoDependentes>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoDependentes> b)
    {
        b.ToTable("ConfiguracoesDependentes", t =>
        {
            t.HasCheckConstraint("CK_ConfigDependentes_Limite", "\"LimitePadraoDependentes\" >= 0 AND \"MaximoConjuges\" >= 0");
            t.HasCheckConstraint("CK_ConfigDependentes_Valor", "\"ValorAdicionalPorDependente\" >= 0");
        });
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ClubeId).IsUnique();
        b.Property(x => x.ValorAdicionalPorDependente).HasPrecision(12, 2);
        b.PrimitiveCollection(x => x.ParentescosPermitidos).ElementType(e => e.HasConversion<string>());
        b.ComplexProperty(x => x.PermissoesPadrao, p =>
        {
            p.Property(x => x.AcessoClube).HasColumnName("PadraoAcessoClube");
            p.Property(x => x.ParticipacaoEventos).HasColumnName("PadraoParticipacaoEventos");
            p.Property(x => x.UsoEspacosEsportivos).HasColumnName("PadraoUsoEspacosEsportivos");
            p.Property(x => x.InscricaoAtividades).HasColumnName("PadraoInscricaoAtividades");
            p.Property(x => x.SolicitacaoReservas).HasColumnName("PadraoSolicitacaoReservas");
            p.Property(x => x.RecebimentoComunicados).HasColumnName("PadraoRecebimentoComunicados");
            p.Property(x => x.UtilizacaoServicos).HasColumnName("PadraoUtilizacaoServicos");
        });
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ArquivoConfiguration : IEntityTypeConfiguration<Arquivo>
{
    public void Configure(EntityTypeBuilder<Arquivo> b)
    {
        b.ToTable("Arquivos", t => t.HasCheckConstraint("CK_Arquivos_Tamanho", "\"TamanhoBytes\" > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.NomeOriginal).HasMaxLength(150).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        b.Property(x => x.HashSha256).HasMaxLength(64).IsRequired();
        b.Property(x => x.Chave).HasMaxLength(300).IsRequired();
        b.HasIndex(x => x.ClubeId);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentoPessoalConfiguration : IEntityTypeConfiguration<DocumentoPessoal>
{
    public void Configure(EntityTypeBuilder<DocumentoPessoal> b)
    {
        b.ToTable("DocumentosPessoais");
        b.HasKey(x => x.Id);
        b.Property(x => x.Descricao).HasMaxLength(200);
        b.Property(x => x.ObservacaoAnalise).HasMaxLength(300);
        b.HasIndex(x => new { x.PessoaId, x.Situacao });
        b.HasOne<Pessoa>().WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Arquivo).WithMany().HasForeignKey(x => x.ArquivoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AcessoClubeConfiguration : IEntityTypeConfiguration<AcessoClube>
{
    public void Configure(EntityTypeBuilder<AcessoClube> b)
    {
        b.ToTable("AcessosClube");
        b.HasKey(x => x.Id);
        b.Property(x => x.Motivo).HasMaxLength(200);
        b.Property(x => x.Observacao).HasMaxLength(300);
        b.HasIndex(x => new { x.ClubeId, x.OcorridoEm });
        b.HasIndex(x => new { x.PessoaId, x.OcorridoEm });
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Pessoa>().WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SequenciaClubeConfiguration : IEntityTypeConfiguration<SequenciaClube>
{
    public void Configure(EntityTypeBuilder<SequenciaClube> b)
    {
        b.ToTable("SequenciasClube");
        b.HasKey(x => new { x.ClubeId, x.Nome });
        b.Property(x => x.Nome).HasMaxLength(40);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Cascade);
    }
}
