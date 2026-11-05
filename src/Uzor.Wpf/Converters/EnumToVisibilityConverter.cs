using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Uzor.Wpf.Converters
{
  /// <summary>
  /// Aparece quando a propriedade enum vale o ConverterParameter (a aba aberta mostra a seção dela):
  ///   Visibility="{Binding Section, Converter={StaticResource WhenIs}, ConverterParameter={x:Static shared:PackageSection.Items}}"
  /// </summary>
  public sealed class EnumToVisibilityConverter : IValueConverter
  {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
      value is not null && value.Equals(parameter) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
