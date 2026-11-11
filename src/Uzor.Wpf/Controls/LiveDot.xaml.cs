using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Uzor.Wpf.Controls
{
  public partial class LiveDot : UserControl
  {
    // Janela transparente (AllowsTransparency, as do Otmetka) refaz e copia a janela inteira pro Windows a cada quadro,
    // mudou um ponto ou mudou tudo: medido no cartão do Otmetka, uns 3,5 ms por quadro, e a bolinha sozinha puxava 100
    // quadros por segundo num monitor de 100 Hz (um terço de um núcleo) e engasgava. Lá ela anda a 30 quadros, que num
    // ponto de 10 px não se nota. Janela normal só redesenha o ponto e fica no ritmo do monitor
    private const int LayeredFrameRate = 30;

    public LiveDot()
    {
      InitializeComponent();
      var pulse = (Storyboard)Resources["Pulse"];
      void Start()
      {
        Timeline.SetDesiredFrameRate(pulse, Window.GetWindow(this) is { AllowsTransparency: true } ? LayeredFrameRate : null);
        pulse.Begin(this, true);
      }

      // Escondida (painel fechado, sem batida) não gasta quadro
      IsVisibleChanged += (_, e) =>
      {
        if (e.NewValue is true)
          Start();
        else
          pulse.Stop(this);
      };
      Loaded += (_, _) =>
      {
        if (IsVisible)
          Start();
      };
    }
  }
}
