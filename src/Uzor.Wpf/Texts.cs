namespace Uzor.Wpf
{
  /// <summary>
  /// Os textos que a própria biblioteca mostra (botões dos diálogos, calendário, menu da tabela, títulos dos avisos),
  /// em português por padrão. App em outro idioma troca na partida, antes de abrir janela:
  /// <c>Texts.Close = L.T("shared.close");</c>. O XAML lê por x:Static, então trocar com a tela aberta não muda o que já
  /// está nela.
  /// </summary>
  public static class Texts
  {
    public static string Close { get; set; } = "Fechar (Esc)";

    public static string Cancel { get; set; } = "Cancelar";

    public static string Ok { get; set; } = "OK";

    public static string ShowDetails { get; set; } = "ver detalhes";

    public static string HideDetails { get; set; } = "esconder detalhes";

    public static string CopyDetails { get; set; } = "Copiar detalhes";

    public static string ShowInFolder { get; set; } = "mostrar na pasta";

    public static string DetailsCopied { get; set; } = "Detalhes copiados pra área de transferência";

    public static string Retry { get; set; } = "Tentar de novo";

    public static string PickDate { get; set; } = "Escolha a data";

    public static string Time { get; set; } = "Horário";

    public static string Today { get; set; } = "Hoje";

    public static string Done { get; set; } = "Pronto";

    public static string TimeUp { get; set; } = "Aumenta (↑ ou rodinha)";

    public static string TimeDown { get; set; } = "Diminui (↓ ou rodinha)";

    public static string Filter { get; set; } = "Filtrar";

    public static string FilterHint { get; set; } = "Filtra a tabela por qualquer coluna, sem acento e sem ligar pra maiúscula";

    public static string Clear { get; set; } = "Limpar";

    public static string CopyTable { get; set; } = "Copiar tabela";

    public static string ExportCsv { get; set; } = "Exportar CSV…";

    /// <summary>O começo do nome do CSV exportado da tabela ("tabela_2026-11-05_1430.csv").</summary>
    public static string ExportFilePrefix { get; set; } = "tabela";

    public static string ToastInfo { get; set; } = "Aviso";

    public static string ToastSuccess { get; set; } = "Pronto";

    public static string ToastWarning { get; set; } = "Atenção";

    public static string ToastError { get; set; } = "Erro";

    /// <summary>O menu das caixas de texto (Behaviors.TextMenu).</summary>
    public static string Cut { get; set; } = "Recortar";

    public static string Copy { get; set; } = "Copiar";

    public static string Paste { get; set; } = "Colar";

    public static string SelectAll { get; set; } = "Selecionar tudo";

    /// <summary>A frase do LoadingOverlay quando quem usa não diz o que está carregando.</summary>
    public static string Loading { get; set; } = "Carregando...";
  }
}
