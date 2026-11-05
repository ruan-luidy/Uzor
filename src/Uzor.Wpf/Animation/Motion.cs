using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Uzor.Wpf.Animation
{
  /// <summary>
  /// Os movimentos curtos do Otmetka: entrada em cascata, tremida de erro, pulo do sucesso, inchada de aviso e
  /// deslize de troca de página.
  /// </summary>
  public static class Motion
  {
    private const int StaggerMs = 28;
    private const int StaggerCap = 12;

    public static readonly DependencyProperty StaggerItemsProperty = DependencyProperty.RegisterAttached(
      "StaggerItems", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnStaggerItemsChanged));

    /// <summary>
    /// Herdada: com ela ligada os movimentos vão direto pro fim. O cartão liga enquanto está fechado, senão a foto do
    /// gênio pega a página no meio do deslize e a tela pula quando ele termina.
    /// </summary>
    public static readonly DependencyProperty QuietProperty = DependencyProperty.RegisterAttached(
      "Quiet", typeof(bool), typeof(Motion), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetQuiet(DependencyObject element) => (bool)element.GetValue(QuietProperty);

    public static void SetQuiet(DependencyObject element, bool value) => element.SetValue(QuietProperty, value);

    public static bool GetStaggerItems(DependencyObject element) => (bool)element.GetValue(StaggerItemsProperty);

    public static void SetStaggerItems(DependencyObject element, bool value) => element.SetValue(StaggerItemsProperty, value);

    /// <summary>Quando vira true, o elemento dá o Pop (o visto do upload liberado, a célula aprovada).</summary>
    public static readonly DependencyProperty PopWhenProperty = DependencyProperty.RegisterAttached(
      "PopWhen", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) =>
      {
        if (d is UIElement element && e.NewValue is true && !GetQuiet(element))
          Pop(element);
      }));

    public static bool GetPopWhen(DependencyObject element) => (bool)element.GetValue(PopWhenProperty);

    public static void SetPopWhen(DependencyObject element, bool value) => element.SetValue(PopWhenProperty, value);

    /// <summary>Sobe 10 px e aparece, depois de <paramref name="delayMs"/>.</summary>
    public static void RiseIn(UIElement element, int delayMs = 0, double distance = 10)
    {
      if (GetQuiet(element))
      {
        Settle(element);
        return;
      }

      var move = new TranslateTransform(0, distance);
      element.RenderTransform = move;
      element.Opacity = 0;

      var begin = TimeSpan.FromMilliseconds(delayMs);
      var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
      move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(distance, 0, TimeSpan.FromMilliseconds(260)) { BeginTime = begin, EasingFunction = ease });
      element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { BeginTime = begin });
    }

    /// <summary>Balança pros lados, como quem diz não.</summary>
    public static void Shake(UIElement element)
    {
      var move = element.RenderTransform as TranslateTransform ?? new TranslateTransform();
      element.RenderTransform = move;

      var shake = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(380) };
      double[] offsets = [0, -9, 8, -6, 4, -2, 0];
      for (var i = 0; i < offsets.Length; i++)
        shake.KeyFrames.Add(new EasingDoubleKeyFrame(offsets[i], KeyTime.FromPercent((double)i / (offsets.Length - 1))));
      move.BeginAnimation(TranslateTransform.XProperty, shake);
    }

    /// <summary>Cresce passando do ponto e volta: o visto do apontamento que entrou.</summary>
    public static void Pop(UIElement element)
    {
      var scale = new ScaleTransform(0.4, 0.4);
      element.RenderTransformOrigin = new Point(0.5, 0.5);
      element.RenderTransform = scale;

      var grow = new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(420)) { EasingFunction = new BackEase { Amplitude = 0.6, EasingMode = EasingMode.EaseOut } };
      scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
      scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
      element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
    }

    /// <summary>Incha um pouco e volta: a alça avisando que a batida mudou.</summary>
    public static void Bump(UIElement element, Point origin)
    {
      var scale = new ScaleTransform(1, 1);
      element.RenderTransformOrigin = origin;
      element.RenderTransform = scale;

      var bump = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(520) };
      bump.KeyFrames.Add(new EasingDoubleKeyFrame(1.1, KeyTime.FromPercent(0.35), new CubicEase { EasingMode = EasingMode.EaseOut }));
      bump.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromPercent(1), new ElasticEase { Oscillations = 1, Springiness = 4, EasingMode = EasingMode.EaseOut }));
      scale.BeginAnimation(ScaleTransform.ScaleXProperty, bump);
      scale.BeginAnimation(ScaleTransform.ScaleYProperty, bump);
    }

    /// <summary>Entra deslizando de lado (troca de página do painel).</summary>
    public static void SlideIn(UIElement element, double fromX)
    {
      if (GetQuiet(element))
      {
        Settle(element);
        return;
      }

      var move = new TranslateTransform(fromX, 0);
      element.RenderTransform = move;
      move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(fromX, 0, TimeSpan.FromMilliseconds(280)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
      element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
    }

    /// <summary>
    /// Herdada, ligada por padrão: o "aperto" do rnzNegativeObserver no clique. Linhas de lista desligam (encolher a
    /// linha inteira fica estranho).
    /// </summary>
    public static readonly DependencyProperty PressFeedbackProperty = DependencyProperty.RegisterAttached(
      "PressFeedback", typeof(bool), typeof(Motion), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetPressFeedback(DependencyObject element) => (bool)element.GetValue(PressFeedbackProperty);

    public static void SetPressFeedback(DependencyObject element, bool value) => element.SetValue(PressFeedbackProperty, value);

    private static bool _pressRegistered;

    /// <summary>
    /// Clique de botão do rnzNegativeObserver em TODO botão do app, num lugar só: encolhe pra 95% e esmaece em
    /// 100 ms ao apertar, volta ao soltar ou quando o mouse sai. Lá foi estilo por estilo; aqui é handler de classe.
    /// </summary>
    public static void EnablePressFeedback()
    {
      if (_pressRegistered)
        return;
      _pressRegistered = true;
      EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler((s, _) => Press((ButtonBase)s, true)), true);
      EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((s, _) => Press((ButtonBase)s, false)), true);
      EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.MouseLeaveEvent, new MouseEventHandler((s, _) => Press((ButtonBase)s, false)), true);
    }

    private static void Press(ButtonBase button, bool down)
    {
      if (!GetPressFeedback(button) || !button.IsEnabled)
        return;

      // só mexe se ninguém mais usa o RenderTransform (o Bump e o RiseIn trocam por um próprio)
      if (button.RenderTransform is not ScaleTransform { IsFrozen: false } scale)
      {
        if (button.RenderTransform is not null && button.RenderTransform != Transform.Identity && button.RenderTransform is not MatrixTransform { Matrix.IsIdentity: true })
          return;
        if (!down)
          return;
        scale = new ScaleTransform(1, 1);
        button.RenderTransformOrigin = new Point(0.5, 0.5);
        button.RenderTransform = scale;
      }

      var to = down ? 0.95 : 1.0;
      var time = TimeSpan.FromMilliseconds(down ? 100 : 160);
      var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
      scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(to, time) { EasingFunction = ease });
      scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(to, time) { EasingFunction = ease });

      if (down)
      {
        button.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.7, time));
        return;
      }
      // na volta solta a animação no fim: segurar o 1 por cima apagaria o "desabilitado" dos estilos
      var back = new DoubleAnimation(1, time);
      back.Completed += (_, _) => button.BeginAnimation(UIElement.OpacityProperty, null);
      button.BeginAnimation(UIElement.OpacityProperty, back);
    }

    private static void Settle(UIElement element)
    {
      element.BeginAnimation(UIElement.OpacityProperty, null);
      element.Opacity = 1;
      element.RenderTransform = Transform.Identity;
    }

    private static void OnStaggerItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not ItemsControl list || e.NewValue is not true)
        return;

      // A cascata é a entrada da lista, uma vez: depois da primeira leva com linhas, recarga (a lista refeita), linha
      // nova e a tabela gerando linha na rolagem aparecem direto. Repetir a cascata a cada recarga fazia a tela piscar.
      var played = false;
      list.ItemContainerGenerator.StatusChanged += (_, _) =>
      {
        if (played || list.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
          return;

        var entering = 0;
        for (var i = 0; i < list.Items.Count; i++)
        {
          if (list.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
            RiseIn(container, Math.Min(entering++, StaggerCap) * StaggerMs);
        }

        played = entering > 0;
      };
    }
  }
}
