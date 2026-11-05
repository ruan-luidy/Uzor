using System.Windows;
using System.Windows.Controls;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// A área das telas que não remonta: cada página (o view model, ou a própria tela) ganha a tela dela uma vez, pelo
  /// DataTemplate, e depois só troca de visibilidade. Com o ContentControl a tela inteira era refeita a cada troca
  /// (tabela, linhas, animação de entrada) e piscava.
  /// <para>As páginas que <see cref="KeepAlive"/> recusa (um editor, um detalhe aberto) saem da memória quando deixam de
  /// ser a atual. Sem <see cref="KeepAlive"/>, todas ficam.</para>
  /// <para>Uso: <c>&lt;controls:PageHost Page="{Binding Current}" /&gt;</c>; a entrada animada é com o <see cref="Shown"/>
  /// (<c>Motion.SlideIn(e.View, 24)</c>).</para>
  /// </summary>
  public class PageHost : Grid
  {
    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(
      nameof(Page), typeof(object), typeof(PageHost), new PropertyMetadata(null, (d, e) => ((PageHost)d).Show(e.NewValue)));

    private readonly Dictionary<object, ContentControl> _views = new(ReferenceEqualityComparer.Instance);

    /// <summary>A página aberta.</summary>
    public object? Page
    {
      get => GetValue(PageProperty);
      set => SetValue(PageProperty, value);
    }

    /// <summary>Quais páginas continuam montadas depois que saem da tela. Nulo = todas.</summary>
    public Predicate<object>? KeepAlive { get; set; }

    /// <summary>A tela da página nova apareceu (pra animar a entrada).</summary>
    public event EventHandler<PageShownEventArgs>? Shown;

    /// <summary>A tela montada de uma página (nula se ela não está aqui).</summary>
    public FrameworkElement? ViewOf(object page) => _views.GetValueOrDefault(page);

    private void Show(object? page)
    {
      ContentControl? shown = null;
      if (page is not null)
      {
        if (!_views.TryGetValue(page, out shown))
        {
          shown = new ContentControl { Content = page, Focusable = false };
          _views[page] = shown;
          Children.Add(shown);
        }

        shown.Visibility = Visibility.Visible;
      }

      foreach (var (other, view) in _views.ToList())
      {
        if (ReferenceEquals(other, page))
          continue;
        if (KeepAlive?.Invoke(other) ?? true)
        {
          view.Visibility = Visibility.Collapsed;
        }
        else
        {
          Children.Remove(view);
          _views.Remove(other);
        }
      }

      if (page is not null && shown is not null)
        Shown?.Invoke(this, new PageShownEventArgs(page, shown));
    }
  }

  /// <summary>A página que entrou e a tela dela.</summary>
  public sealed class PageShownEventArgs(object page, FrameworkElement view) : EventArgs
  {
    public object Page { get; } = page;

    public FrameworkElement View { get; } = view;
  }
}
