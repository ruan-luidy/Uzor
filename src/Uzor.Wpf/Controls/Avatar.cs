using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Uzor.Wpf.Controls
{
  /// <summary>
  /// O avatar de uma pessoa (o do Negativo, 16/11/2026 também do Windfinder): a variante "beam" do <see href="https://github.com/boringdesigners/boring-avatars">Boring
  /// Avatars</see> (MIT), uma carinha numa forma girada sobre um fundo, nas cinco cores da paleta deles (10/11/2026, no
  /// lugar do blobatar: "trocar nosso sistema de avatar por esse, variante beam").
  ///
  /// <para>O desenho sai só do nome, calculado aqui: sem rede, sem cache em disco e sem as iniciais de espera que o
  /// blobatar precisava. Mesmo nome, mesmo desenho, em toda máquina. O nome entra sem espaço nas pontas e em minúsculas,
  /// pra "Ruan" do comentário e "ruan" da atividade serem a mesma cara.</para>
  /// </summary>
  public sealed class Avatar : Control
  {
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
      nameof(Id), typeof(string), typeof(Avatar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>De quem é o avatar (o login ou o nome).</summary>
    public string? Id
    {
      get => (string?)GetValue(IdProperty);
      set => SetValue(IdProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
      var lado = Math.Min(ActualWidth, ActualHeight);
      if (lado <= 0) return;

      var raio = lado / 2;
      var centro = new Point(ActualWidth / 2, ActualHeight / 2);

      // Sem nome: só o círculo no fundo de quem hospeda (o lugar não fica vazio nem pisca)
      if (string.IsNullOrWhiteSpace(Id))
      {
        if (Background is { } fundo) drawingContext.DrawEllipse(fundo, null, centro, raio, raio);
        return;
      }

      drawingContext.PushClip(new EllipseGeometry(centro, raio, raio));
      var escala = lado / BeamAvatar.Lado;
      drawingContext.PushTransform(new TranslateTransform(centro.X - raio, centro.Y - raio));
      drawingContext.PushTransform(new ScaleTransform(escala, escala));
      drawingContext.DrawDrawing(BeamAvatar.Desenho(Id));
      drawingContext.Pop();
      drawingContext.Pop();
      drawingContext.Pop();
    }
  }

  /// <summary>
  /// A conta e o desenho da variante beam, portados de <c>src/lib/components/avatar-beam.tsx</c> e
  /// <c>src/lib/utilities.ts</c> do boring-avatars. As contas seguem o JavaScript ao pé da letra (o hash em 32 bits, o
  /// resto e os dígitos em double), pra o mesmo nome dar o mesmo desenho que no site deles.
  /// </summary>
  public static class BeamAvatar
  {
    /// <summary>O lado do desenho original (o viewBox do SVG).</summary>
    public const double Lado = 36;

    /// <summary>A paleta padrão do boring-avatars.</summary>
    public static readonly IReadOnlyList<string> Paleta = ["#92A1C6", "#146A7C", "#F0AB3D", "#C271B4", "#C20D90"];

    private static readonly ConcurrentDictionary<string, DrawingGroup> Desenhos = new(StringComparer.Ordinal);

    /// <summary>O que sai do nome: cores, deslocamentos e giros da forma e da cara.</summary>
    public sealed record Dados(
      string WrapperColor, string FaceColor, string BackgroundColor,
      double WrapperTranslateX, double WrapperTranslateY, double WrapperRotate, double WrapperScale,
      bool IsMouthOpen, bool IsCircle, double EyeSpread, double MouthSpread,
      double FaceRotate, double FaceTranslateX, double FaceTranslateY);

    /// <summary>O nome como o avatar enxerga: sem espaço nas pontas e em minúsculas.</summary>
    public static string Normalizar(string? nome) => (nome ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>O desenho da pessoa, montado uma vez e congelado (vale pra qualquer thread e o WPF guarda).</summary>
    public static DrawingGroup Desenho(string? nome) => Desenhos.GetOrAdd(Normalizar(nome), Montar);

    public static Dados Gerar(string nome)
    {
      var n = HashCode(nome);
      var range = Paleta.Count;
      var wrapperColor = Paleta[(int)(n % range)];
      var preX = Unit(n, 10, 1);
      var tx = preX < 5 ? preX + Lado / 9 : preX;
      var preY = Unit(n, 10, 2);
      var ty = preY < 5 ? preY + Lado / 9 : preY;

      return new Dados(
        WrapperColor: wrapperColor,
        FaceColor: Contraste(wrapperColor),
        BackgroundColor: Paleta[(int)((n + 13) % range)],
        WrapperTranslateX: tx,
        WrapperTranslateY: ty,
        WrapperRotate: Unit(n, 360),
        WrapperScale: 1 + Unit(n, Lado / 12) / 10,
        IsMouthOpen: Boolean(n, 2),
        IsCircle: Boolean(n, 1),
        EyeSpread: Unit(n, 5),
        MouthSpread: Unit(n, 3),
        FaceRotate: Unit(n, 10, 3),
        FaceTranslateX: tx > Lado / 6 ? tx / 2 : Unit(n, 8, 1),
        FaceTranslateY: ty > Lado / 6 ? ty / 2 : Unit(n, 7, 2));
    }

    private static DrawingGroup Montar(string nome)
    {
      var d = Gerar(nome);
      var grupo = new DrawingGroup();

      // O fundo
      grupo.Children.Add(new GeometryDrawing(Pincel(d.BackgroundColor), null, new RectangleGeometry(new Rect(0, 0, Lado, Lado))));

      // A forma: no SVG "translate rotate(r 18 18) scale(s)", que vale da direita pra esquerda; no WPF o grupo aplica na
      // ordem em que os filhos entram, então entra ao contrário
      var cantos = d.IsCircle ? Lado / 2 : Lado / 6;
      var forma = new RectangleGeometry(new Rect(0, 0, Lado, Lado), cantos, cantos)
      {
        Transform = new TransformGroup
        {
          Children =
          {
            new ScaleTransform(d.WrapperScale, d.WrapperScale),
            new RotateTransform(d.WrapperRotate, Lado / 2, Lado / 2),
            new TranslateTransform(d.WrapperTranslateX, d.WrapperTranslateY),
          },
        },
      };
      grupo.Children.Add(new GeometryDrawing(Pincel(d.WrapperColor), null, forma));

      // A cara: "translate(fx fy) rotate(fr 18 18)"
      var cara = new DrawingGroup
      {
        Transform = new TransformGroup
        {
          Children =
          {
            new RotateTransform(d.FaceRotate, Lado / 2, Lado / 2),
            new TranslateTransform(d.FaceTranslateX, d.FaceTranslateY),
          },
        },
      };
      var corDaCara = Pincel(d.FaceColor);
      var y = 19 + d.MouthSpread;
      if (d.IsMouthOpen)
      {
        // Um sorriso de traço: "M15 y c2 1 4 1 6 0", traço de 1 com ponta redonda
        var traco = new Pen(corDaCara, 1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        traco.Freeze();
        cara.Children.Add(new GeometryDrawing(null, traco, Caminho($"M15,{N(y)} c2,1 4,1 6,0")));
      }
      else
      {
        // A boca cheia: "M13,y a1,0.75 0 0,0 10,0" (o arco cresce até caber, como no SVG: meia elipse de 5 por 3,75)
        cara.Children.Add(new GeometryDrawing(corDaCara, null, Caminho($"M13,{N(y)} a5,3.75 0 0,0 10,0 z")));
      }
      cara.Children.Add(new GeometryDrawing(corDaCara, null, new RectangleGeometry(new Rect(14 - d.EyeSpread, 14, 1.5, 2), 0.75, 1)));
      cara.Children.Add(new GeometryDrawing(corDaCara, null, new RectangleGeometry(new Rect(20 + d.EyeSpread, 14, 1.5, 2), 0.75, 1)));
      grupo.Children.Add(cara);

      grupo.Freeze();
      return grupo;
    }

    private static Geometry Caminho(string dados) => Geometry.Parse(dados);

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

    private static SolidColorBrush Pincel(string hex)
    {
      var pincel = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
      pincel.Freeze();
      return pincel;
    }

    /// <summary>O hashCode do boring-avatars: o de string do Java em 32 bits, e o módulo no fim (em long, que o
    /// Math.abs do JavaScript não estoura no menor inteiro).</summary>
    public static long HashCode(string nome)
    {
      var hash = 0;
      unchecked
      {
        foreach (var c in nome)
          hash = (hash << 5) - hash + c;
      }

      return Math.Abs((long)hash);
    }

    private static double Digit(long n, int casa) => Math.Floor(n / Math.Pow(10, casa) % 10);

    private static bool Boolean(long n, int casa) => Digit(n, casa) % 2 == 0;

    private static double Unit(long n, double range, int casa = 0)
    {
      var valor = n % range;
      return casa != 0 && Digit(n, casa) % 2 == 0 ? -valor : valor;
    }

    /// <summary>Preto ou branco por cima da cor (a conta YIQ deles).</summary>
    private static string Contraste(string hex)
    {
      var h = hex.TrimStart('#');
      var r = Convert.ToInt32(h[..2], 16);
      var g = Convert.ToInt32(h.Substring(2, 2), 16);
      var b = Convert.ToInt32(h.Substring(4, 2), 16);
      var yiq = (r * 299 + g * 587 + b * 114) / 1000.0;
      return yiq >= 128 ? "#000000" : "#FFFFFF";
    }
  }
}
