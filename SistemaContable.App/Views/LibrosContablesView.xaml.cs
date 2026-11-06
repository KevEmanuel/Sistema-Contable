using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SistemaContableDTE.Services;
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
        if (DataContext is not LibrosContablesViewModel viewModel)
        {
            return;
        }

        GuardarLibro(
            "Libro_Diario",
            "Libro Diario",
            new[]
            {
                "Fecha", "Partida", "Estado", "Glosa", "Código de cuenta",
                "Cuenta", "Concepto", "Debe", "Haber"
            },
            viewModel.LibroDiario.Select(linea => new object?[]
            {
                linea.Fecha,
                linea.NumeroPartida,
                linea.EstadoPartida,
                linea.Glosa,
                linea.CodigoCuenta,
                linea.NombreCuenta,
                linea.Concepto,
                linea.Debe,
                linea.Haber
            }));
    }

    private void ExportarMayor_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not LibrosContablesViewModel viewModel)
        {
            return;
        }

        GuardarLibro(
            "Libro_Mayor",
            "Libro Mayor",
            new[]
            {
                "Código de cuenta", "Cuenta", "Fecha", "Partida", "Estado",
                "Glosa", "Concepto", "Debe", "Haber", "Saldo deudor",
                "Saldo acreedor"
            },
            viewModel.LibroMayor.Select(linea => new object?[]
            {
                linea.CodigoCuenta,
                linea.NombreCuenta,
                linea.Fecha,
                linea.NumeroPartida,
                linea.EstadoPartida,
                linea.Glosa,
                linea.Concepto,
                linea.Debe,
                linea.Haber,
                linea.SaldoDeudor,
                linea.SaldoAcreedor
            }));
    }

    private static void GuardarLibro(
        string nombreArchivo,
        string titulo,
        IReadOnlyList<string> encabezados,
        IEnumerable<IEnumerable<object?>> filas)
    {
        var dialogo = new SaveFileDialog
        {
            FileName = nombreArchivo,
            Filter = "Libro de Excel (*.xlsx)|*.xlsx|Documento PDF (*.pdf)|*.pdf|Archivo CSV (*.csv)|*.csv",
            FilterIndex = 1,
            DefaultExt = ".xlsx",
            AddExtension = true
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        try
        {
            string extension = dialogo.FilterIndex switch
            {
                2 => ".pdf",
                3 => ".csv",
                _ => ".xlsx"
            };
            string ruta = Path.ChangeExtension(dialogo.FileName, extension);

            ExportadorLibros.Guardar(
                ruta,
                titulo,
                titulo,
                encabezados,
                filas);
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
        catch (InvalidOperationException error)
        {
            MessageBox.Show(
                error.Message,
                "No se pudo exportar el libro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (ArgumentException error)
        {
            MessageBox.Show(
                error.Message,
                "No se pudo exportar el libro",
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
