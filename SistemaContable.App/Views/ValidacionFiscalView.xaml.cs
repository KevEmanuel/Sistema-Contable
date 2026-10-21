using System.Windows;
using System.Windows.Controls;

namespace SistemaContableDTE.Views;
//modulo para la validacion fiscal 05
public partial class ValidacionFiscalView : UserControl
{
    public ValidacionFiscalView()
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