using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Uzor.Wpf.Controls;

namespace Uzor.Wpf.Services
{
  /// <summary>
  /// Toasts (o UpdateToastService do rnzNegativeObserver, pelo Windfinder) no lugar do Growl do HandyControl:
  /// empilhados no topo, no centro da janela ativa, numa camada de adorno - por cima de tudo, até de diálogo.
  /// No máximo quatro de cada vez: o mais antigo sai pra dar lugar.
  /// </summary>
  public static class ToastService
  {
    private const int MaxToasts = 4;

    private static readonly TimeSpan Short = TimeSpan.FromSeconds(3);

    private static AdornerStack? _stack;
    private static AdornerLayer? _layer;

    public static void Info(string message, string? title = null) => Show(DialogKind.Info, title ?? Texts.ToastInfo, message, Short);

    public static void Success(string message, string? title = null) => Show(DialogKind.Success, title ?? Texts.ToastSuccess, message, Short);

    public static void Warning(string message, string? title = null) => Show(DialogKind.Warning, title ?? Texts.ToastWarning, message, TimeSpan.FromSeconds(4));

    public static void Error(string message, string? title = null) => Show(DialogKind.Error, title ?? Texts.ToastError, message, TimeSpan.FromSeconds(5));

    /// <param name="lifetime">Quanto fica; null = até fechar no X.</param>
    public static void Show(DialogKind kind, string title, string message, TimeSpan? lifetime, string? actionText = null, Action? action = null)
    {
      var app = Application.Current;
      if (app is null)
        return;

      app.Dispatcher.InvokeAsync(() =>
      {
        var window = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow;
        if (window is not { IsLoaded: true, IsVisible: true } || InWindow(window) is not { } stack)
          return;

        var toast = new Toast();
        toast.Closed += (_, _) => stack.Remove(toast);
        stack.Add(toast);
        toast.Show(kind, title, message, lifetime, actionText, action);
      });
    }

    /// <summary>
    /// Pega a ÚLTIMA camada de adorno da árvore: com um diálogo aberto, é a dele, e o toast fica por cima do fundo
    /// escurecido.
    /// </summary>
    private static AdornerStack? InWindow(Window window)
    {
      var decorators = new List<AdornerDecorator>();
      Collect(window, decorators);
      var decorator = decorators.LastOrDefault(d => d.AdornerLayer is not null && d.Child is not null);
      if (decorator?.AdornerLayer is not { } layer || decorator.Child is not { } host)
        return null;

      if (_stack is null || !ReferenceEquals(layer, _layer))
      {
        if (_stack is not null)
          _layer?.Remove(_stack);

        _stack = new AdornerStack(host);
        _layer = layer;
        layer.Add(_stack);
      }

      return _stack;
    }

    private static void Collect(DependencyObject parent, List<AdornerDecorator> found)
    {
      var count = parent is Visual ? VisualTreeHelper.GetChildrenCount(parent) : 0;
      for (var i = 0; i < count; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is AdornerDecorator decorator)
          found.Add(decorator);
        Collect(child, found);
      }
    }

    /// <summary>Centro, logo abaixo da barra de título; só o cartão recebe clique.</summary>
    private sealed class AdornerStack : Adorner
    {
      private readonly StackPanel _panel = new() { HorizontalAlignment = HorizontalAlignment.Center };

      public AdornerStack(UIElement adorned)
        : base(adorned)
      {
        AddVisualChild(_panel);
        AddLogicalChild(_panel);
      }

      public void Add(Toast toast)
      {
        while (_panel.Children.Count >= MaxToasts)
          _panel.Children.RemoveAt(0);

        _panel.Children.Add(toast);
        InvalidateMeasure();
      }

      public void Remove(Toast toast) => _panel.Children.Remove(toast);

      protected override int VisualChildrenCount => 1;

      protected override Visual GetVisualChild(int index) => _panel;

      protected override IEnumerator LogicalChildren
      {
        get { yield return _panel; }
      }

      protected override Size MeasureOverride(Size constraint)
      {
        _panel.Measure(constraint);
        return constraint;
      }

      protected override Size ArrangeOverride(Size finalSize)
      {
        // Em pixel inteiro: no meio de um pixel o texto do toast sai borrado
        var width = _panel.DesiredSize.Width;
        _panel.Arrange(new Rect(Math.Round((finalSize.Width - width) / 2), 8, width, _panel.DesiredSize.Height));
        return finalSize;
      }

      /// <summary>Fora dos cartões o clique passa pra janela embaixo.</summary>
      protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
      {
        foreach (UIElement child in _panel.Children)
        {
          var origin = child.TranslatePoint(new Point(0, 0), this);
          if (new Rect(origin, child.RenderSize).Contains(hitTestParameters.HitPoint))
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        return null;
      }
    }
  }
}
