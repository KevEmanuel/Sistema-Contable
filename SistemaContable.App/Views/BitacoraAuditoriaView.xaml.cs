using System.Windows;
using System.Windows.Controls;
//modulo  para la gestion de Bitacora de activides
namespace SistemaContableDTE.Views;

public partial class BitacoraAuditoriaView : UserControl
{
    public BitacoraAuditoriaView()
    {
        InitializeComponent();
    }

    private void Volver_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is not null)
        {
            Application.Current.MainWindow.Content =
                new PanelPrincipalView();
        }
    }
}