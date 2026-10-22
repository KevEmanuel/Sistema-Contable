using System.Windows;
using System.Windows.Controls;

namespace SistemaContableDTE.Views;
//contabilizacion automatica modulo 06
public partial class ContabilizacionAutomaticaView : UserControl
{
    public ContabilizacionAutomaticaView()
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