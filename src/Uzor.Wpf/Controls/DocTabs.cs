using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// As abas de documento (as telas abertas da janela, no estilo <c>Uzor.DocTabs</c>): pílulas como as do
  /// <c>Uzor.Tabs</c>, com o X. O X manda <see cref="Close"/> com a aba no parâmetro, e quem guarda as abas (a janela)
  /// liga o comando e decide como fechar: a aba às vezes leva uma tela viva junto.
  /// <para>A faixa rola de lado com a roda do mouse (<see cref="WheelScrollsProperty"/>), sem barra.</para>
  /// <para>
  /// Quando não cabe (<see cref="OverflowProperty"/>, 27/11/2026, "mais visível que dá pra ir pro lado"): a ponta que
  /// continua some num degradê, a setinha diz quantas abas tem pra lá (<see cref="HiddenBeforeProperty"/> e
  /// <see cref="HiddenAfterProperty"/>) e anda uma página (<see cref="PageBack"/>, <see cref="PageForward"/>; segurando
  /// continua), a rolagem desliza em vez de pular, e a aba escolhida sempre vem pra vista.
  /// </para>
  /// </summary>
  public static class DocTabs
  {
    public static readonly RoutedUICommand Close = new("Fechar", nameof(Close), typeof(DocTabs));

    /// <summary>Anda as abas uma página pra trás (a setinha da esquerda).</summary>
    public static readonly RoutedUICommand PageBack = new("Abas anteriores", nameof(PageBack), typeof(DocTabs));

    /// <summary>Anda as abas uma página pra frente (a setinha da direita).</summary>
    public static readonly RoutedUICommand PageForward = new("Próximas abas", nameof(PageForward), typeof(DocTabs));

    /// <summary>A ponta coberta pela setinha (transparente) e o degradê logo depois dela, onde a aba vai sumindo.</summary>
    private const double Edge = 44;
    private const double FadeLength = 48;

    /// <summary>A partir de onde uma aba conta como escondida: no meio do degradê.</summary>
    private const double Hidden = Edge + FadeLength / 2;

    /// <summary>Quanto a aba escolhida fica longe da ponta quando a faixa anda até ela (fora da setinha e do degradê).</summary>
    private const double Margin = Edge + FadeLength + 8;

    public static readonly DependencyProperty WheelScrollsProperty = DependencyProperty.RegisterAttached(
      "WheelScrolls", typeof(bool), typeof(DocTabs), new PropertyMetadata(false, OnWheelScrollsChanged));

    public static bool GetWheelScrolls(DependencyObject d) => (bool)d.GetValue(WheelScrollsProperty);

    public static void SetWheelScrolls(DependencyObject d, bool value) => d.SetValue(WheelScrollsProperty, value);

    public static readonly DependencyProperty OverflowProperty = DependencyProperty.RegisterAttached(
      "Overflow", typeof(bool), typeof(DocTabs), new PropertyMetadata(false, OnOverflowChanged));

    public static bool GetOverflow(DependencyObject d) => (bool)d.GetValue(OverflowProperty);

    public static void SetOverflow(DependencyObject d, bool value) => d.SetValue(OverflowProperty, value);

    /// <summary>Quantas abas estão (inteira ou em parte) escondidas antes da vista. A setinha da esquerda mostra.</summary>
    public static readonly DependencyProperty HiddenBeforeProperty = DependencyProperty.RegisterAttached(
      "HiddenBefore", typeof(int), typeof(DocTabs), new PropertyMetadata(0));

    public static int GetHiddenBefore(DependencyObject d) => (int)d.GetValue(HiddenBeforeProperty);

    public static void SetHiddenBefore(DependencyObject d, int value) => d.SetValue(HiddenBeforeProperty, value);

    /// <summary>Quantas abas estão escondidas depois da vista. A setinha da direita mostra.</summary>
    public static readonly DependencyProperty HiddenAfterProperty = DependencyProperty.RegisterAttached(
      "HiddenAfter", typeof(int), typeof(DocTabs), new PropertyMetadata(0));

    public static int GetHiddenAfter(DependencyObject d) => (int)d.GetValue(HiddenAfterProperty);

    public static void SetHiddenAfter(DependencyObject d, int value) => d.SetValue(HiddenAfterProperty, value);

    /// <summary>A setinha da esquerda aparece: tem aba antes da vista.</summary>
    public static readonly DependencyProperty BeforeVisibilityProperty = DependencyProperty.RegisterAttached(
      "BeforeVisibility", typeof(Visibility), typeof(DocTabs), new PropertyMetadata(Visibility.Collapsed));

    public static Visibility GetBeforeVisibility(DependencyObject d) => (Visibility)d.GetValue(BeforeVisibilityProperty);

    public static void SetBeforeVisibility(DependencyObject d, Visibility value) => d.SetValue(BeforeVisibilityProperty, value);

    /// <summary>A setinha da direita aparece: tem aba depois da vista.</summary>
    public static readonly DependencyProperty AfterVisibilityProperty = DependencyProperty.RegisterAttached(
      "AfterVisibility", typeof(Visibility), typeof(DocTabs), new PropertyMetadata(Visibility.Collapsed));

    public static Visibility GetAfterVisibility(DependencyObject d) => (Visibility)d.GetValue(AfterVisibilityProperty);

    public static void SetAfterVisibility(DependencyObject d, Visibility value) => d.SetValue(AfterVisibilityProperty, value);

    /// <summary>A posição animada: cada passo da animação rola a faixa até ela.</summary>
    private static readonly DependencyProperty AnimatedOffsetProperty = DependencyProperty.RegisterAttached(
      "AnimatedOffset", typeof(double), typeof(DocTabs),
      new PropertyMetadata(0.0, (d, e) => (d as ScrollViewer)?.ScrollToHorizontalOffset((double)e.NewValue)));

    /// <summary>Pra onde a animação da vez está indo: a próxima volta da roda soma a partir dali, não do meio do caminho.</summary>
    private static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
      "Target", typeof(double), typeof(DocTabs), new PropertyMetadata(double.NaN));

    private static readonly DependencyProperty HookedProperty = DependencyProperty.RegisterAttached(
      "Hooked", typeof(bool), typeof(DocTabs), new PropertyMetadata(false));

    private static void OnWheelScrollsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not ScrollViewer rolagem)
        return;
      rolagem.PreviewMouseWheel -= OnWheel;
      if ((bool)e.NewValue)
        rolagem.PreviewMouseWheel += OnWheel;
    }

    /// <summary>A roda anda as abas de lado, deslizando; sem nada pra rolar, a roda segue pra quem estiver embaixo.</summary>
    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
      var rolagem = (ScrollViewer)sender;
      if (rolagem.ScrollableWidth <= 0)
        return;
      ScrollTo(rolagem, Target(rolagem) - e.Delta / 2.0);
      e.Handled = true;
    }

    private static void OnOverflowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not ScrollViewer rolagem || e.NewValue is not true || (bool)rolagem.GetValue(HookedProperty))
        return;
      rolagem.SetValue(HookedProperty, true);

      rolagem.ScrollChanged += (_, _) => Update(rolagem);
      rolagem.SizeChanged += (_, _) => Update(rolagem);
      rolagem.CommandBindings.Add(new CommandBinding(PageBack,
        (_, a) => { ScrollTo(rolagem, Target(rolagem) - Page(rolagem)); a.Handled = true; },
        (_, a) => { a.CanExecute = rolagem.HorizontalOffset > 0.5; a.Handled = true; }));
      rolagem.CommandBindings.Add(new CommandBinding(PageForward,
        (_, a) => { ScrollTo(rolagem, Target(rolagem) + Page(rolagem)); a.Handled = true; },
        (_, a) => { a.CanExecute = rolagem.HorizontalOffset < rolagem.ScrollableWidth - 0.5; a.Handled = true; }));

      // A aba escolhida vem pra vista: o TabControl é o dono do molde da faixa
      rolagem.Loaded += (_, _) =>
      {
        if (rolagem.TemplatedParent is not Selector abas || abas.GetValue(HookedProperty) is true)
          return;
        abas.SetValue(HookedProperty, true);
        abas.SelectionChanged += (_, a) =>
        {
          // SelectionChanged sobe da tela de dentro (grade, combo, outra aba): só a troca de aba desta faixa
          if (ReferenceEquals(a.OriginalSource, abas))
            rolagem.Dispatcher.BeginInvoke(() => ShowSelected(rolagem, abas), DispatcherPriority.Loaded);
        };
        rolagem.Dispatcher.BeginInvoke(() => ShowSelected(rolagem, abas), DispatcherPriority.Loaded);
      };
    }

    /// <summary>Uma página: o que se vê da faixa, menos as duas pontas, pra última aba vista continuar à vista.</summary>
    private static double Page(ScrollViewer rolagem) => Math.Max(80, rolagem.ViewportWidth - 2 * (Edge + FadeLength));

    private static double Target(ScrollViewer rolagem) =>
      rolagem.GetValue(TargetProperty) is double alvo && !double.IsNaN(alvo) ? alvo : rolagem.HorizontalOffset;

    /// <summary>Desliza até a posição (220 ms, freando no fim); sem animação no Windows, pula direto.</summary>
    internal static void ScrollTo(ScrollViewer rolagem, double alvo)
    {
      alvo = Math.Clamp(alvo, 0, Math.Max(0, rolagem.ScrollableWidth));
      if (!SystemParameters.ClientAreaAnimation)
      {
        rolagem.ScrollToHorizontalOffset(alvo);
        return;
      }
      rolagem.SetValue(TargetProperty, alvo);
      var animacao = new DoubleAnimation(rolagem.HorizontalOffset, alvo, TimeSpan.FromMilliseconds(220))
      {
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
      };
      animacao.Completed += (_, _) =>
      {
        if (Target(rolagem) == alvo)
          rolagem.SetValue(TargetProperty, double.NaN);
      };
      rolagem.BeginAnimation(AnimatedOffsetProperty, animacao);
    }

    /// <summary>A aba escolhida inteira à vista, longe das pontas (fora da setinha e do degradê).</summary>
    private static void ShowSelected(ScrollViewer rolagem, Selector abas)
    {
      if (rolagem.Content is not Visual painel || abas.SelectedItem is null
          || abas.ItemContainerGenerator.ContainerFromItem(abas.SelectedItem) is not FrameworkElement aba || !aba.IsVisible)
        return;
      double x;
      try
      {
        x = aba.TransformToAncestor(painel).Transform(new Point()).X;
      }
      catch (InvalidOperationException)
      {
        return;
      }
      var vista = Target(rolagem);
      if (x - Margin < vista)
        ScrollTo(rolagem, x - Margin);
      else if (x + aba.ActualWidth + Margin > vista + rolagem.ViewportWidth)
        ScrollTo(rolagem, x + aba.ActualWidth + Margin - rolagem.ViewportWidth);
    }

    /// <summary>
    /// Quantas abas estão pra cada lado, se a setinha aparece e o degradê da ponta que continua. Conta a aba que está,
    /// mesmo em parte, debaixo da ponta: com tudo à vista menos meia aba, a setinha diz 1.
    /// </summary>
    private static void Update(ScrollViewer rolagem)
    {
      var antes = rolagem.HorizontalOffset > 0.5;
      var depois = rolagem.ScrollableWidth > 0.5 && rolagem.HorizontalOffset < rolagem.ScrollableWidth - 0.5;
      int quantosAntes = 0, quantosDepois = 0;
      if ((antes || depois) && rolagem.Content is Panel painel)
      {
        var largura = rolagem.ViewportWidth;
        foreach (UIElement filho in painel.Children)
        {
          if (filho is not FrameworkElement aba || !aba.IsVisible)
            continue;
          var x = aba.TranslatePoint(new Point(), painel).X - rolagem.HorizontalOffset;
          if (antes && x < Hidden)
            quantosAntes++;
          else if (depois && x + aba.ActualWidth > largura - Hidden)
            quantosDepois++;
        }
      }
      SetHiddenBefore(rolagem, antes ? Math.Max(1, quantosAntes) : 0);
      SetHiddenAfter(rolagem, depois ? Math.Max(1, quantosDepois) : 0);
      SetBeforeVisibility(rolagem, antes ? Visibility.Visible : Visibility.Collapsed);
      SetAfterVisibility(rolagem, depois ? Visibility.Visible : Visibility.Collapsed);

      // O degradê na ponta que continua, em pixels da parte visível (o relativo mede a faixa inteira, rolada)
      var alvo = rolagem.Template?.FindName("PART_ScrollContentPresenter", rolagem) as FrameworkElement ?? rolagem;
      var w = alvo.ActualWidth;
      if (!(antes || depois) || w < (Edge + FadeLength) * 3)
      {
        alvo.OpacityMask = null;
        return;
      }
      var coberta = Edge / w;
      var borda = (Edge + FadeLength) / w;
      var mascara = new LinearGradientBrush
      {
        MappingMode = BrushMappingMode.Absolute,
        StartPoint = new Point(0, 0),
        EndPoint = new Point(w, 0),
      };
      mascara.GradientStops.Add(new GradientStop(antes ? Colors.Transparent : Colors.Black, 0));
      mascara.GradientStops.Add(new GradientStop(antes ? Colors.Transparent : Colors.Black, antes ? coberta : 0));
      mascara.GradientStops.Add(new GradientStop(Colors.Black, antes ? borda : 0));
      mascara.GradientStops.Add(new GradientStop(Colors.Black, depois ? 1 - borda : 1));
      mascara.GradientStops.Add(new GradientStop(depois ? Colors.Transparent : Colors.Black, depois ? 1 - coberta : 1));
      mascara.GradientStops.Add(new GradientStop(depois ? Colors.Transparent : Colors.Black, 1));
      mascara.Freeze();
      alvo.OpacityMask = mascara;
    }
  }
}
