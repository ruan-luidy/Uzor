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

    /// <summary>
    /// Tira as linhas de baixo das barras: da vertical e, desde 27/11/2026, da horizontal ("o palito vertical já joguei
    /// pro lado e o horizontal ainda não": na grade que rola pro lado, a barra de baixo ficava em cima da última linha).
    /// A linha da barra de baixo também é Auto e some sem rolagem pro lado, então a grade estreita não perde altura.
    /// Devolve se mexeu (template sem as partes, ou já certo: não mexe).
    /// </summary>
    public static bool Apply(DataGrid grid)
    {
      grid.ApplyTemplate();
      var viewer = Find<ScrollViewer>(grid);
      if (viewer is null)
        return false;
      viewer.ApplyTemplate();
      if (viewer.Template?.FindName("PART_ScrollContentPresenter", viewer) is not ScrollContentPresenter rows
        || VisualTreeHelper.GetParent(rows) is not Grid host)
        return false;

      var changed = false;
      if (viewer.Template.FindName("PART_VerticalScrollBar", viewer) is ScrollBar vertical
        && ReferenceEquals(VisualTreeHelper.GetParent(vertical), host))
      {
        var first = Grid.GetColumn(rows);
        var barColumn = Grid.GetColumn(vertical);
        if (Overlap(Grid.GetRow(rows), Grid.GetRowSpan(rows), Grid.GetRow(vertical), Grid.GetRowSpan(vertical))
          && first < barColumn && barColumn < first + Grid.GetColumnSpan(rows))
        {
          Grid.SetColumnSpan(rows, barColumn - first);
          changed = true;
        }
      }

      // No template do HandyControl a barra de baixo mora na MESMA linha das linhas, colada embaixo (por cima da última).
      // Ela desce pra uma linha Auto logo abaixo, que a grade ganha aqui; a vertical fica onde está e para em cima dela.
      if (viewer.Template.FindName("PART_HorizontalScrollBar", viewer) is ScrollBar horizontal
        && ReferenceEquals(VisualTreeHelper.GetParent(horizontal), host)
        && host.RowDefinitions.Count > 0
        && Overlap(Grid.GetRow(rows), Grid.GetRowSpan(rows), Grid.GetRow(horizontal), Grid.GetRowSpan(horizontal))
        && Overlap(Grid.GetColumn(rows), Grid.GetColumnSpan(rows), Grid.GetColumn(horizontal), Grid.GetColumnSpan(horizontal))
        && Grid.GetRow(rows) + Grid.GetRowSpan(rows) == host.RowDefinitions.Count)
      {
        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(horizontal, host.RowDefinitions.Count - 1);
        Grid.SetRowSpan(horizontal, 1);
        changed = true;
      }
      return changed;
    }

    private static bool Overlap(int start, int span, int otherStart, int otherSpan)
      => start < otherStart + otherSpan && otherStart < start + span;

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
