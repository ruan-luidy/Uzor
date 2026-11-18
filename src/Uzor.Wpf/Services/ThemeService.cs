using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HandyControl.Themes;
using Uzor.Wpf.Animation;

namespace Uzor.Wpf.Services
{
  /// <summary>
  /// Claro ou escuro: os pincéis do tema (os do HandyControl e as camadas da Uzor: o hover, a grade, a barra de
  /// rolagem, o que flutua, os tons do texto) ficam numa camada só, no fim dos mesclados do Application, e quem usa pega
  /// por DynamicResource (ou StaticResource: o pincel é o mesmo objeto o app inteiro).
  /// <para>Só pincel de cor fixa entra (18/11/2026, Windfinder 1.3.1): os degradês do HandyControl pegam a cor de uma
  /// Color do tema por DynamicResource, vinham transparentes copiados e ficam com ele (com as Color da partida).</para>
  /// <para>Pincéis vivos (18/11/2026, "o switch de dark mode pra light ainda dá uma engasgada feia"): a camada entra uma
  /// vez, na partida, e a troca de tema só muda a COR dos pincéis dela, por binding. Trocar dicionário fazia o WPF
  /// percorrer a árvore de todas as telas montadas atrás de quem usava cada chave: eram três passadas por troca (o
  /// HandyControl põe o dicionário novo, tira o velho, a Uzor troca a camada), 0,5 a 0,8 s com a tela parada e até
  /// 2,3 s até ela assentar no PC do Ruan; medido com 40 telas, de 2,8 a 4,5 s pra 13 ms. Mudar a cor de um pincel só
  /// manda redesenhar quem usa. O pincel tem binding pra não ser congelado (o Application congela o que é dele, e
  /// pincel congelado não troca de cor): código que congela caneta ou desenho feito com pincel do tema tem que conferir
  /// o CanFreeze antes.</para>
  /// <para>O HandyControl continua sabendo do tema (o ThemeManager muda, e quem ouve ele também), só não troca mais o
  /// dicionário dele depois da partida; as cores soltas dele (Color, não pincel) ficam as da partida. Guardar a escolha é
  /// do app: ele chama <see cref="Apply"/> na partida e <see cref="Switch"/> quando a pessoa troca.</para>
  /// </summary>
  public static class ThemeService
  {
    public static bool IsDark => ThemeManager.Current.ApplicationTheme != ApplicationTheme.Light;

    public static event EventHandler? Changed;

    /// <summary>
    /// Troca na tela com a onda saindo do mouse (<see cref="ThemeRipple"/>). Sem janela na tela, troca direto.
    /// </summary>
    public static void Switch(bool dark)
    {
      if (dark == IsDark)
        return;

      var window = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;
      if (window is null)
        Apply(dark);
      else
        ThemeRipple.Run(window, () => Apply(dark));
    }

    // A camada de pincéis vivos, e o pincel de cada chave dela
    private static ResourceDictionary? _layer;
    private static readonly Dictionary<object, LiveBrush> _live = [];

    // O HandyControl só troca o dicionário dele com este campo ligado (ThemeManager.ApplyApplicationTheme). Sem o campo
    // (outra versão do HandyControl), a troca volta a ser a dele, inteira: mais lenta, mas certa
    private static readonly FieldInfo? HandyInitialized =
      typeof(ThemeManager).GetField("_applicationInitialized", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>Na hora e sem animação (a partida do app, a foto das telas).</summary>
    public static void Apply(bool dark)
    {
      var first = _layer is null;
      SetHandyControlTheme(dark, swapDictionaries: first || HandyInitialized is null);

      var targets = Targets(dark);
      if (first)
        Install(targets);
      else
      {
        foreach (var (key, target) in targets)
        {
          if (_live.TryGetValue(key, out var live))
            live.Repaint(target);
        }
      }

      Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>A camada de pincéis do tema que está valendo (pra teste e diagnóstico).</summary>
    public static ResourceDictionary? Layer => _layer;

    /// <summary>
    /// O ThemeManager no tema novo. Na partida o HandyControl troca o dicionário dele (as cores soltas ficam certas); nas
    /// trocas seguintes, o campo desligado por um instante: o ThemeManager muda e avisa, mas o dicionário fica.
    /// </summary>
    private static void SetHandyControlTheme(bool dark, bool swapDictionaries)
    {
      var manager = ThemeManager.Current;
      var theme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
      if (swapDictionaries || HandyInitialized!.GetValue(manager) is not true)
      {
        manager.ApplicationTheme = theme;
        return;
      }

      HandyInitialized.SetValue(manager, false);
      try
      {
        manager.ApplicationTheme = theme;
      }
      finally
      {
        HandyInitialized.SetValue(manager, true);
      }
    }

    /// <summary>A camada, uma vez: um pincel vivo por chave, no fim dos mesclados (o último mesclado ganha).</summary>
    private static void Install(Dictionary<object, Brush> targets)
    {
      var layer = new ResourceDictionary();
      foreach (var (key, target) in targets)
      {
        if (LiveBrush.Create(target) is { } live)
        {
          _live[key] = live;
          layer[key] = live.Brush;
        }
      }
      Application.Current.Resources.MergedDictionaries.Add(layer);
      _layer = layer;
    }

    /// <summary>
    /// Os pincéis do tema: os do dicionário do HandyControl daquele tema e, por cima, os da Uzor. A chave que o app põe na
    /// raiz dos recursos fica de fora (a raiz ganha dos mesclados: o destaque do HandyControl, PrimaryBrush e cia, mora
    /// lá).
    /// </summary>
    private static Dictionary<object, Brush> Targets(bool dark)
    {
      var targets = new Dictionary<object, Brush>();
      var root = Application.Current.Resources;
      // Só as chaves da própria raiz: o Contains do ResourceDictionary procura nos mesclados também
      var rootKeys = root.Keys.Cast<object>().ToHashSet();
      if (root.MergedDictionaries.OfType<ThemeResources>().FirstOrDefault() is { } handy)
      {
        foreach (var (key, value) in Entries(handy.GetThemeDictionary(dark ? "Dark" : "Light")))
        {
          // Só o pincel de cor fixa. Os degradês do HandyControl (PrimaryBrush, DangerBrush, SuccessBrush...) e o pincel
          // com a cor por DynamicResource pegam a cor de uma Color do tema: lidos fora de tela vêm transparentes, e
          // copiados assim apagavam o botão marcado do OptionGroup (Windfinder 1.3.1). Esses ficam com o HandyControl
          if (value is SolidColorBrush solid && solid.ReadLocalValue(SolidColorBrush.ColorProperty) is not Expression
            && !rootKeys.Contains(key))
            targets[key] = solid;
        }
      }

      void SetBrush(string key, Color color, double opacity = 1)
      {
        var brush = new SolidColorBrush(color) { Opacity = opacity };
        brush.Freeze();
        targets[key] = brush;
      }

      // No escuro clareia (branco), no claro escurece (preto); a barra de rolagem é cinza nos dois, do lado oposto
      var ink = dark ? Colors.White : Colors.Black;
      SetBrush("Hover.RowBrush", ink, dark ? 0.06 : 0.05);
      SetBrush("Pressed.RowBrush", ink, dark ? 0.13 : 0.09);
      SetBrush("Grid.LineBrush", ink, dark ? 0.05 : 0.07);
      SetBrush("MutedTextBrush", dark ? Color.FromRgb(0x85, 0x85, 0x8C) : Color.FromRgb(0x78, 0x78, 0x80));
      SetBrush("ScrollBar.ThumbBrush", dark ? Color.FromRgb(0x4A, 0x4A, 0x4A) : Color.FromRgb(0xC2, 0xC2, 0xC2));
      SetBrush("ScrollBar.ThumbHoverBrush", dark ? Color.FromRgb(0x6F, 0x6F, 0x6F) : Color.FromRgb(0xA0, 0xA0, 0xA0));
      SetBrush("ScrollBar.ThumbPressedBrush", dark ? Color.FromRgb(0x9A, 0x9A, 0x9A) : Color.FromRgb(0x80, 0x80, 0x80));

      // As camadas: no escuro a tela clareia o fundo; no claro a tela é quase branca e cartão, botão e divisória escurecem
      SetBrush("Window.FallbackBrush", dark ? Color.FromRgb(0x1C, 0x1C, 0x1C) : Color.FromRgb(0xF3, 0xF3, 0xF3));
      SetBrush("Frame.BackgroundBrush", dark ? Color.FromRgb(0x14, 0x14, 0x14) : Color.FromRgb(0xEE, 0xEF, 0xF1));
      SetBrush("Pane.BackgroundBrush", Colors.White, dark ? 0.07 : 0.7);
      SetBrush("Card.BackgroundBrush", ink, dark ? 0.078 : 0.045);
      SetBrush("Divider.Brush", ink, dark ? 0.078 : 0.08);
      SetBrush("Grid.DividerBrush", ink, dark ? 0.05 : 0.06);
      SetBrush("Progress.TrackBrush", ink, dark ? 0.1 : 0.1);
      SetBrush("Button.BackgroundBrush", ink, dark ? 0.09 : 0.06);
      SetBrush("Button.HoverBrush", ink, dark ? 0.14 : 0.1);
      SetBrush("Button.PressedBrush", ink, dark ? 0.055 : 0.035);
      SetBrush("Nav.SelectedBrush", ink, dark ? 0.10 : 0.06);
      SetBrush("Switch.TrackBrush", ink, dark ? 0.2 : 0.22);

      // O que flutua (menu, calendário, diálogo, gaveta, tooltip): o cinza do menu do Windows 11 em cada tema
      SetBrush("Flyout.BackgroundBrush", dark ? Color.FromRgb(0x2B, 0x2B, 0x2E) : Color.FromRgb(0xFB, 0xFB, 0xFC));
      // O diálogo no escuro um degrau abaixo dos menus: no 2B2B2E os cartões e as grades (2D2D30) sumiam dentro dele
      SetBrush("Dialog.BackgroundBrush", dark ? Color.FromRgb(0x1F, 0x1F, 0x22) : Color.FromRgb(0xFB, 0xFB, 0xFC));
      SetBrush("Flyout.BorderBrush", ink, dark ? 0.12 : 0.1);
      SetBrush("Dialog.FooterBrush", Colors.Black, dark ? 0.2 : 0.035);
      SetBrush("Overlay.VeilBrush", dark ? Colors.Black : Colors.White, dark ? 0.35 : 0.55);
      SetBrush("CheckBox.BorderBrush", ink, dark ? 0.45 : 0.42);
      SetBrush("CheckBox.HoverBorderBrush", ink, dark ? 0.7 : 0.65);

      // Texto dos chips: a cor cheia some no fundo branco, então o claro usa um tom mais fechado
      SetBrush("Success.TextBrush", dark ? Color.FromRgb(0x3D, 0xCB, 0x5E) : Color.FromRgb(0x1E, 0x8C, 0x3A));
      SetBrush("Accent.TextBrush", dark ? Color.FromRgb(0x6F, 0xA3, 0xDC) : Color.FromRgb(0x2F, 0x6A, 0xAA));
      SetBrush("Info.TextBrush", dark ? Color.FromRgb(0x3B, 0xC9, 0xDB) : Color.FromRgb(0x00, 0x83, 0x8F));
      // O âmbar do claro é o vivo (06/11/2026): os escuros (9A6B00, A65E00, B87300) liam como marrom/oliva no cinza dos cartões ("cor feia"); D97706 é o âmbar 600, mais claro e sem tom de terra
      SetBrush("Warning.TextBrush", dark ? Color.FromRgb(0xE9, 0xAF, 0x20) : Color.FromRgb(0xD9, 0x77, 0x06));
      SetBrush("Danger.TextBrush", dark ? Color.FromRgb(0xE5, 0x52, 0x5E) : Color.FromRgb(0xC2, 0x29, 0x35));
      SetBrush("Orange.TextBrush", dark ? Color.FromRgb(0xFF, 0x8A, 0x5C) : Color.FromRgb(0xC2, 0x4A, 0x10));
      SetBrush("Purple.TextBrush", dark ? Color.FromRgb(0xC7, 0x7D, 0xDB) : Color.FromRgb(0x7B, 0x2A, 0x92));

      return targets;
    }

    // As entradas de um dicionário com as dos mesclados dele; a de cima ganha (o último mesclado, e o próprio por fim)
    private static IEnumerable<(object Key, object Value)> Entries(ResourceDictionary dictionary)
    {
      var entries = new Dictionary<object, object>();
      void Collect(ResourceDictionary d)
      {
        foreach (var merged in d.MergedDictionaries)
          Collect(merged);
        foreach (var key in d.Keys)
          entries[key] = d[key];
      }
      Collect(dictionary);
      return entries.Select(e => (e.Key, e.Value));
    }

    /// <summary>
    /// Um pincel do tema que é sempre o mesmo objeto: a cor e a opacidade vêm por binding de uma <see cref="Paint"/>, e
    /// trocar o tema é trocar a tinta.
    /// </summary>
    private sealed class LiveBrush
    {
      private readonly Paint _paint;

      private LiveBrush(SolidColorBrush brush, Paint paint)
      {
        Brush = brush;
        _paint = paint;
      }

      public SolidColorBrush Brush { get; }

      public static LiveBrush? Create(Brush model)
      {
        if (model is not SolidColorBrush solid)
          return null;
        var paint = new Paint(solid.Color, solid.Opacity);
        var brush = new SolidColorBrush();
        Bind(brush, SolidColorBrush.ColorProperty, paint, nameof(Paint.Color));
        Bind(brush, System.Windows.Media.Brush.OpacityProperty, paint, nameof(Paint.Opacity));
        return new LiveBrush(brush, paint);
      }

      /// <summary>A cor do pincel do outro tema.</summary>
      public void Repaint(Brush target)
      {
        if (target is not SolidColorBrush solid)
          return;
        _paint.Color = solid.Color;
        _paint.Opacity = solid.Opacity;
      }

      private static void Bind(DependencyObject target, DependencyProperty property, Paint source, string path) =>
        BindingOperations.SetBinding(target, property, new Binding(path) { Source = source, Mode = BindingMode.OneWay });
    }

    /// <summary>A tinta de um pincel vivo: cor e opacidade que avisam quando mudam.</summary>
    private sealed class Paint(Color color, double opacity) : INotifyPropertyChanged
    {
      private Color _color = color;
      private double _opacity = opacity;

      public Color Color
      {
        get => _color;
        set
        {
          if (_color == value)
            return;
          _color = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
        }
      }

      public double Opacity
      {
        get => _opacity;
        set
        {
          if (_opacity.Equals(value))
            return;
          _opacity = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Opacity)));
        }
      }

      public event PropertyChangedEventHandler? PropertyChanged;
    }
  }
}
