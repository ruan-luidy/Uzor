using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// As abas de documento (as telas abertas da janela, no estilo <c>Uzor.DocTabs</c>): pílulas como as do
  /// <c>Uzor.Tabs</c>, com o X. O X manda <see cref="Close"/> com a aba no parâmetro, e quem guarda as abas (a janela)
  /// liga o comando e decide como fechar: a aba às vezes leva uma tela viva junto.
  /// <para>A faixa rola de lado com a roda do mouse (<see cref="WheelScrollsProperty"/>), sem barra.</para>
  /// </summary>
  public static class DocTabs
  {
    public static readonly RoutedUICommand Close = new("Fechar", nameof(Close), typeof(DocTabs));

    public static readonly DependencyProperty WheelScrollsProperty = DependencyProperty.RegisterAttached(
      "WheelScrolls", typeof(bool), typeof(DocTabs), new PropertyMetadata(false, OnWheelScrollsChanged));

    public static bool GetWheelScrolls(DependencyObject d) => (bool)d.GetValue(WheelScrollsProperty);

    public static void SetWheelScrolls(DependencyObject d, bool value) => d.SetValue(WheelScrollsProperty, value);

    private static void OnWheelScrollsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not ScrollViewer rolagem)
        return;
      rolagem.PreviewMouseWheel -= OnWheel;
      if ((bool)e.NewValue)
        rolagem.PreviewMouseWheel += OnWheel;
    }

    /// <summary>A roda anda as abas de lado; sem nada pra rolar, a roda segue pra quem estiver embaixo.</summary>
    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
      var rolagem = (ScrollViewer)sender;
      if (rolagem.ScrollableWidth <= 0)
        return;
      rolagem.ScrollToHorizontalOffset(rolagem.HorizontalOffset - e.Delta / 3.0);
      e.Handled = true;
    }
  }
}
