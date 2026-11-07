using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// DataGrid agrupado que nasce escondido (só aparece quando chegam linhas) mede as colunas sem espaço nenhum, aperta
  /// todas até o MinWidth (20) e não devolve a largura quando aparece: ficava tudo espremido. Ligado, a tabela reaplica a
  /// largura de cada coluna sempre que fica visível ou muda de tamanho com alguma coluna espremida.
  ///
  /// E quando as linhas chegam (07/11/2026, o Estoque do Depósito F): uma coluna Auto que cresce com o conteúdo tira a
  /// largura das colunas À DIREITA dela, não da Descrição em * que fica à esquerda. A última coluna sem mínimo saía com
  /// um ponto só, e as outras paravam no mínimo cortando o texto, com a Descrição sobrando. Aqui a coluna Auto abaixo do
  /// que o conteúdo pede conta como espremida, e a reaplicação faz a de estrela ceder. Uma vez por lote: a grade recebe
  /// milhares de linhas uma a uma.
  /// </summary>
  public static class ColumnWidthsFix
  {
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
      "IsEnabled", typeof(bool), typeof(ColumnWidthsFix), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    // O que liga a grade às linhas (pra desligar) e a reaplicação já agendada (pra não agendar de novo)
    private static readonly DependencyProperty ItemsHandlerProperty = DependencyProperty.RegisterAttached(
      "ItemsHandler", typeof(NotifyCollectionChangedEventHandler), typeof(ColumnWidthsFix));

    private static readonly DependencyProperty IsScheduledProperty = DependencyProperty.RegisterAttached(
      "IsScheduled", typeof(bool), typeof(ColumnWidthsFix));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not DataGrid grid)
        return;
      grid.IsVisibleChanged -= OnVisibleChanged;
      grid.SizeChanged -= OnSizeChanged;
      if (grid.GetValue(ItemsHandlerProperty) is NotifyCollectionChangedEventHandler antigo)
        ((INotifyCollectionChanged)grid.Items).CollectionChanged -= antigo;
      grid.ClearValue(ItemsHandlerProperty);
      if (e.NewValue is not true)
        return;
      grid.IsVisibleChanged += OnVisibleChanged;
      grid.SizeChanged += OnSizeChanged;
      NotifyCollectionChangedEventHandler handler = (_, _) => Schedule(grid);
      ((INotifyCollectionChanged)grid.Items).CollectionChanged += handler;
      grid.SetValue(ItemsHandlerProperty, handler);
    }

    private static void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is true)
        Schedule((DataGrid)sender);
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e) => Schedule((DataGrid)sender);

    // Depois do layout em andamento: aí a tabela já tem o tamanho de verdade e as células novas já foram medidas
    private static void Schedule(DataGrid grid)
    {
      if ((bool)grid.GetValue(IsScheduledProperty))
        return;
      grid.SetValue(IsScheduledProperty, true);
      grid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
      {
        grid.SetValue(IsScheduledProperty, false);
        if (!grid.IsVisible || !grid.Columns.Any(Squeezed))
          return;
        foreach (var column in grid.Columns)
        {
          var width = column.Width;
          column.Width = new DataGridLength(0);
          column.Width = width;
        }
      });
    }

    // Coluna de largura fixa abaixo do que pediu, de estrela parada no mínimo, ou Auto abaixo do que o conteúdo pede
    private static bool Squeezed(DataGridColumn column) =>
      column.Visibility == Visibility.Visible
      && (column.Width.IsAbsolute ? column.ActualWidth < column.Width.Value - 0.5
        : column.Width.IsStar ? column.ActualWidth <= column.MinWidth
        : column.Width.IsAuto && column.ActualWidth < column.Width.DesiredValue - 0.5);
  }
}
