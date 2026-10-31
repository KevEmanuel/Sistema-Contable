using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using SistemaContableDTE.ViewModels;


//Modulo  de la vista de exportacion 07

namespace SistemaContableDTE.Views;

public partial class ExportacionFiscalView : UserControl
{
    public ExportacionFiscalView()
    {
        InitializeComponent();
    }

    private void GuardarArchivo_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not ExportacionFiscalViewModel viewModel ||
            viewModel.ExportacionSeleccionada is null)
        {
            return;
        }

        var dialogo = new SaveFileDialog
        {
            FileName = viewModel.ExportacionSeleccionada.NombreArchivo,
            Filter = "Archivo CSV (*.csv)|*.csv"
        };

        if (dialogo.ShowDialog() == true)
        {
            viewModel.GuardarArchivo(dialogo.FileName);
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