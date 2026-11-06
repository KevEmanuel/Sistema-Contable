using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;
using SistemaContableDTE.ViewModels;

namespace SistemaContableDTE.Views;
//modulo para la gestio de la vista de libro de iva 05
public partial class LibroIvaView : UserControl
{
    public LibroIvaView()
    {
        InitializeComponent();
    }

    private void ExportarCompras_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not LibroIvaViewModel viewModel)
        {
            return;
        }

        GuardarLibro(
            "Libro_Compras",
            "Libro de Compras",
            new[]
            {
                "Fecha", "Tipo DTE", "Número de control", "Proveedor", "NIT",
                "NRC", "Base gravada", "Monto exento", "Monto no sujeto",
                "Crédito fiscal", "Total operación", "Estado"
            },
            viewModel.Registros
                .Where(registro => registro.TipoLibro == TipoLibroIva.Compras)
                .Select(registro => new object?[]
                {
                    registro.FechaRegistro,
                    registro.DocumentoDte?.TipoDte,
                    registro.DocumentoDte?.NumeroControl,
                    registro.NombreTercero,
                    registro.NitTercero,
                    registro.NrcTercero,
                    registro.BaseGravada,
                    registro.MontoExento,
                    registro.MontoNoSujeto,
                    registro.CreditoFiscal,
                    registro.TotalOperacion,
                    registro.Estado
                }));
    }

    private void ExportarVentas_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not LibroIvaViewModel viewModel)
        {
            return;
        }

        GuardarLibro(
            "Libro_Ventas",
            "Libro de Ventas",
            new[]
            {
                "Fecha", "Libro", "Tipo DTE", "Número de control", "Cliente",
                "NIT", "NRC", "Base gravada", "Monto exento",
                "Monto no sujeto", "Débito fiscal", "Total operación",
                "Estado"
            },
            viewModel.Registros
                .Where(registro => registro.TipoLibro is
                    TipoLibroIva.VentasContribuyentes or
                    TipoLibroIva.VentasConsumidorFinal)
                .Select(registro => new object?[]
                {
                    registro.FechaRegistro,
                    registro.TipoLibro,
                    registro.DocumentoDte?.TipoDte,
                    registro.DocumentoDte?.NumeroControl,
                    registro.NombreTercero,
                    registro.NitTercero,
                    registro.NrcTercero,
                    registro.BaseGravada,
                    registro.MontoExento,
                    registro.MontoNoSujeto,
                    registro.DebitoFiscal,
                    registro.TotalOperacion,
                    registro.Estado
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

        string extension = dialogo.FilterIndex switch
        {
            2 => ".pdf",
            3 => ".csv",
            _ => ".xlsx"
        };
        string ruta = Path.ChangeExtension(dialogo.FileName, extension);

        try
        {
            ExportadorLibros.Guardar(
                ruta,
                titulo,
                titulo,
                encabezados,
                filas);
        }
        catch (IOException error)
        {
            MostrarErrorExportacion(error);
        }
        catch (UnauthorizedAccessException error)
        {
            MostrarErrorExportacion(error);
        }
        catch (InvalidOperationException error)
        {
            MostrarErrorExportacion(error);
        }
        catch (ArgumentException error)
        {
            MostrarErrorExportacion(error);
        }
    }

    private static void MostrarErrorExportacion(Exception error)
    {
        MessageBox.Show(
            error.Message,
            "No se pudo exportar el libro",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
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