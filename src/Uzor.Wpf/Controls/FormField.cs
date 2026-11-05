using CommunityToolkit.Mvvm.ComponentModel;

namespace Uzor.Wpf.Controls
{
  public enum FieldKind
  {
    Text,
    Multiline,
    DateTime,
    Choice,
    Password,
  }

  /// <summary>Um campo do <see cref="FormDialog"/>: rótulo, valor, a regra e o erro embaixo.</summary>
  public sealed partial class FormField : ObservableObject
  {
    [ObservableProperty] private string _value = string.Empty;
    [ObservableProperty] private DateTime? _date;
    [ObservableProperty] private string? _error;

    public FormField(string label, FieldKind kind = FieldKind.Text)
    {
      Label = label;
      Kind = kind;
    }

    public string Label { get; }

    public FieldKind Kind { get; }

    public string? Placeholder { get; init; }

    /// <summary>Linha de ajuda embaixo do campo (some quando há erro).</summary>
    public string? Hint { get; init; }

    public int MaxLength { get; init; }

    /// <summary>Fonte de número (CNPJ, chave).</summary>
    public bool Monospace { get; init; }

    /// <summary>Devolve o erro, ou nulo quando o valor passa.</summary>
    public Func<FormField, string?>? Validate { get; init; }

    public bool IsText => Kind is FieldKind.Text or FieldKind.Multiline;

    public bool IsChoice => Kind == FieldKind.Choice;

    public bool IsPassword => Kind == FieldKind.Password;

    /// <summary>As opções de um campo Choice (o Value guarda a escolhida).</summary>
    public IReadOnlyList<string> Options { get; init; } = [];

    public bool IsMultiline => Kind == FieldKind.Multiline;

    public bool IsDate => Kind == FieldKind.DateTime;

    public string Trimmed => Value.Trim();

    public bool Check()
    {
      Error = Validate?.Invoke(this);
      return Error is null;
    }

    partial void OnValueChanged(string value) => Error = null;

    partial void OnDateChanged(DateTime? value) => Error = null;

    /// <summary>Regra comum: obrigatório, com mínimo e máximo de caracteres.</summary>
    public static Func<FormField, string?> Required(string what, int min = 1, int max = int.MaxValue) => field =>
      field.Trimmed.Length == 0 ? $"Preencha {what}."
        : field.Trimmed.Length < min ? $"Escreva pelo menos {min} caracteres."
        : field.Trimmed.Length > max ? $"No máximo {max} caracteres."
        : null;
  }
}
