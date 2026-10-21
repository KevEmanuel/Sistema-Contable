using System.Windows;
using System.Windows.Controls;

namespace SistemaContableDTE.Views;
//modulo para la gestio de la vista de libro de iva 05
public partial class LibroIvaView : UserControl
{
    public LibroIvaView()
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