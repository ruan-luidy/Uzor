using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using HandyControl.Controls;
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
      // O texto azul do hover no PRÓPRIO segmento, e não só no apresentador do molde, como faz o HandyControl (11/11/2026,
      // "não fica com a letra azul que nem os outros"): conteúdo montado à mão (o "Todas do produto 5" do Acompanhamento,
      // texto com o número em negrito) é filho lógico do segmento e herda a cor DELE, não a do apresentador. O marcado
      // vem depois e ganha: o escolhido segue branco no azul.
      hover.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension("PrimaryBrush")));
      style.Triggers.Add(hover);
      // ToggleButton marcado (filtro de várias escolhas, 07/11/2026): o HandyControl pinta o fundo de destaque e deixa o
      // texto na cor de sempre - no claro, preto no azul. O RadioButton ele pinta; aqui os dois pegam o texto de
      // destaque do grupo (o branco do OptionGroup).
      if (typeof(ToggleButton).IsAssignableFrom(baseStyle.TargetType))
      {
        var marcado = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        marcado.Setters.Add(new Setter(Control.ForegroundProperty, new Binding
        {
          Path = new PropertyPath(VisualElement.HighlightForegroundProperty),
          RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ButtonGroup), 1),
        }));
        style.Triggers.Add(marcado);
      }
      style.Seal();
      Derived[baseStyle] = style;
      return style;
    }
  }
}
