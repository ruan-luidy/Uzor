using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// DataGrid agrupado que nasce escondido (só aparece quando chegam linhas) mede as colunas sem espaço nenhum, aperta
  /// todas até o MinWidth (20) e não devolve a largura quando aparece: ficava tudo espremido. Ligado, a tabela reaplica a
  /// largura de cada coluna sempre que fica visível ou muda de tamanho com alguma coluna espremida.
  /// </summary>
  public static class ColumnWidthsFix
  {
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
      "IsEnabled", typeof(bool), typeof(ColumnWidthsFix), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not DataGrid grid)
        return;
      grid.IsVisibleChanged -= OnVisibleChanged;
      grid.SizeChanged -= OnSizeChanged;
      if (e.NewValue is not true)
        return;
      grid.IsVisibleChanged += OnVisibleChanged;
      grid.SizeChanged += OnSizeChanged;
    }

    private static void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is true)
        Schedule((DataGrid)sender);
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e) => Schedule((DataGrid)sender);

    // Depois do layout em andamento: aí a tabela já tem o tamanho de verdade
    private static void Schedule(DataGrid grid) =>
      grid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
      {
        if (!grid.IsVisible || !grid.Columns.Any(Squeezed))
          return;
        foreach (var column in grid.Columns)
        {
          var width = column.Width;
          column.Width = new DataGridLength(0);
          column.Width = width;
        }
      });

    // Coluna de largura fixa abaixo do que pediu, ou de estrela parada no mínimo
    private static bool Squeezed(DataGridColumn column) =>
      column.Visibility == Visibility.Visible
      && (column.Width.IsAbsolute ? column.ActualWidth < column.Width.Value - 0.5 : column.Width.IsStar && column.ActualWidth <= column.MinWidth);
  }
}
