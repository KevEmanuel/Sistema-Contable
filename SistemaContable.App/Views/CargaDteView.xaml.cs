using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using SistemaContableDTE.ViewModels;

namespace SistemaContableDTE.Views;
//carga del DTE 04
public partial class CargaDteView : UserControl
{
    public CargaDteView()
    {
        InitializeComponent();
    }

    private void SeleccionarArchivo_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialogo = new OpenFileDialog
        {
            Title = "Selecciona un Documento Tributario Electrónico",
            Filter = "Archivos JSON (*.json)|*.json|Todos los archivos (*.*)|*.*",
            Multiselect = false
        };

        if (dialogo.ShowDialog() == true &&
            DataContext is CargaDteViewModel viewModel)
        {
            viewModel.ImportarArchivo(dialogo.FileName);
        }
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