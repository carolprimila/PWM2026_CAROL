using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Application.Cadastros;

public enum AcaoPessoa
{
    VerDados,
    VerFoto,
    EditarFoto,
    VerDocumentos,
    EnviarDocumentos,
    RegenerarCarteirinha,
}

/// <summary>Quem é o usuário logado no portal do associado/dependente, dentro do clube atual.</summary>
public sealed record ContextoPortal(Pessoa Pessoa, Associado? Associado, Dependente? Dependente)
{
    public bool Titular => Associado is not null;
}

public interface IAcessoPessoas
{
    /// <summary>
    /// Garante que o usuário atual pode executar a ação sobre a pessoa: pela permissão administrativa ou,
    /// no portal, por ser a própria pessoa ou titular dela. Caso contrário responde 404 (não revela existência).
    /// </summary>
    Task<Pessoa> ExigirAsync(Guid pessoaId, AcaoPessoa acao, CancellationToken ct);

    /// <summary>Contexto do portal; lança 403 se o usuário não for associado/dependente deste clube.</summary>
    Task<ContextoPortal> ObterPortalAsync(CancellationToken ct);

    /// <summary>No portal: exige que a pessoa seja o próprio usuário ou um dependente dele (404 caso contrário).</summary>
    Task<ContextoPortal> ExigirPortalSobreAsync(Guid pessoaId, CancellationToken ct);

    /// <summary>Retorna o contexto do portal ou nulo, sem lançar.</summary>
    Task<ContextoPortal?> TentarObterPortalAsync(CancellationToken ct);
}

public sealed class AcessoPessoas(IAppDbContext db, IUsuarioAtual atual, IPermissaoResolver permissoes) : IAcessoPessoas
{
    private static readonly Dictionary<AcaoPessoa, string[]> PermissoesAdmin = new()
    {
        [AcaoPessoa.VerDados] = [Permissoes.Associados.Visualizar, Permissoes.Dependentes.Visualizar],
        [AcaoPessoa.VerFoto] = [Permissoes.Associados.Visualizar, Permissoes.Dependentes.Visualizar, Permissoes.ControleAcesso.Validar],
        [AcaoPessoa.EditarFoto] = [Permissoes.Associados.Editar, Permissoes.Dependentes.Editar],
        [AcaoPessoa.VerDocumentos] = [Permissoes.Documentos.Visualizar],
        [AcaoPessoa.EnviarDocumentos] = [Permissoes.Documentos.Gerenciar],
        [AcaoPessoa.RegenerarCarteirinha] = [Permissoes.Associados.Editar, Permissoes.Dependentes.Editar],
    };

    public async Task<Pessoa> ExigirAsync(Guid pessoaId, AcaoPessoa acao, CancellationToken ct)
    {
        var clubeId = atual.ClubeIdObrigatorio;
        var pessoa = await db.Pessoas.FirstOrDefaultAsync(p => p.Id == pessoaId && p.ClubeId == clubeId, ct)
                     ?? throw new NaoEncontradoException("Pessoa");

        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        if (PermissoesAdmin[acao].Any(minhas.Contains)) return pessoa;

        var portal = minhas.Contains(Permissoes.Portal.Acessar) ? await TentarObterPortalAsync(ct) : null;
        if (portal is not null && await PessoaDoPortalAsync(portal, pessoaId, ct)) return pessoa;

        throw new NaoEncontradoException("Pessoa");
    }

    public async Task<ContextoPortal> ObterPortalAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Portal.Acessar, ct);
        return await TentarObterPortalAsync(ct)
               ?? throw new AcessoNegadoException("Seu usuário não está vinculado a um associado ou dependente deste clube.");
    }

    public async Task<ContextoPortal> ExigirPortalSobreAsync(Guid pessoaId, CancellationToken ct)
    {
        var portal = await ObterPortalAsync(ct);
        if (!await PessoaDoPortalAsync(portal, pessoaId, ct)) throw new NaoEncontradoException("Pessoa");
        return portal;
    }

    public async Task<ContextoPortal?> TentarObterPortalAsync(CancellationToken ct)
    {
        if (atual.UsuarioId is not { } usuarioId || atual.ClubeId is not { } clubeId) return null;
        var pessoa = await db.Pessoas.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.ClubeId == clubeId, ct);
        if (pessoa is null) return null;

        var associado = await db.Associados.Include(a => a.Categoria)
            .Where(a => a.PessoaId == pessoa.Id && a.Situacao != SituacaoCadastral.Rejeitado)
            .OrderBy(a => a.Situacao == SituacaoCadastral.Ativo ? 0 : 1).ThenByDescending(a => a.CriadoEm)
            .FirstOrDefaultAsync(ct);
        var dependente = associado is not null ? null : await db.Dependentes.Include(d => d.AssociadoTitular).ThenInclude(a => a.Pessoa)
            .Where(d => d.PessoaId == pessoa.Id && d.Situacao != SituacaoCadastral.Rejeitado)
            .OrderBy(d => d.Situacao == SituacaoCadastral.Ativo ? 0 : 1).ThenByDescending(d => d.CriadoEm)
            .FirstOrDefaultAsync(ct);
        return associado is null && dependente is null ? null : new ContextoPortal(pessoa, associado, dependente);
    }

    private async Task<bool> PessoaDoPortalAsync(ContextoPortal portal, Guid pessoaId, CancellationToken ct)
    {
        if (portal.Pessoa.Id == pessoaId) return true;
        if (portal.Associado is not { } titular) return false;
        return await db.Dependentes.AnyAsync(d => d.AssociadoTitularId == titular.Id && d.PessoaId == pessoaId
                                                  && d.Situacao != SituacaoCadastral.Rejeitado, ct);
    }
}
