using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Uzor.Wpf.Controls
{
  public partial class LoadingDots : UserControl
  {
    public LoadingDots()
    {
      InitializeComponent();
      var wave = (Storyboard)Resources["Wave"];
      // escondida não gasta quadro (igual ao LiveDot)
      IsVisibleChanged += (_, e) =>
      {
        if (e.NewValue is true)
          wave.Begin(this, true);
        else
          wave.Stop(this);
      };
      Loaded += (_, _) =>
      {
        if (IsVisible)
          wave.Begin(this, true);
      };
    }
  }
}
