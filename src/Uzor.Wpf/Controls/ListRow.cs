using System.Windows;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// A linha da lista selecionável (PlainListBox) que virou editor: o nome e a cor ali mesmo, no lugar do texto. Ligada,
  /// a linha fica no fundo da escolhida, sem a barrinha azul (que ficava solta no meio do editor) e sem a mãozinha.
  /// <para>Na tela: <c>&lt;Style BasedOn="{StaticResource Uzor.PlainListItem}"&gt;</c> com o Setter
  /// <c>uzor:ListRow.IsEditing="{Binding IsEditing}"</c>.</para>
  /// </summary>
  public static class ListRow
  {
    public static readonly DependencyProperty IsEditingProperty = DependencyProperty.RegisterAttached(
      "IsEditing", typeof(bool), typeof(ListRow), new PropertyMetadata(false));

    public static bool GetIsEditing(DependencyObject element) => (bool)element.GetValue(IsEditingProperty);

    public static void SetIsEditing(DependencyObject element, bool value) => element.SetValue(IsEditingProperty, value);
  }
}
