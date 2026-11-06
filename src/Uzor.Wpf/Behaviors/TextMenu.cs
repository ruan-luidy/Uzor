using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MahApps.Metro.IconPacks;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// O menu de recortar, copiar e colar das caixas de texto no jeito do menu de contexto da Uzor (cartão dos
  /// flutuantes, ícone, atalho em cinza). Sem isto o WPF monta o dele, de um tipo interno que o estilo implícito do
  /// ContextMenu não pega, e ele sai no cinza clássico do Windows. A caixa ganha o nosso antes do WPF olhar: no botão
  /// direito ou quando recebe o foco (a tecla de menu e o Shift+F10). Caixa com menu próprio, ou com
  /// <c>ContextMenu="{x:Null}"</c> pra não ter menu, fica como está.
  ///
  /// Liga uma vez na partida, antes de abrir janela: <c>TextMenu.Install();</c>. Vale pra TextBox (e tudo que deriva
  /// dela: o hc:TextBox, o campo do DatePicker, o do combo editável), RichTextBox e PasswordBox (só colar e selecionar
  /// tudo).
  /// </summary>
  public static class TextMenu
  {
    private static bool _installed;

    public static void Install()
    {
      if (_installed)
        return;
      _installed = true;

      foreach (var type in new[] { typeof(TextBoxBase), typeof(PasswordBox) })
      {
        EventManager.RegisterClassHandler(type, UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler((sender, _) => Give(sender)), true);
        EventManager.RegisterClassHandler(type, UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler((sender, _) => Give(sender)), true);
      }
    }

    /// <summary>Põe o menu na caixa se ninguém pôs nada nela (nem menu, nem o x:Null de "sem menu").</summary>
    internal static void Give(object sender)
    {
      if (sender is not Control box)
        return;
      if (DependencyPropertyHelper.GetValueSource(box, FrameworkElement.ContextMenuProperty).BaseValueSource != BaseValueSource.Default)
        return;

      box.ContextMenu = Build(box);
    }

    internal static ContextMenu Build(Control box)
    {
      var menu = new ContextMenu();
      if (box is TextBoxBase)
      {
        menu.Items.Add(Item(Texts.Cut, ApplicationCommands.Cut, PackIconPhosphorIconsKind.ScissorsBold, box));
        menu.Items.Add(Item(Texts.Copy, ApplicationCommands.Copy, PackIconPhosphorIconsKind.CopyBold, box));
      }
      menu.Items.Add(Item(Texts.Paste, ApplicationCommands.Paste, PackIconPhosphorIconsKind.ClipboardTextBold, box));
      menu.Items.Add(new Separator());
      menu.Items.Add(Item(Texts.SelectAll, ApplicationCommands.SelectAll, PackIconPhosphorIconsKind.SelectionAllBold, box));

      // O apagado de "Recortar" sem seleção e de "Colar" sem nada copiado vem do CanExecute: confere na hora de abrir
      menu.Opened += (_, _) => CommandManager.InvalidateRequerySuggested();
      return menu;
    }

    private static MenuItem Item(string header, RoutedUICommand command, PackIconPhosphorIconsKind icon, Control box) => new()
    {
      Header = header,
      Command = command,
      CommandTarget = box,
      Icon = new PackIconPhosphorIcons { Kind = icon },
    };
  }
}
