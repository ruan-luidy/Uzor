using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// TabControl que GUARDA o conteúdo das abas já abertas (06/11/2026, Detalhes do pedido: "a troca de abas dá uma
  /// travadinha", grade de 4 mil linhas). O do WPF tira o conteúdo da árvore ao sair da aba e monta de novo ao voltar -
  /// a grade inteira refaz modelo, medida e linhas. Aqui cada aba é montada na PRIMEIRA vez que abre e depois só
  /// aparece e some: a abertura da tela continua custando só a aba escolhida, e a volta a uma aba é imediata.
  ///
  /// Usa os estilos Uzor.Tabs e Uzor.SegmentedTabs (eles têm o PART_ItemsHolder ao lado do PART_SelectedContentHost; o
  /// TabControl comum usa o segundo, este usa o primeiro).
  ///   <uzor:KeepAliveTabControl Style="{StaticResource Uzor.Tabs}"> <TabItem Header="Itens">...</TabItem> </uzor:KeepAliveTabControl>
  /// </summary>
  public class KeepAliveTabControl : TabControl
  {
    private Panel? _holder;
    private readonly Dictionary<object, ContentPresenter> _mounted = new();

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();
      _holder = GetTemplateChild("PART_ItemsHolder") as Panel;
      if (_holder is null)
        return;

      // O apresentador padrão sai do caminho: o mesmo conteúdo não pode morar em dois lugares
      if (GetTemplateChild("PART_SelectedContentHost") is ContentPresenter padrao)
      {
        padrao.Content = null;
        padrao.Visibility = Visibility.Collapsed;
      }

      Mostrar();
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
      base.OnSelectionChanged(e);
      Mostrar();
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
      base.OnItemsChanged(e);
      if (_holder is null)
        return;

      // Aba que saiu leva o conteúdo junto
      foreach (var chave in new List<object>(_mounted.Keys))
      {
        if (Items.Contains(chave))
          continue;
        _holder.Children.Remove(_mounted[chave]);
        _mounted.Remove(chave);
      }
      Mostrar();
    }

    /// <summary>Monta a aba escolhida se ainda não foi montada, mostra ela e esconde as outras.</summary>
    private void Mostrar()
    {
      if (_holder is null)
        return;

      var escolhida = SelectedItem;
      if (escolhida is not null && !_mounted.ContainsKey(escolhida))
      {
        var conteudo = escolhida is TabItem aba ? aba.Content : escolhida;
        var apresentador = new ContentPresenter
        {
          Content = conteudo,
          ContentTemplate = escolhida is TabItem t ? t.ContentTemplate : ContentTemplate,
          ContentTemplateSelector = escolhida is TabItem ts ? ts.ContentTemplateSelector : ContentTemplateSelector,
        };
        _mounted[escolhida] = apresentador;
        _holder.Children.Add(apresentador);
      }

      foreach (var (chave, apresentador) in _mounted)
        apresentador.Visibility = ReferenceEquals(chave, escolhida) ? Visibility.Visible : Visibility.Collapsed;
    }
  }
}
