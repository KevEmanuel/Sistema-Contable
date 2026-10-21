using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;
// carga del archivo DTES 04
public class CargaDteViewModel : ViewModelBase
{
    private readonly RelayCommand cargarCommand;

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

    public CargaDteViewModel()
    {
        cargarCommand = new RelayCommand(_ => CargarDocumentos());

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
            DocumentoDte documento =
                ServicioCargaDte.ImportarArchivo(
                    SesionActual.EmpresaId.Value,
                    rutaArchivo);

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
}