using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// Coluna Auto na largura do texto mais comprido de TODAS as linhas, medida uma vez quando os dados chegam, e fixa
  /// depois disso (09/11/2026, "toda coluna tá com width auto, a gente tinha que fazer a conta de qual o tamanho que
  /// esse cara vai precisar quando carregar e deixar no tamanho máximo").
  ///
  /// <para><b>Por quê.</b> A grade só cria as linhas que estão na tela, e a coluna Auto só mede essas. Rolando, aparece
  /// um texto mais comprido e a coluna cresce - e a grade refaz o layout de todas as linhas visíveis. Ela nunca encolhe:
  /// no WPF (DataGridColumn.UpdateDesiredWidthForAutoColumn) a largura desejada só é trocada quando a célula medida é
  /// MAIOR. Era o "fica mudando". Largura fixa é a mais rápida pra rolar nos dois sentidos (a Telerik mede: fixa, depois
  /// Auto, depois estrela, a mais lenta).</para>
  ///
  /// <para><b>Como.</b> Quando os itens mudam, as colunas de texto (DataGridTextColumn com Binding de caminho simples)
  /// voltam a Auto pra grade medir o título e as linhas visíveis do jeito dela; no layout seguinte, cada uma fica na
  /// maior entre essa medida e o texto mais comprido de todos os itens, com o mesmo formato da célula (caminho,
  /// conversor, StringFormat, cultura) e a mesma folga da célula (Padding, margem). A fonte é a da célula na tela, e se
  /// algum gatilho do estilo deixa o texto mais pesado, a medida usa o mais pesado. Coluna de modelo (chip, barra,
  /// ícone), de estrela ou com largura em pixel no XAML fica como está.</para>
  ///
  /// <para><b>A largura de quem arrastou vale.</b> A coluna ajustada aqui guarda a largura que recebeu
  /// (<see cref="IsFitted"/>); se a pessoa arrasta, a largura muda e a coluna deixa de ser daqui. Quem salva o layout
  /// das colunas pergunta <see cref="IsFitted"/> pra não gravar a medida como se fosse escolha da pessoa.</para>
  /// </summary>
  public static class ColumnFit
  {
    /// <summary>Teto de uma coluna sem MaxWidth: uma descrição de dois mil caracteres não vira uma coluna de dez mil pixels.</summary>
    public const double MaxWidthDefault = 800;

    /// <summary>Quantos dos textos mais compridos de cada coluna são medidos (o comprimento escolhe, a fonte decide).</summary>
    private const int Candidatos = 48;

    /// <summary>Até quantos itens a conta olha: grade de 200 mil linhas não segura a tela pra medir.</summary>
    private const int ItensNaConta = 50_000;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
      "IsEnabled", typeof(bool), typeof(ColumnFit), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    /// <summary>A largura que a coluna recebeu daqui (NaN = nunca recebeu).</summary>
    public static readonly DependencyProperty FittedWidthProperty = DependencyProperty.RegisterAttached(
      "FittedWidth", typeof(double), typeof(ColumnFit), new PropertyMetadata(double.NaN));

    public static double GetFittedWidth(DependencyObject element) => (double)element.GetValue(FittedWidthProperty);

    private static void SetFittedWidth(DependencyObject element, double value) => element.SetValue(FittedWidthProperty, value);

    /// <summary>A coluna está na largura que recebeu daqui: ninguém arrastou depois.</summary>
    public static bool IsFitted(DataGridColumn column)
    {
      var medida = GetFittedWidth(column);
      return !double.IsNaN(medida) && column.Width.IsAbsolute && Math.Abs(column.Width.Value - medida) < 0.5;
    }

    private static readonly DependencyProperty ItemsHandlerProperty = DependencyProperty.RegisterAttached(
      "ItemsHandler", typeof(NotifyCollectionChangedEventHandler), typeof(ColumnFit));

    private static readonly DependencyProperty IsScheduledProperty = DependencyProperty.RegisterAttached(
      "IsScheduled", typeof(bool), typeof(ColumnFit));

    /// <summary>Os itens mudaram com a grade escondida: mede quando ela aparecer.</summary>
    private static readonly DependencyProperty IsPendingProperty = DependencyProperty.RegisterAttached(
      "IsPending", typeof(bool), typeof(ColumnFit));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not DataGrid grid)
        return;
      grid.IsVisibleChanged -= OnVisibleChanged;
      if (grid.GetValue(ItemsHandlerProperty) is NotifyCollectionChangedEventHandler antigo)
        ((INotifyCollectionChanged)grid.Items).CollectionChanged -= antigo;
      grid.ClearValue(ItemsHandlerProperty);
      if (e.NewValue is not true)
        return;
      grid.IsVisibleChanged += OnVisibleChanged;
      NotifyCollectionChangedEventHandler handler = (_, _) => Schedule(grid);
      ((INotifyCollectionChanged)grid.Items).CollectionChanged += handler;
      grid.SetValue(ItemsHandlerProperty, handler);
      Schedule(grid);
    }

    private static void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is true && sender is DataGrid grid && (bool)grid.GetValue(IsPendingProperty))
        Schedule(grid);
    }

    // Uma vez por lote: a grade recebe milhares de linhas uma a uma (Clear + Add), e a conta roda depois da última
    private static void Schedule(DataGrid grid)
    {
      if ((bool)grid.GetValue(IsScheduledProperty))
        return;
      grid.SetValue(IsScheduledProperty, true);
      grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
      {
        grid.SetValue(IsScheduledProperty, false);
        // Desligado depois de agendar (o estilo liga e a tela desliga): não mede
        if (GetIsEnabled(grid))
          Fit(grid);
      });
    }

    /// <summary>Volta as colunas de texto a Auto, lê os textos de todos os itens e, no layout seguinte, fixa.</summary>
    private static void Fit(DataGrid grid)
    {
      if (!grid.IsVisible)
      {
        grid.SetValue(IsPendingProperty, true);
        return;
      }
      grid.SetValue(IsPendingProperty, false);
      if (grid.Items.Count == 0)
        return;

      var cultura = Cultura(grid);
      var textos = new Dictionary<DataGridColumn, List<string>>();
      foreach (var coluna in grid.Columns)
      {
        if (coluna.Visibility != Visibility.Visible || coluna is not DataGridTextColumn { Binding: Binding binding })
          continue;
        if (!(coluna.Width.IsAuto || IsFitted(coluna)))
          continue;
        if (MaisCompridos(grid, binding, cultura) is { } lista)
          textos[coluna] = lista;
      }
      if (textos.Count == 0)
        return;

      // Auto de novo (zera o que a grade já tinha medido), e a medida dela - título e linhas visíveis - no próximo layout
      foreach (var coluna in textos.Keys)
        coluna.Width = DataGridLength.Auto;

      EventHandler? depois = null;
      depois = (_, _) =>
      {
        grid.LayoutUpdated -= depois;
        Fixar(grid, textos);
      };
      grid.LayoutUpdated += depois;
      grid.InvalidateMeasure();
    }

    private static void Fixar(DataGrid grid, Dictionary<DataGridColumn, List<string>> textos)
    {
      var linhas = Linhas(grid);
      var dip = VisualTreeHelper.GetDpi(grid).PixelsPerDip;
      foreach (var (coluna, lista) in textos)
      {
        if (!grid.Columns.Contains(coluna) || coluna.Visibility != Visibility.Visible)
          continue;

        // A célula na tela dá a fonte e a folga (Padding da célula, margem do texto): a medida da grade menos o texto
        TextBlock? exemplo = null;
        var folga = double.NaN;
        foreach (var linha in linhas)
        {
          if (coluna.GetCellContent(linha) is not TextBlock texto || Pai<DataGridCell>(texto) is not { } celula)
            continue;
          exemplo ??= texto;
          var medida = celula.DesiredSize.Width - Largura(texto.Text, Fonte(texto, coluna), texto.FontSize, dip);
          if (double.IsNaN(folga) || medida > folga)
            folga = medida;
        }
        if (exemplo is null || double.IsNaN(folga))
          continue;

        var fonte = Fonte(exemplo, coluna);
        var maior = 0d;
        foreach (var s in lista)
          maior = Math.Max(maior, Largura(s, fonte, exemplo.FontSize, dip));

        var daGrade = double.IsNaN(coluna.Width.DesiredValue) ? coluna.ActualWidth : coluna.Width.DesiredValue;
        var teto = double.IsInfinity(coluna.MaxWidth) ? MaxWidthDefault : coluna.MaxWidth;
        var largura = Math.Ceiling(Math.Max(coluna.MinWidth, Math.Min(teto, Math.Max(daGrade, folga + maior))));
        coluna.Width = new DataGridLength(largura);
        SetFittedWidth(coluna, largura);
      }
    }

    // ─── Os textos ───────────────────────────────────────────────────────────

    /// <summary>
    /// Os textos mais compridos da coluna, escritos como a célula escreve. Nulo quando o caminho não é simples
    /// (indexador, propriedade anexada) ou a leitura falha: a coluna fica com o Auto da grade.
    /// </summary>
    private static List<string>? MaisCompridos(DataGrid grid, Binding binding, CultureInfo cultura)
    {
      var caminho = binding.Path?.Path ?? string.Empty;
      if (caminho.IndexOfAny(['[', '(', ')', ']']) >= 0 || binding.Source is not null || binding.RelativeSource is not null
          || !string.IsNullOrEmpty(binding.ElementName))
        return null;
      var partes = caminho is "" or "." ? [] : caminho.Split('.');
      var culturaDoTexto = binding.ConverterCulture ?? cultura;

      var vistos = new HashSet<string>();
      var maiores = new List<string>();
      var quantos = 0;
      try
      {
        foreach (var item in grid.Items)
        {
          if (++quantos > ItensNaConta)
            break;
          if (item is null || item == CollectionView.NewItemPlaceholder)
            continue;
          var texto = Texto(Valor(item, partes), binding, culturaDoTexto);
          if (texto.Length == 0 || !vistos.Add(texto))
            continue;
          maiores.Add(texto);
          // Guarda os mais compridos sem ordenar tudo: quando passa do dobro, corta pela metade de cima
          if (maiores.Count >= Candidatos * 2)
          {
            maiores.Sort((a, b) => b.Length.CompareTo(a.Length));
            maiores.RemoveRange(Candidatos, maiores.Count - Candidatos);
          }
        }
      }
      catch (Exception)
      {
        return null;
      }
      return maiores;
    }

    private static object? Valor(object item, string[] partes)
    {
      object? atual = item;
      foreach (var parte in partes)
      {
        if (atual is null)
          return null;
        var propriedade = Propriedade(atual.GetType(), parte);
        if (propriedade is null)
          return null;
        atual = propriedade.GetValue(atual);
      }
      return atual;
    }

    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Propriedades = new();

    private static PropertyInfo? Propriedade(Type tipo, string nome)
      => Propriedades.GetOrAdd((tipo, nome), k => k.Item1.GetProperty(k.Item2, BindingFlags.Public | BindingFlags.Instance));

    /// <summary>O texto da célula: o conversor, o StringFormat e a cultura do Binding, como o WPF faz.</summary>
    private static string Texto(object? valor, Binding binding, CultureInfo cultura)
    {
      if (binding.Converter is { } conversor)
        valor = conversor.Convert(valor, typeof(string), binding.ConverterParameter, cultura);
      if (valor == DependencyProperty.UnsetValue || valor == Binding.DoNothing)
        return string.Empty;
      if (valor is null)
        return binding.TargetNullValue?.ToString() ?? string.Empty;
      if (!string.IsNullOrEmpty(binding.StringFormat))
      {
        var formato = binding.StringFormat;
        return formato.Contains('{') ? string.Format(cultura, formato, valor) : string.Format(cultura, "{0:" + formato + "}", valor);
      }
      return valor is IFormattable formatavel ? formatavel.ToString(null, cultura) : valor.ToString() ?? string.Empty;
    }

    private static CultureInfo Cultura(FrameworkElement elemento)
    {
      try
      {
        return elemento.Language.GetSpecificCulture();
      }
      catch (InvalidOperationException)
      {
        return CultureInfo.CurrentCulture;
      }
    }

    // ─── A medida ────────────────────────────────────────────────────────────

    /// <summary>A fonte da célula, com o peso mais forte que algum gatilho do estilo da coluna aplica.</summary>
    private static Typeface Fonte(TextBlock texto, DataGridColumn coluna)
    {
      var peso = texto.FontWeight;
      for (var estilo = (coluna as DataGridTextColumn)?.ElementStyle; estilo is not null; estilo = estilo.BasedOn)
        foreach (var gatilho in estilo.Triggers)
        {
          var setters = gatilho switch
          {
            Trigger t => t.Setters,
            DataTrigger t => t.Setters,
            MultiTrigger t => t.Setters,
            MultiDataTrigger t => t.Setters,
            _ => null,
          };
          if (setters is null)
            continue;
          foreach (var setter in setters.OfType<Setter>())
            if (setter.Property == TextBlock.FontWeightProperty && setter.Value is FontWeight p && p.ToOpenTypeWeight() > peso.ToOpenTypeWeight())
              peso = p;
        }
      return new Typeface(texto.FontFamily, texto.FontStyle, peso, texto.FontStretch);
    }

    /// <summary>
    /// A largura do texto em DIP: a soma dos avanços dos glifos da fonte (rápido), ou o FormattedText quando a fonte não
    /// tem algum caractere (o WPF cai pra outra fonte e a soma mentiria).
    /// </summary>
    internal static double Largura(string texto, Typeface fonte, double tamanho, double dip)
    {
      if (texto.Length == 0)
        return 0;
      if (fonte.TryGetGlyphTypeface(out var glifos))
      {
        var soma = 0d;
        var todos = true;
        foreach (var c in texto)
        {
          if (!glifos.CharacterToGlyphMap.TryGetValue(c, out var indice))
          {
            todos = false;
            break;
          }
          soma += glifos.AdvanceWidths[indice];
        }
        if (todos)
          return soma * tamanho;
      }
      return new FormattedText(texto, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, fonte, tamanho, Brushes.Black, dip)
        .WidthIncludingTrailingWhitespace;
    }

    // ─── A árvore ────────────────────────────────────────────────────────────

    /// <summary>As linhas que existem agora (as visíveis e as poucas em volta).</summary>
    private static List<DataGridRow> Linhas(DependencyObject raiz)
    {
      var linhas = new List<DataGridRow>();
      var fila = new Queue<DependencyObject>();
      fila.Enqueue(raiz);
      while (fila.Count > 0)
      {
        var atual = fila.Dequeue();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(atual); i++)
        {
          var filho = VisualTreeHelper.GetChild(atual, i);
          if (filho is DataGridRow linha)
            linhas.Add(linha);
          else
            fila.Enqueue(filho);
        }
      }
      return linhas;
    }

    private static T? Pai<T>(DependencyObject? elemento) where T : DependencyObject
    {
      while (elemento is not null && elemento is not T)
        elemento = VisualTreeHelper.GetParent(elemento);
      return elemento as T;
    }
  }
}
