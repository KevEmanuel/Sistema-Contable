using System.Windows;
using System.Windows.Controls;
//modulo para la gestion de reglas contables
namespace SistemaContableDTE.Views;

public partial class ReglasContablesView : UserControl
{
    public ReglasContablesView()
    {
        InitializeComponent();
    }

    private void VolverAlPanelPrincipal_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is not null)
        {
            Application.Current.MainWindow.Content =
                new PanelPrincipalView();
        }
    }
}