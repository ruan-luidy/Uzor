using System.Windows;
using Uzor.Wpf.Services;

namespace Uzor.Gallery
{
  public partial class App : Application
  {
    protected override async void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      ThemeService.Apply(dark: true);

      var window = new MainWindow();
      MainWindow = window;
      if (Snapshots.FolderFrom(e.Args) is { } folder)
      {
        await Snapshots.RunAsync(window, folder).ConfigureAwait(true);
        return;
      }

      window.Show();
    }
  }
}
