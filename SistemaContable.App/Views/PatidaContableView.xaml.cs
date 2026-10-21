using System.Windows;
using System.Windows.Controls;

namespace SistemaContableDTE.Views;
//modulo para partidas manueales 02, 
//Referencia a PartidaContableView.xaml
public partial class PartidasContablesView : UserControl
{
    public PartidasContablesView()
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