using System.Windows;
using System.Windows.Controls;
using HandyControl.Tools;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// Os segmentos do OptionGroup no jeito dos botões (sem contorno, fundo translúcido, o respiro de 1 px vem do OptionGroupPanel).
  /// O HandyControl escolhe o estilo de cada segmento (primeiro, meio, último) nos dicionários internos dele, que não
  /// dá pra sobrescrever pela chave: aqui pega o dele e põe o nosso por cima.
  /// </summary>
  public class OptionGroupItemStyleSelector : StyleSelector
  {
    private static readonly ButtonGroupItemStyleSelector HandyControl = new();
    private static readonly Dictionary<Style, Style> Derived = [];

    public override Style? SelectStyle(object item, DependencyObject container)
    {
      if (HandyControl.SelectStyle(item, container) is not { } baseStyle)
        return null;
      if (Derived.TryGetValue(baseStyle, out var style))
        return style;

      style = new Style(baseStyle.TargetType, baseStyle);
      style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
      style.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("Button.BackgroundBrush")));
      // O respiro entre eles é do OptionGroupPanel, em pixel inteiro
      style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0)));
      // Sem o pontilhado do foco do WPF em volta do segmento escolhido (o marcado já é o azul cheio)
      style.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
      var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
      hover.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("Button.HoverBrush")));
      style.Triggers.Add(hover);
      style.Seal();
      Derived[baseStyle] = style;
      return style;
    }
  }
}
