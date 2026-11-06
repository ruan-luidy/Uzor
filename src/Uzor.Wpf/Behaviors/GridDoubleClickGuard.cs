using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// Duplo clique no CABEÇALHO da grade (a divisa que ajusta a largura da coluna) ou na barra de rolagem não é duplo
  /// clique na linha (06/11/2026: "dou double click pra auto arrumar a largura e ele abre o primeiro item"). O WPF
  /// entrega o MouseDoubleClick pra grade de qualquer lugar dela, e as telas que abrem o item no duplo clique abriam o
  /// selecionado. Aqui ele é marcado como tratado antes de chegar nelas; o ajuste da largura roda na própria divisa,
  /// antes, e continua funcionando.
  ///
  /// Liga uma vez na partida: <c>GridDoubleClickGuard.Install();</c>
  /// </summary>
  public static class GridDoubleClickGuard
  {
    private static bool _installed;

    public static void Install()
    {
      if (_installed)
        return;
      _installed = true;
      EventManager.RegisterClassHandler(typeof(DataGrid), Control.MouseDoubleClickEvent, new MouseButtonEventHandler(OnDoubleClick));
    }

    private static void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (ForaDasLinhas(e.OriginalSource as DependencyObject, sender as DependencyObject))
        e.Handled = true;
    }

    /// <summary>O clique veio do cabeçalho, da divisa de coluna ou da barra de rolagem (e não de uma linha)?</summary>
    internal static bool ForaDasLinhas(DependencyObject? origem, DependencyObject? grade)
    {
      for (var no = origem; no is not null && !ReferenceEquals(no, grade); no = Pai(no))
      {
        if (no is DataGridRow)
          return false;
        if (no is DataGridColumnHeader or DataGridColumnHeadersPresenter or ScrollBar)
          return true;
      }
      return false;
    }

    private static DependencyObject? Pai(DependencyObject no) =>
      no is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(no) : LogicalTreeHelper.GetParent(no);
  }
}
