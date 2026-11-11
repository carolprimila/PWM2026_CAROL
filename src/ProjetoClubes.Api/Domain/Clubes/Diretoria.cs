using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Clubes;

/// <summary>Membro da diretoria da associação (presidente, vice, tesoureiro, diretores...) com o período do mandato.</summary>
public class MembroDiretoria : AuditableEntity, ITenantScoped
{
    public const string CargoPresidente = "Presidente";
    /// <summary>Mandato padrão do presidente (renovado a cada 2 anos).</summary>
    public const int AnosMandatoPadrao = 2;

    private MembroDiretoria() { }

    public MembroDiretoria(Guid clubeId, string nome, string cargo, DateOnly inicio, DateOnly? fim)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("diretoria.nome", "Informe o nome.");
        if (string.IsNullOrWhiteSpace(cargo)) throw new DomainException("diretoria.cargo", "Informe o cargo.");
        var termino = fim ?? inicio.AddYears(AnosMandatoPadrao).AddDays(-1);
        if (termino < inicio) throw new DomainException("diretoria.periodo", "O fim do mandato deve ser depois do início.");
        ClubeId = clubeId;
        Nome = nome.Trim();
        Cargo = cargo.Trim();
        InicioMandato = inicio;
        FimMandato = termino;
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Cargo { get; private set; } = null!;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public DateOnly InicioMandato { get; private set; }
    public DateOnly FimMandato { get; private set; }
    /// <summary>Mandato encerrado antes do previsto (renúncia, substituição).</summary>
    public DateOnly? EncerradoEm { get; private set; }

    public bool Presidente => string.Equals(Cargo, CargoPresidente, StringComparison.OrdinalIgnoreCase);
    public DateOnly TerminoEfetivo => EncerradoEm ?? FimMandato;
    public bool Atual(DateOnly hoje) => InicioMandato <= hoje && hoje <= TerminoEfetivo;

    public void Encerrar(DateOnly hoje)
    {
        if (EncerradoEm is not null || hoje > FimMandato) throw new DomainException("diretoria.encerrado", "Este mandato já terminou.");
        EncerradoEm = hoje < InicioMandato ? InicioMandato : hoje;
    }
}
