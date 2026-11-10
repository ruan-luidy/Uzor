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
  /// círculo na foto a partir do mouse; quando o círculo passa do canto mais longe, a foto sai.
  /// <para>Leve de propósito (06/11/2026, "trava tudo"): 450 ms, a foto desenhada 1:1 sem filtro, e em sessão remota
  /// ou sem placa de vídeo a foto só esmaece. O que segurava a tela de verdade era a troca dos pincéis, agora num
  /// dicionário só (ThemeService).</para>
  /// <para>Borda seca (10/11/2026, "tira o efeito gradiente"): o círculo é um furo no recorte da foto (Clip), como no
  /// SlimeNull, e não mais uma máscara com degradê na borda. A máscara ainda obrigava a foto a ser desenhada numa
  /// camada à parte a cada quadro; o recorte não.</para>
  /// </summary>
  public static class ThemeRipple
  {
    private static readonly Duration Length = TimeSpan.FromMilliseconds(450);

    /// <summary>O esmaecer de quando não tem GPU (sessão remota, placa sem aceleração).</summary>
    private static readonly Duration FadeLength = TimeSpan.FromMilliseconds(200);

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
      var light = SystemParameters.IsRemoteSession || (RenderCapability.Tier >> 16) < 2;

      // Do mouse (o botão que trocou o tema); fora da janela, do meio
      var center = Mouse.GetPosition(root);
      if (center.X < 0 || center.Y < 0 || center.X > size.Width || center.Y > size.Height)
        center = new Point(size.Width / 2, size.Height / 2);
      var reach = Math.Max(
        Math.Max(new Vector(center.X, center.Y).Length, new Vector(size.Width - center.X, center.Y).Length),
        Math.Max(new Vector(center.X, size.Height - center.Y).Length, new Vector(size.Width - center.X, size.Height - center.Y).Length));

      // A foto aparece fora do círculo e some dentro dele: o retângulo da janela com o círculo furado (par-ímpar). Quando
      // o círculo passa da janela, a parte dele de fora entra no recorte, mas ali a foto não tem nada pra mostrar
      var hole = new EllipseGeometry(center, 0, 0);
      var clip = new GeometryGroup { FillRule = FillRule.EvenOdd };
      clip.Children.Add(new RectangleGeometry(new Rect(size)));
      clip.Children.Add(hole);

      var photo = new Grid { Clip = light ? null : clip };
      photo.Children.Add(new Border { Background = backdrop });
      var image = new Image { Source = shot, Stretch = Stretch.Fill };
      RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
      photo.Children.Add(image);

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
      Grid.SetRowSpan(veil, 64);
      Grid.SetColumnSpan(veil, 64);
      Panel.SetZIndex(veil, int.MaxValue);
      host.Children.Add(veil);

      change();

      // A onda abre depois que o tema novo assentou embaixo (a tabela refaz as linhas em segundo plano): até lá a foto
      // do tema velho cobre tudo, e o miolo do círculo nunca mostra tela pela metade
      window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
      {
        if (light)
        {
          var fade = new DoubleAnimation(1, 0, FadeLength);
          fade.Completed += (_, _) => host.Children.Remove(veil);
          photo.BeginAnimation(UIElement.OpacityProperty, fade);
          return;
        }

        var grow = new DoubleAnimation(0, reach, Length) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        grow.Completed += (_, _) => host.Children.Remove(veil);
        hole.BeginAnimation(EllipseGeometry.RadiusXProperty, grow);
        hole.BeginAnimation(EllipseGeometry.RadiusYProperty, grow);
      });
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
