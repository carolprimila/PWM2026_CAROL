using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.Application.Cadastros;

/// <summary>
/// Cria as configurações iniciais de cadastro de um clube (categorias, interesses, regras de dependentes).
/// São apenas pontos de partida: cada clube edita tudo depois. Idempotente.
/// </summary>
public interface IInicializadorClube
{
    Task GarantirPadroesAsync(Guid clubeId, CancellationToken ct);
}

public sealed class InicializadorClube(IAppDbContext db) : IInicializadorClube
{
    private sealed record ModeloCategoria(string Chave, string Nome, string Descricao, bool Voto, bool PodeSerVotado, bool Dependentes, int Ordem);

    private static readonly ModeloCategoria[] Categorias =
    [
        new("fundador", "Sócio fundador", "Participou da fundação do clube.", true, true, true, 1),
        new("associado", "Sócio associado", "Associado regular.", true, true, true, 2),
        new("convidado", "Sócio convidado", "Convidado pela diretoria, sem direito a voto.", false, false, true, 3),
        new("simpatizante", "Simpatizante", "Participa das atividades sem direitos políticos.", false, false, false, 4),
    ];

    private static readonly string[] Interesses =
        ["Pesca", "Caça", "Esportes", "Gastronomia", "Bebidas", "Atividades sociais", "Outros"];

    private static readonly string[] CategoriasReceita =
        ["Eventos", "Locação de espaços", "Doações", "Patrocínios", "Outras receitas"];

    private static readonly string[] CategoriasDespesa =
        ["Energia elétrica", "Água", "Manutenção", "Folha de pagamento", "Limpeza", "Impostos e taxas", "Outras despesas"];

    public async Task GarantirPadroesAsync(Guid clubeId, CancellationToken ct)
    {
        var semFiltro = FiltrosConsulta.SomenteTenant;

        if (!await db.CategoriasAssociados.IgnoreQueryFilters().AnyAsync(c => c.ClubeId == clubeId, ct))
            foreach (var m in Categorias)
                db.CategoriasAssociados.Add(new CategoriaAssociado(clubeId, m.Nome, m.Chave)
                {
                    Descricao = m.Descricao, DireitoVoto = m.Voto, PodeSerVotado = m.PodeSerVotado,
                    PermiteDependentes = m.Dependentes, Ordem = m.Ordem,
                });

        if (!await db.Interesses.IgnoreQueryFilters().AnyAsync(i => i.ClubeId == clubeId, ct))
            foreach (var nome in Interesses) db.Interesses.Add(new Interesse(clubeId, nome));

        if (!await db.ConfiguracoesDependentes.IgnoreQueryFilters(semFiltro).AnyAsync(c => c.ClubeId == clubeId, ct))
            db.ConfiguracoesDependentes.Add(new ConfiguracaoDependentes(clubeId));

        if (!await db.ConfiguracoesFinanceiras.IgnoreQueryFilters(semFiltro).AnyAsync(c => c.ClubeId == clubeId, ct))
            db.ConfiguracoesFinanceiras.Add(new ConfiguracaoFinanceira(clubeId));

        if (!await db.CategoriasFinanceiras.IgnoreQueryFilters().AnyAsync(c => c.ClubeId == clubeId, ct))
        {
            foreach (var nome in CategoriasReceita) db.CategoriasFinanceiras.Add(new CategoriaFinanceira(clubeId, nome, TipoLancamento.Receita));
            foreach (var nome in CategoriasDespesa) db.CategoriasFinanceiras.Add(new CategoriaFinanceira(clubeId, nome, TipoLancamento.Despesa));
        }
    }
}
