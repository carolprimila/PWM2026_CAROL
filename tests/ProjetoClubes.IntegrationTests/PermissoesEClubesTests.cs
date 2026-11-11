using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

[Collection(ColecaoApi.Nome)]
public class PermissoesTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    [Fact]
    public async Task Tesoureiro_nao_gerencia_usuarios_ate_receber_permissao_individual()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var (tesoureiroId, emailTesoureiro) = await _c.CriarUsuarioAsync(presidente, "Tesoureiro");
        var tesoureiro = await _c.LoginAsync(emailTesoureiro);

        Assert.Contains("financeiro.gerenciar", tesoureiro.Permissoes);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(tesoureiro.AccessToken).GetAsync("/api/v1/usuarios")).StatusCode);

        var perfilTesoureiro = await _c.PerfilIdAsync(presidente.AccessToken, "Tesoureiro");
        var conceder = await api.Cliente(presidente.AccessToken).PutAsJsonAsync($"/api/v1/usuarios/{tesoureiroId}/acesso", new
        {
            perfilIds = new[] { perfilTesoureiro },
            permissoesIndividuais = new[] { new { codigo = "usuarios.visualizar", concedida = true } },
        });
        Assert.Equal(HttpStatusCode.OK, conceder.StatusCode);
        // Vale na hora, sem novo login.
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(tesoureiro.AccessToken).GetAsync("/api/v1/usuarios")).StatusCode);

        var negar = await api.Cliente(presidente.AccessToken).PutAsJsonAsync($"/api/v1/usuarios/{tesoureiroId}/acesso", new
        {
            perfilIds = new[] { perfilTesoureiro },
            permissoesIndividuais = new[] { new { codigo = "financeiro.gerenciar", concedida = false } },
        });
        Assert.Equal(HttpStatusCode.OK, negar.StatusCode);
        var detalhe = await negar.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        var efetivas = detalhe.GetProperty("permissoesEfetivas").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.DoesNotContain("financeiro.gerenciar", efetivas);
        Assert.DoesNotContain("usuarios.visualizar", efetivas);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(tesoureiro.AccessToken).GetAsync("/api/v1/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Nao_permite_conceder_permissoes_que_o_proprio_usuario_nao_tem()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);

        // Perfil "Gestor de acessos" só com gestão de usuários.
        var perfil = await api.Cliente(presidente.AccessToken).PostAsJsonAsync("/api/v1/perfis", new
        {
            nome = "Gestor de acessos", descricao = "Somente usuários",
            permissoes = new[] { "usuarios.visualizar", "usuarios.gerenciar" },
        });
        Assert.Equal(HttpStatusCode.Created, perfil.StatusCode);

        var (_, emailGestor) = await _c.CriarUsuarioAsync(presidente, "Gestor de acessos");
        var gestor = await _c.LoginAsync(emailGestor);

        var perfilPresidente = await _c.PerfilIdAsync(gestor.AccessToken, "Presidente");
        var escalada = await api.Cliente(gestor.AccessToken).PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Cúmplice", email = $"cumplice.{Guid.NewGuid():N}@testes.local", perfilIds = new[] { perfilPresidente },
            senhaInicial = Cenario.SenhaPadrao,
        });
        Assert.Equal(HttpStatusCode.Forbidden, escalada.StatusCode);

        // Pode criar com um perfil contido nas próprias permissões.
        await _c.CriarUsuarioAsync(gestor, "Gestor de acessos");
    }

    [Fact]
    public async Task Remover_acesso_ao_clube_derruba_o_token_na_hora()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var (id, email) = await _c.CriarUsuarioAsync(presidente, "Secretário");
        var secretario = await _c.LoginAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(secretario.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await api.Cliente(presidente.AccessToken).PostAsync($"/api/v1/usuarios/{id}/inativar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(secretario.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Somente_superadmin_bloqueia_conta()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var (id, email) = await _c.CriarUsuarioAsync(presidente, "Funcionário");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await api.Cliente(presidente.AccessToken).PostAsync($"/api/v1/usuarios/{id}/bloquear", null)).StatusCode);

        var sa = await _c.SuperAdminAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await api.Cliente(sa.AccessToken).PostAsync($"/api/v1/usuarios/{id}/bloquear", null)).StatusCode);
        var login = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Cenario.SenhaPadrao });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Perfil_padrao_nao_pode_ser_excluido_e_perfil_em_uso_tambem_nao()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var cliente = api.Cliente(presidente.AccessToken);

        var padrao = await _c.PerfilIdAsync(presidente.AccessToken, "Tesoureiro");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cliente.DeleteAsync($"/api/v1/perfis/{padrao}")).StatusCode);

        await cliente.PostAsJsonAsync("/api/v1/perfis", new { nome = "Bilheteria", permissoes = new[] { "eventos.visualizar" } });
        await _c.CriarUsuarioAsync(presidente, "Bilheteria");
        var bilheteria = await _c.PerfilIdAsync(presidente.AccessToken, "Bilheteria");
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync($"/api/v1/perfis/{bilheteria}")).StatusCode);

        await cliente.PostAsJsonAsync("/api/v1/perfis", new { nome = "Temporário", permissoes = Array.Empty<string>() });
        var temporario = await _c.PerfilIdAsync(presidente.AccessToken, "Temporário");
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/v1/perfis/{temporario}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/perfis/{temporario}")).StatusCode);
    }
}

[Collection(ColecaoApi.Nome)]
public class ClubesTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    [Fact]
    public async Task Clube_novo_recebe_os_perfis_padrao()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var perfis = await api.Cliente(presidente.AccessToken).GetFromJsonAsync<JsonElement>("/api/v1/perfis", Cenario.Json);
        var nomes = perfis.EnumerateArray().Select(p => p.GetProperty("nome").GetString()).ToHashSet();
        Assert.Superset(new HashSet<string?> { "Presidente", "Vice-presidente", "Secretário", "Tesoureiro", "Administrador",
            "Funcionário", "Associado", "Dependente" }, nomes);
    }

    [Fact]
    public async Task Somente_superadmin_cria_clube()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var r = await api.Cliente(presidente.AccessToken).PostAsJsonAsync("/api/v1/clubes", new { nome = "Pirata", sigla = "PIRATA" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Validacao_e_duplicidade_retornam_mensagens_claras()
    {
        var sa = await _c.SuperAdminAsync();
        var cliente = api.Cliente(sa.AccessToken);

        var invalido = await cliente.PostAsJsonAsync("/api/v1/clubes", new { nome = "", sigla = "X", cnpj = "11.222.333/0001-80" });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        var erros = (await invalido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(erros.TryGetProperty("nome", out _));
        Assert.True(erros.TryGetProperty("sigla", out _));
        Assert.True(erros.TryGetProperty("cnpj", out _));

        var existente = await _c.CriarClubeComPresidenteAsync();
        var duplicado = await cliente.PostAsJsonAsync("/api/v1/clubes", new { nome = "Outro", sigla = existente.Sigla });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
    }

    [Fact]
    public async Task Edicao_e_configuracao_ficam_no_historico_de_auditoria()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var cliente = api.Cliente(presidente.AccessToken);

        var editar = await cliente.PutAsJsonAsync($"/api/v1/clubes/{clube.Id}", new
        {
            nome = "Nome Atualizado", sigla = clube.Sigla, cnpj = "11.222.333/0001-81", telefone = "(45) 99999-0000",
            endereco = new { cidade = "Guaíra", uf = "pr", cep = "85980-000" },
        });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        var dto = await editar.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("11222333000181", dto.GetProperty("cnpj").GetString());
        Assert.Equal("PR", dto.GetProperty("endereco").GetProperty("uf").GetString());

        var config = await cliente.PutAsJsonAsync($"/api/v1/clubes/{clube.Id}/configuracoes", new
        {
            corPrimaria = "#1f4e79", nomeExibicao = "Meu Clube", fusoHorario = "America/Sao_Paulo",
            exigeAprovacaoAssociados = true, exigeAprovacaoDependentes = false,
        });
        Assert.Equal(HttpStatusCode.OK, config.StatusCode);

        var hist = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/clubes/{clube.Id}/historico", Cenario.Json);
        var itens = hist.GetProperty("itens").EnumerateArray().ToList();
        Assert.Contains(itens, i => i.GetProperty("entidade").GetString() == "Clube" && i.GetProperty("acao").GetString() == "Alteracao"
                                    && i.GetProperty("alteracoes").GetString()!.Contains("Nome Atualizado"));
        Assert.Contains(itens, i => i.GetProperty("entidade").GetString() == "ConfiguracaoClube"
                                    && i.GetProperty("usuarioId").GetGuid() == presidente.UsuarioId);
    }

    [Fact]
    public async Task Novo_usuario_sem_senha_recebe_convite_e_define_a_senha()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var perfil = await _c.PerfilIdAsync(presidente.AccessToken, "Secretário");
        var email = $"convidado.{Guid.NewGuid():N}@testes.local";

        var r = await api.Cliente(presidente.AccessToken).PostAsJsonAsync("/api/v1/usuarios",
            new { nome = "Convidado", email, perfilIds = new[] { perfil } });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.True((await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("conviteEnviado").GetBoolean());

        var token = api.Emails.UltimoToken(email);
        var definir = await api.Cliente().PostAsJsonAsync("/api/v1/auth/redefinir-senha", new { token, novaSenha = "SenhaDoConvite2026" });
        Assert.Equal(HttpStatusCode.NoContent, definir.StatusCode);
        var s = await _c.LoginAsync(email, "SenhaDoConvite2026");
        Assert.Equal(clube.Id, s.ClubeAtualId);
    }
}
