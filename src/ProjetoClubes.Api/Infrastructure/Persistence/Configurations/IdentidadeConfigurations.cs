using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Infrastructure.Persistence.Configurations;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.EmailNormalizado).HasMaxLength(254).IsRequired();
        b.Property(x => x.SenhaHash).HasMaxLength(300);
        b.Property(x => x.CarimboSeguranca).HasMaxLength(64).IsRequired();
        // E-mail único entre contas não excluídas.
        b.HasIndex(x => x.EmailNormalizado).IsUnique().HasFilter("NOT \"Excluido\"");
        b.HasIndex(x => x.SuperAdmin).HasFilter("\"SuperAdmin\"");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubePadraoId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Clubes).WithOne(x => x.Usuario).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Clubes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UsuarioClubeConfiguration : IEntityTypeConfiguration<UsuarioClube>
{
    public void Configure(EntityTypeBuilder<UsuarioClube> b)
    {
        b.ToTable("UsuarioClubes");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UsuarioId, x.ClubeId }).IsUnique();
        b.HasIndex(x => new { x.ClubeId, x.Situacao });
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Perfis).WithOne().HasForeignKey(x => x.UsuarioClubeId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Permissoes).WithOne().HasForeignKey(x => x.UsuarioClubeId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Perfis).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Permissoes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UsuarioClubePerfilConfiguration : IEntityTypeConfiguration<UsuarioClubePerfil>
{
    public void Configure(EntityTypeBuilder<UsuarioClubePerfil> b)
    {
        b.ToTable("UsuarioClubePerfis");
        b.HasKey(x => new { x.UsuarioClubeId, x.PerfilId });
        b.HasIndex(x => x.PerfilId);
        b.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UsuarioPermissaoConfiguration : IEntityTypeConfiguration<UsuarioPermissao>
{
    public void Configure(EntityTypeBuilder<UsuarioPermissao> b)
    {
        b.ToTable("UsuarioPermissoes");
        b.HasKey(x => new { x.UsuarioClubeId, x.PermissaoId });
        b.HasOne(x => x.Permissao).WithMany().HasForeignKey(x => x.PermissaoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> b)
    {
        b.ToTable("Perfis");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(300);
        b.Property(x => x.ChaveModelo).HasMaxLength(40);
        b.HasIndex(x => new { x.ClubeId, x.Nome }).IsUnique().HasFilter("NOT \"Excluido\"");
        b.HasIndex(x => new { x.ClubeId, x.ChaveModelo }).IsUnique().HasFilter("\"ChaveModelo\" IS NOT NULL AND NOT \"Excluido\"");
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Permissoes).WithOne().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Permissoes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PerfilPermissaoConfiguration : IEntityTypeConfiguration<PerfilPermissao>
{
    public void Configure(EntityTypeBuilder<PerfilPermissao> b)
    {
        b.ToTable("PerfilPermissoes");
        b.HasKey(x => new { x.PerfilId, x.PermissaoId });
        b.HasOne(x => x.Permissao).WithMany().HasForeignKey(x => x.PermissaoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PermissaoConfiguration : IEntityTypeConfiguration<Permissao>
{
    public void Configure(EntityTypeBuilder<Permissao> b)
    {
        b.ToTable("Permissoes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
        b.Property(x => x.Modulo).HasMaxLength(80).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
    }
}

internal sealed class SessaoUsuarioConfiguration : IEntityTypeConfiguration<SessaoUsuario>
{
    public void Configure(EntityTypeBuilder<SessaoUsuario> b)
    {
        b.ToTable("SessoesUsuarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.RefreshTokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.RefreshTokenHashAnterior).HasMaxLength(64);
        b.Property(x => x.MotivoRevogacao).HasMaxLength(200);
        b.Property(x => x.Ip).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.Dispositivo).HasMaxLength(100);
        b.HasIndex(x => x.RefreshTokenHash).IsUnique();
        b.HasIndex(x => x.RefreshTokenHashAnterior);
        b.HasIndex(x => new { x.UsuarioId, x.RevogadaEm });
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Clube>().WithMany().HasForeignKey(x => x.ClubeId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class TokenRecuperacaoSenhaConfiguration : IEntityTypeConfiguration<TokenRecuperacaoSenha>
{
    public void Configure(EntityTypeBuilder<TokenRecuperacaoSenha> b)
    {
        b.ToTable("TokensRecuperacaoSenha");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.CarimboSeguranca).HasMaxLength(64).IsRequired();
        b.Property(x => x.Ip).HasMaxLength(64);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UsuarioId);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class HistoricoAcessoConfiguration : IEntityTypeConfiguration<HistoricoAcesso>
{
    public void Configure(EntityTypeBuilder<HistoricoAcesso> b)
    {
        b.ToTable("HistoricoAcessos");
        b.HasKey(x => x.Id);
        b.Property(x => x.EmailInformado).HasMaxLength(254);
        b.Property(x => x.Ip).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.Detalhe).HasMaxLength(300);
        b.HasIndex(x => new { x.UsuarioId, x.OcorridoEm });
        b.HasIndex(x => new { x.ClubeId, x.OcorridoEm });
        // Sem FKs: o histórico deve sobreviver à exclusão de usuários/clubes (rastreabilidade).
    }
}
