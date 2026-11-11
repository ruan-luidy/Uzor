using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uzor.Wpf.Icons;
using MahApps.Metro.IconPacks;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// Um aviso passageiro (o toast do rnzNegativeObserver): círculo no tom, título, mensagem, X e, se tiver, um botão
  /// de ação. Some sozinho depois de uns segundos, e a linha fina no pé mostra quanto falta - o relógio para com o
  /// mouse em cima. Pode trocar de conteúdo sem sair da tela ("processando" virando "pronto"). Quem cria, empilha e
  /// remove é o <see cref="Services.ToastService"/>.
  /// </summary>
  public partial class Toast : UserControl
  {
    private Storyboard? _life;
    private Action? _action;
    private bool _shown;

    public Toast()
    {
      InitializeComponent();
      MouseEnter += (_, _) => _life?.Pause(this);
      MouseLeave += (_, _) => _life?.Resume(this);
    }

    /// <summary>Terminou de sair (o host tira da tela).</summary>
    public event EventHandler? Closed;

    /// <summary>Já está saindo (o X, o tempo, ou o host tirou pra dar lugar): trocar o conteúdo não adianta mais.</summary>
    public bool IsClosing { get; private set; }

    /// <param name="seconds">Quanto fica na tela; null = até o usuário fechar.</param>
    /// <param name="icon">Outro ícone no lugar do padrão do tom (a seta da atualização, por exemplo).</param>
    public void Show(DialogKind kind, string title, string message, TimeSpan? seconds, string? actionText = null,
      Action? action = null, PackIconPhosphorIconsKind? icon = null)
    {
      // Os ícones do Negativo, com as cores do chip de status do Uzor (fundo apagado, ícone cheio)
      var (standard, tone) = kind switch
      {
        DialogKind.Success => (PackIconPhosphorIconsKind.CheckCircleFill, "Success"),
        DialogKind.Warning => (PackIconPhosphorIconsKind.WarningFill, "Warning"),
        DialogKind.Error => (PackIconPhosphorIconsKind.WarningCircleFill, "Danger"),
        _ => (PackIconPhosphorIconsKind.InfoFill, "Accent"),
      };
      Tone(tone);
      IconPath.Data = PhosphorExtension.Get(icon ?? standard);
      IconPath.Visibility = Visibility.Visible;
      BusyDots.Visibility = Visibility.Collapsed;
      Text(title, message);

      // Botão só pra ação de verdade (Reiniciar...), e em destaque. O "OK" automático do erro saiu em 05/11/2026: ele só
      // fechava, que é o que o X já faz, e deixava o cartão do erro com o dobro da altura dos outros
      _action = action;
      ActionsBar.Visibility = actionText is null ? Visibility.Collapsed : Visibility.Visible;
      ActionText.Text = actionText;
      ActionButton.SetResourceReference(StyleProperty, action is null ? "ButtonDefault" : "ButtonPrimary");

      Present();
      Live(seconds);
    }

    /// <summary>Algo rodando: as bolinhas no lugar do ícone e sem prazo - fica até virar o resultado.</summary>
    public void ShowBusy(string title, string message)
    {
      Tone("Accent");
      IconPath.Visibility = Visibility.Collapsed;
      BusyDots.Visibility = Visibility.Visible;
      Text(title, message);
      _action = null;
      ActionsBar.Visibility = Visibility.Collapsed;

      Present();
      Live(null);
    }

    public void Hide()
    {
      if (IsClosing)
        return;

      // A linha do tempo CONGELA onde estava enquanto o cartão sai (11/11/2026, "quando clico pra fechar a barrinha
      // volta pra 100 e daí fecha"): parar o relógio (Stop) devolve a escala ao valor de base, a linha cheia. Pausado,
      // ele segura o valor; e sem a referência o fim dele não chama o Hide de novo nem o mouse o retoma.
      IsClosing = true;
      _life?.Pause(this);
      _life = null;
      var slideOut = ((Storyboard)Resources["SlideOut"]).Clone();
      slideOut.Completed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
      slideOut.Begin(this);
    }

    /// <summary>Sai sem animação (o host tirou da pilha pra dar lugar a um novo).</summary>
    internal void Detach()
    {
      IsClosing = true;
      _life?.Stop(this);
      _life = null;
    }

    private void Tone(string tone)
    {
      IconDisc.SetResourceReference(Border.BackgroundProperty, tone + ".SoftBrush");
      IconPath.SetResourceReference(Shape.FillProperty, tone + ".TextBrush");
      BusyDots.SetResourceReference(ForegroundProperty, tone + ".TextBrush");
      LifeBar.SetResourceReference(Border.BackgroundProperty, tone + ".TextBrush");
    }

    private void Text(string title, string message)
    {
      TitleText.Text = title;
      MessageText.Text = message;
      MessageText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>A primeira vez o cartão entra descendo; depois só o conteúdo acende, no mesmo lugar.</summary>
    private void Present()
    {
      if (_shown)
      {
        ((Storyboard)Resources["Swap"]).Begin(this);
        return;
      }

      _shown = true;
      ((Storyboard)Resources["SlideIn"]).Begin(this);
    }

    /// <summary>O relógio do toast é a própria linha do pé encolhendo: no fim dela, ele sai.</summary>
    private void Live(TimeSpan? seconds)
    {
      _life?.Stop(this);
      _life = null;

      if (seconds is not { } lifetime)
      {
        LifeTrack.Visibility = Visibility.Collapsed;
        return;
      }

      LifeTrack.Visibility = Visibility.Visible;
      var shrink = new DoubleAnimation(1, 0, new Duration(lifetime));
      Storyboard.SetTarget(shrink, LifeBar);
      Storyboard.SetTargetProperty(shrink, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

      var life = new Storyboard();
      life.Children.Add(shrink);
      life.Completed += (_, _) =>
      {
        if (ReferenceEquals(_life, life))
          Hide();
      };
      _life = life;
      life.Begin(this, isControllable: true);
      if (IsMouseOver)
        life.Pause(this);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    private void Action_Click(object sender, RoutedEventArgs e)
    {
      _action?.Invoke();
      Hide();
    }
  }
}
