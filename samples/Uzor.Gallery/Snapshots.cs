using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Uzor.Gallery.Pages;
using Uzor.Wpf.Controls;
using Uzor.Wpf.Services;

namespace Uzor.Gallery
{
  /// <summary>
  /// A foto de tudo sem aparecer na tela de ninguém (<c>Uzor.Gallery.exe --snapshots pasta</c>): a janela abre fora da
  /// área visível, sem foco e sem barra de tarefas, e cada página, diálogo e tooltip vira PNG, no escuro e no claro. É a
  /// revisão visual de uma mudança na Uzor antes de ela chegar nos apps.
  /// </summary>
  internal static class Snapshots
  {
    public static string? FolderFrom(string[] args)
    {
      var at = Array.IndexOf(args, "--snapshots");
      return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
    }

    public static async Task RunAsync(MainWindow window, string folder)
    {
      Directory.CreateDirectory(folder);
      window.WindowStartupLocation = WindowStartupLocation.Manual;
      window.Left = -32000;
      window.Top = -32000;
      window.ShowActivated = false;
      window.ShowInTaskbar = false;
      window.Show();
      await Task.Delay(800).ConfigureAwait(true);

      foreach (var dark in new[] { true, false })
      {
        ThemeService.Apply(dark);
        var theme = dark ? "escuro" : "claro";
        var n = 0;
        foreach (var name in window.PageNames)
        {
          window.ShowPage(name);
          await Task.Delay(700).ConfigureAwait(true);
          Save(window, Path.Combine(folder, $"{theme}-{++n:00}-{name}.png"));
        }

        window.ShowPage("campos");
        await Task.Delay(400).ConfigureAwait(true);
        SavePopup(window, Path.Combine(folder, $"{theme}-20-calendario.png"));

        window.ShowPage("dialogos");
        await DialogAsync(window, Path.Combine(folder, $"{theme}-21-aviso.png"), () => _ = DialogsPage.InfoAsync()).ConfigureAwait(true);
        await DialogAsync(window, Path.Combine(folder, $"{theme}-22-confirmar.png"), () => _ = DialogsPage.ConfirmAsync()).ConfigureAwait(true);
        await DialogAsync(window, Path.Combine(folder, $"{theme}-23-erro.png"), () => _ = DialogsPage.ErrorAsync()).ConfigureAwait(true);
        await DialogAsync(window, Path.Combine(folder, $"{theme}-24-formulario.png"), () => _ = DialogsPage.FormAsync()).ConfigureAwait(true);

        ToastService.Success("Rascunho criado.");
        ToastService.Error("O servidor não respondeu.", "Não deu pra salvar");
        await Task.Delay(700).ConfigureAwait(true);
        Save(window, Path.Combine(folder, $"{theme}-25-avisos.png"));
        await Task.Delay(5500).ConfigureAwait(true);

        SaveToolTip(new ToolTip { Content = "Buscar desenhos nas pastas" }, Path.Combine(folder, $"{theme}-26-tooltip-curto.png"));
        SaveToolTip(new ToolTip { Content = $"Aprovado 05/11/2026 14:30{Environment.NewLine}Justificativa: o menor preço não atende o prazo da montagem." }, Path.Combine(folder, $"{theme}-27-tooltip-titulo.png"));
      }

      // A onda da troca de tema, do escuro pro claro
      ThemeService.Apply(dark: true);
      window.ShowPage("botoes");
      await Task.Delay(700).ConfigureAwait(true);
      ThemeService.Switch(dark: false);
      await Task.Delay(300).ConfigureAwait(true);
      Save(window, Path.Combine(folder, "onda-1.png"));
      await Task.Delay(200).ConfigureAwait(true);
      Save(window, Path.Combine(folder, "onda-2.png"));
      await Task.Delay(1000).ConfigureAwait(true);
      Save(window, Path.Combine(folder, "onda-3.png"));

      window.Close();
    }

    private static async Task DialogAsync(Window window, string path, Action open)
    {
      open();
      await Task.Delay(900).ConfigureAwait(true);
      Save(window, path);
      foreach (var dialog in Descendants(window).OfType<HandyControl.Controls.Dialog>().ToList())
        dialog.Close();
      await Task.Delay(400).ConfigureAwait(true);
    }

    // O cartão do calendário desenhado sem abrir o Popup (aberto, ele iria pra borda da tela de quem usa o PC)
    private static void SavePopup(Window window, string path)
    {
      if (Descendants(window).OfType<DateTimeField>().FirstOrDefault(f => f.IsVisible) is not { } field
        || field.FindName("DropDown") is not Popup { Child: FrameworkElement card })
        return;
      card.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
      card.Arrange(new Rect(card.DesiredSize));
      Save(card, path);
    }

    private static void SaveToolTip(ToolTip tip, string path)
    {
      tip.ApplyTemplate();
      tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
      tip.Arrange(new Rect(tip.DesiredSize));
      Save(tip, path);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
      for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
      {
        var child = VisualTreeHelper.GetChild(root, i);
        yield return child;
        foreach (var inner in Descendants(child))
          yield return inner;
      }
    }

    private static void Save(FrameworkElement element, string path)
    {
      element.UpdateLayout();
      var dpi = VisualTreeHelper.GetDpi(element);
      var bitmap = new RenderTargetBitmap(
        (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
        (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY),
        dpi.PixelsPerInchX,
        dpi.PixelsPerInchY,
        PixelFormats.Pbgra32);
      bitmap.Render(element);
      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmap));
      using var file = File.Create(path);
      encoder.Save(file);
    }
  }
}
