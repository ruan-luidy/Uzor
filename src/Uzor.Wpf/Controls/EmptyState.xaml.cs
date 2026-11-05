using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Uzor.Wpf.Animation;

namespace Uzor.Wpf.Controls
{
  /// <summary>Estado vazio de painel: ícone + título + recado + ação opcional. O visual está no XAML.</summary>
  public partial class EmptyState : UserControl
  {
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
      nameof(Icon), typeof(Geometry), typeof(EmptyState), new PropertyMetadata(null, OnPartChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
      nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata(null, OnPartChanged));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text), typeof(string), typeof(EmptyState), new PropertyMetadata(null, OnPartChanged));

    public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register(
      nameof(ActionText), typeof(string), typeof(EmptyState));

    public static readonly DependencyProperty ActionCommandProperty = DependencyProperty.Register(
      nameof(ActionCommand), typeof(ICommand), typeof(EmptyState));

    public EmptyState()
    {
      InitializeComponent();
      UpdateParts();
      // Aparece subindo, como os passos do Otmetka
      IsVisibleChanged += (_, e) =>
      {
        if (e.NewValue is true)
          Motion.RiseIn(this, 60, 8);
      };
    }

    public Geometry? Icon
    {
      get => (Geometry?)GetValue(IconProperty);
      set => SetValue(IconProperty, value);
    }

    public string? Title
    {
      get => (string?)GetValue(TitleProperty);
      set => SetValue(TitleProperty, value);
    }

    public string? Text
    {
      get => (string?)GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    /// <summary>Rótulo do botão de ação; sem ele o botão não aparece.</summary>
    public string? ActionText
    {
      get => (string?)GetValue(ActionTextProperty);
      set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
      get => (ICommand?)GetValue(ActionCommandProperty);
      set => SetValue(ActionCommandProperty, value);
    }

    private static void OnPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).UpdateParts();

    // O que está vazio não ocupa lugar (sem título, o recado sobe pra perto do ícone)
    private void UpdateParts()
    {
      IconTile.Visibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
      TitleText.Visibility = string.IsNullOrEmpty(Title) ? Visibility.Collapsed : Visibility.Visible;
      BodyText.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;
    }
  }
}
