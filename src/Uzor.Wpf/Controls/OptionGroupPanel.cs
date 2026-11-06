using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// Os segmentos do OptionGroup lado a lado, da mesma largura, com 1 px de tela entre eles. O UniformGrid dividia a
  /// largura em frações (256 em 6 = 42,67) e o respiro saía 1 num par e 2 no outro, ou borrado: aqui a conta é em
  /// pixel de tela inteiro, e a sobra vai espalhada (uns ficam com 42, outros com 43).
  /// </summary>
  public class OptionGroupPanel : Panel
  {
    private const double Gap = 1;

    protected override Size MeasureOverride(Size availableSize)
    {
      var count = InternalChildren.Count;
      if (count == 0)
        return default;

      var each = double.IsInfinity(availableSize.Width) ? availableSize.Width : Math.Max(0, (availableSize.Width - Gap * (count - 1)) / count);
      double width = 0, height = 0;
      foreach (UIElement child in InternalChildren)
      {
        child.Measure(new Size(each, availableSize.Height));
        width = Math.Max(width, child.DesiredSize.Width);
        height = Math.Max(height, child.DesiredSize.Height);
      }
      return new Size(width * count + Gap * (count - 1), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      var count = InternalChildren.Count;
      if (count == 0)
        return finalSize;

      var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
      var gap = Math.Max(1, Math.Round(Gap * scale));
      var total = Math.Round(finalSize.Width * scale);
      for (var i = 0; i < count; i++)
      {
        var left = Math.Round(i * (total + gap) / count);
        var right = Math.Round((i + 1) * (total + gap) / count) - gap;
        InternalChildren[i].Arrange(new Rect(left / scale, 0, Math.Max(0, right - left) / scale, finalSize.Height));
      }
      return finalSize;
    }
  }
}
