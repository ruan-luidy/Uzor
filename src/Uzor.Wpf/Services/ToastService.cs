using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using Uzor.Wpf.Controls;

namespace Uzor.Wpf.Services
{
  /// <summary>
  /// Toasts (o UpdateToastService do rnzNegativeObserver, pelo Windfinder) no lugar do Growl do HandyControl:
  /// empilhados no topo, no centro da janela ativa, numa camada de adorno - por cima de tudo, até de diálogo.
  /// No máximo quatro de cada vez: o mais antigo sai pra dar lugar.
  ///
  /// <para>Todo toast devolve um <see cref="ToastHandle"/>: dá pra abrir "processando" e trocar pelo resultado no
  /// MESMO cartão, sem um sair e outro entrar.</para>
  /// </summary>
  public static class ToastService
  {
    private const int MaxToasts = 4;

    internal static readonly TimeSpan Short = TimeSpan.FromSeconds(3);
    internal static readonly TimeSpan Medium = TimeSpan.FromSeconds(4);
    internal static readonly TimeSpan Long = TimeSpan.FromSeconds(5);

    private static AdornerStack? _stack;
    private static AdornerLayer? _layer;

    public static ToastHandle Info(string message, string? title = null) => Show(DialogKind.Info, title ?? Texts.ToastInfo, message, Short);

    public static ToastHandle Success(string message, string? title = null) => Show(DialogKind.Success, title ?? Texts.ToastSuccess, message, Short);

    public static ToastHandle Warning(string message, string? title = null) => Show(DialogKind.Warning, title ?? Texts.ToastWarning, message, Medium);

    public static ToastHandle Error(string message, string? title = null) => Show(DialogKind.Error, title ?? Texts.ToastError, message, Long);

    /// <param name="lifetime">Quanto fica; null = até fechar no X.</param>
    /// <param name="icon">Outro ícone no lugar do padrão do tom.</param>
    public static ToastHandle Show(DialogKind kind, string title, string message, TimeSpan? lifetime, string? actionText = null,
      Action? action = null, PackIconPhosphorIconsKind? icon = null)
    {
      var handle = new ToastHandle();
      handle.Show(kind, title, message, lifetime, actionText, action, icon);
      return handle;
    }

    /// <summary>Algo rodando, sem prazo: troque pelo resultado com o handle (Success, Error...) quando acabar.</summary>
    public static ToastHandle Busy(string title, string message)
    {
      var handle = new ToastHandle();
      handle.Busy(title, message);
      return handle;
    }

    /// <summary>
    /// Roda no cartão do handle, na thread da tela. Se ele já saiu (fechou no X, acabou o tempo, deu lugar a outro),
    /// abre um novo: o resultado de uma operação não pode se perder porque o "processando" foi fechado.
    /// </summary>
    internal static void Run(ToastHandle handle, Action<Toast> configure)
    {
      var app = Application.Current;
      if (app is null)
        return;

      app.Dispatcher.InvokeAsync(() =>
      {
        if (handle.Card is not { IsClosing: false } toast)
        {
          var window = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app.MainWindow;
          if (window is not { IsLoaded: true, IsVisible: true } || InWindow(window) is not { } stack)
            return;

          toast = new Toast();
          var added = toast;
          added.Closed += (_, _) => stack.Remove(added);
          stack.Add(added);
          handle.Card = added;
        }

        configure(toast);
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
        {
          if (_panel.Children[0] is Toast oldest)
            oldest.Detach();
          _panel.Children.RemoveAt(0);
        }

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

  /// <summary>
  /// Um toast já aberto (ou a caminho da tela): troca o conteúdo dele no lugar, ou fecha. Pode ser chamado de qualquer
  /// thread - tudo vai pra fila da tela, na ordem.
  /// </summary>
  public sealed class ToastHandle
  {
    /// <summary>O cartão na tela; só mexido na thread da tela.</summary>
    internal Toast? Card { get; set; }

    public void Show(DialogKind kind, string title, string message, TimeSpan? lifetime, string? actionText = null,
      Action? action = null, PackIconPhosphorIconsKind? icon = null)
      => ToastService.Run(this, t => t.Show(kind, title, message, lifetime, actionText, action, icon));

    public void Busy(string title, string message) => ToastService.Run(this, t => t.ShowBusy(title, message));

    public void Info(string message, string? title = null) => Show(DialogKind.Info, title ?? Texts.ToastInfo, message, ToastService.Short);

    public void Success(string message, string? title = null) => Show(DialogKind.Success, title ?? Texts.ToastSuccess, message, ToastService.Short);

    public void Warning(string message, string? title = null) => Show(DialogKind.Warning, title ?? Texts.ToastWarning, message, ToastService.Medium);

    public void Error(string message, string? title = null) => Show(DialogKind.Error, title ?? Texts.ToastError, message, ToastService.Long);

    public void Hide()
    {
      Application.Current?.Dispatcher.InvokeAsync(() => Card?.Hide());
    }
  }
}
