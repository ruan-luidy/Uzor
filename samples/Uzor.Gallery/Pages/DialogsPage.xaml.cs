using System.Windows;
using System.Windows.Controls;
using Uzor.Wpf;
using Uzor.Wpf.Controls;
using Uzor.Wpf.Services;

namespace Uzor.Gallery.Pages
{
  public partial class DialogsPage : UserControl
  {
    public DialogsPage()
    {
      InitializeComponent();
    }

    public static Task InfoAsync() => DialogService.InfoAsync("Pacote publicado", "Os fornecedores convidados recebem o aviso por e-mail.");

    public static Task<bool> ConfirmAsync() => DialogService.ConfirmAsync("Apagar rascunho", "Apagar o rascunho PKG-0014? Itens, desenhos e convites saem junto. Não dá pra desfazer.", "Apagar", "Voltar", DialogKind.Error);

    public static Task ErrorAsync() => DialogService.ErrorAsync("Não deu pra publicar", "O servidor recusou o pacote. Tente de novo em alguns minutos.", "HttpRequestException: 503 Service Unavailable (api.exemplo.com)");

    public static Task<bool> FormAsync()
    {
      var name = new FormField("Nome") { Placeholder = "Como aparece pros fornecedores", Validate = FormField.Required("o nome") };
      var deadline = new FormField("Prazo de resposta", FieldKind.DateTime) { Date = DateTime.Today.AddDays(7).AddHours(17), Hint = "Até quando dá pra mandar cotação." };
      var kind = new FormField("Tipo", FieldKind.Choice) { Options = ["Usinagem", "Tratamento térmico", "Solda"], Value = "Usinagem" };
      var notes = new FormField("Observação (opcional)", FieldKind.Multiline) { Placeholder = "Não vai pro fornecedor" };
      return FormDialog.ShowAsync("Novo pacote", "Começa como rascunho: itens e convidados vêm depois", null, "Criar rascunho", [name, deadline, kind, notes], icon: MahApps.Metro.IconPacks.PackIconPhosphorIconsKind.PackageBold);
    }

    private async void Info_Click(object sender, RoutedEventArgs e) => await InfoAsync().ConfigureAwait(true);

    private async void Warning_Click(object sender, RoutedEventArgs e) =>
      await DialogService.WarningAsync("Prazo curto", "Faltam menos de 24 horas pro fim do prazo de resposta.").ConfigureAwait(true);

    private async void Error_Click(object sender, RoutedEventArgs e) => await ErrorAsync().ConfigureAwait(true);

    private async void Confirm_Click(object sender, RoutedEventArgs e) => await ConfirmAsync().ConfigureAwait(true);

    private async void Form_Click(object sender, RoutedEventArgs e) => await FormAsync().ConfigureAwait(true);

    private void ToastSuccess_Click(object sender, RoutedEventArgs e) => ToastService.Success("Rascunho criado.");

    private void ToastInfo_Click(object sender, RoutedEventArgs e) => ToastService.Info("Inclua itens no pacote antes de buscar os desenhos.");

    private void ToastWarning_Click(object sender, RoutedEventArgs e) => ToastService.Warning("Dois itens ficaram sem desenho.");

    private void ToastError_Click(object sender, RoutedEventArgs e) => ToastService.Error("O servidor não respondeu.", "Não deu pra salvar");
  }
}
