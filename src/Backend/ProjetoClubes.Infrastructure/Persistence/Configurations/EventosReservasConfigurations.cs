using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Eventos;
using ProjetoClubes.Domain.Pessoas;
using ProjetoClubes.Domain.Reservas;
using ProjetoClubes.Domain.Solicitacoes;

namespace ProjetoClubes.Infrastructure.Persistence.Configurations;

internal sealed class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> b)
    {
        b.ToTable("Eventos", t =>
        {
            t.HasCheckConstraint("CK_Eventos_Periodo", "\"Fim\" > \"Inicio\"");
            t.HasCheckConstraint("CK_Eventos_Capacidade", "\"Capacidade\" IS NULL OR \"Capacidade\" > 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(2000);
        b.Property(x => x.Local).HasMaxLength(150);
        b.Property(x => x.MotivoCancelamento).HasMaxLength(300);
        b.Property(x => x.Valor).HasPrecision(12, 2);
        b.HasIndex(x => new { x.ClubeId, x.Inicio });
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Espaco>().WithMany().HasForeignKey(x => x.EspacoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InscricaoEventoConfiguration : IEntityTypeConfiguration<InscricaoEvento>
{
    public void Configure(EntityTypeBuilder<InscricaoEvento> b)
    {
        b.ToTable("InscricoesEventos", t => t.HasCheckConstraint("CK_Inscricoes_Acompanhantes", "\"Acompanhantes\" >= 0"));
        b.HasKey(x => x.Id);
        b.Ignore(x => x.Lugares);
        // Uma inscrição ativa por pessoa e evento.
        b.HasIndex(x => new { x.EventoId, x.PessoaId }).IsUnique().HasFilter("\"Situacao\" <> 'Cancelada'")
            .HasDatabaseName("UX_Inscricoes_PessoaEvento");
        b.HasIndex(x => new { x.EventoId, x.Situacao });
        b.HasOne<Evento>().WithMany().HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Pessoa>().WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EspacoConfiguration : IEntityTypeConfiguration<Espaco>
{
    public void Configure(EntityTypeBuilder<Espaco> b)
    {
        b.ToTable("Espacos", t => t.HasCheckConstraint("CK_Espacos_Valores", "\"ValorLocacao\" >= 0 AND \"TaxaAdicional\" >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(1000);
        b.Property(x => x.Regras).HasMaxLength(2000);
        b.Property(x => x.ValorLocacao).HasPrecision(12, 2);
        b.Property(x => x.TaxaAdicional).HasPrecision(12, 2);
        b.HasIndex(x => new { x.ClubeId, x.Nome }).IsUnique().HasFilter("NOT \"Excluido\"");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> b)
    {
        // A restrição de exclusão contra sobreposição (EX_Reservas_SemConflito) é criada na migration "Eventos" via SQL.
        b.ToTable("Reservas", t => t.HasCheckConstraint("CK_Reservas_Periodo", "\"Fim\" > \"Inicio\""));
        b.HasKey(x => x.Id);
        b.Ignore(x => x.Ocupa);
        b.Property(x => x.Finalidade).HasMaxLength(200);
        b.Property(x => x.Motivo).HasMaxLength(300);
        b.Property(x => x.Valor).HasPrecision(12, 2);
        b.HasIndex(x => new { x.EspacoId, x.Inicio });
        b.HasIndex(x => new { x.ClubeId, x.Situacao });
        b.HasIndex(x => x.PessoaId);
        b.HasIndex(x => x.EventoId);
        b.HasOne(x => x.Espaco).WithMany().HasForeignKey(x => x.EspacoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Pessoa>().WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Evento>().WithMany().HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SolicitacaoConfiguration : IEntityTypeConfiguration<Solicitacao>
{
    public void Configure(EntityTypeBuilder<Solicitacao> b)
    {
        b.ToTable("Solicitacoes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Protocolo).HasMaxLength(20).IsRequired();
        b.Property(x => x.Assunto).HasMaxLength(150).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.ClubeId, x.Protocolo }).IsUnique();
        b.HasIndex(x => new { x.ClubeId, x.Situacao });
        b.HasIndex(x => x.PessoaId);
        b.HasMany(x => x.Respostas).WithOne().HasForeignKey(x => x.SolicitacaoId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Respostas).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne<Pessoa>().WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RespostaSolicitacaoConfiguration : IEntityTypeConfiguration<RespostaSolicitacao>
{
    public void Configure(EntityTypeBuilder<RespostaSolicitacao> b)
    {
        b.ToTable("RespostasSolicitacoes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Texto).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.SolicitacaoId, x.CriadaEm });
    }
}
