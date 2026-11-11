using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoClubes.Domain.Auditoria;

namespace ProjetoClubes.Infrastructure.Persistence.Configurations;

internal sealed class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> b)
    {
        b.ToTable("LogsAuditoria");
        b.HasKey(x => x.Id);
        b.Property(x => x.Entidade).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntidadeId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Ip).HasMaxLength(64);
        b.Property(x => x.CorrelationId).HasMaxLength(64);
        b.HasIndex(x => new { x.ClubeId, x.OcorridoEm });
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
        b.HasIndex(x => x.UsuarioId);
    }
}
