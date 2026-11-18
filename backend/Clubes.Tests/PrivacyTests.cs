using System.Net;
using System.Text.Json;
using Clubes.Application.Access;
using Clubes.Application.Finance;
using Clubes.Application.People;
using Clubes.Domain;

namespace Clubes.Tests;

[Collection("api")]
public class PrivacyTests(ApiFixture fixture)
{
    [Fact]
    public async Task Anonimizacao_remove_dados_pessoais_e_preserva_o_historico_financeiro()
    {
        var club = await fixture.NewClubAsync();
        var (member, api) = await club.NewMemberWithLoginAsync();
        var dependent = await club.NewDependentAsync(member.Id, "Filho Identificável");
        var charge = (await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Taxa paga", 60, ClubScope.Today))).Charge;
        await club.Admin.PostAsync<PaymentDto>("/api/pagamentos", new RegisterPaymentRequest(charge.Id, 60, PaymentMethod.Cash, null, null, null, Guid.NewGuid().ToString()));

        // Direito de acesso do titular: cópia dos próprios dados.
        var export = await api.GetAsync<JsonElement>("/api/associados/eu/meus-dados");
        Assert.Equal(member.FullName, export.GetProperty("associado").GetProperty("fullName").GetString());

        // Só associações canceladas podem ser anonimizadas.
        await club.Admin.FailAsync(HttpMethod.Post, $"/api/associados/{member.Id}/anonimizar", HttpStatusCode.Conflict, new StatusChangeRequest("Pedido do titular"));
        await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/cancelar", new StatusChangeRequest("Desligamento"));
        await club.Admin.OkAsync(HttpMethod.Post, $"/api/associados/{member.Id}/anonimizar", new StatusChangeRequest("Pedido do titular"));

        var after = await club.Admin.GetAsync<MemberDto>($"/api/associados/{member.Id}");
        Assert.DoesNotContain(member.FullName, after.FullName);
        Assert.NotEqual(member.Cpf, after.Cpf);
        Assert.False(after.HasAccess);
        Assert.DoesNotContain("Identificável", (await club.Admin.GetAsync<DependentDto>($"/api/dependentes/{dependent.Id}")).FullName);
        await fixture.Anonymous().FailAsync(HttpMethod.Post, "/api/auth/login", HttpStatusCode.Unauthorized, new LoginRequest(api.Auth!.User.Email, "qualquer-senha-1", null));

        // O pagamento continua no histórico financeiro, sem identificação pessoal.
        Assert.Equal(60m, (await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{charge.Id}")).Charge.PaidAmount);
    }

    [Fact]
    public async Task Consentimento_de_privacidade_e_registrado_por_usuario_e_versao()
    {
        var club = await fixture.NewClubAsync();
        var (_, api) = await club.NewMemberWithLoginAsync();
        Assert.False(api.Auth!.User.PrivacyAccepted);
        await api.OkAsync(HttpMethod.Post, "/api/auth/aceitar-privacidade");
        Assert.True((await api.GetAsync<MeDto>("/api/auth/me")).PrivacyAccepted);

        // Nova versão da política exige novo aceite.
        await club.ConfigureAsync(s => { s.PrivacyPolicy = "Política revisada"; s.PrivacyPolicyVersion = "2"; });
        Assert.False((await api.GetAsync<MeDto>("/api/auth/me")).PrivacyAccepted);
    }
}
