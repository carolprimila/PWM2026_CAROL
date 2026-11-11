using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ProjetoClubes.Application.Common;

/// <summary>Parâmetros comuns de listagem: ?pagina=1&amp;tamanhoPagina=20&amp;busca=&amp;ordenarPor=nome&amp;direcao=asc</summary>
public class ConsultaPaginada
{
    public const int TamanhoMaximo = 100;

    private int _pagina = 1;
    private int _tamanho = 20;

    public int Pagina { get => _pagina; set => _pagina = value < 1 ? 1 : value; }
    public int TamanhoPagina { get => _tamanho; set => _tamanho = Math.Clamp(value, 1, TamanhoMaximo); }
    public string? Busca { get; set; }
    public string? OrdenarPor { get; set; }
    public string? Direcao { get; set; }

    public bool Descendente => string.Equals(Direcao, "desc", StringComparison.OrdinalIgnoreCase);
}

public sealed record Pagina<T>(IReadOnlyList<T> Itens, int PaginaAtual, int TamanhoPagina, int TotalItens)
{
    public int TotalPaginas => TamanhoPagina == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}

public static class PaginacaoExtensions
{
    public static async Task<Pagina<T>> PaginarAsync<T>(this IQueryable<T> query, ConsultaPaginada c,
        CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var itens = await query.Skip((c.Pagina - 1) * c.TamanhoPagina).Take(c.TamanhoPagina).ToListAsync(ct);
        return new Pagina<T>(itens, c.Pagina, c.TamanhoPagina, total);
    }

    /// <summary>
    /// Ordenação por lista branca: somente campos mapeados explicitamente são aceitos
    /// (evita ordenação por propriedades arbitrárias vindas do cliente).
    /// </summary>
    public static IQueryable<T> Ordenar<T>(this IQueryable<T> query, ConsultaPaginada c,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> campos, string padrao)
    {
        var chave = c.OrdenarPor is not null && campos.ContainsKey(c.OrdenarPor.ToLowerInvariant())
            ? c.OrdenarPor.ToLowerInvariant()
            : padrao;
        var expr = campos[chave];
        return c.Descendente ? query.OrderByDescending(expr) : query.OrderBy(expr);
    }
}
