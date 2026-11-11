using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// O OptionGroup a partir de uma lista (as origens de um projeto no Otmetka). O ButtonGroup do HandyControl só aceita
  /// botão como item: ele percorre os itens no desenho e quebra com dado. Aqui cada item da lista vira um RadioButton
  /// com o texto e o marcado ligados nele, e a lista mudando (limpa, acrescenta) refaz os botões.
  /// <code>
  /// &lt;hc:ButtonGroup Style="{StaticResource OptionGroup}"
  ///   uzor:OptionItems.LabelPath="Label" uzor:OptionItems.CheckedPath="IsSelected"
  ///   uzor:OptionItems.Source="{Binding Origins}" /&gt;
  /// </code>
  /// Os caminhos vêm antes da fonte: a fonte é que monta os botões.
  /// </summary>
  public static class OptionItems
  {
    public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
      "Source", typeof(IEnumerable), typeof(OptionItems), new PropertyMetadata(null, OnSourceChanged));

    public static IEnumerable? GetSource(DependencyObject element) => (IEnumerable?)element.GetValue(SourceProperty);

    public static void SetSource(DependencyObject element, IEnumerable? value) => element.SetValue(SourceProperty, value);

    /// <summary>A propriedade do item com o texto do botão.</summary>
    public static readonly DependencyProperty LabelPathProperty = DependencyProperty.RegisterAttached(
      "LabelPath", typeof(string), typeof(OptionItems), new PropertyMetadata(string.Empty));

    public static string GetLabelPath(DependencyObject element) => (string)element.GetValue(LabelPathProperty);

    public static void SetLabelPath(DependencyObject element, string value) => element.SetValue(LabelPathProperty, value);

    /// <summary>A propriedade booleana do item que diz se ele está escolhido (nos dois sentidos).</summary>
    public static readonly DependencyProperty CheckedPathProperty = DependencyProperty.RegisterAttached(
      "CheckedPath", typeof(string), typeof(OptionItems), new PropertyMetadata(string.Empty));

    public static string GetCheckedPath(DependencyObject element) => (string)element.GetValue(CheckedPathProperty);

    public static void SetCheckedPath(DependencyObject element, string value) => element.SetValue(CheckedPathProperty, value);

    /// <summary>O ouvinte da lista de agora, pra soltar quando a fonte trocar.</summary>
    private static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached(
      "Handler", typeof(NotifyCollectionChangedEventHandler), typeof(OptionItems), new PropertyMetadata(null));

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not ItemsControl group)
        return;

      if (e.OldValue is INotifyCollectionChanged old && group.GetValue(HandlerProperty) is NotifyCollectionChangedEventHandler handler)
        old.CollectionChanged -= handler;
      group.ClearValue(HandlerProperty);

      if (e.NewValue is INotifyCollectionChanged live)
      {
        NotifyCollectionChangedEventHandler rebuild = (_, _) => Build(group);
        live.CollectionChanged += rebuild;
        group.SetValue(HandlerProperty, rebuild);
      }

      Build(group);
    }

    private static void Build(ItemsControl group)
    {
      group.Items.Clear();
      if (GetSource(group) is not { } source)
        return;

      var label = GetLabelPath(group);
      var check = GetCheckedPath(group);
      foreach (var item in source)
      {
        // Sem GroupName, como os outros OptionGroup: o WPF agrupa pelo pai
        var radio = new RadioButton { DataContext = item };
        if (label.Length > 0)
          radio.SetBinding(ContentControl.ContentProperty, new Binding(label));
        else
          radio.Content = item;
        if (check.Length > 0)
          radio.SetBinding(ToggleButton.IsCheckedProperty, new Binding(check) { Mode = BindingMode.TwoWay });
        group.Items.Add(radio);
      }
    }
  }
}
