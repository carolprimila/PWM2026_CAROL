using Microsoft.Maui.Controls.Shapes;

namespace Clubes.Mobile.Controls;

public enum ChoiceTone { Normal, Unavailable, Mine }

/// <summary>Chip selecionável (horários, datas, filtros). O toque é tratado pela página.</summary>
public class ChoiceChip : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(ChoiceChip), "",
        propertyChanged: (b, _, _) => ((ChoiceChip)b).Update());
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(nameof(Caption), typeof(string), typeof(ChoiceChip), null,
        propertyChanged: (b, _, _) => ((ChoiceChip)b).Update());
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(ChoiceChip), false,
        propertyChanged: (b, _, _) => ((ChoiceChip)b).Update());
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(nameof(Tone), typeof(ChoiceTone), typeof(ChoiceChip), ChoiceTone.Normal,
        propertyChanged: (b, _, _) => ((ChoiceChip)b).Update());

    private readonly Label _text = new() { FontFamily = "OpenSansSemibold", FontSize = 14, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _caption = new() { FontSize = 11, HorizontalTextAlignment = TextAlignment.Center, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Border _border;

    public ChoiceChip()
    {
        _border = new Border
        {
            StrokeThickness = 1, Padding = new Thickness(12, 8), StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new VerticalStackLayout { Spacing = 0, Children = { _text, _caption } },
        };
        Content = _border;
        Update();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public ChoiceTone Tone { get => (ChoiceTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    private void Update()
    {
        _text.Text = Text;
        _caption.Text = Caption;
        _caption.IsVisible = !string.IsNullOrWhiteSpace(Caption);
        var (bg, fg, stroke) = IsSelected
            ? (Palette.Primary, Colors.White, Palette.Primary)
            : Tone switch
            {
                ChoiceTone.Unavailable => (Palette.NeutralSoft, Palette.Muted, Palette.NeutralSoft),
                ChoiceTone.Mine => (Palette.SuccessSoft, Palette.Success, Palette.SuccessSoft),
                _ => (Palette.Surface, Palette.Text, Palette.Border),
            };
        _border.BackgroundColor = bg;
        _border.Stroke = stroke;
        _text.TextColor = fg;
        _caption.TextColor = fg;
        _text.TextDecorations = !IsSelected && Tone == ChoiceTone.Unavailable ? TextDecorations.Strikethrough : TextDecorations.None;
    }
}

/// <summary>Linha de menu: ícone, texto e seta.</summary>
public class MenuRow : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(MenuRow), "",
        propertyChanged: (b, _, n) => ((MenuRow)b)._text.Text = (string)n);
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(MenuRow), null,
        propertyChanged: (b, _, n) => ((MenuRow)b)._icon.Source = (string?)n);
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(System.Windows.Input.ICommand), typeof(MenuRow), null,
        propertyChanged: (b, _, n) => ((MenuRow)b)._tap.Command = (System.Windows.Input.ICommand?)n);
    public static readonly BindableProperty IsDangerProperty = BindableProperty.Create(nameof(IsDanger), typeof(bool), typeof(MenuRow), false,
        propertyChanged: (b, _, n) => ((MenuRow)b)._text.TextColor = (bool)n ? Palette.Danger : Palette.Text);

    private readonly Label _text = new() { FontSize = 15, TextColor = Palette.Text, VerticalOptions = LayoutOptions.Center };
    private readonly Image _icon = new() { WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center };
    private readonly TapGestureRecognizer _tap = new();

    public MenuRow()
    {
        var chevron = new Image { Source = "ic_chevron.png", WidthRequest = 20, HeightRequest = 20, VerticalOptions = LayoutOptions.Center };
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
        };
        grid.Add(_icon, 0);
        grid.Add(_text, 1);
        grid.Add(chevron, 2);
        var border = new Border
        {
            BackgroundColor = Palette.Surface, Stroke = Palette.Border, StrokeThickness = 1, Padding = new Thickness(14, 14),
            StrokeShape = new RoundRectangle { CornerRadius = 14 }, Content = grid,
        };
        border.GestureRecognizers.Add(_tap);
        Content = border;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public System.Windows.Input.ICommand? Command { get => (System.Windows.Input.ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public bool IsDanger { get => (bool)GetValue(IsDangerProperty); set => SetValue(IsDangerProperty, value); }
}
