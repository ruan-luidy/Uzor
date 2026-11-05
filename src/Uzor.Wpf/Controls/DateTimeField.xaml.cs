using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// Data e hora (o prazo de resposta): a caixa fechada igual à do <see cref="SelectField"/> e, aberta, o calendário em
  /// português com o horário em 24 h embaixo (no <see cref="TimeEditor"/> ou num atalho). No lugar do DateTimePicker do
  /// HandyControl, que vinha em inglês, com relógio de ponteiro e AM/PM.
  /// </summary>
  public partial class DateTimeField : UserControl
  {
    public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register(
      nameof(SelectedDateTime),
      typeof(DateTime?),
      typeof(DateTimeField),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLookChanged));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
      nameof(Placeholder), typeof(string), typeof(DateTimeField), new PropertyMetadata(Texts.PickDate, OnLookChanged));

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Os horários de sempre do expediente: começo, almoço, fim do expediente.</summary>
    private static readonly TimeSpan[] Shortcuts = [new(8, 0, 0), new(12, 0, 0), new(17, 0, 0), new(18, 0, 0)];

    /// <summary>Sem nada escolhido, a data nova entra no fim do expediente.</summary>
    private static readonly TimeSpan DefaultTime = new(17, 0, 0);

    /// <summary>Folga entre a caixa e o cartão, e a margem da sombra (PopupShadowRoom), como no SelectField.</summary>
    private const double Gap = 4;

    private static readonly Thickness ShadowRoom = new(12, 10, 12, 16);

    private bool _syncing;

    public DateTimeField()
    {
      InitializeComponent();
      DropDown.CustomPopupPlacementCallback = Place;
      TimeBox.Fallback = DefaultTime;
      foreach (var time in Shortcuts)
      {
        var chip = new RadioButton { Content = time.ToString(@"hh\:mm", PtBr), Tag = time, Style = (Style)Resources["TimeChip"], GroupName = "Times" + GetHashCode() };
        chip.Checked += (_, _) =>
        {
          if (!_syncing)
            Apply(SelectedDateTime?.Date ?? DateTime.Today, (TimeSpan)chip.Tag);
        };
        Chips.Children.Add(chip);
      }
      Show();
    }

    public DateTime? SelectedDateTime
    {
      get => (DateTime?)GetValue(SelectedDateTimeProperty);
      set => SetValue(SelectedDateTimeProperty, value);
    }

    /// <summary>O que aparece na caixa enquanto nada está escolhido.</summary>
    public string Placeholder
    {
      get => (string)GetValue(PlaceholderProperty);
      set => SetValue(PlaceholderProperty, value);
    }

    private static void OnLookChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DateTimeField)d).Show();

    private void Show()
    {
      var value = SelectedDateTime;
      Label.Text = value is { } v ? v.ToString("dd/MM/yyyy  HH:mm", PtBr) : Placeholder;
      Label.SetResourceReference(TextBlock.ForegroundProperty, value is null ? "SecondaryTextBrush" : "PrimaryTextBrush");

      _syncing = true;
      Picker.SelectedDate = value?.Date;
      if (value is { } shown)
        Picker.DisplayDate = shown.Date;
      TimeBox.Time = value?.TimeOfDay;
      foreach (RadioButton chip in Chips.Children)
        chip.IsChecked = value is { } t && t.TimeOfDay == (TimeSpan)chip.Tag;
      _syncing = false;
    }

    private void Apply(DateTime date, TimeSpan time) => SelectedDateTime = date.Date + time;

    private void Picker_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
      if (_syncing || Picker.SelectedDate is not { } date)
        return;
      Apply(date, SelectedDateTime?.TimeOfDay ?? TimeBox.Time ?? DefaultTime);
    }

    // O Calendar segura o mouse depois do clique num dia: sem soltar, o próximo clique fora dele não pega
    private void Picker_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
      if (Mouse.Captured is CalendarItem or CalendarDayButton)
        Mouse.Capture(null);
    }

    // Mexeu no horário: vale na data escolhida (ou hoje, se ainda não tem data)
    private void TimeBox_Edited(object? sender, EventArgs e)
    {
      if (TimeBox.Time is { } time)
        Apply(SelectedDateTime?.Date ?? Picker.SelectedDate ?? DateTime.Today, time);
    }

    private void Today_Click(object sender, RoutedEventArgs e) =>
      Apply(DateTime.Today, SelectedDateTime?.TimeOfDay ?? DefaultTime);

    private void Done_Click(object sender, RoutedEventArgs e) => Toggle.IsChecked = false;

    private void DropDown_Closed(object? sender, EventArgs e) => Show();

    /// <summary>O zoom e o fade do menu de botão direito, a cada vez que abre (o mesmo do SelectField).</summary>
    private void DropDown_Opened(object? sender, EventArgs e)
    {
      Show();
      var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
      var duration = TimeSpan.FromMilliseconds(130);
      var scale = (ScaleTransform)Card.RenderTransform;
      scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
      scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
      Card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration));
    }

    /// <summary>Embaixo da caixa; sem espaço na tela, em cima. Alinhado à esquerda, descontando a margem da sombra.</summary>
    private CustomPopupPlacement[] Place(Size popup, Size target, Point offset)
    {
      var scale = VisualTreeHelper.GetDpi(this).DpiScaleY;
      var x = -ShadowRoom.Left * scale;
      return
      [
        new CustomPopupPlacement(new Point(x, target.Height + (Gap - ShadowRoom.Top) * scale), PopupPrimaryAxis.Horizontal),
        new CustomPopupPlacement(new Point(x, -popup.Height + (ShadowRoom.Bottom - Gap) * scale), PopupPrimaryAxis.Horizontal),
      ];
    }
  }
}
