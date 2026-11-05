using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using Uzor.Wpf.Icons;
using Uzor.Wpf.Services;
using MahApps.Metro.IconPacks;

namespace Uzor.Wpf.Controls
{
  /// <summary>Tom do diálogo: decide ícone e cor.</summary>
  public enum DialogKind
  {
    Info,
    Success,
    Warning,
    Error,
  }

  /// <summary>O cartão do diálogo. O visual está no XAML; quem abre e fecha é o <see cref="DialogService"/>.</summary>
  public partial class AlertDialog : UserControl
  {
    private readonly string? _details;

    private readonly string? _filePath;

    /// <summary>A largura de antes de abrir os detalhes (420, ou 520 com mensagem comprida).</summary>
    private readonly double _largura;

    /// <param name="filePath">
    /// O arquivo que a mensagem cita (o que foi exportado ou salvo): aparece o nome dele com o atalho pra pasta, que
    /// abre o Explorer com ele selecionado. Nulo = sem atalho.
    /// </param>
    public AlertDialog(DialogKind kind, string title, string message, string primaryText, string? secondaryText = null, string? details = null,
      string? filePath = null)
    {
      InitializeComponent();

      TitleText.Text = title;
      MessageText.Text = message;
      // Mensagem de várias linhas (a confirmação com os totais): mais largo, pra linha não quebrar no meio do valor
      if (message.Length > 160 || message.Count(c => c == '\n') >= 3)
        Width = 520;
      PrimaryButton.Content = primaryText;
      _details = string.IsNullOrWhiteSpace(details) ? null : details;
      DetailsLink.Visibility = _details is null ? Visibility.Collapsed : Visibility.Visible;

      _filePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath.Trim();
      if (_filePath is not null)
      {
        FileLinkText.Text = $"{System.IO.Path.GetFileName(_filePath)} · {Texts.ShowInFolder}";
        FileLink.ToolTip = _filePath;
        FileLink.Visibility = Visibility.Visible;
      }

      if (secondaryText is not null)
      {
        SecondaryButton.Content = secondaryText;
        SecondaryButton.Visibility = Visibility.Visible;
      }

      // O quadradinho no tom do aviso: o fundo suave e o desenho na cor cheia
      var (icon, tone) = kind switch
      {
        DialogKind.Success => (PackIconPhosphorIconsKind.CheckCircleBold, "Success"),
        DialogKind.Warning => (PackIconPhosphorIconsKind.WarningBold, "Warning"),
        DialogKind.Error => (PackIconPhosphorIconsKind.WarningOctagonBold, "Danger"),
        _ => (PackIconPhosphorIconsKind.InfoBold, "Accent"),
      };
      HeaderIcon.Data = PhosphorExtension.Get(icon);
      IconTile.SetResourceReference(Border.BackgroundProperty, tone + ".SoftBrush");
      HeaderIcon.SetResourceReference(Shape.FillProperty, tone + ".TextBrush");

      // Confirmar o que não tem volta (apagar, cancelar de vez): o botão principal no vermelho
      if (kind == DialogKind.Error && secondaryText is not null)
        PrimaryButton.Style = (Style)FindResource("Action.Dialog.Danger");

      _largura = Width;
      Loaded += OnLoaded;
      PreviewKeyDown += OnPreviewKeyDown;
    }

    public event EventHandler? PrimaryClicked;
    public event EventHandler? SecondaryClicked;

    /// <summary>Foco no botão principal depois que o overlay terminou de montar: Enter funciona na hora.</summary>
    private void OnLoaded(object sender, RoutedEventArgs e) =>
      Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
      {
        FocusManager.SetFocusedElement(this, PrimaryButton);
        Keyboard.Focus(PrimaryButton);
      });

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter && Keyboard.FocusedElement is not TextBox)
      {
        PrimaryButton_Click(PrimaryButton, e);
        e.Handled = true;
      }
      else if (e.Key == Key.Escape)
      {
        if (SecondaryButton.Visibility == Visibility.Visible)
          SecondaryButton_Click(SecondaryButton, e);
        else
          PrimaryButton_Click(PrimaryButton, e);
        e.Handled = true;
      }
    }

    /// <summary>"ver detalhes": o texto técnico aparece ali mesmo, e o cartão alarga pra caber.</summary>
    private void DetailsLink_Click(object sender, RoutedEventArgs e)
    {
      var open = DetailsCard.Visibility != Visibility.Visible;
      DetailsText.Text = _details;
      DetailsCard.Visibility = CopyButton.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
      DetailsLinkText.Text = open ? Texts.HideDetails : Texts.ShowDetails;
      Width = open ? 560 : _largura;
    }

    /// <summary>O Explorer com o arquivo selecionado; sem o arquivo, a pasta dele (se existir).</summary>
    private void FileLink_Click(object sender, RoutedEventArgs e)
    {
      if (_filePath is null)
        return;
      try
      {
        if (System.IO.File.Exists(_filePath))
          System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_filePath}\"");
        else if (System.IO.Path.GetDirectoryName(_filePath) is { } folder && System.IO.Directory.Exists(folder))
          System.Diagnostics.Process.Start("explorer.exe", $"\"{folder}\"");
      }
      catch (System.ComponentModel.Win32Exception)
      {
        // Sem Explorer (sessão restrita): o caminho continua no tooltip pra copiar
      }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
      if (_details is null)
        return;

      Clipboard.SetText(_details);
      ToastService.Success(Texts.DetailsCopied);
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e) => PrimaryClicked?.Invoke(this, EventArgs.Empty);

    private void SecondaryButton_Click(object sender, RoutedEventArgs e) => SecondaryClicked?.Invoke(this, EventArgs.Empty);
  }
}
