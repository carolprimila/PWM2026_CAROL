using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Documentos e fotos: armazenamento privado, validação de formato e autorização.</summary>
[Collection(ColecaoApi.Nome)]
public class ArquivosTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    public static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << >> endobj\ntrailer << >>\n%%EOF");
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    public static MultipartFormDataContent Formulario(byte[] conteudo, string nome, string contentType, string? tipo = null)
    {
        var form = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(conteudo);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(arquivo, "arquivo", nome);
        if (tipo is not null) form.Add(new StringContent(tipo), "tipo");
        return form;
    }

    public static async Task<JsonElement> EnviarDocumentoAsync(ApiFixture api, string token, Guid pessoaId)
    {
        var r = await api.Cliente(token).PostAsync($"/api/v1/pessoas/{pessoaId}/documentos",
            Formulario(Pdf, "certidao.pdf", "application/pdf", "CertidaoCasamento"));
        Assert.True(r.StatusCode == HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    [Fact]
    public async Task Upload_download_analise_e_validacao_de_formato()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var pessoaId = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("pessoa").GetProperty("id").GetGuid();
        var cliente = api.Cliente(p.AccessToken);

        var doc = await EnviarDocumentoAsync(api, p.AccessToken, pessoaId);
        Assert.Equal("Pendente", doc.GetProperty("situacao").GetString());
        var docId = doc.GetProperty("id").GetGuid();

        var download = await cliente.GetAsync($"/api/v1/documentos/{docId}/conteudo");
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Pdf, await download.Content.ReadAsByteArrayAsync());

        // Executável disfarçado de PDF é recusado pela assinatura do conteúdo.
        var falso = await cliente.PostAsync($"/api/v1/pessoas/{pessoaId}/documentos",
            Formulario("MZ\x90\0 executavel"u8.ToArray(), "documento.pdf", "application/pdf", "Rg"));
        Assert.Equal(HttpStatusCode.BadRequest, falso.StatusCode);

        var rejeitar = await cliente.PostAsJsonAsync($"/api/v1/documentos/{docId}/analisar", new { aprovado = false, observacao = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejeitar.StatusCode);
        var aprovar = await cliente.PostAsJsonAsync($"/api/v1/documentos/{docId}/analisar", new { aprovado = true });
        Assert.Equal("Aprovado", (await aprovar.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("situacao").GetString());

        var lista = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/pessoas/{pessoaId}/documentos", Cenario.Json);
        Assert.Single(lista.EnumerateArray());
    }

    [Fact]
    public async Task Foto_aceita_somente_imagem_e_fica_disponivel_para_quem_pode_ver()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var pessoaId = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("pessoa").GetProperty("id").GetGuid();
        var cliente = api.Cliente(p.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/pessoas/{pessoaId}/foto")).StatusCode);
        var pdfComoFoto = await cliente.PutAsync($"/api/v1/pessoas/{pessoaId}/foto", Formulario(Pdf, "foto.png", "image/png"));
        Assert.Equal(HttpStatusCode.BadRequest, pdfComoFoto.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await cliente.PutAsync($"/api/v1/pessoas/{pessoaId}/foto", Formulario(Png, "foto.png", "image/png"))).StatusCode);
        var foto = await cliente.GetAsync($"/api/v1/pessoas/{pessoaId}/foto");
        Assert.Equal("image/png", foto.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-store", foto.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Documentos_de_outro_clube_nao_sao_acessiveis()
    {
        var a = await _c.CriarClubeComPresidenteAsync();
        var b = await _c.CriarClubeComPresidenteAsync();
        var pa = await _c.LoginAsync(a.EmailPresidente);
        var pb = await _c.LoginAsync(b.EmailPresidente);
        var pessoaA = (await _c.CriarAssociadoAsync(pa.AccessToken)).GetProperty("pessoa").GetProperty("id").GetGuid();
        var docId = (await EnviarDocumentoAsync(api, pa.AccessToken, pessoaA)).GetProperty("id").GetGuid();
        var clienteB = api.Cliente(pb.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.GetAsync($"/api/v1/documentos/{docId}/conteudo")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.GetAsync($"/api/v1/pessoas/{pessoaA}/documentos")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.PostAsync($"/api/v1/pessoas/{pessoaA}/documentos",
            Formulario(Pdf, "x.pdf", "application/pdf", "Rg"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.DeleteAsync($"/api/v1/documentos/{docId}")).StatusCode);
    }

    [Fact]
    public async Task Tesoureiro_sem_permissao_de_documentos_nao_ve_documentos()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var pessoaId = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("pessoa").GetProperty("id").GetGuid();
        var docId = (await EnviarDocumentoAsync(api, p.AccessToken, pessoaId)).GetProperty("id").GetGuid();
        var (_, email) = await _c.CriarUsuarioAsync(p, "Tesoureiro");
        var tesoureiro = api.Cliente((await _c.LoginAsync(email)).AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, (await tesoureiro.GetAsync($"/api/v1/documentos/{docId}/conteudo")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await tesoureiro.GetAsync($"/api/v1/associados?busca=")).StatusCode);
    }
}
