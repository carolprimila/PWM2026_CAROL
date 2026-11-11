using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados de demonstração. Ficam no banco (nunca em arrays no frontend), marcados com Clube.Demonstracao = true
/// e usuários com domínio @demo.projetoclubes.local, para que possam ser identificados e removidos.
/// A senha dos usuários de demonstração vem de configuração (Seed__SenhaDemo) — não existe senha no código.
/// </summary>
internal static class DadosDemonstracao
{
    private const string Dominio = "demo.projetoclubes.local";

    private sealed record ClubeDemo(string Nome, string Sigla, string Cidade, string Cor, (string Perfil, string Nome, string Login)[] Usuarios);

    private static readonly ClubeDemo[] Clubes =
    [
        new("Clube Foz 2", "FOZ2", "Foz do Iguaçu", "#2F5D50",
        [
            (ModelosPerfis.Presidente, "Presidente (demonstração) — Foz 2", "presidente.foz"),
            (ModelosPerfis.Tesoureiro, "Tesoureiro (demonstração) — Foz 2", "tesoureiro.foz"),
            (ModelosPerfis.Secretario, "Secretário (demonstração) — Foz 2", "secretario.foz"),
            (ModelosPerfis.Funcionario, "Portaria (demonstração) — Foz 2", "portaria.foz"),
        ]),
        new("Clube Guaíra 2", "GUAIRA2", "Guaíra", "#3B5B92",
        [
            (ModelosPerfis.Presidente, "Presidente (demonstração) — Guaíra 2", "presidente.guaira"),
            (ModelosPerfis.Tesoureiro, "Tesoureiro (demonstração) — Guaíra 2", "tesoureiro.guaira"),
        ]),
    ];

    public static async Task CriarAsync(IServiceProvider sp, string? senha, ILogger logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 10)
        {
            logger.LogWarning("Seed:Demo está ativo, mas Seed:SenhaDemo não foi definida (mínimo 10 caracteres). " +
                              "Dados de demonstração não foram criados.");
            return;
        }

        var db = sp.GetRequiredService<AppDbContext>();
        var hash = sp.GetRequiredService<IHashSenha>();
        var agora = sp.GetRequiredService<TimeProvider>().GetUtcNow();
        var catalogo = await db.Permissoes.ToDictionaryAsync(p => p.Codigo, p => p.Id, ct);
        var senhaHash = hash.Gerar(senha);

        foreach (var demo in Clubes)
        {
            if (await db.Clubes.IgnoreQueryFilters().AnyAsync(c => c.Sigla == demo.Sigla, ct)) continue;

            var clube = new Clube(demo.Nome, demo.Sigla, null, demonstracao: true);
            clube.Endereco.Cidade = demo.Cidade;
            clube.Endereco.Uf = "PR";
            clube.Configuracao.CorPrimaria = demo.Cor;
            clube.Configuracao.PrefixoMatricula = demo.Sigla;
            db.Clubes.Add(clube);

            var perfis = ModelosPerfis.InstanciarPara(clube.Id, catalogo);
            db.Perfis.AddRange(perfis);
            await sp.GetRequiredService<IInicializadorClube>().GarantirPadroesAsync(clube.Id, ct);

            foreach (var (perfilChave, nome, login) in demo.Usuarios)
            {
                var email = $"{login}@{Dominio}";
                var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == email, ct);
                if (usuario is null)
                {
                    usuario = new Usuario(nome, email);
                    usuario.DefinirSenhaHash(senhaHash, agora);
                    db.Usuarios.Add(usuario);
                }
                var vinculo = new UsuarioClube(usuario.Id, clube.Id);
                vinculo.DefinirPerfis([perfis.Single(p => p.ChaveModelo == perfilChave).Id]);
                db.UsuarioClubes.Add(vinculo);
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Clube de demonstração {Clube} criado com {Usuarios} usuário(s) @{Dominio}",
                demo.Nome, demo.Usuarios.Length, Dominio);
        }

        await CriarCadastrosAsync(sp, db, senhaHash, agora, logger, ct);
        await CriarPlanosAsync(db, logger, ct);
        await CriarEspacosEventosAsync(db, agora, logger, ct);
        await CriarEtapa7Async(sp, db, agora, logger, ct);
    }

    /// <summary>Comunicados, funcionários, patrimônio, estoque e uma assembleia convocada (Etapa 7).</summary>
    private static async Task CriarEtapa7Async(IServiceProvider sp, AppDbContext db, DateTimeOffset agora, ILogger logger, CancellationToken ct)
    {
        var sequencias = sp.GetRequiredService<IGeradorSequencia>();
        foreach (var sigla in Clubes.Select(c => c.Sigla))
        {
            var clube = await db.Clubes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Sigla == sigla && c.Demonstracao, ct);
            if (clube is null || await db.Funcionarios.IgnoreQueryFilters().AnyAsync(f => f.ClubeId == clube.Id, ct)) continue;
            var id = clube.Id;
            var hoje = DateOnly.FromDateTime(agora.UtcDateTime);

            var piscina = new Domain.Comunicacao.Comunicado(id, "Horário de verão da piscina",
                "A partir de dezembro a piscina abre de terça a domingo, das 9h às 20h. Às segundas fica fechada para manutenção.",
                Domain.Comunicacao.PublicoComunicado.Todos, exigeConfirmacao: false, fixado: true, expiraEm: null);
            piscina.Publicar(agora.AddDays(-2));
            var regulamento = new Domain.Comunicacao.Comunicado(id, "Novo regulamento do salão social",
                "O regulamento do salão social foi atualizado: som até 23h e limpeza incluída na taxa adicional. Leia e confirme.",
                Domain.Comunicacao.PublicoComunicado.Titulares, exigeConfirmacao: true, fixado: false, expiraEm: null);
            regulamento.Publicar(agora.AddDays(-1));
            db.Comunicados.AddRange(piscina, regulamento);

            var jose = new Domain.Operacional.Funcionario(id, "José Pereira", null, "Zelador", "Manutenção", new DateOnly(2019, 4, 1))
                { Telefone = "(45) 99811-2030", Escala = "Seg a Sáb, 7h às 16h" };
            var ana = new Domain.Operacional.Funcionario(id, "Ana Souza", null, "Recepcionista", "Secretaria", new DateOnly(2022, 8, 15))
                { Telefone = "(45) 99702-4411", Escala = "Ter a Dom, 9h às 18h" };
            var paulo = new Domain.Operacional.Funcionario(id, "Paulo Mendes", null, "Salva-vidas", "Esportes", new DateOnly(2024, 11, 1))
                { Escala = "Ter a Dom, 9h às 20h (temporada)" };
            paulo.RegistrarAusencia(Domain.Operacional.TipoAusencia.Ferias, hoje.AddDays(40), hoje.AddDays(69), "Férias após a temporada");
            db.Funcionarios.AddRange(jose, ana, paulo);
            foreach (var f in new[] { jose, ana, paulo })
                db.HistoricoFuncionarios.Add(new Domain.Operacional.HistoricoFuncionario(id, f.Id, "Admissao",
                    $"Admitido como {f.Cargo} ({f.Departamento}) em {f.Admissao:dd/MM/yyyy}.", null, agora));

            foreach (var (nome, local, valor, estado) in new[]
                     {
                         ("Mesa de sinuca oficial", "Salão de jogos", 4200m, Domain.Operacional.EstadoConservacao.Bom),
                         ("Cortador de grama a gasolina", "Depósito", 2300m, Domain.Operacional.EstadoConservacao.Regular),
                         ("Freezer horizontal 500 L", "Bar da piscina", 3100m, Domain.Operacional.EstadoConservacao.Bom),
                     })
            {
                var codigo = $"PAT-{await sequencias.ProximoAsync(id, "patrimonio", ct):000000}";
                var bem = new Domain.Operacional.Bem(id, codigo, nome, null, local, valor, new DateOnly(2023, 6, 1), estado);
                db.Bens.Add(bem);
                db.HistoricoBens.Add(new Domain.Operacional.HistoricoBem(id, bem.Id, "Cadastro", $"Cadastrado em {local}, estado {estado}.", null, agora));
            }

            foreach (var (nome, unidade, minimo, saldo, categoria) in new[]
                     {
                         ("Cloro granulado", "kg", 20m, 35m, "Piscina"),
                         ("Detergente neutro", "L", 10m, 4m, "Limpeza"),
                         ("Copos descartáveis 300 ml", "cx", 5m, 12m, "Bar"),
                         ("Carvão vegetal", "kg", 30m, 18m, "Churrasqueiras"),
                     })
            {
                var produto = new Domain.Operacional.Produto(id, nome, unidade, minimo, categoria);
                db.Produtos.Add(produto);
                db.MovimentacoesEstoque.Add(produto.Movimentar(Domain.Operacional.TipoMovimentacaoEstoque.Entrada, saldo, "Saldo inicial", null, agora));
            }

            var brasilia = TimeSpan.FromHours(-3);
            var data = new DateTimeOffset(agora.Year, agora.Month, 1, 19, 30, 0, brasilia).AddMonths(1).AddDays(14);
            var ago = new Domain.Assembleias.Assembleia(id, "Assembleia Geral Ordinária", Domain.Assembleias.TipoAssembleia.Ordinaria, data,
                "Salão social", "Ficam convocados os associados para a Assembleia Geral Ordinária, em primeira chamada às 19h30 e " +
                "em segunda chamada às 20h com qualquer número de presentes.", exigeAdimplencia: true);
            ago.AdicionarPauta("Prestação de contas do exercício", "Apresentação do balanço e parecer do conselho fiscal.", true, null);
            ago.AdicionarPauta("Eleição do conselho fiscal", null, true, ["Chapa única", "Contra", "Abstenção"]);
            ago.AdicionarPauta("Informes da diretoria", null, false, null);
            ago.Convocar(agora);
            db.Assembleias.Add(ago);

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dados da Etapa 7 (comunicados, funcionários, patrimônio, estoque, assembleia) criados no clube {Clube}", clube.Nome);
        }
    }

    /// <summary>Espaços e um evento de demonstração (feijoada que ocupa o salão de um dia para o outro).</summary>
    private static async Task CriarEspacosEventosAsync(AppDbContext db, DateTimeOffset agora, ILogger logger, CancellationToken ct)
    {
        foreach (var sigla in Clubes.Select(c => c.Sigla))
        {
            var clube = await db.Clubes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Sigla == sigla && c.Demonstracao, ct);
            if (clube is null || await db.Espacos.IgnoreQueryFilters().AnyAsync(e => e.ClubeId == clube.Id, ct)) continue;

            var salao = new Domain.Reservas.Espaco(clube.Id, "Salão social", Domain.Reservas.TipoEspaco.SalaoSocial)
            {
                Capacidade = 150, ValorLocacao = 600m, TaxaAdicional = 120m, ExigeAprovacao = true,
                Descricao = "Salão com cozinha industrial e banheiros.", Regras = "Som até 23h. Limpeza inclusa na taxa adicional.",
            };
            var churrasqueira = new Domain.Reservas.Espaco(clube.Id, "Churrasqueira 1", Domain.Reservas.TipoEspaco.Churrasqueira)
            { Capacidade = 30, ValorLocacao = 80m, ExigeAprovacao = false };
            var quadra = new Domain.Reservas.Espaco(clube.Id, "Quadra poliesportiva", Domain.Reservas.TipoEspaco.Quadra)
            { Capacidade = 20, ExigeAprovacao = false };
            db.Espacos.AddRange(salao, churrasqueira, quadra);

            var brasilia = TimeSpan.FromHours(-3);
            var dia = new DateTimeOffset(agora.Year, agora.Month, 1, 11, 0, 0, brasilia).AddMonths(1).AddDays(6);
            var evento = new Domain.Eventos.Evento(clube.Id, "Feijoada beneficente", dia, dia.AddHours(14), Domain.Eventos.PublicoEvento.AssociadosEDependentes)
            {
                Descricao = "Feijoada com música ao vivo. Renda revertida para a reforma do parquinho.",
                Local = salao.Nome, EspacoId = salao.Id, Capacidade = 120, Valor = 45m, MaximoAcompanhantes = 2,
            };
            db.Eventos.Add(evento);
            db.Reservas.Add(new Domain.Reservas.Reserva(clube.Id, salao.Id, null, evento.Id, evento.Inicio, evento.Fim,
                $"Evento: {evento.Nome}", 0, exigeAprovacao: false));
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Espaços e evento de demonstração criados no clube {Clube}", clube.Nome);
        }
    }

    /// <summary>Planos de mensalidade de demonstração (um por categoria que tem associados).</summary>
    private static async Task CriarPlanosAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var valores = new Dictionary<string, decimal> { ["fundador"] = 80m, ["associado"] = 120m, ["convidado"] = 90m, ["simpatizante"] = 50m };
        var semTenant = FiltrosConsulta.SomenteTenant;
        foreach (var sigla in Clubes.Select(c => c.Sigla))
        {
            var clube = await db.Clubes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Sigla == sigla && c.Demonstracao, ct);
            if (clube is null || await db.PlanosMensalidade.IgnoreQueryFilters().AnyAsync(p => p.ClubeId == clube.Id, ct)) continue;
            var categorias = await db.CategoriasAssociados.IgnoreQueryFilters(semTenant)
                .Where(c => c.ClubeId == clube.Id && c.ChaveModelo != null).ToListAsync(ct);
            foreach (var c in categorias.Where(c => valores.ContainsKey(c.ChaveModelo!)))
                db.PlanosMensalidade.Add(new Domain.Financeiro.PlanoMensalidade(clube.Id, $"Mensalidade {c.Nome}", c.Id, valores[c.ChaveModelo!], 10));
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Planos de mensalidade de demonstração criados no clube {Clube}", clube.Nome);
        }
    }

    private sealed record DependenteDemo(string Nome, Parentesco Parentesco, int Idade, Sexo Sexo, bool Pendente = false);

    private sealed record AssociadoDemo(string Clube, string Nome, string Categoria, Sexo Sexo, int Idade, SituacaoCadastral Situacao,
        string? LoginApp, DependenteDemo[] Dependentes);

    private static readonly AssociadoDemo[] Associados =
    [
        new("FOZ2", "Carlos Alberto Ribeiro", "fundador", Sexo.Masculino, 58, SituacaoCadastral.Ativo, "carlos.associado",
        [
            new("Mariana Costa Ribeiro", Parentesco.Conjuge, 54, Sexo.Feminino),
            new("Pedro Costa Ribeiro", Parentesco.Filho, 12, Sexo.Masculino),
        ]),
        new("FOZ2", "Ana Paula Souza", "associado", Sexo.Feminino, 41, SituacaoCadastral.Ativo, null,
        [
            new("Laura Souza Mendes", Parentesco.Filho, 8, Sexo.Feminino, Pendente: true),
        ]),
        new("FOZ2", "João Batista Lima", "convidado", Sexo.Masculino, 35, SituacaoCadastral.Pendente, null, []),
        new("GUAIRA2", "Roberto Fernandes", "associado", Sexo.Masculino, 47, SituacaoCadastral.Ativo, null,
        [
            new("Cláudia Fernandes", Parentesco.Conjuge, 45, Sexo.Feminino),
        ]),
    ];

    /// <summary>Associados e dependentes de demonstração (CPFs válidos gerados, sem correspondência com pessoas reais).</summary>
    private static async Task CriarCadastrosAsync(IServiceProvider sp, AppDbContext db, string senhaHash, DateTimeOffset agora,
        ILogger logger, CancellationToken ct)
    {
        var tokens = sp.GetRequiredService<ITokenService>();
        var sequencias = sp.GetRequiredService<IGeradorSequencia>();
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);
        var indiceCpf = 0;

        foreach (var grupo in Associados.GroupBy(a => a.Clube))
        {
            var clube = await db.Clubes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Sigla == grupo.Key && c.Demonstracao, ct);
            if (clube is null) continue;
            var semTenant = FiltrosConsulta.SomenteTenant;
            if (await db.Associados.IgnoreQueryFilters().AnyAsync(a => a.ClubeId == clube.Id, ct)) continue;

            var categorias = await db.CategoriasAssociados.IgnoreQueryFilters(semTenant).Where(c => c.ClubeId == clube.Id)
                .ToDictionaryAsync(c => c.ChaveModelo ?? c.Nome, ct);
            var cfg = await db.ConfiguracoesDependentes.IgnoreQueryFilters(semTenant).FirstAsync(c => c.ClubeId == clube.Id, ct);
            var perfilAssociado = await db.Perfis.IgnoreQueryFilters(semTenant)
                .FirstAsync(p => p.ClubeId == clube.Id && p.ChaveModelo == ModelosPerfis.Associado, ct);

            foreach (var a in grupo)
            {
                var pessoa = NovaPessoa(clube.Id, a.Nome, a.Sexo, a.Idade, hoje, ++indiceCpf, tokens, agora);
                var matricula = $"{grupo.Key}{await sequencias.ProximoAsync(clube.Id, "matricula", ct):000000}";
                var associado = new Associado(clube.Id, pessoa, matricula, categorias[a.Categoria].Id, hoje.AddYears(-3),
                    a.Situacao == SituacaoCadastral.Pendente, agora);
                db.Pessoas.Add(pessoa);
                db.Associados.Add(associado);
                db.HistoricoAssociados.Add(new HistoricoAssociado(clube.Id, associado.Id, "Cadastro", "Cadastro de demonstração.", null, agora));

                if (a.LoginApp is { } login)
                {
                    var email = $"{login}@{Dominio}";
                    pessoa.Email = email;
                    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == email, ct);
                    if (usuario is null)
                    {
                        usuario = new Usuario(a.Nome, email);
                        usuario.DefinirSenhaHash(senhaHash, agora);
                        db.Usuarios.Add(usuario);
                    }
                    var vinculo = new UsuarioClube(usuario.Id, clube.Id);
                    vinculo.DefinirPerfis([perfilAssociado.Id]);
                    db.UsuarioClubes.Add(vinculo);
                    pessoa.UsuarioId = usuario.Id;
                }

                var n = 0;
                foreach (var d in a.Dependentes)
                {
                    var pd = NovaPessoa(clube.Id, d.Nome, d.Sexo, d.Idade, hoje, ++indiceCpf, tokens, agora);
                    db.Pessoas.Add(pd);
                    var dep = new Dependente(clube.Id, pd, associado, d.Parentesco, null, $"{matricula}-{++n:00}",
                        RegrasDependentes.PermissoesIniciais(cfg, pd.DataNascimento, hoje), d.Pendente, null, agora);
                    db.Dependentes.Add(dep);
                    db.HistoricoDependentes.Add(new HistoricoDependente(clube.Id, dep.Id, "Inclusao", "Cadastro de demonstração.", null, agora));
                }
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Associados e dependentes de demonstração criados no clube {Clube}", clube.Nome);
        }
    }

    private static Pessoa NovaPessoa(Guid clubeId, string nome, Sexo sexo, int idade, DateOnly hoje, int indice, ITokenService tokens,
        DateTimeOffset agora)
    {
        var p = new Pessoa(clubeId, nome, tokens.GerarTokenOpaco(), agora)
        {
            Sexo = sexo,
            DataNascimento = hoje.AddYears(-idade).AddDays(-indice * 17),
        };
        p.Endereco.Cidade = "Foz do Iguaçu";
        p.Endereco.Uf = "PR";
        p.DefinirCpf(CpfDemonstracao(indice));
        return p;
    }

    /// <summary>CPF válido sintético (base 900.000.0xx) usado apenas em dados de demonstração.</summary>
    internal static string CpfDemonstracao(int indice)
    {
        var d = $"9000000{indice:00}".Select(c => c - '0').ToList();
        for (var tamanho = 9; tamanho <= 10; tamanho++)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += d[i] * (tamanho + 1 - i);
            var r = soma % 11;
            d.Add(r < 2 ? 0 : 11 - r);
        }
        return string.Concat(d);
    }
}
