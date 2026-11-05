using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using HandyControl.Controls;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// Carregando no jeito do rnzNegativeObserver: o CircleProgressBar do HandyControl (não as bolinhas do
  /// LoadingCircle) e a frase do que está carregando. Cobre a célula do Grid onde está (e as que o
  /// Grid.RowSpan/ColumnSpan pegar). O círculo só anima visível: o LoadingCircle seguia girando escondido e
  /// redesenhava a janela à toa.
  ///
  /// <para>Desde 05/11/2026 o círculo e a frase ficam num cartão flutuante (a superfície do menu do Windows 11, com
  /// sombra) sobre um véu leve: a frase grande e branca direto em cima da grade não se lia no escuro e sumia no claro.</para>
  ///
  /// Uso: <c>&lt;uzor:LoadingOverlay Text="Carregando reservas..." Visibility="{Binding IsLoading, Converter=...}" /&gt;</c>.
  /// </summary>
  public sealed class LoadingOverlay : Border
  {
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text), typeof(string), typeof(LoadingOverlay), new PropertyMetadata(null, (d, _) => ((LoadingOverlay)d).ShowText()));

    public static readonly DependencyProperty BlocksInputProperty = DependencyProperty.Register(
      nameof(BlocksInput), typeof(bool), typeof(LoadingOverlay), new PropertyMetadata(false, (d, e) => ((LoadingOverlay)d).IsHitTestVisible = (bool)e.NewValue));

    private readonly TextBlock _text;

    public LoadingOverlay()
    {
      IsHitTestVisible = false;
      SetResourceReference(BackgroundProperty, "Overlay.VeilBrush");

      var circle = new CircleProgressBar
      {
        Width = 40,
        Height = 40,
        ArcThickness = 3,
        HorizontalAlignment = HorizontalAlignment.Center,
        Background = Brushes.Transparent,
      };
      circle.SetBinding(CircleProgressBar.IsIndeterminateProperty, new Binding(nameof(IsVisible)) { Source = this, Mode = BindingMode.OneWay });

      _text = new TextBlock
      {
        Margin = new Thickness(0, 12, 0, 0),
        MaxWidth = 280,
        HorizontalAlignment = HorizontalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        FontWeight = FontWeights.SemiBold,
      };
      _text.SetResourceReference(TextBlock.FontSizeProperty, "FontSize.Body");
      _text.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");

      var content = new StackPanel { Children = { circle, _text } };

      // A sombra numa borda só dela, atrás do cartão: com o efeito no cartão do texto, o texto borra
      var radius = TryFindResource("Radius.XL") is CornerRadius r ? r : new CornerRadius(12);
      var shadow = new Border
      {
        CornerRadius = radius,
        Effect = new DropShadowEffect { BlurRadius = 24, Direction = 270, ShadowDepth = 6, Opacity = 0.28, Color = Colors.Black },
      };
      shadow.SetResourceReference(BackgroundProperty, "Flyout.BackgroundBrush");
      var card = new Border
      {
        CornerRadius = radius,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(28, 20, 28, 18),
        MinWidth = 180,
        Child = content,
      };
      card.SetResourceReference(BackgroundProperty, "Flyout.BackgroundBrush");
      card.SetResourceReference(BorderBrushProperty, "Flyout.BorderBrush");

      Child = new Grid
      {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(16),
        Children = { shadow, card },
      };
      ShowText();
    }

    /// <summary>O que está carregando ("Carregando reservas..."); vazio fica no "Carregando..." do <see cref="Texts"/>.</summary>
    public string? Text
    {
      get => (string?)GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    /// <summary>Segura o clique enquanto carrega (a janela inteira, um diálogo). Desligado, a barra de cima continua usável.</summary>
    public bool BlocksInput
    {
      get => (bool)GetValue(BlocksInputProperty);
      set => SetValue(BlocksInputProperty, value);
    }

    private void ShowText() => _text.Text = string.IsNullOrWhiteSpace(Text) ? Texts.Loading : Text;
  }
}
