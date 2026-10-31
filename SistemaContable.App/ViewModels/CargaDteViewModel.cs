using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;
// carga del archivo DTES 04
public class CargaDteViewModel : ViewModelBase
{
    private readonly RelayCommand cargarCommand;
    private readonly RelayCommand eliminarCommand;

    private DocumentoDte? documentoSeleccionado;
    private string archivoSeleccionado = string.Empty;
    private string mensaje = string.Empty;

    public ObservableCollection<DocumentoDte> Documentos { get; } = new();

    public DocumentoDte? DocumentoSeleccionado
    {
        get => documentoSeleccionado;
        set => EstablecerPropiedad(ref documentoSeleccionado, value);
    }

    public string ArchivoSeleccionado
    {
        get => archivoSeleccionado;
        private set => EstablecerPropiedad(ref archivoSeleccionado, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand CargarCommand => cargarCommand;
    public ICommand EliminarCommand => eliminarCommand;

    public CargaDteViewModel()
    {
        cargarCommand = new RelayCommand(_ => CargarDocumentos());
        eliminarCommand = new RelayCommand(Eliminar);

        CargarDocumentos();
    }

    public void ImportarArchivo(string rutaArchivo)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            //modifiaciones de la inserscion del json
            //DocumentoDte documento =
            // ServicioCargaDte.ImportarArchivo(
            //   SesionActual.EmpresaId.Value,
            // rutaArchivo);
            DocumentoDte documento= ServicioCargaDte.ImportarArchivo(
                SesionActual.EmpresaId.Value,
                            rutaArchivo,
                SesionActual.Usuario?.UsuarioId);  

            ArchivoSeleccionado = Path.GetFileName(rutaArchivo);

            CargarDocumentos();

            DocumentoSeleccionado = documento;

            Mensaje =
                $"DTE {documento.CodigoGeneracion} importado correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void CargarDocumentos()
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        Documentos.Clear();

        foreach (DocumentoDte documento in
                 ServicioCargaDte.ObtenerPorEmpresa(
                     SesionActual.EmpresaId.Value))
        {
            Documentos.Add(documento);
        }
    }

    private void Eliminar(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            DocumentoSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE para eliminar.";
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            "Se eliminará el DTE, el archivo original y sus resultados de validación. Solo se permite cuando ya no tenga evidencias ni eventos y no esté ligado a libros fiscales o partidas. ¿Deseas continuar?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmacion != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            ServicioCargaDte.Eliminar(
                SesionActual.EmpresaId.Value,
                DocumentoSeleccionado.DocumentoDteId,
                SesionActual.Usuario?.UsuarioId);
            DocumentoSeleccionado = null;
            CargarDocumentos();
            Mensaje = "DTE eliminado. Puedes importar el archivo corregido.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}