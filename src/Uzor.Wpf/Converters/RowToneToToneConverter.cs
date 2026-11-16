using System.Globalization;
using System.Windows.Data;

namespace Uzor.Wpf.Converters
{
  /// <summary>
  /// O tom da linha (<see cref="RowTone"/>: a bolinha, a pílula, o texto colorido) no tom do <see cref="Controls.Chip"/>:
  /// a coluna de status da grade em chip, como no Negativo, sem mudar o tom que a linha já tem. Erro vira o vermelho do
  /// chip; Apagado e Neutro, o cinza.
  ///   &lt;uzor:Chip Content="{Binding Status}" Tone="{Binding Tone, Converter={StaticResource RowToneToTone}}" /&gt;
  /// </summary>
  public sealed class RowToneToToneConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
      RowTone.Success => Tone.Success,
      RowTone.Warning => Tone.Warning,
      RowTone.Error => Tone.Danger,
      _ => Tone.Neutral,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
