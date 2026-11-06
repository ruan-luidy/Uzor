using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// A barra de quanto já foi (separado, feito do previsto, carga, consumo) com o número ao lado: trilho fino, o
  /// preenchimento na cor CHEIA do tom e o texto fora da barra, no tom de texto do tema. É o "Feito do previsto" da
  /// Gerencial virado controle. A barra do HandyControl escrevia o "88 %" branco por cima do âmbar e tinha um brilho que
  /// corria sem parar (o PART_GlowRect, que o ProgressBar do WPF anima sozinho); aqui não tem nem um nem outro.
  /// <para>O preenchimento é reto e o trilho recorta (CornerClip.Pill): com valor pequeno sai uma lasca na ponta
  /// redonda, e não o canto do preenchimento espremendo ele numa elipse.</para>
  ///   &lt;uzor:MeterBar Value="{Binding CompletionPercentage}" Tone="{Binding ProgressLevel}" /&gt;
  ///   &lt;uzor:MeterBar Maximum="1" Value="{Binding DoneShare}" Text="{Binding DonePercentText}" /&gt;
  /// O visual mora no Kit.xaml.
  /// </summary>
  [TemplatePart(Name = "PART_Track", Type = typeof(FrameworkElement))]
  [TemplatePart(Name = "PART_Fill", Type = typeof(FrameworkElement))]
  [TemplatePart(Name = "PART_Target", Type = typeof(FrameworkElement))]
  public class MeterBar : Control
  {
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
      nameof(Value), typeof(double), typeof(MeterBar), new PropertyMetadata(0d, OnValuesChanged));

    /// <summary>O cheio. 100 pra porcentagem (o padrão), 1 pra fração.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
      nameof(Maximum), typeof(double), typeof(MeterBar), new PropertyMetadata(100d, OnValuesChanged));

    /// <summary>O tom: a cor cheia do preenchimento e o tom de texto do número. Accent é o azul.</summary>
    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(
      nameof(Tone), typeof(Tone), typeof(MeterBar), new PropertyMetadata(Tone.Accent));

    /// <summary>O texto ao lado. Sem ele sai a porcentagem de Value sobre Maximum ("88%").</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text), typeof(string), typeof(MeterBar), new PropertyMetadata(null, OnValuesChanged));

    public static readonly DependencyProperty ShowTextProperty = DependencyProperty.Register(
      nameof(ShowText), typeof(bool), typeof(MeterBar), new PropertyMetadata(true));

    public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
      nameof(BarHeight), typeof(double), typeof(MeterBar), new PropertyMetadata(8d, OnValuesChanged));

    /// <summary>Largura mínima do texto: as barras da coluna ficam do mesmo tamanho com "5%" e "100%".</summary>
    public static readonly DependencyProperty TextWidthProperty = DependencyProperty.Register(
      nameof(TextWidth), typeof(double), typeof(MeterBar), new PropertyMetadata(36d));

    /// <summary>A meta, na mesma régua do Value (ex.: 100 de 120): um traço em pé em cima da barra. NaN = sem meta.</summary>
    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
      nameof(Target), typeof(double), typeof(MeterBar), new PropertyMetadata(double.NaN, OnValuesChanged));

    /// <summary>A cor do preenchimento. O estilo põe a do tom; dá pra trocar direto.</summary>
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
      nameof(Fill), typeof(Brush), typeof(MeterBar), new PropertyMetadata(null));

    private static readonly DependencyPropertyKey FractionKey = DependencyProperty.RegisterReadOnly(
      nameof(Fraction), typeof(double), typeof(MeterBar), new PropertyMetadata(0d));

    public static readonly DependencyProperty FractionProperty = FractionKey.DependencyProperty;

    private static readonly DependencyPropertyKey DisplayTextKey = DependencyProperty.RegisterReadOnly(
      nameof(DisplayText), typeof(string), typeof(MeterBar), new PropertyMetadata("0%"));

    public static readonly DependencyProperty DisplayTextProperty = DisplayTextKey.DependencyProperty;

    private FrameworkElement? _track;
    private FrameworkElement? _fill;
    private FrameworkElement? _target;

    static MeterBar()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(MeterBar), new FrameworkPropertyMetadata(typeof(MeterBar)));
    }

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public Tone Tone { get => (Tone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public bool ShowText { get => (bool)GetValue(ShowTextProperty); set => SetValue(ShowTextProperty, value); }

    public double BarHeight { get => (double)GetValue(BarHeightProperty); set => SetValue(BarHeightProperty, value); }

    public double TextWidth { get => (double)GetValue(TextWidthProperty); set => SetValue(TextWidthProperty, value); }

    public double Target { get => (double)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>Quanto da barra está cheio, de 0 a 1.</summary>
    public double Fraction => (double)GetValue(FractionProperty);

    /// <summary>O que aparece ao lado: o Text, ou a porcentagem.</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    /// <summary>De 0 a 1. Passar do cheio enche a barra (o número ao lado diz quanto passou); sem cheio, vazia.</summary>
    public static double FractionOf(double value, double maximum) =>
      maximum <= 0 || double.IsNaN(value) || double.IsNaN(maximum) ? 0 : Math.Clamp(value / maximum, 0, 1);

    /// <summary>"88%": a porcentagem sem casa, sem espaço antes do %, sem teto (125% é 125%).</summary>
    public static string PercentText(double value, double maximum, CultureInfo? culture = null) =>
      maximum <= 0 || double.IsNaN(value) || double.IsNaN(maximum)
        ? "-"
        : (value / maximum * 100).ToString("F0", culture ?? CultureInfo.CurrentCulture) + "%";

    public override void OnApplyTemplate()
    {
      if (_track is not null)
        _track.SizeChanged -= OnTrackSizeChanged;

      base.OnApplyTemplate();
      _track = GetTemplateChild("PART_Track") as FrameworkElement;
      _fill = GetTemplateChild("PART_Fill") as FrameworkElement;
      _target = GetTemplateChild("PART_Target") as FrameworkElement;

      if (_track is not null)
        _track.SizeChanged += OnTrackSizeChanged;
      Refresh();
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((MeterBar)d).Refresh();

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e) => Layout();

    private void Refresh()
    {
      SetValue(FractionKey, FractionOf(Value, Maximum));
      SetValue(DisplayTextKey, Text ?? PercentText(Value, Maximum));
      Layout();
    }

    /// <summary>O preenchimento e o traço da meta vão por largura: a barra mede a coluna, não um número fixo.</summary>
    private void Layout()
    {
      var width = _track?.ActualWidth ?? 0;
      if (_fill is not null)
        _fill.Width = Math.Round(width * Fraction);

      if (_target is null)
        return;
      if (double.IsNaN(Target) || width <= 0)
      {
        _target.Visibility = Visibility.Collapsed;
        return;
      }
      _target.Visibility = Visibility.Visible;
      _target.Height = BarHeight + 6;
      _target.Margin = new Thickness(Math.Max(0, Math.Round(width * FractionOf(Target, Maximum)) - 1), 0, 0, 0);
    }
  }
}
