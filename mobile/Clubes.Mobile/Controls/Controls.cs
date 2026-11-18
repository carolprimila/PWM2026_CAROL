using System.Windows.Input;
using Clubes.Mobile.Models;
using Clubes.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Clubes.Mobile.Controls;

/// <summary>Cores do design system para os controles construídos em código (espelha Resources/Styles/Colors.xaml).</summary>
public static class Palette
{
    public static readonly Color Primary = Color.FromArgb("#0F5C4A");
    public static readonly Color PrimarySoft = Color.FromArgb("#E2EFEB");
    public static readonly Color Background = Color.FromArgb("#F6F7F5");
    public static readonly Color Surface = Colors.White;
    public static readonly Color Text = Color.FromArgb("#14201C");
    public static readonly Color Muted = Color.FromArgb("#6B7770");
    public static readonly Color Success = Color.FromArgb("#1E8E5A");
    public static readonly Color SuccessSoft = Color.FromArgb("#E3F4EB");
    public static readonly Color Warning = Color.FromArgb("#B7791F");
    public static readonly Color WarningSoft = Color.FromArgb("#FBF1DD");
    public static readonly Color Danger = Color.FromArgb("#C0392B");
    public static readonly Color DangerSoft = Color.FromArgb("#FBE6E3");
    public static readonly Color Border = Color.FromArgb("#E3E7E3");
    public static readonly Color NeutralSoft = Color.FromArgb("#EEF1EE");
}

/// <summary>Indicador de situação (chip) com tom visual.</summary>
public class StatusChip : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(StatusChip), "",
        propertyChanged: (b, _, _) => ((StatusChip)b).Update());
    public static readonly BindableProperty KindProperty = BindableProperty.Create(nameof(Kind), typeof(ChipKind), typeof(StatusChip), ChipKind.Neutral,
        propertyChanged: (b, _, _) => ((StatusChip)b).Update());

    private readonly Label _label = new() { FontSize = 12, FontFamily = "OpenSansSemibold", LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 };
    private readonly Border _border;

    public StatusChip()
    {
        _border = new Border { StrokeThickness = 0, Padding = new Thickness(10, 3), StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = _label };
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Content = _border;
        Update();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public ChipKind Kind { get => (ChipKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

    private void Update()
    {
        _label.Text = Text;
        var (bg, fg) = Kind switch
        {
            ChipKind.Success => (Palette.SuccessSoft, Palette.Success),
            ChipKind.Warning => (Palette.WarningSoft, Palette.Warning),
            ChipKind.Danger => (Palette.DangerSoft, Palette.Danger),
            ChipKind.Info => (Palette.PrimarySoft, Palette.Primary),
            _ => (Palette.NeutralSoft, Palette.Muted),
        };
        _border.BackgroundColor = bg;
        _label.TextColor = fg;
        IsVisible = !string.IsNullOrWhiteSpace(Text);
    }
}

/// <summary>Sobreposição de carregamento e de erro (com "Tentar novamente") para listas e detalhes.</summary>
public class StateView : ContentView
{
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(StateView), false,
        propertyChanged: (b, _, _) => ((StateView)b).Update());
    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(StateView), null,
        propertyChanged: (b, _, _) => ((StateView)b).Update());
    public static readonly BindableProperty RetryCommandProperty = BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(StateView), null,
        propertyChanged: (b, _, n) => ((StateView)b)._retry.Command = (ICommand?)n);

    private readonly ActivityIndicator _spinner = new() { Color = Palette.Primary, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, WidthRequest = 36, HeightRequest = 36 };
    private readonly Label _message = new() { HorizontalTextAlignment = TextAlignment.Center, TextColor = Palette.Text, FontSize = 15 };
    private readonly Button _retry = new() { Text = "Tentar novamente", HorizontalOptions = LayoutOptions.Center };
    private readonly VerticalStackLayout _error;

    public StateView()
    {
        _error = new VerticalStackLayout
        {
            Spacing = 16, Padding = 32, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = "ic_warning.png", WidthRequest = 40, HeightRequest = 40, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "Algo não saiu como esperado", FontFamily = "OpenSansSemibold", FontSize = 17, HorizontalTextAlignment = TextAlignment.Center, TextColor = Palette.Text },
                _message,
                _retry,
            },
        };
        BackgroundColor = Palette.Background;
        Content = new Grid { Children = { _spinner, _error } };
        Update();
    }

    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public string? ErrorMessage { get => (string?)GetValue(ErrorMessageProperty); set => SetValue(ErrorMessageProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    private void Update()
    {
        var hasError = !IsLoading && !string.IsNullOrWhiteSpace(ErrorMessage);
        _spinner.IsVisible = _spinner.IsRunning = IsLoading;
        _error.IsVisible = hasError;
        _message.Text = ErrorMessage;
        IsVisible = IsLoading || hasError;
    }
}

/// <summary>Estado vazio: ícone, mensagem e ação opcional.</summary>
public class EmptyView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(EmptyView), "",
        propertyChanged: (b, _, n) => ((EmptyView)b)._text.Text = (string)n);
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(EmptyView), "ic_inbox.png",
        propertyChanged: (b, _, n) => ((EmptyView)b)._icon.Source = (string)n);
    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyView), null,
        propertyChanged: (b, _, n) => { var v = (EmptyView)b; v._action.Text = (string?)n; v._action.IsVisible = !string.IsNullOrWhiteSpace((string?)n); });
    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(EmptyView), null,
        propertyChanged: (b, _, n) => ((EmptyView)b)._action.Command = (ICommand?)n);

    private readonly Image _icon = new() { Source = "ic_inbox.png", WidthRequest = 44, HeightRequest = 44, HorizontalOptions = LayoutOptions.Center, Opacity = 0.8 };
    private readonly Label _text = new() { HorizontalTextAlignment = TextAlignment.Center, TextColor = Palette.Muted, FontSize = 15 };
    private readonly Button _action = new() { IsVisible = false, HorizontalOptions = LayoutOptions.Center };

    public EmptyView()
    {
        Content = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(32, 48), VerticalOptions = LayoutOptions.Center, Children = { _icon, _text, _action } };
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
}

/// <summary>Imagem protegida da API (GET /api/arquivos/{id} com Bearer), carregada pelo FileService.</summary>
public class AuthImage : Image
{
    public static readonly BindableProperty FileIdProperty = BindableProperty.Create(nameof(FileId), typeof(Guid?), typeof(AuthImage), null,
        propertyChanged: (b, _, _) => ((AuthImage)b).Reload());
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(AuthImage), null,
        propertyChanged: (b, _, _) => ((AuthImage)b).Reload());

    public Guid? FileId { get => (Guid?)GetValue(FileIdProperty); set => SetValue(FileIdProperty, value); }
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    private async void Reload()
    {
        var id = FileId;
        Source = string.IsNullOrEmpty(Placeholder) ? null : ImageSource.FromFile(Placeholder);
        if (id is null) return;
        var files = IPlatformApplication.Current?.Services.GetService<FileService>();
        if (files is null) return;
        try
        {
            var source = await files.GetImageAsync(id);
            if (source is not null && FileId == id) Source = source;
        }
        catch (Exception)
        {
            // Mantém o marcador quando a imagem não pode ser carregada.
        }
    }
}

/// <summary>Campo de formulário: rótulo, entrada (com máscara opcional) e mensagem de validação.</summary>
public class FormField : ContentView
{
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(FormField), "",
        propertyChanged: (b, _, n) => { var f = (FormField)b; f._label.Text = (string)n; f._label.IsVisible = !string.IsNullOrEmpty((string)n); });
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(FormField), "", BindingMode.TwoWay,
        propertyChanged: (b, _, n) => ((FormField)b).PushText((string?)n));
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FormField), "",
        propertyChanged: (b, _, _) => ((FormField)b).Configure());
    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(FormField), Keyboard.Default,
        propertyChanged: (b, _, _) => ((FormField)b).Configure());
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(FormField), false,
        propertyChanged: (b, _, _) => ((FormField)b).Configure());
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(FormField), 200,
        propertyChanged: (b, _, _) => ((FormField)b).Configure());
    public static readonly BindableProperty MaskProperty = BindableProperty.Create(nameof(Mask), typeof(MaskKind), typeof(FormField), MaskKind.None,
        propertyChanged: (b, _, _) => ((FormField)b).Configure());
    public static readonly BindableProperty MultilineProperty = BindableProperty.Create(nameof(Multiline), typeof(bool), typeof(FormField), false,
        propertyChanged: (b, _, _) => ((FormField)b).Rebuild());
    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(nameof(Error), typeof(string), typeof(FormField), null,
        propertyChanged: (b, _, _) => ((FormField)b).UpdateError());

    private readonly Label _label = new() { FontFamily = "OpenSansSemibold", FontSize = 13, TextColor = Palette.Text, IsVisible = false };
    private readonly Label _error = new() { FontSize = 12, TextColor = Palette.Danger, IsVisible = false };
    private readonly Border _border = new() { BackgroundColor = Palette.Surface, Stroke = Palette.Border, StrokeThickness = 1, Padding = new Thickness(12, 0), StrokeShape = new RoundRectangle { CornerRadius = 10 } };
    private InputView _input = null!;
    private bool _syncing;

    public FormField()
    {
        Content = new VerticalStackLayout { Spacing = 4, Children = { _label, _border, _error } };
        Rebuild();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
    public MaskKind Mask { get => (MaskKind)GetValue(MaskProperty); set => SetValue(MaskProperty, value); }
    public bool Multiline { get => (bool)GetValue(MultilineProperty); set => SetValue(MultilineProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    private void Rebuild()
    {
        if (_input is not null) _input.TextChanged -= OnInputTextChanged;
        _input = Multiline
            ? new Editor { AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 96 }
            : new Entry { ReturnType = ReturnType.Next };
        _input.TextChanged += OnInputTextChanged;
        _border.Padding = Multiline ? new Thickness(12, 4) : new Thickness(12, 0);
        _border.Content = _input;
        Configure();
        PushText(Text);
    }

    private void Configure()
    {
        if (_input is null) return;
        _input.Placeholder = Placeholder;
        _input.MaxLength = MaxLength;
        _input.Keyboard = Mask is MaskKind.Cpf or MaskKind.Phone or MaskKind.Cep or MaskKind.Digits ? Keyboard.Numeric : Keyboard;
        if (_input is Entry entry) entry.IsPassword = IsPassword;
    }

    private void PushText(string? value)
    {
        if (_input is null || _syncing) return;
        var masked = Masks.Apply(Mask, value ?? "");
        if (_input.Text != masked)
        {
            _syncing = true;
            _input.Text = masked;
            _syncing = false;
        }
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing) return;
        var raw = e.NewTextValue ?? "";
        var masked = Masks.Apply(Mask, raw);
        _syncing = true;
        if (masked != raw) _input.Text = masked;
        Text = masked;
        _syncing = false;
    }

    private void UpdateError()
    {
        var has = !string.IsNullOrWhiteSpace(Error);
        _error.Text = Error;
        _error.IsVisible = has;
        _border.Stroke = has ? Palette.Danger : Palette.Border;
    }
}
