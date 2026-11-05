using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Uzor.Wpf.Animation
{
  /// <summary>
  /// A troca de tema em onda (a ideia do LightDarkModeSwitchAnimation, do SlimeNull): fotografa a janela no tema
  /// velho, põe a foto por cima de tudo (barra de título e diálogo aberto inclusive), troca o tema por baixo e abre um
  /// círculo na foto a partir do mouse. A borda da onda é macia e leva um brilho fino no azul do app; quando o círculo
  /// passa do canto mais longe, a foto sai.
  /// </summary>
  public static class ThemeRipple
  {
    private static readonly Duration Length = TimeSpan.FromMilliseconds(680);

    /// <summary>Onde a borda macia começa, na fração do raio (8% do raio é degradê).</summary>
    private const double SoftEdge = 0.92;

    /// <summary>Faz a troca (<paramref name="change"/>) com a onda; sem janela na tela, troca direto.</summary>
    public static void Run(Window window, Action change)
    {
      ArgumentNullException.ThrowIfNull(window);
      ArgumentNullException.ThrowIfNull(change);
      if (!window.IsVisible
        || VisualTreeHelper.GetChildrenCount(window) == 0
        || VisualTreeHelper.GetChild(window, 0) is not FrameworkElement root
        || root.ActualWidth < 1
        || FindHost(root) is not Panel host)
      {
        change();
        return;
      }

      var size = new Size(root.ActualWidth, root.ActualHeight);
      var dpi = VisualTreeHelper.GetDpi(root);
      var shot = new RenderTargetBitmap(
        (int)Math.Ceiling(size.Width * dpi.DpiScaleX),
        (int)Math.Ceiling(size.Height * dpi.DpiScaleY),
        dpi.PixelsPerInchX,
        dpi.PixelsPerInchY,
        PixelFormats.Pbgra32);
      shot.Render(root);
      shot.Freeze();

      // O fundo do tema velho embaixo da foto: com Mica a janela é transparente e a foto teria buracos
      var backdrop = (window.TryFindResource("Window.FallbackBrush") as Brush)?.CloneCurrentValue() ?? Brushes.Transparent;
      var accent = (window.TryFindResource("PrimaryBrush") as SolidColorBrush)?.Color ?? Color.FromRgb(0x37, 0x78, 0xBF);

      // Do mouse (o botão que trocou o tema); fora da janela, do meio
      var center = Mouse.GetPosition(root);
      if (center.X < 0 || center.Y < 0 || center.X > size.Width || center.Y > size.Height)
        center = new Point(size.Width / 2, size.Height / 2);
      var reach = Math.Max(
        Math.Max(new Vector(center.X, center.Y).Length, new Vector(size.Width - center.X, center.Y).Length),
        Math.Max(new Vector(center.X, size.Height - center.Y).Length, new Vector(size.Width - center.X, size.Height - center.Y).Length));

      // A foto aparece fora do círculo (preto na máscara) e some dentro dele, com o degradê na borda
      var mask = Circle(center, (Colors.Transparent, 0), (Colors.Transparent, SoftEdge), (Colors.Black, 1));
      var glow = Circle(center, (Colors.Transparent, 0), (Colors.Transparent, 0.9), (Color.FromArgb(0x60, accent.R, accent.G, accent.B), 0.975), (Colors.Transparent, 1));

      var photo = new Grid { OpacityMask = mask };
      photo.Children.Add(new Border { Background = backdrop });
      photo.Children.Add(new Image { Source = shot, Stretch = Stretch.Fill });
      var ring = new Border { Background = glow };

      // Por cima de tudo no painel raiz da janela, alinhado com a raiz (a borda da janela desloca o painel)
      var offset = host.TranslatePoint(default, root);
      var veil = new Grid
      {
        Width = size.Width,
        Height = size.Height,
        Margin = new Thickness(-offset.X, -offset.Y, 0, 0),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        IsHitTestVisible = false,
      };
      veil.Children.Add(photo);
      veil.Children.Add(ring);
      Grid.SetRowSpan(veil, 64);
      Grid.SetColumnSpan(veil, 64);
      Panel.SetZIndex(veil, int.MaxValue);
      host.Children.Add(veil);

      change();

      // A onda abre depois que o tema novo assentou embaixo (a tabela refaz as linhas em segundo plano): até lá a foto
      // do tema velho cobre tudo, e o miolo do círculo nunca mostra tela pela metade
      window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
      {
        var grow = new DoubleAnimation(0, reach / SoftEdge, Length) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        grow.Completed += (_, _) => host.Children.Remove(veil);
        foreach (var brush in new[] { mask, glow })
        {
          brush.BeginAnimation(RadialGradientBrush.RadiusXProperty, grow);
          brush.BeginAnimation(RadialGradientBrush.RadiusYProperty, grow);
        }

        // O brilho acompanha a frente e apaga chegando nos cantos
        ring.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(320)) { BeginTime = TimeSpan.FromMilliseconds(360) });
      });
    }

    private static RadialGradientBrush Circle(Point center, params (Color Color, double Offset)[] stops)
    {
      var brush = new RadialGradientBrush
      {
        MappingMode = BrushMappingMode.Absolute,
        Center = center,
        GradientOrigin = center,
        RadiusX = 0.01,
        RadiusY = 0.01,
      };
      foreach (var (color, offset) in stops)
        brush.GradientStops.Add(new GradientStop(color, offset));
      return brush;
    }

    /// <summary>O primeiro painel do modelo da janela (o que segura a barra de título e o conteúdo).</summary>
    private static Panel? FindHost(DependencyObject root)
    {
      var queue = new Queue<DependencyObject>([root]);
      while (queue.Count > 0)
      {
        var node = queue.Dequeue();
        if (node is Panel panel)
          return panel;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
          queue.Enqueue(VisualTreeHelper.GetChild(node, i));
      }

      return null;
    }
  }
}
