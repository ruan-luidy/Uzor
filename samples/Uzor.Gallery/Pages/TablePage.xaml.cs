using System.Windows;
using System.Windows.Controls;
using Uzor.Wpf;
using Uzor.Wpf.Controls;
using Uzor.Wpf.Services;

namespace Uzor.Gallery.Pages
{
  public partial class TablePage : UserControl
  {
    public TablePage()
    {
      InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);
      Table.ItemsSource = new[]
      {
        new Row("10042", "Eixo de transmissão Ø32", 40, new Status("Aberto", Tone.Accent), "em 2 dias"),
        new Row("10043", "Bucha de bronze 25×40", 120, new Status("Aprovado", Tone.Success), "07/11"),
        new Row("AB-77.1", "Flange de acoplamento", 8, new Status("Atrasado", Tone.Danger), "há 1 dia"),
        new Row("10057", "Pino guia temperado", 300, new Status("Em usinagem", Tone.Info), "15/11"),
        new Row("20110", "Engrenagem cilíndrica Z32 m2", 12, new Status("Aguardando", Tone.Warning), "21/11"),
        new Row("20111", "Engrenagem cilíndrica Z48 m2", 12, new Status("Rascunho", Tone.Neutral), "—"),
      };
    }

    public sealed record Row(string Code, string Description, int Quantity, Status Status, string Deadline);
  }
}
