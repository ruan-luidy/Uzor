using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// Botão direito SELECIONA a linha embaixo do mouse (o do Negativo, 23/11/2026 na Uzor). O WPF não faz isso sozinho:
  /// o menu de contexto de uma grade lê o <c>SelectedItem</c>, e sem um clique esquerdo antes ele vem nulo ou é a linha
  /// de antes - o item do menu age na linha errada, ou não faz nada e sem log. Linha que já está na seleção (várias
  /// marcadas) fica como está, pro menu agir em todas.
  /// <para>Ligado no StandardDataGrid; grade de fora dele liga com <c>uzor:SelectRowOnRightClick.IsEnabled="True"</c>.</para>
  /// </summary>
  public static class SelectRowOnRightClick
  {
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
      "IsEnabled",
      typeof(bool),
      typeof(SelectRowOnRightClick),
      new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not DataGrid grid)
        return;
      grid.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
      if (e.NewValue is true)
        grid.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
    }

    private static void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is not DataGrid grid)
        return;

      var node = e.OriginalSource as DependencyObject;
      while (node is not null and not DataGridRow)
        node = node is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);

      if (node is DataGridRow { IsSelected: false } row)
      {
        grid.SelectedItem = row.Item;
        row.IsSelected = true;
        row.Focus();
      }
    }
  }
}
