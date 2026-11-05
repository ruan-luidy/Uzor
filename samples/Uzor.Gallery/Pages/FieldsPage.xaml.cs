using System.Windows;
using System.Windows.Controls;
using Uzor.Wpf;
using Uzor.Wpf.Controls;
using Uzor.Wpf.Services;

namespace Uzor.Gallery.Pages
{
  public partial class FieldsPage : UserControl
  {
    public FieldsPage()
    {
      InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);
      Select.ItemsSource = new[] { "Usinagem Alfa Ltda", "Beta Tornearia ME", "Gama Ferramentaria Ltda" };
      Deadline.SelectedDateTime = DateTime.Today.AddDays(7).AddHours(17);
      Time.Time = new TimeSpan(17, 30, 0);
    }
  }
}
