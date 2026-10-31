using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SistemaContableDTE.ViewModels;

namespace SistemaContableDTE.Views;

public partial class LibrosContablesView : UserControl
{
    public LibrosContablesView()
    {
        InitializeComponent();
    }

    private void ExportarDiario_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is LibrosContablesViewModel viewModel)
        {
            GuardarCsv(
                viewModel.CrearCsvDiario(),
                "Libro_Diario.csv");
        }
    }

    private void ExportarMayor_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is LibrosContablesViewModel viewModel)
        {
            GuardarCsv(
                viewModel.CrearCsvMayor(),
                "Libro_Mayor.csv");
        }
    }

    private static void GuardarCsv(string contenido, string nombreArchivo)
    {
        var dialogo = new SaveFileDialog
        {
            FileName = nombreArchivo,
            Filter = "Archivo CSV (*.csv)|*.csv"
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        try
        {
            File.WriteAllText(
                dialogo.FileName,
                contenido,
                new UTF8Encoding(true));
        }
        catch (IOException error)
        {
            MessageBox.Show(
                error.Message,
                "No se pudo guardar el libro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (UnauthorizedAccessException error)
        {
            MessageBox.Show(
                error.Message,
                "No se pudo guardar el libro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
