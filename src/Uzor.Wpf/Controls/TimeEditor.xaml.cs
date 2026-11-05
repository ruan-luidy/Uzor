using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// Horário em duas partes (hora e minuto), sem caixa de texto: a parte marcada fica azul e recebe o que se digita.
  /// <list type="bullet">
  /// <item>Dígitos: "1" e "7" dão 17 e pula pros minutos; "9" já vira 09 e pula. Nos minutos, "3" e "0" dão 30.</item>
  /// <item>↑ ↓, a rodinha e as setinhas: hora de 1 em 1, minuto de <see cref="MinuteStep"/> em <see cref="MinuteStep"/>.</item>
  /// <item>← → e ":" trocam de parte; Tab vai da hora pro minuto e depois sai; Backspace zera a parte.</item>
  /// </list>
  /// Só avisa (<see cref="Edited"/>) quando quem usa mexe; trocar o <see cref="Time"/> pelo código não avisa.
  /// </summary>
  public partial class TimeEditor : UserControl
  {
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
      nameof(Time),
      typeof(TimeSpan?),
      typeof(TimeEditor),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((TimeEditor)d).Show()));

    public static readonly DependencyProperty MinuteStepProperty = DependencyProperty.Register(
      nameof(MinuteStep), typeof(int), typeof(TimeEditor), new PropertyMetadata(5));

    public static readonly DependencyProperty FallbackProperty = DependencyProperty.Register(
      nameof(Fallback), typeof(TimeSpan), typeof(TimeEditor), new PropertyMetadata(TimeSpan.Zero));

    private Part _part;

    /// <summary>O primeiro dígito da parte, esperando o segundo (vazio = o próximo dígito começa a parte de novo).</summary>
    private int? _pending;

    public TimeEditor()
    {
      InitializeComponent();
      Show();
    }

    private enum Part
    {
      Hour,
      Minute,
    }

    /// <summary>Quando quem usa muda o horário (digitando, nas setas, na rodinha).</summary>
    public event EventHandler? Edited;

    public TimeSpan? Time
    {
      get => (TimeSpan?)GetValue(TimeProperty);
      set => SetValue(TimeProperty, value);
    }

    /// <summary>De quanto em quanto o minuto anda nas setas e na rodinha (digitando, vai o minuto exato).</summary>
    public int MinuteStep
    {
      get => (int)GetValue(MinuteStepProperty);
      set => SetValue(MinuteStepProperty, value);
    }

    /// <summary>De onde as setas partem quando ainda não tem horário.</summary>
    public TimeSpan Fallback
    {
      get => (TimeSpan)GetValue(FallbackProperty);
      set => SetValue(FallbackProperty, value);
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
      base.OnGotKeyboardFocus(e);
      // Chegou pelo Shift+Tab: começa no minuto, que é a parte "de trás"
      if (e.NewFocus == this && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && e.KeyboardDevice.IsKeyDown(Key.Tab))
        _part = Part.Minute;
      _pending = null;
      Show();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
      base.OnLostKeyboardFocus(e);
      _pending = null;
      Show();
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
      base.OnMouseEnter(e);
      Show();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
      base.OnMouseLeave(e);
      Show();
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
      base.OnPreviewTextInput(e);
      foreach (var c in e.Text)
      {
        if (char.IsAsciiDigit(c))
          Type(c - '0');
        else if (c is ':' or 'h' or 'H')
          Go(Part.Minute);
      }

      e.Handled = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
      base.OnPreviewKeyDown(e);
      switch (e.Key)
      {
        case Key.Up:
          Step(+1);
          break;
        case Key.Down:
          Step(-1);
          break;
        case Key.Left:
          Go(Part.Hour);
          break;
        case Key.Right:
          Go(Part.Minute);
          break;
        case Key.Back or Key.Delete:
          Set(_part == Part.Hour ? 0 : Current.Hours, _part == Part.Minute ? 0 : Current.Minutes);
          break;
        // Tab anda dentro do campo antes de sair dele
        case Key.Tab when !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && _part == Part.Hour:
          Go(Part.Minute);
          break;
        case Key.Tab when Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && _part == Part.Minute:
          Go(Part.Hour);
          break;
        default:
          return;
      }

      e.Handled = true;
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
      base.OnPreviewMouseWheel(e);
      // A rodinha mexe na parte que está embaixo do mouse
      if (MinutePart.IsMouseOver)
        _part = Part.Minute;
      else if (HourPart.IsMouseOver)
        _part = Part.Hour;
      Step(e.Delta > 0 ? +1 : -1);
      e.Handled = true;
    }

    private TimeSpan Current => Time ?? Fallback;

    private void Type(int digit)
    {
      if (_part == Part.Hour)
      {
        if (_pending is { } first && first * 10 + digit < 24)
        {
          Set(first * 10 + digit, Current.Minutes);
          Go(Part.Minute);
        }
        else
        {
          Set(digit, Current.Minutes);
          // Hora começando em 3 ou mais não tem segundo dígito: já pula
          if (digit > 2)
            Go(Part.Minute);
          else
            _pending = digit;
        }

        return;
      }

      if (_pending is { } tens)
      {
        Set(Current.Hours, tens * 10 + digit);
        _pending = null;
      }
      else
      {
        Set(Current.Hours, digit);
        _pending = digit > 5 ? null : digit;
      }
    }

    private void Step(int direction)
    {
      _pending = null;
      var now = Current;
      if (_part == Part.Hour)
      {
        Set((now.Hours + direction + 24) % 24, now.Minutes);
        return;
      }

      // Minuto na grade do passo: 17:32 sobe pra 17:35 e desce pra 17:30
      var step = Math.Max(1, MinuteStep);
      var minutes = direction > 0
        ? (now.Minutes / step + 1) * step
        : now.Minutes % step == 0 ? now.Minutes - step : now.Minutes - now.Minutes % step;
      Set(now.Hours, (minutes + 60) % 60);
    }

    private void Set(int hours, int minutes)
    {
      Time = new TimeSpan(hours, minutes, 0);
      Edited?.Invoke(this, EventArgs.Empty);
    }

    private void Go(Part part)
    {
      _part = part;
      _pending = null;
      Show();
    }

    private void Show()
    {
      var time = Time;
      HourText.Text = time is { } t ? t.Hours.ToString("00", CultureInfo.InvariantCulture) : "--";
      MinuteText.Text = time is { } m ? m.Minutes.ToString("00", CultureInfo.InvariantCulture) : "--";

      // Com foco, a parte que recebe o que se digita fica no azul, como texto selecionado
      var focused = IsKeyboardFocusWithin;
      Paint(HourPart, HourText, focused && _part == Part.Hour, time is null);
      Paint(MinutePart, MinuteText, focused && _part == Part.Minute, time is null);
      Box.SetResourceReference(Border.BackgroundProperty, focused || IsMouseOver ? "Button.HoverBrush" : "Button.BackgroundBrush");
    }

    private static void Paint(Border part, TextBlock text, bool active, bool empty)
    {
      if (active)
      {
        part.SetResourceReference(Border.BackgroundProperty, "PrimaryBrush");
        text.Foreground = Brushes.White;
      }
      else
      {
        part.Background = Brushes.Transparent;
        text.SetResourceReference(TextBlock.ForegroundProperty, empty ? "SecondaryTextBrush" : "PrimaryTextBrush");
      }
    }

    private void HourPart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Pick(Part.Hour, e);

    private void MinutePart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Pick(Part.Minute, e);

    private void Box_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      Focus();
      e.Handled = true;
    }

    private void Pick(Part part, MouseButtonEventArgs e)
    {
      _part = part;
      _pending = null;
      Focus();
      Show();
      e.Handled = true;
    }

    private void Up_Click(object sender, RoutedEventArgs e)
    {
      Focus();
      Step(+1);
    }

    private void Down_Click(object sender, RoutedEventArgs e)
    {
      Focus();
      Step(-1);
    }
  }
}
