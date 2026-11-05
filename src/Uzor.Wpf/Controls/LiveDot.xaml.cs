using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Uzor.Wpf.Controls
{
  public partial class LiveDot : UserControl
  {
    public LiveDot()
    {
      InitializeComponent();
      var pulse = (Storyboard)Resources["Pulse"];
      // Escondida (painel fechado, sem batida) não gasta quadro
      IsVisibleChanged += (_, e) =>
      {
        if (e.NewValue is true)
          pulse.Begin(this, true);
        else
          pulse.Stop(this);
      };
      Loaded += (_, _) =>
      {
        if (IsVisible)
          pulse.Begin(this, true);
      };
    }
  }
}
