using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Uzor.Wpf.Animation;
using Uzor.Wpf.Icons;
using Uzor.Wpf.Services;
using MahApps.Metro.IconPacks;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// Formulário curto num diálogo. Quem abre passa os campos e, opcionalmente, o que fazer ao confirmar
  /// (<c>submit</c>): ele roda com o diálogo aberto e, se a API recusar, o recado dela aparece embaixo.
  /// </summary>
  public partial class FormDialog : UserControl, IDialogContent
  {
    private readonly IReadOnlyList<FormField> _fields;
    private readonly Func<Task>? _submit;

    public FormDialog(string title, string? subtitle, string? message, string confirmText, IReadOnlyList<FormField> fields, Func<Task>? submit = null, bool danger = false, PackIconPhosphorIconsKind? icon = null)
    {
      InitializeComponent();
      _fields = fields;
      _submit = submit;
      Frame.Title = title;
      Frame.Subtitle = subtitle;
      MessageText.Text = message;
      MessageCard.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
      ConfirmButton.Content = confirmText;
      if (danger)
      {
        // O que não tem volta: o botão e o recado no vermelho
        ConfirmButton.Style = (Style)FindResource("Action.Dialog.Danger");
        MessageCard.SetResourceReference(Border.BackgroundProperty, "Danger.SoftBrush");
        MessageIcon.Data = PhosphorExtension.Get(PackIconPhosphorIconsKind.WarningBold);
        MessageIcon.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "Danger.TextBrush");
      }
      if (icon is { } kind)
        Frame.Icon = PhosphorExtension.Get(kind);
      FieldsList.ItemsSource = fields;
      Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, FocusFirst);
    }

    public event EventHandler<bool>? Closed;

    /// <summary>Reconhece o erro de sessão encerrada no submit (o diálogo fecha calado em vez de mostrar o recado).</summary>
    public static Func<Exception, bool>? IsSessionEnd { get; set; }

    /// <summary>Abre e espera: true quando confirmou (e o submit, se houver, deu certo).</summary>
    public static Task<bool> ShowAsync(string title, string? subtitle, string? message, string confirmText, IReadOnlyList<FormField> fields, Func<Task>? submit = null, bool danger = false, PackIconPhosphorIconsKind? icon = null) =>
      DialogService.ShowAsync(new FormDialog(title, subtitle, message, confirmText, fields, submit, danger, icon));

    private void FocusFirst()
    {
      if (_fields.Count == 0 || FieldsList.ItemContainerGenerator.ContainerFromIndex(0) is not DependencyObject container)
        return;

      if (FindChild<TextBox>(container) is { IsVisible: true } box)
      {
        box.Focus();
        box.SelectAll();
      }
      else if (FindChild<PasswordBox>(container) is { IsVisible: true } password)
      {
        password.Focus();
      }
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
      ServerError.Visibility = Visibility.Collapsed;
      var invalid = _fields.Where(f => !f.Check()).ToList();
      if (invalid.Count > 0)
      {
        foreach (var field in invalid)
        {
          if (FieldsList.ItemContainerGenerator.ContainerFromItem(field) is UIElement container)
            Motion.Shake(container);
        }

        return;
      }

      if (_submit is not null)
      {
        ConfirmButton.IsEnabled = CancelButton.IsEnabled = false;
        Cursor = Cursors.Wait;
        try
        {
          await _submit().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
          // A sessão caiu (o app diz como reconhecer): fecha sem recado, quem cuida do login é o app
          if (IsSessionEnd?.Invoke(ex) == true)
          {
            Closed?.Invoke(this, false);
            return;
          }

          ServerErrorText.Text = ex.Message;
          ServerError.Visibility = Visibility.Visible;
          Motion.Shake(ServerError);
          return;
        }
        finally
        {
          ConfirmButton.IsEnabled = CancelButton.IsEnabled = true;
          Cursor = null;
        }
      }

      Closed?.Invoke(this, true);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Closed?.Invoke(this, false);

    private void Password_Changed(object sender, RoutedEventArgs e)
    {
      if (sender is PasswordBox { DataContext: FormField field } box)
        field.Value = box.Password;
    }

    private static T? FindChild<T>(DependencyObject parent)
      where T : DependencyObject
    {
      for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
        if (child is T found)
          return found;
        if (FindChild<T>(child) is { } deeper)
          return deeper;
      }

      return null;
    }
  }
}
