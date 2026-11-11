using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProjetoClubes.Mobile;

/// <summary>
/// App do associado/dependente: login, carteirinhas com QR Code, dependentes (titular) e dados de contato.
/// Fala somente com a API REST (portal do associado). O refresh token fica no SecureStorage do aparelho.
/// </summary>
public partial class MainPage : ContentPage
{
    // No emulador Android, 10.0.2.2 é o computador onde a API roda.
    private static readonly string BaseApi = Configuracao.UrlApi;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string ChaveRefresh = "pc.refresh";

    private readonly HttpClient _api = new() { BaseAddress = new Uri(BaseApi), Timeout = TimeSpan.FromSeconds(20) };
    private string? _access;
    private string? _refresh;
    private Perfil? _perfil;
    private string _corClube = "#2F5D50";
    private bool _mostrarSenha;

    private static readonly (string Valor, string Rotulo)[] TiposSolicitacao =
        [("Solicitacao", "Solicitação"), ("Reclamacao", "Reclamação"), ("Sugestao", "Sugestão"), ("ProblemaEstrutural", "Problema no clube"),
         ("Documento", "Pedido de documento"), ("AlteracaoCadastral", "Alteração cadastral")];

    private List<EspacoApp> _espacos = [];

    private static readonly (string Valor, string Rotulo)[] Parentescos =
        [("Conjuge", "Cônjuge"), ("Filho", "Filho(a)"), ("Pai", "Pai"), ("Mae", "Mãe"), ("Irmao", "Irmão(ã)")];

    public MainPage()
    {
        InitializeComponent();
        PreencherLoginDemonstracao();
        DepParentescoPicker.ItemsSource = Parentescos.Select(p => p.Rotulo).ToList();
        DepNascimentoPicker.MaximumDate = DateTime.Today;
        DepNascimentoPicker.Date = DateTime.Today.AddYears(-10);
        TipoSolicitacaoPicker.ItemsSource = TiposSolicitacao.Select(t => t.Rotulo).ToList();
        TipoSolicitacaoPicker.SelectedIndex = 0;
        ReservaDiaPicker.MinimumDate = DateTime.Today;
        ReservaDiaPicker.Date = DateTime.Today.AddDays(7);
        ReservaInicioPicker.Time = new TimeSpan(12, 0, 0);
        ReservaFimPicker.Time = new TimeSpan(16, 0, 0);
        AuthPanel.IsVisible = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_access is not null) return;
        // Retoma a sessão salva no aparelho, se houver.
        var salvo = await LerRefreshAsync();
        if (salvo is not null && await RenovarAsync(salvo)) await AbrirAreaAsync();
    }

    // ------------------------------------------------------------------ login

    private void MostrarSenhaClicked(object? sender, EventArgs e)
    {
        _mostrarSenha = !_mostrarSenha;
        LoginSenhaEntry.IsPassword = !_mostrarSenha;
        MostrarSenhaBotao.Text = _mostrarSenha ? "Ocultar" : "Mostrar";
    }

    /// <summary>Envia o link de nova senha para o e-mail informado (a API responde igual exista ou não o e-mail).</summary>
    private async void EsqueciSenhaClicked(object? sender, EventArgs e)
    {
        var email = LoginEmailEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            email = (await DisplayPromptAsync("Esqueci minha senha", "Informe o e-mail do seu acesso:", "Enviar", "Cancelar", keyboard: Keyboard.Email))?.Trim();
        if (string.IsNullOrWhiteSpace(email)) return;
        try
        {
            using var r = await _api.PostAsJsonAsync("api/v1/auth/esqueci-senha", new { email }, Json);
            LoginMensagemLabel.Text = "";
            await DisplayAlertAsync("Esqueci minha senha",
                r.IsSuccessStatusCode ? "Se o e-mail estiver cadastrado, você vai receber um link para criar uma nova senha." : await MensagemErroAsync(r), "OK");
        }
        catch (Exception) { LoginMensagemLabel.Text = "Sem conexão com o servidor do clube."; }
    }

    /// <summary>LGPD: baixa os dados que o clube guarda sobre a pessoa e abre o compartilhamento do sistema.</summary>
    private async void ExportarDadosClicked(object? sender, EventArgs e)
    {
        try
        {
            using var r = await EnviarAsync(HttpMethod.Get, "api/v1/portal/meus-dados", null);
            if (!r.IsSuccessStatusCode) { StatusLabel.Text = await MensagemErroAsync(r); return; }
            var arquivo = Path.Combine(FileSystem.CacheDirectory, $"meus-dados-{DateTime.Now:yyyyMMdd}.json");
            await File.WriteAllBytesAsync(arquivo, await r.Content.ReadAsByteArrayAsync());
            StatusLabel.Text = "Arquivo com os seus dados gerado.";
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Meus dados do clube", File = new ShareFile(arquivo, "application/json") });
        }
        catch (Exception ex) { StatusLabel.Text = ex.Message; }
    }

    private async void EntrarClicked(object? sender, EventArgs e)
    {
        LoginMensagemLabel.Text = "";
        if (string.IsNullOrWhiteSpace(LoginEmailEntry.Text) || string.IsNullOrWhiteSpace(LoginSenhaEntry.Text))
        {
            LoginMensagemLabel.Text = "Informe e-mail e senha.";
            return;
        }
        EntrarBotao.IsEnabled = false;
        try
        {
            using var r = await _api.PostAsJsonAsync("api/v1/auth/login",
                new { email = LoginEmailEntry.Text.Trim(), senha = LoginSenhaEntry.Text, dispositivo = $"App {DeviceInfo.Platform}" }, Json);
            if (!r.IsSuccessStatusCode) { LoginMensagemLabel.Text = await MensagemErroAsync(r); return; }
            var t = (await r.Content.ReadFromJsonAsync<Tokens>(Json))!;
            if (!t.Usuario.Permissoes.Contains("portal.acessar") && !t.Usuario.SuperAdmin)
            {
                LoginMensagemLabel.Text = "Este aplicativo é para associados e dependentes. Use o sistema Web da administração.";
                return;
            }
            await GuardarAsync(t);
            LoginSenhaEntry.Text = "";
            await AbrirAreaAsync();
        }
        catch (Exception)
        {
            LoginMensagemLabel.Text = "Não foi possível conectar ao servidor do clube. Verifique sua internet e tente novamente.";
        }
        finally { EntrarBotao.IsEnabled = true; }
    }

    private async void SairClicked(object? sender, EventArgs e)
    {
        try { using var _ = await EnviarAsync(HttpMethod.Post, "api/v1/auth/logout", new { }); } catch { /* sai mesmo sem conexão */ }
        _access = _refresh = null;
        _perfil = null;
        SecureStorage.Default.Remove(ChaveRefresh);
        AppPanel.IsVisible = false;
        AuthPanel.IsVisible = true;
        PreencherLoginDemonstracao();
    }

    /// <summary>Em Debug, deixa o login pronto com o associado de demonstração (em Release não faz nada).</summary>
    private void PreencherLoginDemonstracao()
    {
        if (Configuracao.LoginDemonstracao is not { } demo) return;
        LoginEmailEntry.Text = demo.Email;
        LoginSenhaEntry.Text = demo.Senha;
    }

    // ------------------------------------------------------------------ área do associado

    private async Task AbrirAreaAsync()
    {
        AuthPanel.IsVisible = false;
        AppPanel.IsVisible = true;
        await CarregarAsync();
    }

    private async void AtualizarRefreshing(object? sender, EventArgs e)
    {
        await CarregarAsync();
        Atualizador.IsRefreshing = false;
    }

    private async Task CarregarAsync()
    {
        try
        {
            _perfil = await GetAsync<Perfil>("api/v1/portal/perfil");
            var periodo = DateTime.Now.Hour < 12 ? "Bom dia" : DateTime.Now.Hour < 18 ? "Boa tarde" : "Boa noite";
            SaudacaoLabel.Text = $"{periodo}, {_perfil.Pessoa.NomeCompleto.Split(' ')[0]}";
            SituacaoLabel.Text = _perfil.Tipo == "Associado"
                ? $"Associado · {_perfil.Categoria} · {_perfil.Situacao}"
                : $"Dependente de {_perfil.TitularNome} · {_perfil.Situacao}";
            TelefoneEntry.Text = _perfil.Pessoa.Telefone;
            EmailEntry.Text = _perfil.Pessoa.Email;
            TelefoneEntry.IsEnabled = EmailEntry.IsEnabled = SalvarContatoBotao.IsEnabled = _perfil.PodeEditarContato;

            await CarregarCarteirinhasAsync();
            FinanceiroCartao.IsVisible = _perfil.Tipo == "Associado";
            if (FinanceiroCartao.IsVisible) await CarregarFinanceiroAsync();
            await CarregarAvisosAsync();
            await CarregarComunicadosAsync();
            await CarregarEventosAsync();
            await CarregarAssembleiasAsync();
            await CarregarReservasAsync();
            await CarregarSolicitacoesAsync();
            DependentesCartao.IsVisible = _perfil.PodeGerenciarDependentes;
            if (_perfil.PodeGerenciarDependentes) await CarregarDependentesAsync();
            StatusLabel.Text = $"Atualizado às {DateTime.Now:HH:mm}. Puxe para baixo para atualizar.";
        }
        catch (Exception ex) { StatusLabel.Text = ex.Message; }
    }

    private async Task CarregarCarteirinhasAsync()
    {
        var cartoes = await GetAsync<List<Carteirinha>>("api/v1/portal/carteirinhas");
        CarteirinhasLista.Children.Clear();
        if (cartoes.FirstOrDefault()?.Clube is { } clube)
        {
            _corClube = clube.CorPrimaria;
            ClubeLabel.Text = clube.NomeExibicao ?? clube.Nome;
            SiglaLabel.Text = clube.Sigla.Length > 3 ? clube.Sigla[..3] : clube.Sigla;
            SiglaBorda.BackgroundColor = Color.FromArgb(_corClube);
        }
        foreach (var c in cartoes) CarteirinhasLista.Children.Add(MontarCarteirinha(c));
    }

    private View MontarCarteirinha(Carteirinha c)
    {
        var qr = new Image { WidthRequest = 150, HeightRequest = 150, HorizontalOptions = LayoutOptions.Center };
        _ = CarregarQrAsync(qr, c.PessoaId);

        var detalhes = new VerticalStackLayout { Spacing = 2 };
        detalhes.Add(new Label { Text = c.Nome, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1D2127") });
        detalhes.Add(Linha($"Nº {c.NumeroIdentificacao}"));
        if (c.Categoria is not null) detalhes.Add(Linha(c.Categoria));
        if (c.TitularNome is not null) detalhes.Add(Linha($"{Rotulo(c.Parentesco)} de {c.TitularNome}"));
        detalhes.Add(Situacao(c.Situacao));

        var corpo = new VerticalStackLayout { Padding = 16, Spacing = 14 };
        corpo.Add(detalhes);
        corpo.Add(new Border { Content = qr, Padding = 8, Stroke = Color.FromArgb("#E7E9EE"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }, HorizontalOptions = LayoutOptions.Center, BackgroundColor = Colors.White });
        corpo.Add(new Label { Text = "Apresente este QR Code na portaria.", FontSize = 12, TextColor = Color.FromArgb("#8A909C"), HorizontalTextAlignment = TextAlignment.Center });

        var topo = new Grid { BackgroundColor = Color.FromArgb(_corClube), Padding = new Thickness(16, 12) };
        topo.Add(new Label { Text = $"{c.Clube.NomeExibicao ?? c.Clube.Nome}  ·  {c.Tipo.ToUpperInvariant()}", TextColor = Colors.White, FontSize = 12, FontAttributes = FontAttributes.Bold });

        var conteudo = new VerticalStackLayout();
        conteudo.Add(topo);
        conteudo.Add(corpo);
        return new Border
        {
            Content = conteudo, StrokeThickness = 1, Stroke = Color.FromArgb("#E7E9EE"), BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
        };
    }

    private async Task CarregarQrAsync(Image destino, Guid pessoaId)
    {
        try
        {
            using var r = await EnviarAsync(HttpMethod.Get, $"api/v1/pessoas/{pessoaId}/qrcode", null);
            if (!r.IsSuccessStatusCode) return;
            var bytes = await r.Content.ReadAsByteArrayAsync();
            destino.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch { /* sem conexão: o cartão aparece sem QR até atualizar */ }
    }

    // ------------------------------------------------------------------ eventos

    private async Task CarregarEventosAsync()
    {
        var eventos = await GetAsync<List<PortalEvento>>("api/v1/portal/eventos");
        EventosLista.Children.Clear();
        if (eventos.Count == 0) { EventosLista.Children.Add(Linha("Nenhum evento programado.")); return; }
        var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        foreach (var pe in eventos.Take(10))
        {
            var e = pe.Evento;
            var minha = pe.MinhasInscricoes.FirstOrDefault(i => i.PessoaId == _perfil!.Pessoa.Id);
            var bloco = new VerticalStackLayout { Spacing = 2, Padding = new Thickness(0, 4) };
            bloco.Add(new Label { Text = e.Nome, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1D2127") });
            bloco.Add(Linha($"{e.Inicio.ToLocalTime().ToString("dd/MM", pt)} às {e.Inicio.ToLocalTime():HH:mm} · {e.Espaco ?? e.Local ?? "Local a definir"}"));
            var vagas = e.Capacidade is { } c ? $"{Math.Max(0, c - e.LugaresOcupados)} vaga(s)" : "vagas livres";
            bloco.Add(Linha($"{(e.Valor > 0 ? e.Valor.ToString("C", pt) + " · " : "")}{vagas}"));
            if (e.Situacao == "Cancelado") bloco.Add(Situacao("Cancelado"));
            else if (minha is not null)
            {
                var linha = new HorizontalStackLayout { Spacing = 8 };
                linha.Add(Situacao(minha.Situacao == "ListaEspera" ? "Lista de espera" : "Inscrito"));
                var cancelar = new Button { Text = "Cancelar inscrição", FontSize = 12, Padding = new Thickness(10, 0), HeightRequest = 32, BackgroundColor = Color.FromArgb("#F1F2F5"), TextColor = Color.FromArgb("#3F4550"), CornerRadius = 8 };
                cancelar.Clicked += async (_, _) => await AcaoAsync(HttpMethod.Delete, $"api/v1/eventos/inscricoes/{minha.Id}", null, "Inscrição cancelada.");
                linha.Add(cancelar);
                bloco.Add(linha);
            }
            else if (e.Situacao == "InscricoesAbertas")
            {
                var participar = new Button { Text = "Quero participar", FontSize = 13, HeightRequest = 36, BackgroundColor = Color.FromArgb(_corClube), TextColor = Colors.White, CornerRadius = 10, HorizontalOptions = LayoutOptions.Start };
                participar.Clicked += async (_, _) => await AcaoAsync(HttpMethod.Post, $"api/v1/portal/eventos/{e.Id}/inscricoes", new { acompanhantes = 0 }, "Inscrição feita!");
                bloco.Add(participar);
            }
            EventosLista.Children.Add(bloco);
        }
    }

    // ------------------------------------------------------------------ avisos e comunicados

    private async Task CarregarAvisosAsync()
    {
        var caixa = await GetAsync<CaixaAvisos>("api/v1/notificacoes");
        AvisosCartao.IsVisible = caixa.Itens.Count > 0;
        AvisosTitulo.Text = caixa.NaoLidas > 0 ? $"Avisos ({caixa.NaoLidas} novo{(caixa.NaoLidas > 1 ? "s" : "")})" : "Avisos";
        MarcarLidasBotao.IsVisible = caixa.NaoLidas > 0;
        AvisosLista.Children.Clear();
        foreach (var n in caixa.Itens.Take(5))
        {
            var bloco = new VerticalStackLayout { Spacing = 0 };
            bloco.Add(new Label
            {
                Text = n.Titulo, FontSize = 14, TextColor = Color.FromArgb("#1D2127"),
                FontAttributes = n.Lida ? FontAttributes.None : FontAttributes.Bold,
            });
            bloco.Add(Linha($"{n.CriadaEm.ToLocalTime():dd/MM HH:mm} · {n.Texto}"));
            AvisosLista.Children.Add(bloco);
        }
    }

    private async void MarcarAvisosLidosClicked(object? sender, EventArgs e) =>
        await AcaoAsync(HttpMethod.Post, "api/v1/notificacoes/lidas", null, "Avisos marcados como lidos.");

    private async Task CarregarComunicadosAsync()
    {
        var lista = await GetAsync<List<ComunicadoApp>>("api/v1/portal/comunicados");
        ComunicadosLista.Children.Clear();
        if (lista.Count == 0) { ComunicadosLista.Children.Add(Linha("Nenhum comunicado no momento.")); return; }
        foreach (var c in lista.Take(10))
        {
            var bloco = new VerticalStackLayout { Spacing = 2, Padding = new Thickness(0, 2) };
            var cabecalho = new HorizontalStackLayout { Spacing = 8 };
            cabecalho.Add(new Label { Text = (c.Fixado ? "📌 " : "") + c.Titulo, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1D2127"), VerticalOptions = LayoutOptions.Center });
            if (!c.Lido) cabecalho.Add(Situacao("Novo"));
            else if (c.ExigeConfirmacao && !c.Confirmado) cabecalho.Add(Situacao("Confirme a leitura"));
            bloco.Add(cabecalho);
            bloco.Add(Linha($"{c.PublicadoEm.ToLocalTime():dd/MM} · {(c.Texto.Length > 90 ? c.Texto[..90] + "… toque para ler" : c.Texto)}"));
            var toque = new TapGestureRecognizer();
            toque.Tapped += async (_, _) => await LerComunicadoAsync(c);
            bloco.GestureRecognizers.Add(toque);
            ComunicadosLista.Children.Add(bloco);
        }
    }

    private async Task LerComunicadoAsync(ComunicadoApp c)
    {
        var confirmar = false;
        if (c.ExigeConfirmacao && !c.Confirmado)
            confirmar = await DisplayAlertAsync(c.Titulo, c.Texto, "Li e estou ciente", "Depois");
        else
            await DisplayAlertAsync(c.Titulo, c.Texto, "Fechar");
        if (c.Lido && !confirmar) return;
        await AcaoAsync(HttpMethod.Post, $"api/v1/portal/comunicados/{c.Id}/leitura?confirmar={(confirmar ? "true" : "false")}", null,
            confirmar ? "Leitura confirmada. Obrigado!" : "Comunicado lido.");
    }

    // ------------------------------------------------------------------ assembleias

    private async Task CarregarAssembleiasAsync()
    {
        var lista = await GetAsync<List<AssembleiaApp>>("api/v1/portal/assembleias");
        AssembleiasCartao.IsVisible = lista.Count > 0; // dependentes não participam: a lista vem vazia
        AssembleiasLista.Children.Clear();
        foreach (var a in lista)
        {
            var bloco = new VerticalStackLayout { Spacing = 4 };
            var cabecalho = new HorizontalStackLayout { Spacing = 8 };
            cabecalho.Add(new Label { Text = a.Titulo, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1D2127"), VerticalOptions = LayoutOptions.Center });
            cabecalho.Add(Situacao(a.Situacao switch { "Convocada" => "Convocada", "EmAndamento" => "Em andamento", _ => "Encerrada" }));
            bloco.Add(cabecalho);
            bloco.Add(Linha($"{a.DataHora.ToLocalTime():dd/MM} às {a.DataHora.ToLocalTime():HH:mm}{(a.Local is null ? "" : $" · {a.Local}")}"));
            if (a.Situacao == "Convocada" && a.Convocacao is not null)
            {
                var edital = Linha("Toque para ler a convocação e as pautas.");
                var toque = new TapGestureRecognizer();
                toque.Tapped += async (_, _) => await DisplayAlertAsync("Convocação", a.Convocacao + "\n\nPautas:\n" +
                    string.Join("\n", a.Pautas.Select(p => $"{p.Ordem}. {p.Titulo}")), "Fechar");
                edital.GestureRecognizers.Add(toque);
                bloco.Add(edital);
            }
            if (a.Situacao == "EmAndamento")
            {
                if (!a.Presente) bloco.Add(Linha("Registre sua presença na mesa da assembleia (carteirinha) para votar."));
                else if (!a.AptoVotar) bloco.Add(Linha($"Você está presente, mas não vota: {a.MotivoInapto}"));
            }
            foreach (var p in a.Pautas.Where(p => p.ComVotacao))
            {
                var pauta = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(0, 4, 0, 0) };
                pauta.Add(new Label { Text = $"{p.Ordem}. {p.Titulo}", FontSize = 14, TextColor = Color.FromArgb("#1D2127") });
                if (p.Apuracao is { } ap)
                    pauta.Add(Linha("Resultado: " + string.Join(" · ", ap.Select(x => $"{x.Opcao} {x.Votos}")) + (p.Decisao is null ? "" : $"\nDecisão: {p.Decisao}")));
                else if (p.Votei) pauta.Add(Situacao("Voto registrado"));
                else if (p.Votacao == "Aberta" && a.Presente && a.AptoVotar)
                {
                    var opcoes = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
                    for (var i = 0; i < p.Opcoes.Count; i++)
                    {
                        var indice = i;
                        var botao = new Button { Text = p.Opcoes[i], FontSize = 13, HeightRequest = 36, Margin = new Thickness(0, 0, 8, 6), BackgroundColor = Color.FromArgb(_corClube), TextColor = Colors.White, CornerRadius = 10 };
                        botao.Clicked += async (_, _) => await VotarAsync(a, p, indice);
                        opcoes.Add(botao);
                    }
                    pauta.Add(opcoes);
                }
                else if (p.Votacao == "Aberta") pauta.Add(Linha("Votação aberta."));
                else pauta.Add(Linha("Votação ainda não aberta."));
                bloco.Add(pauta);
            }
            AssembleiasLista.Children.Add(bloco);
        }
    }

    private async Task VotarAsync(AssembleiaApp a, PautaApp p, int opcao)
    {
        if (!await DisplayAlertAsync("Confirmar voto", $"{p.Titulo}\n\nSeu voto: {p.Opcoes[opcao]}\n\nO voto não pode ser alterado.", "Votar", "Voltar"))
            return;
        await AcaoAsync(HttpMethod.Post, $"api/v1/portal/assembleias/{a.Id}/pautas/{p.Id}/voto", new { opcao }, "Voto registrado.");
    }

    // ------------------------------------------------------------------ reservas

    private async Task CarregarReservasAsync()
    {
        _espacos = await GetAsync<List<EspacoApp>>("api/v1/espacos");
        EspacoPicker.ItemsSource = _espacos.Select(e => $"{e.Nome}{(e.ValorLocacao + e.TaxaAdicional > 0 ? $" · R$ {e.ValorLocacao + e.TaxaAdicional:N2}" : "")}").ToList();
        var reservas = await GetAsync<List<ReservaApp>>("api/v1/portal/reservas");
        ReservasLista.Children.Clear();
        if (reservas.Count == 0) { ReservasLista.Children.Add(Linha("Você não tem reservas.")); return; }
        foreach (var r in reservas.Take(10))
        {
            var g = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)], Padding = new Thickness(0, 4) };
            var info = new VerticalStackLayout();
            info.Add(new Label { Text = r.Espaco, FontSize = 15, TextColor = Color.FromArgb("#1D2127") });
            info.Add(Linha($"{r.Inicio.ToLocalTime():dd/MM HH:mm} até {r.Fim.ToLocalTime():HH:mm}{(r.Motivo is null ? "" : $" · {r.Motivo}")}"));
            g.Add(info);
            g.Add(Situacao(r.Situacao == "Aprovada" ? "Ativo" : r.Situacao == "Recusada" ? "Rejeitado" : r.Situacao), 1);
            ReservasLista.Children.Add(g);
        }
    }

    private void AlternarReservaClicked(object? sender, EventArgs e)
    {
        ReservaForm.IsVisible = !ReservaForm.IsVisible;
        ReservarBotao.Text = ReservaForm.IsVisible ? "Cancelar" : "＋ Reservar";
    }

    private async void SolicitarReservaClicked(object? sender, EventArgs e)
    {
        if (EspacoPicker.SelectedIndex < 0) { await DisplayAlertAsync("Reserva", "Escolha o espaço.", "OK"); return; }
        var dia = ReservaDiaPicker.Date ?? DateTime.Today;
        var inicio = new DateTimeOffset(dia.Date + (ReservaInicioPicker.Time ?? TimeSpan.Zero));
        var fim = new DateTimeOffset(dia.Date + (ReservaFimPicker.Time ?? TimeSpan.Zero));
        if (fim <= inicio) fim = fim.AddDays(1); // termina depois da meia-noite
        await AcaoAsync(HttpMethod.Post, "api/v1/portal/reservas",
            new { espacoId = _espacos[EspacoPicker.SelectedIndex].Id, inicio, fim, finalidade = ReservaFinalidadeEntry.Text },
            "Reserva solicitada. Acompanhe a aprovação aqui.");
        if (StatusLabel.Text.StartsWith("Reserva solicitada")) AlternarReservaClicked(null, EventArgs.Empty);
    }

    // ------------------------------------------------------------------ solicitações

    private async Task CarregarSolicitacoesAsync()
    {
        var lista = await GetAsync<List<SolicitacaoApp>>("api/v1/portal/solicitacoes");
        SolicitacoesLista.Children.Clear();
        if (lista.Count == 0) { SolicitacoesLista.Children.Add(Linha("Nenhuma solicitação aberta.")); return; }
        foreach (var x in lista.Take(10))
        {
            var g = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)], Padding = new Thickness(0, 4) };
            var info = new VerticalStackLayout();
            info.Add(new Label { Text = x.Assunto, FontSize = 15, TextColor = Color.FromArgb("#1D2127") });
            info.Add(Linha($"Protocolo {x.Protocolo}{(x.Respostas > 0 ? $" · {x.Respostas} mensagem(ns) — toque para ver" : "")}"));
            g.Add(info);
            var rotulo = x.Situacao switch { "EmAndamento" => "Em andamento", "Concluida" => "Concluída", "Aberta" => "Em aberto", _ => x.Situacao };
            g.Add(Situacao(rotulo), 1);
            var toque = new TapGestureRecognizer();
            toque.Tapped += async (_, _) => await VerSolicitacaoAsync(x.Id);
            g.GestureRecognizers.Add(toque);
            SolicitacoesLista.Children.Add(g);
        }
    }

    private async Task VerSolicitacaoAsync(Guid id)
    {
        try
        {
            var d = await GetAsync<SolicitacaoDetalheApp>($"api/v1/portal/solicitacoes/{id}");
            var conversa = string.Join("\n\n", d.Respostas.Select(r => $"{(r.DaAdministracao ? "Secretaria" : "Você")} ({r.CriadaEm.ToLocalTime():dd/MM HH:mm}):\n{r.Texto}"));
            var corpo = conversa.Length > 0 ? d.Descricao + "\n\n" + conversa : d.Descricao + "\n\nAguardando resposta da secretaria.";
            await DisplayAlertAsync($"Protocolo {d.Protocolo}", corpo, "Fechar");
        }
        catch (Exception ex) { StatusLabel.Text = ex.Message; }
    }

    private void AlternarSolicitacaoClicked(object? sender, EventArgs e)
    {
        SolicitacaoForm.IsVisible = !SolicitacaoForm.IsVisible;
        SolicitarBotao.Text = SolicitacaoForm.IsVisible ? "Cancelar" : "＋ Nova";
    }

    private async void EnviarSolicitacaoClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AssuntoEntry.Text) || string.IsNullOrWhiteSpace(DescricaoEditor.Text))
        {
            await DisplayAlertAsync("Solicitação", "Preencha o assunto e a descrição.", "OK");
            return;
        }
        await AcaoAsync(HttpMethod.Post, "api/v1/portal/solicitacoes",
            new { tipo = TiposSolicitacao[Math.Max(0, TipoSolicitacaoPicker.SelectedIndex)].Valor, assunto = AssuntoEntry.Text.Trim(), descricao = DescricaoEditor.Text.Trim() },
            "Solicitação enviada. A secretaria responderá por aqui.");
        if (StatusLabel.Text.StartsWith("Solicitação enviada"))
        {
            AssuntoEntry.Text = DescricaoEditor.Text = "";
            AlternarSolicitacaoClicked(null, EventArgs.Empty);
        }
    }

    /// <summary>Executa uma ação na API, mostra o resultado e recarrega a tela.</summary>
    private async Task AcaoAsync(HttpMethod metodo, string url, object? corpo, string sucesso)
    {
        try
        {
            using var r = await EnviarAsync(metodo, url, corpo ?? (metodo == HttpMethod.Delete ? null : new { }));
            if (!r.IsSuccessStatusCode) { await DisplayAlertAsync("Não foi possível concluir", await MensagemErroAsync(r), "OK"); return; }
            await CarregarAsync();
            StatusLabel.Text = sucesso;
        }
        catch (Exception ex) { await DisplayAlertAsync("Erro", ex.Message, "OK"); }
    }

    private async Task CarregarFinanceiroAsync()
    {
        var f = await GetAsync<SituacaoFinanceira>("api/v1/portal/financeiro");
        var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        FinanceiroResumoLabel.Text = f.Mensalidades.Count == 0
            ? "Nenhuma mensalidade lançada ainda."
            : f.Adimplente
                ? (f.EmAberto == 0 ? "Tudo em dia." : $"Em dia · {f.EmAberto} em aberto, total {f.TotalDevido.ToString("C", pt)}")
                : $"{f.Vencidas} vencida(s) · total devido {f.TotalDevido.ToString("C", pt)} (com multa e juros)";
        FinanceiroResumoLabel.TextColor = Color.FromArgb(f.Adimplente ? "#5B6270" : "#A12A28");
        MensalidadesLista.Children.Clear();
        foreach (var m in f.Mensalidades)
        {
            var g = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)], Padding = new Thickness(0, 6) };
            var info = new VerticalStackLayout();
            info.Add(new Label { Text = $"{m.Competencia[5..]}/{m.Competencia[..4]} · {(m.Pagamento?.Valor ?? m.ValorAtualizado).ToString("C", pt)}", FontSize = 15, TextColor = Color.FromArgb("#1D2127") });
            info.Add(Linha(m.Situacao == "Paga" && m.Pagamento is { } p
                ? $"Pago em {p.Data:dd/MM/yyyy} · recibo nº {p.NumeroRecibo}"
                : $"Vence em {m.Vencimento:dd/MM/yyyy}"));
            g.Add(info);
            g.Add(Situacao(m.Situacao == "Aberta" ? "Em aberto" : m.Situacao), 1);
            MensalidadesLista.Children.Add(g);
        }
    }

    private async Task CarregarDependentesAsync()
    {
        var deps = await GetAsync<List<Dependente>>("api/v1/portal/dependentes");
        DependentesLista.Children.Clear();
        if (deps.Count == 0)
        {
            DependentesLista.Children.Add(Linha("Nenhum dependente cadastrado."));
            return;
        }
        foreach (var d in deps)
        {
            var g = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)], Padding = new Thickness(0, 6) };
            var nome = new VerticalStackLayout();
            nome.Add(new Label { Text = d.Nome, FontSize = 15, TextColor = Color.FromArgb("#1D2127") });
            nome.Add(Linha($"{Rotulo(d.Parentesco)}{(d.Idade is { } i ? $" · {i} anos" : "")} · Nº {d.NumeroIdentificacao}"));
            g.Add(nome);
            g.Add(Situacao(d.Situacao), 1);
            DependentesLista.Children.Add(g);
        }
    }

    private void AlternarFormularioClicked(object? sender, EventArgs e)
    {
        DependenteForm.IsVisible = !DependenteForm.IsVisible;
        IncluirBotao.Text = DependenteForm.IsVisible ? "Cancelar" : "＋ Incluir";
    }

    private async void IncluirDependenteClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DepNomeEntry.Text) || DepParentescoPicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Dados incompletos", "Informe o nome completo e o parentesco.", "OK");
            return;
        }
        try
        {
            using var r = await EnviarAsync(HttpMethod.Post, "api/v1/portal/dependentes", new
            {
                pessoa = new
                {
                    nomeCompleto = DepNomeEntry.Text.Trim(),
                    cpf = string.IsNullOrWhiteSpace(DepCpfEntry.Text) ? null : DepCpfEntry.Text.Trim(),
                    dataNascimento = DateOnly.FromDateTime(DepNascimentoPicker.Date ?? DateTime.Today),
                    sexo = "NaoInformado",
                },
                parentesco = Parentescos[DepParentescoPicker.SelectedIndex].Valor,
            });
            if (!r.IsSuccessStatusCode) { await DisplayAlertAsync("Não foi possível incluir", await MensagemErroAsync(r), "OK"); return; }
            DepNomeEntry.Text = DepCpfEntry.Text = "";
            DepParentescoPicker.SelectedIndex = -1;
            AlternarFormularioClicked(null, EventArgs.Empty);
            StatusLabel.Text = "Dependente enviado. O cadastro aguarda aprovação do clube.";
            await CarregarAsync();
        }
        catch (Exception ex) { await DisplayAlertAsync("Erro", ex.Message, "OK"); }
    }

    private async void SalvarContatoClicked(object? sender, EventArgs e)
    {
        try
        {
            using var r = await EnviarAsync(HttpMethod.Put, "api/v1/portal/perfil/contato", new
            {
                telefone = string.IsNullOrWhiteSpace(TelefoneEntry.Text) ? null : TelefoneEntry.Text.Trim(),
                email = string.IsNullOrWhiteSpace(EmailEntry.Text) ? null : EmailEntry.Text.Trim(),
            });
            StatusLabel.Text = r.IsSuccessStatusCode ? "Contato atualizado." : await MensagemErroAsync(r);
        }
        catch (Exception ex) { StatusLabel.Text = ex.Message; }
    }

    // ------------------------------------------------------------------ API e sessão

    private async Task<T> GetAsync<T>(string url)
    {
        using var r = await EnviarAsync(HttpMethod.Get, url, null);
        if (!r.IsSuccessStatusCode) throw new InvalidOperationException(await MensagemErroAsync(r));
        return (await r.Content.ReadFromJsonAsync<T>(Json))!;
    }

    /// <summary>Envia com o token; se ele expirou, renova uma vez e repete.</summary>
    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, object? corpo)
    {
        HttpRequestMessage Montar()
        {
            var m = new HttpRequestMessage(metodo, url);
            if (_access is not null) m.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _access);
            if (corpo is not null) m.Content = JsonContent.Create(corpo, options: Json);
            return m;
        }

        HttpResponseMessage r;
        try { r = await _api.SendAsync(Montar()); }
        catch (Exception) { throw new InvalidOperationException("Sem conexão com o servidor do clube."); }

        if (r.StatusCode == HttpStatusCode.Unauthorized && _refresh is not null)
        {
            r.Dispose();
            if (await RenovarAsync(_refresh)) return await _api.SendAsync(Montar());
            MainThread.BeginInvokeOnMainThread(() => SairClicked(null, EventArgs.Empty));
            throw new InvalidOperationException("Sua sessão expirou. Entre novamente.");
        }
        return r;
    }

    private async Task<bool> RenovarAsync(string refresh)
    {
        try
        {
            using var r = await _api.PostAsJsonAsync("api/v1/auth/refresh", new { refreshToken = refresh }, Json);
            if (!r.IsSuccessStatusCode) return false;
            await GuardarAsync((await r.Content.ReadFromJsonAsync<Tokens>(Json))!);
            return true;
        }
        catch { return false; }
    }

    private async Task GuardarAsync(Tokens t)
    {
        _access = t.AccessToken;
        _refresh = t.RefreshToken;
        try { await SecureStorage.Default.SetAsync(ChaveRefresh, t.RefreshToken); } catch { /* aparelho sem armazenamento seguro */ }
    }

    private static async Task<string?> LerRefreshAsync()
    {
        try { return await SecureStorage.Default.GetAsync(ChaveRefresh); } catch { return null; }
    }

    private static async Task<string> MensagemErroAsync(HttpResponseMessage r)
    {
        try
        {
            var p = await r.Content.ReadFromJsonAsync<JsonElement>(Json);
            if (p.TryGetProperty("errors", out var erros) && erros.ValueKind == JsonValueKind.Object)
                foreach (var campo in erros.EnumerateObject())
                    if (campo.Value.GetArrayLength() > 0) return campo.Value[0].GetString() ?? "Dados inválidos.";
            return p.TryGetProperty("title", out var t) ? t.GetString() ?? "Não foi possível concluir." : "Não foi possível concluir.";
        }
        catch { return "Não foi possível concluir."; }
    }

    // ------------------------------------------------------------------ visual

    private static Label Linha(string texto) => new() { Text = texto, FontSize = 13, TextColor = Color.FromArgb("#5B6270") };

    private static View Situacao(string s)
    {
        var (fundo, texto) = s switch
        {
            "Ativo" => ("#E3F4EA", "#1F7A45"),
            "Pendente" or "Em aberto" or "Confirme a leitura" => ("#FFF4D6", "#8A6100"),
            "Novo" or "Convocada" or "Em andamento" => ("#E3EDFB", "#1F4F8A"),
            "Voto registrado" => ("#E3F4EA", "#1F7A45"),
            "Paga" => ("#E3F4EA", "#1F7A45"),
            "Vencida" => ("#FDE3E3", "#A12A28"),
            "Suspenso" => ("#FFE9D9", "#A34A00"),
            "Rejeitado" => ("#FDE3E3", "#A12A28"),
            _ => ("#EEF0F3", "#5B6270"),
        };
        return new Border
        {
            BackgroundColor = Color.FromArgb(fundo), StrokeThickness = 0, Padding = new Thickness(10, 3),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = s, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(texto) },
        };
    }

    private static string Rotulo(string? parentesco) =>
        Parentescos.FirstOrDefault(p => p.Valor == parentesco).Rotulo ?? parentesco ?? "";

    // ------------------------------------------------------------------ modelos (respostas da API)

    private sealed record Usuario(string Nome, bool SuperAdmin, List<string> Permissoes);
    private sealed record AvisoApp(Guid Id, string Tipo, string Titulo, string Texto, DateTimeOffset CriadaEm, bool Lida);
    private sealed record CaixaAvisos(int NaoLidas, List<AvisoApp> Itens);
    private sealed record ComunicadoApp(Guid Id, string Titulo, string Texto, bool Fixado, DateTimeOffset PublicadoEm, bool ExigeConfirmacao, bool Lido, bool Confirmado);
    private sealed record ApuracaoApp(string Opcao, int Votos);
    private sealed record PautaApp(Guid Id, int Ordem, string Titulo, bool ComVotacao, List<string> Opcoes, string? Votacao, bool Votei,
        List<ApuracaoApp>? Apuracao, string? Decisao);
    private sealed record AssembleiaApp(Guid Id, string Titulo, DateTimeOffset DataHora, string? Local, string? Convocacao, string Situacao,
        bool Presente, bool AptoVotar, string? MotivoInapto, List<PautaApp> Pautas);
    private sealed record Tokens(string AccessToken, string RefreshToken, Usuario Usuario);
    private sealed record Pessoa(Guid Id, string NomeCompleto, string? Telefone, string? Email);
    private sealed record Perfil(Pessoa Pessoa, string Tipo, string NumeroIdentificacao, string Situacao, string? Categoria,
        string? TitularNome, bool PodeEditarContato, bool PodeGerenciarDependentes);
    private sealed record Clube(string Nome, string Sigla, string CorPrimaria, string? NomeExibicao);
    private sealed record Carteirinha(Guid PessoaId, string Tipo, string Nome, string NumeroIdentificacao, string Situacao,
        string? Categoria, string? TitularNome, string? Parentesco, Clube Clube);
    private sealed record EventoApp(Guid Id, string Nome, string? Local, string? Espaco, DateTimeOffset Inicio, int? Capacidade,
        int LugaresOcupados, decimal Valor, string Situacao);
    private sealed record InscricaoApp(Guid Id, Guid PessoaId, string Situacao);
    private sealed record PortalEvento(EventoApp Evento, List<InscricaoApp> MinhasInscricoes);
    private sealed record EspacoApp(Guid Id, string Nome, decimal ValorLocacao, decimal TaxaAdicional);
    private sealed record ReservaApp(Guid Id, string Espaco, DateTimeOffset Inicio, DateTimeOffset Fim, string Situacao, string? Motivo);
    private sealed record SolicitacaoApp(Guid Id, string Protocolo, string Assunto, string Situacao, int Respostas);
    private sealed record RespostaApp(string Texto, bool DaAdministracao, DateTimeOffset CriadaEm);
    private sealed record SolicitacaoDetalheApp(string Protocolo, string Descricao, List<RespostaApp> Respostas);
    private sealed record PagamentoResumo(long NumeroRecibo, DateOnly Data, decimal Valor);
    private sealed record Mensalidade(string Competencia, DateOnly Vencimento, decimal ValorAtualizado, string Situacao, PagamentoResumo? Pagamento);
    private sealed record SituacaoFinanceira(bool Adimplente, int EmAberto, int Vencidas, decimal TotalDevido, List<Mensalidade> Mensalidades);
    private sealed record Dependente(Guid Id, string Nome, int? Idade, string Parentesco, string NumeroIdentificacao, string Situacao);
}
