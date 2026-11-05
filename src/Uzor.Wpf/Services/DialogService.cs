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
      content.Closed += (_, ok) =>
      {
        dialog?.Close();
        result.TrySetResult(ok);
      };
      dialog = Dialog.Show(content, token);
      return result.Task;
    }

    private static Task<bool> ShowAlertAsync(AlertDialog content, string token)
    {
      var result = new TaskCompletionSource<bool>();
      Dialog? dialog = null;

      void Finish(bool value)
      {
        dialog?.Close();
        result.TrySetResult(value);
      }

      content.PrimaryClicked += (_, _) => Finish(true);
      content.SecondaryClicked += (_, _) => Finish(false);
      dialog = Dialog.Show(content, token);
      return result.Task;
    }

    /// <summary>O token da janela que pode receber o overlay (ele entra na camada de adorno dela).</summary>
    private static string? Host() =>
      Application.Current?.MainWindow is { IsLoaded: true, IsVisible: true } window
        ? HandyControl.Controls.Dialog.GetToken(window) is { Length: > 0 } token ? token : null
        : null;
  }
}
