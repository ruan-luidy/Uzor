using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// A barra de rolagem vertical da grade na sua própria faixa, e não por cima da última coluna (09/11/2026: "tem como
  /// deixar essa barrinha sair de cima do número ali?"). No template do HandyControl as linhas (o
  /// <c>PART_ScrollContentPresenter</c>) ocupam as três colunas da grade de dentro, e a barra mora na terceira: o número
  /// alinhado à direita da última coluna ficava embaixo do polegar. Aqui as linhas param antes da coluna da barra. A
  /// coluna é Auto e a barra some quando não há o que rolar, então a grade sem rolagem não perde nada; com rolagem, as
  /// colunas em estrela se ajustam à largura que sobra, e o cabeçalho fica com o vão em cima da barra, como no Windows.
  ///
  /// Liga uma vez na partida: <c>GridScrollGutter.Install();</c>. Vale pra toda DataGrid do app.
  /// </summary>
  public static class GridScrollGutter
  {
    private static bool _installed;

    public static void Install()
    {
      if (_installed)
        return;
      _installed = true;
      EventManager.RegisterClassHandler(typeof(DataGrid), FrameworkElement.LoadedEvent, new RoutedEventHandler((s, _) => Apply((DataGrid)s)));
    }

    /// <summary>Tira as linhas de baixo da barra vertical. Devolve se mexeu (template sem as partes, ou já certo: não mexe).</summary>
    public static bool Apply(DataGrid grid)
    {
      grid.ApplyTemplate();
      var viewer = Find<ScrollViewer>(grid);
      if (viewer is null)
        return false;
      viewer.ApplyTemplate();
      if (viewer.Template?.FindName("PART_ScrollContentPresenter", viewer) is not ScrollContentPresenter rows
        || viewer.Template.FindName("PART_VerticalScrollBar", viewer) is not ScrollBar bar
        || VisualTreeHelper.GetParent(rows) is not Grid host
        || !ReferenceEquals(VisualTreeHelper.GetParent(bar), host))
        return false;

      var first = Grid.GetColumn(rows);
      var barColumn = Grid.GetColumn(bar);
      var sameRow = Grid.GetRow(rows) < Grid.GetRow(bar) + Grid.GetRowSpan(bar)
        && Grid.GetRow(bar) < Grid.GetRow(rows) + Grid.GetRowSpan(rows);
      if (!sameRow || barColumn <= first || first + Grid.GetColumnSpan(rows) <= barColumn)
        return false;

      Grid.SetColumnSpan(rows, barColumn - first);
      return true;
    }

    private static T? Find<T>(DependencyObject root) where T : DependencyObject
    {
      for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
      {
        var child = VisualTreeHelper.GetChild(root, i);
        if (child is T found)
          return found;
        if (Find<T>(child) is { } deeper)
          return deeper;
      }
      return null;
    }
  }
}
