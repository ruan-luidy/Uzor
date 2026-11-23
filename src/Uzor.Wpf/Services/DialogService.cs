using System.Windows;
using Uzor.Wpf.Controls;
using HandyControl.Controls;

namespace Uzor.Wpf.Services
{
  /// <summary>
  /// Diálogos (o DialogService do rnzNegativeObserver, pelo Windfinder): o <see cref="AlertDialog"/> ou um
  /// <see cref="IDialogContent"/> por cima da janela, com o fundo escurecido do Dialog do HandyControl - nada de
  /// janela nova nem do MessageBox cru. Tudo assíncrono: quem pergunta espera a resposta com await.
  /// Sem janela na tela ainda (erro na partida), cai no MessageBox do Windows, que não depende de nada.
  /// </summary>
  public static class DialogService
  {
    /// <summary>
    /// Token da janela principal (hc:Dialog.Token): o diálogo abre sempre nela. A janela do app marca o mesmo valor:
    /// <c>hc:Dialog.Token="{x:Static services:DialogService.Token}"</c>.
    /// </summary>
    public const string Token = "Uzor.Main";

    public static Task InfoAsync(string title, string message, string? details = null) =>
      AlertAsync(DialogKind.Info, title, message, details);

    public static Task WarningAsync(string title, string message, string? details = null) =>
      AlertAsync(DialogKind.Warning, title, message, details);

    public static Task ErrorAsync(string title, string message, string? details = null) =>
      AlertAsync(DialogKind.Error, title, message, details);

    /// <summary>Um botão (OK). Termina quando o usuário fecha.</summary>
    public static async Task AlertAsync(DialogKind kind, string title, string message, string? details = null)
    {
      if (Host() is not { } token)
      {
        var icon = kind == DialogKind.Error ? MessageBoxImage.Error : kind == DialogKind.Warning ? MessageBoxImage.Warning : MessageBoxImage.Information;
        System.Windows.MessageBox.Show(details is null ? message : $"{message}\n\n{details}", title, MessageBoxButton.OK, icon);
        return;
      }

      await ShowAlertAsync(new AlertDialog(kind, title, message, Texts.Ok, details: details), token).ConfigureAwait(true);
    }

    /// <summary>Dois botões: true no principal (confirmar), false no secundário ou no Esc.</summary>
    public static async Task<bool> ConfirmAsync(string title, string message, string confirmText, string? cancelText = null, DialogKind kind = DialogKind.Warning)
    {
      if (Host() is not { } token)
        return System.Windows.MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;

      return await ShowAlertAsync(new AlertDialog(kind, title, message, confirmText, cancelText ?? Texts.Cancel), token).ConfigureAwait(true);
    }

    /// <summary>
    /// Abre um diálogo próprio (configurações) por cima da janela do <paramref name="token"/>. O diálogo diz quando
    /// terminou pelo <see cref="IDialogContent.Closed"/>; o resultado volta pelo await.
    /// </summary>
    public static Task<bool> ShowAsync(IDialogContent content, string token = Token)
    {
      ArgumentNullException.ThrowIfNull(content);
      var result = new TaskCompletionSource<bool>();
      Dialog? dialog = null;
      var restore = FitHost(content, token);
      content.Closed += (_, ok) =>
      {
        dialog?.Close();
        restore?.Invoke();
        result.TrySetResult(ok);
      };
      dialog = Dialog.Show(content, token);
      return result.Task;
    }

    private static Task<bool> ShowAlertAsync(AlertDialog content, string token)
    {
      var result = new TaskCompletionSource<bool>();
      Dialog? dialog = null;
      var restore = FitHost(content, token);

      void Finish(bool value)
      {
        dialog?.Close();
        restore?.Invoke();
        result.TrySetResult(value);
      }

      content.PrimaryClicked += (_, _) => Finish(true);
      content.SecondaryClicked += (_, _) => Finish(false);
      dialog = Dialog.Show(content, token);
      return result.Task;
    }

    // O que o diálogo precisa em volta dele dentro da janela: a sombra do cartão e um respiro
    private const double DialogRoom = 48;

    /// <summary>
    /// Janela menor que o diálogo cortava ele (a de login do Handoff tem 400 de largura e a altura do formulário; as
    /// Configurações têm 540 e mais altura que isso, o aviso 420): enquanto ele está aberto, a janela cresce pelos lados e pra cima e
    /// pra baixo, e volta ao tamanho dela no fim. Devolve o que desfaz, ou nulo quando o diálogo já cabia.
    /// </summary>
    private static Action? FitHost(object content, string token)
    {
      if (content is not FrameworkElement { Width: var width } element || double.IsNaN(width) || width <= 0)
        return null;
      var window = Application.Current?.Windows.OfType<System.Windows.Window>()
        .FirstOrDefault(w => w.IsVisible && w.WindowState == WindowState.Normal && HandyControl.Controls.Dialog.GetToken(w) == token);
      if (window is null)
        return null;

      element.Measure(new System.Windows.Size(width, double.PositiveInfinity));
      var chrome = window.Content is FrameworkElement { ActualHeight: > 0 } inner ? window.ActualHeight - inner.ActualHeight : 0;
      var area = SystemParameters.WorkArea;
      var neededWidth = width + DialogRoom;
      var neededHeight = Math.Min(element.DesiredSize.Height + DialogRoom + chrome, area.Height);
      var (oldWidth, oldHeight, oldSizing) = (window.Width, window.Height, window.SizeToContent);
      var (oldActualWidth, oldActualHeight) = (window.ActualWidth, window.ActualHeight);
      var wider = oldActualWidth < neededWidth;
      var taller = oldActualHeight < neededHeight;
      if (!wider && !taller)
        return null;

      // Só segura dentro da tela principal quem já estava nela: em outro monitor (ou fora da tela, nas fotos) só cresce
      var inside = window.Left >= area.Left && window.Left + oldActualWidth <= area.Right && window.Top >= area.Top && window.Top + oldActualHeight <= area.Bottom;
      window.SizeToContent = SizeToContent.Manual;
      if (wider)
      {
        var left = window.Left - (neededWidth - oldActualWidth) / 2;
        window.Width = neededWidth;
        window.Left = inside ? Math.Max(area.Left, Math.Min(left, area.Right - neededWidth)) : left;
      }

      if (taller)
      {
        var top = window.Top - (neededHeight - oldActualHeight) / 2;
        window.Height = neededHeight;
        window.Top = inside ? Math.Max(area.Top, Math.Min(top, area.Bottom - neededHeight)) : top;
      }

      return () =>
      {
        // Volta pelo centro de onde ela estiver agora (pode ter sido arrastada com o diálogo aberto)
        var centerX = window.Left + window.ActualWidth / 2;
        var centerY = window.Top + window.ActualHeight / 2;
        window.Width = oldWidth;
        window.Height = oldHeight;
        window.SizeToContent = oldSizing;
        window.Left = centerX - oldActualWidth / 2;
        window.Top = centerY - oldActualHeight / 2;
      };
    }

    /// <summary>
    /// Se dá pra abrir um diálogo por cima da janela agora. Com o app só no relógio ou numa janelinha sem o token (o
    /// Finder do Windfinder), o diálogo não aparecia e o await nunca terminava: quem tem janela própria usa ela.
    /// </summary>
    public static bool CanShowOverlay() => Host() is not null;

    /// <summary>O token da janela que pode receber o overlay (ele entra na camada de adorno dela).</summary>
    private static string? Host() =>
      Application.Current?.MainWindow is { IsLoaded: true, IsVisible: true } window
        ? HandyControl.Controls.Dialog.GetToken(window) is { Length: > 0 } token ? token : null
        : null;
  }
}
