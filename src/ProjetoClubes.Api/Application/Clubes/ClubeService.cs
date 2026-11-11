using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Application.Usuarios;
using ProjetoClubes.Domain.Auditoria;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Clubes;

public sealed record LogAuditoriaDto(Guid Id, string Entidade, string EntidadeId, string Acao, string? Alteracoes,
    Guid? UsuarioId, string? UsuarioNome, DateTimeOffset OcorridoEm);

public interface IClubeService
{
    Task<Pagina<ClubeListaDto>> ListarAsync(ConsultaClubes consulta, CancellationToken ct);
    Task<ClubeDetalheDto> ObterAsync(Guid id, CancellationToken ct);
    Task<ClubeDetalheDto> CriarAsync(SalvarClubeRequest request, CancellationToken ct);
    Task<ClubeDetalheDto> AtualizarAsync(Guid id, SalvarClubeRequest request, CancellationToken ct);
    Task AlterarSituacaoAsync(Guid id, bool ativo, CancellationToken ct);
    Task<ConfiguracaoClubeDto> AtualizarConfiguracaoAsync(Guid id, AtualizarConfiguracaoClubeRequest request, CancellationToken ct);
    Task<Guid> AdicionarAdministradorAsync(Guid id, AdicionarAdministradorRequest request, CancellationToken ct);
    Task<Pagina<LogAuditoriaDto>> ListarHistoricoAsync(Guid id, ConsultaPaginada consulta, CancellationToken ct);
}

/// <summary>
/// Cadastro de clubes. Somente o Superadmin cria, ativa e desativa clubes. Usuários de um clube só
/// enxergam e alteram o próprio clube selecionado, conforme suas permissões.
/// </summary>
public sealed class ClubeService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IProvisionamentoUsuarios provisionamento,
    Cadastros.IInicializadorClube inicializador,
    IValidator<SalvarClubeRequest> salvarValidator,
    IValidator<AtualizarConfiguracaoClubeRequest> configValidator,
    IValidator<AdicionarAdministradorRequest> adminValidator) : IClubeService
{
    private static readonly Dictionary<string, Expression<Func<Clube, object?>>> Ordenacoes = new()
    {
        ["nome"] = c => c.Nome,
        ["sigla"] = c => c.Sigla,
        ["cidade"] = c => c.Endereco.Cidade,
        ["criadoem"] = c => c.CriadoEm,
    };

    public async Task<Pagina<ClubeListaDto>> ListarAsync(ConsultaClubes consulta, CancellationToken ct)
    {
        var query = ClubesVisiveis().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(consulta.Busca))
        {
            var termo = consulta.Busca.Trim().ToLower();
            query = query.Where(c => c.Nome.ToLower().Contains(termo) || c.Sigla.ToLower().Contains(termo)
                                     || (c.Endereco.Cidade != null && c.Endereco.Cidade.ToLower().Contains(termo)));
        }
        if (Enum.TryParse<SituacaoClube>(consulta.Situacao, true, out var situacao))
            query = query.Where(c => c.Situacao == situacao);

        var pagina = await query.Ordenar(consulta, Ordenacoes, "nome")
            .Select(c => new
            {
                c.Id, c.Nome, c.Sigla, c.Endereco.Cidade, c.Endereco.Uf, c.Situacao, c.Demonstracao,
                c.Configuracao.CorPrimaria,
                TotalUsuarios = db.UsuarioClubes.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
                    .Count(v => v.ClubeId == c.Id && v.Situacao == SituacaoVinculo.Ativo),
            })
            .PaginarAsync(consulta, ct);

        return new Pagina<ClubeListaDto>(
            pagina.Itens.Select(c => new ClubeListaDto(c.Id, c.Nome, c.Sigla, c.Cidade, c.Uf, c.Situacao.ToString(),
                c.Demonstracao, c.CorPrimaria, c.TotalUsuarios)).ToList(),
            pagina.PaginaAtual, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<ClubeDetalheDto> ObterAsync(Guid id, CancellationToken ct)
    {
        var clube = await ClubesVisiveis().AsNoTracking().Include(c => c.Configuracao)
                        .FirstOrDefaultAsync(c => c.Id == id, ct)
                    ?? throw new NaoEncontradoException("Clube");
        return ParaDetalhe(clube);
    }

    public async Task<ClubeDetalheDto> CriarAsync(SalvarClubeRequest request, CancellationToken ct)
    {
        ExigirSuperAdmin();
        await salvarValidator.ValidarAsync(request, ct);
        await GarantirUnicidadeAsync(null, request, ct);

        var clube = new Clube(request.Nome, request.Sigla, request.Cnpj);
        Aplicar(clube, request);
        db.Clubes.Add(clube);

        // Copia os perfis-modelo para o novo clube; a partir daqui o clube os edita livremente.
        var catalogo = await db.Permissoes.ToDictionaryAsync(p => p.Codigo, p => p.Id, ct);
        db.Perfis.AddRange(ModelosPerfis.InstanciarPara(clube.Id, catalogo));
        await inicializador.GarantirPadroesAsync(clube.Id, ct);

        await db.SaveChangesAsync(ct);
        return ParaDetalhe(clube);
    }

    public async Task<ClubeDetalheDto> AtualizarAsync(Guid id, SalvarClubeRequest request, CancellationToken ct)
    {
        await ExigirNoClubeAsync(id, Permissoes.Clubes.Editar, ct);
        await salvarValidator.ValidarAsync(request, ct);
        var clube = await db.Clubes.Include(c => c.Configuracao).FirstOrDefaultAsync(c => c.Id == id, ct)
                    ?? throw new NaoEncontradoException("Clube");
        await GarantirUnicidadeAsync(id, request, ct);

        clube.Nome = request.Nome.Trim();
        clube.DefinirSigla(request.Sigla);
        clube.Cnpj = string.IsNullOrWhiteSpace(request.Cnpj) ? null : Documentos.SomenteDigitos(request.Cnpj);
        Aplicar(clube, request);
        await db.SaveChangesAsync(ct);
        return ParaDetalhe(clube);
    }

    public async Task AlterarSituacaoAsync(Guid id, bool ativo, CancellationToken ct)
    {
        ExigirSuperAdmin();
        var clube = await db.Clubes.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NaoEncontradoException("Clube");
        if (ativo) clube.Ativar(); else clube.Desativar();
        await db.SaveChangesAsync(ct);
    }

    public async Task<ConfiguracaoClubeDto> AtualizarConfiguracaoAsync(Guid id, AtualizarConfiguracaoClubeRequest request,
        CancellationToken ct)
    {
        await ExigirNoClubeAsync(id, Permissoes.Clubes.Configurar, ct);
        await configValidator.ValidarAsync(request, ct);
        var config = await db.ConfiguracoesClube.FirstOrDefaultAsync(c => c.ClubeId == id, ct)
                     ?? throw new NaoEncontradoException("Configuração do clube");

        config.CorPrimaria = request.CorPrimaria.ToUpperInvariant();
        config.NomeExibicao = string.IsNullOrWhiteSpace(request.NomeExibicao) ? null : request.NomeExibicao.Trim();
        config.FusoHorario = request.FusoHorario;
        config.ExigeAprovacaoAssociados = request.ExigeAprovacaoAssociados;
        config.ExigeAprovacaoDependentes = request.ExigeAprovacaoDependentes;
        config.PrefixoMatricula = string.IsNullOrWhiteSpace(request.PrefixoMatricula) ? null : request.PrefixoMatricula.Trim();
        await db.SaveChangesAsync(ct);
        return ParaConfigDto(config);
    }

    public async Task<Guid> AdicionarAdministradorAsync(Guid id, AdicionarAdministradorRequest request, CancellationToken ct)
    {
        ExigirSuperAdmin();
        await adminValidator.ValidarAsync(request, ct);
        if (!await db.Clubes.AnyAsync(c => c.Id == id, ct)) throw new NaoEncontradoException("Clube");

        var perfil = await db.Perfis.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
                         .FirstOrDefaultAsync(p => p.ClubeId == id && p.ChaveModelo == request.PerfilChave, ct)
                     ?? throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                         nameof(request.PerfilChave), "Perfil não encontrado neste clube.")]);

        var resultado = await provisionamento.CriarOuVincularAsync(request.Nome, request.Email, id, [perfil.Id],
            request.SenhaInicial, ct);
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return resultado.Usuario.Id;
    }

    public async Task<Pagina<LogAuditoriaDto>> ListarHistoricoAsync(Guid id, ConsultaPaginada consulta, CancellationToken ct)
    {
        await ExigirNoClubeAsync(id, Permissoes.Auditoria.Visualizar, ct);
        var idTexto = id.ToString();
        var query = db.LogsAuditoria.AsNoTracking()
            .Where(l => (l.Entidade == nameof(Clube) && l.EntidadeId == idTexto)
                        || (l.Entidade == nameof(ConfiguracaoClube) && l.ClubeId == id))
            .OrderByDescending(l => l.OcorridoEm)
            .Select(l => new LogAuditoriaDto(l.Id, l.Entidade, l.EntidadeId, l.Acao.ToString(), l.Alteracoes, l.UsuarioId,
                db.Usuarios.IgnoreQueryFilters().Where(u => u.Id == l.UsuarioId).Select(u => u.Nome).FirstOrDefault(),
                l.OcorridoEm));
        return await query.PaginarAsync(consulta, ct);
    }

    // ---------------------------------------------------------------- auxiliares

    /// <summary>Superadmin vê todos; demais usuários veem apenas clubes com vínculo ativo.</summary>
    private IQueryable<Clube> ClubesVisiveis()
    {
        if (atual.SuperAdmin) return db.Clubes;
        var usuarioId = atual.UsuarioIdObrigatorio;
        return db.Clubes.Where(c => db.UsuarioClubes.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
            .Any(v => v.ClubeId == c.Id && v.UsuarioId == usuarioId && v.Situacao == SituacaoVinculo.Ativo));
    }

    private void ExigirSuperAdmin()
    {
        if (!atual.SuperAdmin) throw new AcessoNegadoException("Somente o Superadmin pode realizar esta operação.");
    }

    /// <summary>Exige que a operação seja sobre o clube selecionado (ou Superadmin) e que o usuário tenha a permissão.</summary>
    private async Task ExigirNoClubeAsync(Guid clubeId, string permissao, CancellationToken ct)
    {
        if (atual.SuperAdmin) return;
        if (atual.ClubeId != clubeId) throw new NaoEncontradoException("Clube");
        await permissoes.ExigirAsync(permissao, ct);
    }

    private async Task GarantirUnicidadeAsync(Guid? id, SalvarClubeRequest request, CancellationToken ct)
    {
        var sigla = request.Sigla.Trim().ToUpperInvariant();
        if (await db.Clubes.IgnoreQueryFilters().AnyAsync(c => c.Sigla == sigla && c.Id != id, ct))
            throw new ConflitoException("clube.sigla_duplicada", "Já existe um clube com esta sigla.");

        if (!string.IsNullOrWhiteSpace(request.Cnpj))
        {
            var cnpj = Documentos.SomenteDigitos(request.Cnpj);
            if (await db.Clubes.AnyAsync(c => c.Cnpj == cnpj && c.Id != id, ct))
                throw new ConflitoException("clube.cnpj_duplicado", "Já existe um clube com este CNPJ.");
        }
    }

    private static void Aplicar(Clube clube, SalvarClubeRequest r)
    {
        clube.Telefone = string.IsNullOrWhiteSpace(r.Telefone) ? null : Documentos.SomenteDigitos(r.Telefone);
        clube.Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim();
        clube.DataFundacao = r.DataFundacao;
        var e = r.Endereco;
        clube.Endereco.Logradouro = e?.Logradouro?.Trim();
        clube.Endereco.Numero = e?.Numero?.Trim();
        clube.Endereco.Complemento = e?.Complemento?.Trim();
        clube.Endereco.Bairro = e?.Bairro?.Trim();
        clube.Endereco.Cidade = e?.Cidade?.Trim();
        clube.Endereco.Uf = e?.Uf?.Trim().ToUpperInvariant();
        clube.Endereco.Cep = string.IsNullOrWhiteSpace(e?.Cep) ? null : Documentos.SomenteDigitos(e.Cep);
    }

    private static ClubeDetalheDto ParaDetalhe(Clube c) => new(
        c.Id, c.Nome, c.Sigla, c.Cnpj,
        new EnderecoDto(c.Endereco.Logradouro, c.Endereco.Numero, c.Endereco.Complemento, c.Endereco.Bairro,
            c.Endereco.Cidade, c.Endereco.Uf, c.Endereco.Cep),
        c.Telefone, c.Email, c.LogotipoArquivo, c.DataFundacao, c.Situacao.ToString(), c.Demonstracao, c.CriadoEm,
        ParaConfigDto(c.Configuracao));

    private static ConfiguracaoClubeDto ParaConfigDto(ConfiguracaoClube c) => new(c.CorPrimaria, c.NomeExibicao,
        c.FusoHorario, c.ExigeAprovacaoAssociados, c.ExigeAprovacaoDependentes, c.PrefixoMatricula);
}
