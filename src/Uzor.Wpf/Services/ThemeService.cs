using System.Windows;
using System.Windows.Media;
using HandyControl.Themes;
using Uzor.Wpf.Animation;

namespace Uzor.Wpf.Services
{
  /// <summary>
  /// Claro ou escuro: o ThemeManager do HandyControl troca as cores dele na hora, e os pincéis da Uzor (as camadas, o
  /// hover, a grade, a barra de rolagem, o que flutua, os tons do texto) ganham a versão do tema por cima, no
  /// Application.Resources. Quem usa eles pega por DynamicResource; os valores dos XAML são os do escuro.
  /// <para>Guardar a escolha é do app: ele chama <see cref="Apply"/> na partida e <see cref="Switch"/> quando a pessoa
  /// troca.</para>
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

    /// <summary>Na hora e sem animação (a partida do app, a foto das telas).</summary>
    public static void Apply(bool dark)
    {
      ThemeManager.Current.ApplicationTheme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

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
      SetBrush("Flyout.BorderBrush", ink, dark ? 0.12 : 0.1);
      SetBrush("Dialog.FooterBrush", Colors.Black, dark ? 0.2 : 0.035);
      SetBrush("CheckBox.BorderBrush", ink, dark ? 0.45 : 0.42);
      SetBrush("CheckBox.HoverBorderBrush", ink, dark ? 0.7 : 0.65);

      // Texto dos chips: a cor cheia some no fundo branco, então o claro usa um tom mais fechado
      SetBrush("Success.TextBrush", dark ? Color.FromRgb(0x3D, 0xCB, 0x5E) : Color.FromRgb(0x1E, 0x8C, 0x3A));
      SetBrush("Accent.TextBrush", dark ? Color.FromRgb(0x6F, 0xA3, 0xDC) : Color.FromRgb(0x2F, 0x6A, 0xAA));
      SetBrush("Info.TextBrush", dark ? Color.FromRgb(0x3B, 0xC9, 0xDB) : Color.FromRgb(0x00, 0x83, 0x8F));
      SetBrush("Warning.TextBrush", dark ? Color.FromRgb(0xE9, 0xAF, 0x20) : Color.FromRgb(0x9A, 0x6B, 0x00));
      SetBrush("Danger.TextBrush", dark ? Color.FromRgb(0xE5, 0x52, 0x5E) : Color.FromRgb(0xC2, 0x29, 0x35));
      Changed?.Invoke(null, EventArgs.Empty);
    }

    private static void SetBrush(string key, Color color, double opacity = 1)
    {
      var brush = new SolidColorBrush(color) { Opacity = opacity };
      brush.Freeze();
      Application.Current.Resources[key] = brush;
    }
  }
}
