using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Uzor.Wpf.Converters
{
  /// <summary>
  /// "A célula desta coluna é alinhada à direita?", pro cabeçalho ir junto. Número fica à direita na célula
  /// (DataGridCellRightAlign) e o nome da coluna ficava na esquerda: a coluna deixava de se ler como uma coisa só, e
  /// pôr o cabeçalho de número coluna por coluna não pegou (308 colunas de número no Negativo, 37 com o cabeçalho
  /// certo). Olha o ElementStyle da coluna e os BasedOn dele: HorizontalAlignment ou TextAlignment = Right.
  /// <para>Coluna de modelo (DataGridTemplateColumn) não tem ElementStyle: o cabeçalho dela continua por chave
  /// (NumberColumnHeader / DataGridHeaderRightAlign).</para>
  /// </summary>
  public sealed class RightAlignedColumnConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value is DataGridBoundColumn column && IsRightAligned(column.ElementStyle);

    /// <summary>O estilo (ou um BasedOn dele) põe o texto à direita? O setter mais perto vale, como no WPF.</summary>
    public static bool IsRightAligned(Style? style)
    {
      for (var s = style; s is not null; s = s.BasedOn)
      {
        foreach (var setter in s.Setters.OfType<Setter>())
        {
          if (setter.Property == FrameworkElement.HorizontalAlignmentProperty && setter.Value is HorizontalAlignment h)
            return h == HorizontalAlignment.Right;
          if (setter.Property == TextBlock.TextAlignmentProperty && setter.Value is TextAlignment t)
            return t == TextAlignment.Right;
        }
      }
      return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
