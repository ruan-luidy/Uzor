namespace Uzor.Wpf
{
  /// <summary>O tom de um chip de status: decide o fundo suave e a cor do texto (ver o estilo do Chip).</summary>
  public enum Tone
  {
    Neutral,
    Accent,
    Success,
    Warning,
    Danger,
    Info,
  }

  /// <summary>Um texto de status com o tom dele.</summary>
  public readonly record struct Status(string Text, Tone Tone);
}
