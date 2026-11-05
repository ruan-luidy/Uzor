using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Data;
using HandyControl.Tools;
using MahApps.Metro.IconPacks;
using Uzor.Gallery.Pages;
using Uzor.Wpf.Animation;
using Uzor.Wpf.Icons;
using Uzor.Wpf.Services;

namespace Uzor.Gallery
{
  public partial class MainWindow : HandyControl.Controls.Window
  {
    private readonly List<(string Name, string Title, UserControl Page, RadioButton Item)> _pages = [];

    public MainWindow()
    {
      InitializeComponent();
      ApplyBackdrop();
      Add("botoes", "Botões e escolhas", PackIconPhosphorIconsKind.CursorClickBold, new ButtonsPage());
      Add("campos", "Campos", PackIconPhosphorIconsKind.TextboxBold, new FieldsPage());
      Add("tabela", "Tabela", PackIconPhosphorIconsKind.TableBold, new TablePage());
      Add("cartoes", "Cartões e estados", PackIconPhosphorIconsKind.CardsBold, new CardsPage());
      Add("dialogos", "Diálogos e avisos", PackIconPhosphorIconsKind.ChatCenteredTextBold, new DialogsPage());
      Add("tooltips", "Tooltips", PackIconPhosphorIconsKind.ChatTeardropTextBold, new TooltipsPage());
      Host.Shown += (_, e) => Motion.SlideIn(e.View, 24);
      ShowPage("botoes");
    }

    public IReadOnlyList<string> PageNames => _pages.Select(p => p.Name).ToList();

    public void ShowPage(string name)
    {
      var page = _pages.First(p => p.Name == name);
      page.Item.IsChecked = true;
      Host.Page = page.Page;
    }

    private void Add(string name, string title, PackIconPhosphorIconsKind icon, UserControl page)
    {
      var item = new RadioButton
      {
        Content = title,
        GroupName = "Nav",
        Tag = PhosphorExtension.Get(icon),
        Style = (Style)FindResource("SideNavItem"),
      };
      item.Checked += (_, _) => Host.Page = page;
      Nav.Children.Add(item);
      _pages.Add((name, title, page, item));
    }

    // Mica no Windows 11; sem ele, o cinza sólido do tema
    private void ApplyBackdrop()
    {
      if (BackdropType.Mica.IsSupported())
      {
        SystemBackdropType = BackdropType.Mica;
        return;
      }

      SetResourceReference(BackgroundProperty, "Window.FallbackBrush");
    }

    private void Theme_Click(object sender, RoutedEventArgs e) => ThemeService.Switch(!ThemeService.IsDark);
  }
}
