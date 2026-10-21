using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore.Storage;

namespace SistemaContableDTE.Views;

public partial class PanelPrincipalView : UserControl
{
    public PanelPrincipalView()
    {
        InitializeComponent();
    }
    //modulo para generar la regla contable
    private void AbrirCatalogoCuentas_Click(object sender, RoutedEventArgs e)
    {
        if(Application.Current.MainWindow is not null)
        {
            Application.Current.MainWindow.Content = new CatalogoCuentasView();
        }
    
    }
    //mododulo de habilitar las reglas contables
    private void AbrirReglasContables_Click(
    object sender,
    RoutedEventArgs e)
{
    if (Application.Current.MainWindow is not null)
    {
        Application.Current.MainWindow.Content =
            new ReglasContablesView();
    }
}
}