using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using Uzor.Wpf.Services;

namespace Uzor.Wpf.Behaviors
{
  /// <summary>
  /// Botão direito em qualquer tabela do terminal → "Copiar tabela" (cola no Excel/Sheets) ou "Exportar CSV…".
  /// Ligado no estilo TerminalDataGrid, então toda tabela ganha sem mexer em widget nenhum.
  ///
  /// Exporta o que está na tela, coluna por coluna: coluna com Binding lê o caminho dela; coluna de template tem o
  /// template instanciado uma vez pra achar os Bindings de texto dentro (TextBlock.Text e Run.Text) - é assim que
  /// "ATIVO" sai como o ticker e "VAR %" como "+1.23%", igual ao que se vê. Coluna sem cabeçalho (botões) fica de fora.
  /// </summary>
  public static class DataGridExport
  {
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
      "IsEnabled",
      typeof(bool),
      typeof(DataGridExport),
      new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    // Já pôs os itens nesta grade (o Loaded volta a cada vez que a grade entra de novo na tela)
    private static readonly DependencyProperty AttachedProperty = DependencyProperty.RegisterAttached(
      "Attached", typeof(bool), typeof(DataGridExport), new PropertyMetadata(false));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not DataGrid grid || e.NewValue is not true)
        return;

      // Monta quando a grade carrega, não aqui: o estilo liga isto antes do XAML pôr o menu da própria grade
      // (<DataGrid.ContextMenu>), e o menu de lá trocava o nosso - a grade com menu de linha perdia o copiar/exportar
      if (grid.IsLoaded)
        Attach(grid);
      else
        grid.Loaded += OnLoaded;
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
      var grid = (DataGrid)sender;
      grid.Loaded -= OnLoaded;
      Attach(grid);
    }

    private static void Attach(DataGrid grid)
    {
      if ((bool)grid.GetValue(AttachedProperty))
        return;
      grid.SetValue(AttachedProperty, true);

      var copy = new MenuItem { Header = Texts.CopyTable, Icon = new PackIconPhosphorIcons { Kind = PackIconPhosphorIconsKind.CopyBold } };
      copy.Click += (_, _) => Copy(grid);
      var export = new MenuItem { Header = Texts.ExportCsv, Icon = new PackIconPhosphorIcons { Kind = PackIconPhosphorIconsKind.FileCsvBold } };
      export.Click += (_, _) => Export(grid);

      // A tabela inteira vai no fim, depois do que é da linha (o menu da grade) e do "Limpar todos os filtros" do
      // filtro do cabeçalho (DataGridFilterBehavior), separada deles
      var menu = grid.ContextMenu ?? new ContextMenu();
      if (menu.Items.Count > 0)
        menu.Items.Add(new Separator());
      menu.Items.Add(copy);
      menu.Items.Add(export);
      menu.Opened += (_, _) => TidySeparators(menu);
      grid.ContextMenu = menu;
    }

    /// <summary>
    /// Separador sem item visível de um dos lados some (o da linha que esconde as ações dela, a tela só de leitura): o
    /// menu não abre com uma linha solta em cima nem com duas seguidas. Conferido a cada vez que abre.
    /// </summary>
    public static void TidySeparators(ItemsControl menu)
    {
      ArgumentNullException.ThrowIfNull(menu);
      var items = menu.Items.OfType<FrameworkElement>().ToList();
      bool Shown(FrameworkElement item) => item is not Separator && item.Visibility == Visibility.Visible;
      for (var i = 0; i < items.Count; i++)
      {
        if (items[i] is not Separator separator)
          continue;
        var before = items.Take(i).LastOrDefault(e => Shown(e) || e is Separator { Visibility: Visibility.Visible });
        var after = items.Skip(i + 1).Any(Shown);
        // SetCurrentValue: o separador que tem Visibility ligada no XAML continua ligado
        separator.SetCurrentValue(UIElement.VisibilityProperty, before is not null && before is not Separator && after ? Visibility.Visible : Visibility.Collapsed);
      }
    }

    private static void Copy(DataGrid grid)
    {
      var (headers, rows) = Read(grid);
      var text = new StringBuilder();
      text.AppendLine(string.Join('\t', headers));
      foreach (var row in rows)
        text.AppendLine(string.Join('\t', row.Select(v => v.Replace('\t', ' '))));

      Clipboard.SetText(text.ToString());
      ToastService.Success($"{rows.Count} linhas copiadas");
    }

    private static void Export(DataGrid grid)
    {
      var (headers, rows) = Read(grid);
      var dialog = new SaveFileDialog
      {
        Filter = "CSV (*.csv)|*.csv",
        FileName = $"{Texts.ExportFilePrefix}_{DateTime.Now:yyyy-MM-dd_HHmm}.csv",
      };

      if (dialog.ShowDialog(Window.GetWindow(grid)) != true)
        return;

      // Ponto e vírgula + BOM: o Excel em português abre direto nas colunas certas e com acento.
      var csv = new StringBuilder();
      csv.AppendLine(string.Join(';', headers.Select(Quote)));
      foreach (var row in rows)
        csv.AppendLine(string.Join(';', row.Select(Quote)));

      File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
      ToastService.Success($"{rows.Count} linhas em {Path.GetFileName(dialog.FileName)}");
    }

    private static string Quote(string value) =>
      value.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static (List<string> Headers, List<List<string>> Rows) Read(DataGrid grid)
    {
      var columns = grid.Columns
        .Where(c => c.Visibility == Visibility.Visible && c.Header is not null && !string.IsNullOrWhiteSpace(c.Header.ToString()))
        .OrderBy(c => c.DisplayIndex)
        .Select(c => (Header: c.Header!.ToString()!, Paths: PathsOf(c)))
        .Where(c => c.Paths.Count > 0)
        .ToList();

      var rows = new List<List<string>>();
      foreach (var item in (IEnumerable?)grid.ItemsSource ?? grid.Items)
      {
        if (item is null || item == CollectionView.NewItemPlaceholder)
          continue;

        rows.Add(columns.Select(c => string.Join(" ", c.Paths.Select(p => Evaluate(item, p)).Where(v => v.Length > 0))).ToList());
      }

      return (columns.Select(c => c.Header).ToList(), rows);
    }

    /// <summary>Os caminhos de Binding que dão o texto da coluna.</summary>
    private static List<string> PathsOf(DataGridColumn column)
    {
      if (column is DataGridBoundColumn { Binding: Binding { Path.Path: { Length: > 0 } path } })
        return [path];

      if (column is not DataGridTemplateColumn { CellTemplate: { } template })
        return [];

      var paths = new List<string>();
      if (template.LoadContent() is DependencyObject root)
        Collect(root, paths);

      return paths;
    }

    private static void Collect(DependencyObject node, List<string> paths)
    {
      if (node is TextBlock textBlock)
      {
        if (BindingOperations.GetBinding(textBlock, TextBlock.TextProperty) is { Path.Path: { Length: > 0 } path })
          paths.Add(path);

        foreach (var run in textBlock.Inlines.OfType<Run>())
        {
          if (BindingOperations.GetBinding(run, Run.TextProperty) is { Path.Path: { Length: > 0 } runPath })
            paths.Add(runPath);
        }
      }

      // O template ainda não está na árvore visual: anda pela lógica (filhos de painel, Child de Border...).
      foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
        Collect(child, paths);
    }

    /// <summary>Lê "Symbol.Ticker" do item por reflexão (o que a tela mostra, formatado se já for string).</summary>
    private static string Evaluate(object item, string path)
    {
      object? current = item;
      foreach (var part in path.Split('.'))
      {
        if (current is null)
          return string.Empty;

        current = current.GetType().GetProperty(part)?.GetValue(current);
      }

      return current switch
      {
        null => string.Empty,
        string text => text,
        Freezable => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => current.ToString() ?? string.Empty,
      };
    }
  }
}
