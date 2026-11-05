using System.Windows;
using System.Windows.Controls;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// O chip de status (pílula de raio 4): fundo da cor do tom a ~18% e o texto na cor cheia, como no Otmetka e no
  /// Handoff. O visual mora no Kit.xaml.
  ///   &lt;controls:Chip Content="{Binding Status.Text}" Tone="{Binding Status.Tone}" /&gt;
  /// </summary>
  public class Chip : ContentControl
  {
    public static readonly DependencyProperty ToneProperty =
      DependencyProperty.Register(nameof(Tone), typeof(Tone), typeof(Chip), new PropertyMetadata(Tone.Neutral));

    static Chip()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(Chip), new FrameworkPropertyMetadata(typeof(Chip)));
    }

    public Tone Tone
    {
      get => (Tone)GetValue(ToneProperty);
      set => SetValue(ToneProperty, value);
    }
  }
}
