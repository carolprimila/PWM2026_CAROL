using System.Net;
using Clubes.Application.Access;
using Clubes.Application.Admin;
using Clubes.Application.Common;
using Clubes.Application.People;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Tests;

[Collection("api")]
public class AuthTests(ApiFixture fixture)
{
    [Fact]
    public async Task Login_devolve_tokens_e_identifica_usuario_e_clube()
    {
        var club = await fixture.NewClubAsync();
        var me = await club.Admin.GetAsync<MeDto>("/api/auth/me");
        Assert.Equal(UserKind.ClubAdmin, me.Kind);
        Assert.Equal(club.ClubId, me.ClubId);
        Assert.Contains(Permissions.MembersManage, me.Permissions);
    }

    [Fact]
    public async Task Senha_invalida_e_recusada_e_bloqueia_apos_tentativas()
    {
        var club = await fixture.NewClubAsync();
        var email = club.Admin.Auth!.User.Email;
        var anonymous = fixture.Anonymous();
        for (var i = 0; i < 5; i++)
            await anonymous.FailAsync(HttpMethod.Post, "/api/auth/login", HttpStatusCode.Unauthorized, new LoginRequest(email, "senha-errada-1", null));
        // Após o limite de tentativas, até a senha correta é recusada temporariamente.
        var error = await anonymous.FailAsync(HttpMethod.Post, "/api/auth/login", HttpStatusCode.TooManyRequests, new LoginRequest(email, "senha-errada-1", null));
        Assert.Contains("tentativas", error.Message);

        var history = await club.Admin.GetAsync<PagedResult<LoginHistoryDto>>("/api/usuarios/historico-acessos?success=false");
        Assert.True(history.Total >= 5);
    }

    [Fact]
    public async Task Endpoints_exigem_autenticacao()
    {
        var anonymous = fixture.Anonymous();
        await anonymous.FailAsync(HttpMethod.Get, "/api/associados", HttpStatusCode.Unauthorized);
        await anonymous.FailAsync(HttpMethod.Get, "/api/financeiro/cobrancas", HttpStatusCode.Unauthorized);
        await anonymous.FailAsync(HttpMethod.Post, "/api/portaria/validar", HttpStatusCode.Unauthorized, new ValidateRequest("12345678", null));
    }

    [Fact]
    public async Task Refresh_token_e_rotacionado_e_reuso_encerra_as_sessoes()
    {
        var club = await fixture.NewClubAsync();
        var anonymous = fixture.Anonymous();
        var first = club.Admin.Auth!;
        var second = await anonymous.PostAsync<AuthResponse>("/api/auth/refresh", new RefreshRequest(first.RefreshToken));
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        // O token antigo já foi consumido: reutilizá-lo é tratado como vazamento e derruba também a sessão nova.
        await anonymous.FailAsync(HttpMethod.Post, "/api/auth/refresh", HttpStatusCode.Unauthorized, new RefreshRequest(first.RefreshToken));
        await anonymous.FailAsync(HttpMethod.Post, "/api/auth/refresh", HttpStatusCode.Unauthorized, new RefreshRequest(second.RefreshToken));
    }

    [Fact]
    public async Task Usuario_bloqueado_nao_entra_e_sessoes_sao_encerradas()
    {
        var club = await fixture.NewClubAsync();
        var staff = await club.NewStaffAsync("Secretaria");
        var userId = staff.Auth!.User.Id;
        await club.Admin.OkAsync(HttpMethod.Post, $"/api/usuarios/{userId}/bloquear", new BlockRequest("Desligamento"));

        await fixture.Anonymous().FailAsync(HttpMethod.Post, "/api/auth/refresh", HttpStatusCode.Unauthorized, new RefreshRequest(staff.Auth.RefreshToken));
        var users = await club.Admin.GetAsync<PagedResult<UserDto>>("/api/usuarios");
        Assert.True(users.Items.First(u => u.Id == userId).IsBlocked);
    }

    [Fact]
    public async Task Alterar_senha_exige_senha_atual_e_politica_minima()
    {
        var club = await fixture.NewClubAsync();
        await club.Admin.FailAsync(HttpMethod.Post, "/api/auth/alterar-senha", HttpStatusCode.BadRequest, new ChangePasswordRequest("errada-123", "NovaSenha123"), "senha_invalida");
        await club.Admin.FailAsync(HttpMethod.Post, "/api/auth/alterar-senha", HttpStatusCode.BadRequest, new ChangePasswordRequest("qualquer-1", "somenteletras"), "senha_fraca");
    }
}

[Collection("api")]
public class AuthorizationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Portaria_valida_qr_mas_nao_acessa_financeiro_nem_cadastros()
    {
        var club = await fixture.NewClubAsync();
        var gate = await club.NewStaffAsync("Portaria");
        await gate.GetAsync<OccupancyDto>("/api/portaria/ocupacao");
        await gate.FailAsync(HttpMethod.Get, "/api/financeiro/cobrancas", HttpStatusCode.Forbidden);
        await gate.FailAsync(HttpMethod.Get, "/api/associados", HttpStatusCode.Forbidden);
        await gate.FailAsync(HttpMethod.Put, "/api/configuracoes", HttpStatusCode.Forbidden, new { settings = new { }, justification = "x" });
    }

    [Fact]
    public async Task Permissao_individual_concede_e_nega_alem_das_funcoes()
    {
        var club = await fixture.NewClubAsync();
        var gate = await club.NewStaffAsync("Portaria");
        var userId = gate.Auth!.User.Id;
        await club.Admin.PutAsync<UserDto>($"/api/usuarios/{userId}/permissoes",
            new SetOverridesRequest([new PermissionOverride(Permissions.MembersView, true), new PermissionOverride(Permissions.GateHistory, false)]));

        await gate.GetAsync<PagedResult<MemberListDto>>("/api/associados");
        await gate.FailAsync(HttpMethod.Get, "/api/acessos", HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Funcao_configuravel_sem_alterar_codigo()
    {
        var club = await fixture.NewClubAsync();
        var role = await club.Admin.PostAsync<RoleDto>("/api/funcoes", new SaveRoleRequest("Eventos", "Equipe de eventos", [Permissions.EventsView, Permissions.EventsManage]));
        var access = await club.Admin.PostAsync<TemporaryPasswordDto>("/api/usuarios", new CreateUserRequest("Ana Eventos", $"eventos-{Guid.NewGuid():N}@teste.local", UserKind.Staff, null, [role.Id]));
        var staff = await fixture.LoginAsync(access.Email, access.TemporaryPassword);
        Assert.Equal([Permissions.EventsManage, Permissions.EventsView], staff.Auth!.User.Permissions);
        await staff.FailAsync(HttpMethod.Get, "/api/reservas", HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Associado_nao_acessa_areas_administrativas()
    {
        var club = await fixture.NewClubAsync();
        var (_, member) = await club.NewMemberWithLoginAsync();
        await member.FailAsync(HttpMethod.Get, "/api/associados", HttpStatusCode.Forbidden);
        await member.FailAsync(HttpMethod.Get, "/api/usuarios", HttpStatusCode.Forbidden);
        await member.FailAsync(HttpMethod.Post, "/api/mensalidades/gerar", HttpStatusCode.Forbidden, new { competence = "2026-01" });
        await member.FailAsync(HttpMethod.Get, "/api/auditoria", HttpStatusCode.Forbidden);
    }
}

[Collection("api")]
public class TenancyTests(ApiFixture fixture)
{
    [Fact]
    public async Task Dados_de_um_clube_nao_sao_visiveis_nem_alteraveis_por_outro()
    {
        var a = await fixture.NewClubAsync();
        var b = await fixture.NewClubAsync();
        var memberB = await b.NewMemberAsync(name: "Associado do Clube B");

        var listA = await a.Admin.GetAsync<PagedResult<MemberListDto>>("/api/associados");
        Assert.DoesNotContain(listA.Items, m => m.Id == memberB.Id);
        await a.Admin.FailAsync(HttpMethod.Get, $"/api/associados/{memberB.Id}", HttpStatusCode.NotFound);
        await a.Admin.FailAsync(HttpMethod.Put, $"/api/associados/{memberB.Id}", HttpStatusCode.NotFound, new SaveMemberRequest { FullName = "Invasão", Cpf = ClubScope.NewCpf() });
        await a.Admin.FailAsync(HttpMethod.Post, $"/api/associados/{memberB.Id}/cancelar", HttpStatusCode.NotFound, new StatusChangeRequest("tentativa"));
    }

    [Fact]
    public async Task Cabecalho_de_clube_e_ignorado_para_quem_nao_e_superadministrador()
    {
        var a = await fixture.NewClubAsync();
        var b = await fixture.NewClubAsync();
        await b.NewMemberAsync();
        a.Admin.Http.DefaultRequestHeaders.Add("X-Club-Id", b.ClubId.ToString());
        var list = await a.Admin.GetAsync<PagedResult<MemberListDto>>("/api/associados");
        Assert.Equal(0, list.Total);
        var current = await a.Admin.GetAsync<Application.Clubs.ClubDto>("/api/clubes/atual");
        Assert.Equal(a.ClubId, current.Id);
    }

    [Fact]
    public async Task Superadministrador_opera_no_clube_selecionado()
    {
        var a = await fixture.NewClubAsync();
        var member = await a.NewMemberAsync();
        var super = await fixture.SuperAdminAsync();
        await super.FailAsync(HttpMethod.Get, "/api/clubes/atual", HttpStatusCode.BadRequest, code: "clube_nao_selecionado");
        super.Http.DefaultRequestHeaders.Add("X-Club-Id", a.ClubId.ToString());
        var list = await super.GetAsync<PagedResult<MemberListDto>>("/api/associados");
        Assert.Contains(list.Items, m => m.Id == member.Id);
    }

    [Fact]
    public async Task Banco_rejeita_vinculo_entre_registros_de_clubes_diferentes()
    {
        var a = await fixture.NewClubAsync();
        var b = await fixture.NewClubAsync();
        var memberB = await b.NewMemberAsync();

        // Mesmo contornando a aplicação (SQL direto), a chave estrangeira composta (ClubId, MemberId) impede o vínculo.
        var error = await Assert.ThrowsAnyAsync<Exception>(() => fixture.DbAsync(null, async db =>
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO "Dependent" ("Id", "ClubId", "MemberId", "FullName", "Relationship", "Status", "IsDeleted", "CreatedAt")
                VALUES ({Guid.NewGuid()}, {a.ClubId}, {memberB.Id}, 'Intruso', 'Filho', 'Active', false, now())
                """)));
        Assert.Contains("FK_Dependent_Member", error.Message);
    }

    [Fact]
    public async Task Aplicacao_bloqueia_gravacao_em_clube_diferente_do_contexto()
    {
        var a = await fixture.NewClubAsync();
        var b = await fixture.NewClubAsync();
        await Assert.ThrowsAsync<DomainException>(() => fixture.DbAsync(a.ClubId, async db =>
        {
            db.Set<Supplier>().Add(new Supplier { ClubId = b.ClubId, Name = "Fornecedor de outro clube" });
            return await db.SaveChangesAsync();
        }));
    }
}

[Collection("api")]
public class AuditTests(ApiFixture fixture)
{
    [Fact]
    public async Task Alteracao_cadastral_registra_usuario_valores_anteriores_e_posteriores()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync(name: "Nome Original");
        await club.Admin.PutAsync<MemberDto>($"/api/associados/{member.Id}", new SaveMemberRequest { FullName = "Nome Alterado", Cpf = member.Cpf });

        var logs = await club.Admin.GetAsync<PagedResult<AuditLogDto>>($"/api/auditoria?entityType=Member&entityId={member.Id}&operation=Alteração");
        var log = Assert.Single(logs.Items, l => l.AfterJson!.Contains("Nome Alterado"));
        Assert.Contains("Nome Original", log.BeforeJson);
        Assert.Equal(club.Admin.Auth!.User.Id, log.UserId);
        Assert.False(string.IsNullOrEmpty(log.Origin));
    }

    [Fact]
    public async Task Justificativa_e_gravada_em_operacoes_sensiveis()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/suspender", new StatusChangeRequest("Descumprimento do regimento"));
        var logs = await club.Admin.GetAsync<PagedResult<AuditLogDto>>($"/api/auditoria?entityType=Member&entityId={member.Id}");
        Assert.Contains(logs.Items, l => l.Justification == "Descumprimento do regimento");
    }

    [Fact]
    public async Task Registros_de_auditoria_sao_imutaveis_no_banco()
    {
        var club = await fixture.NewClubAsync();
        await club.NewMemberAsync();
        var update = await Assert.ThrowsAnyAsync<Exception>(() => fixture.DbAsync(null, db => db.Database.ExecuteSqlAsync($"""UPDATE "AuditLog" SET "UserName" = 'adulterado' WHERE "ClubId" = {club.ClubId}""")));
        Assert.Contains("imutáveis", update.Message);
        var delete = await Assert.ThrowsAnyAsync<Exception>(() => fixture.DbAsync(null, db => db.Database.ExecuteSqlAsync($"""DELETE FROM "AuditLog" WHERE "ClubId" = {club.ClubId}""")));
        Assert.Contains("imutáveis", delete.Message);
    }

    [Fact]
    public async Task Auditoria_de_um_clube_nao_expoe_registros_de_outro()
    {
        var a = await fixture.NewClubAsync();
        var b = await fixture.NewClubAsync();
        var memberB = await b.NewMemberAsync();
        var logs = await a.Admin.GetAsync<PagedResult<AuditLogDto>>($"/api/auditoria?entityId={memberB.Id}");
        Assert.Equal(0, logs.Total);
    }
}
