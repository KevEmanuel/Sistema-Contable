using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

//modulo para la validacion del vista fisca 05
namespace SistemaContableDTE.ViewModels;

public class ValidacionFiscalViewModel : ViewModelBase
{
    private readonly RelayCommand registrarEvidenciaCommand;
    private readonly RelayCommand registrarEventoCommand;
    private readonly RelayCommand cargarCommand;

    private DocumentoDte? documentoSeleccionado;

    private FuenteEvidenciaFiscal fuenteSeleccionada;
    private EstadoDocumentoDte estadoDocumentoSeleccionado;

    private string? selloRecibido;
    private string? estadoHacienda;
    private DateTime? fechaProcesamiento = DateTime.Today;
    private string? codigoMensaje;
    private string? descripcionMensaje;
    private string? observaciones;
    private string? respuestaJson;

    private string tipoEvento = string.Empty;
    private string estadoEvento = string.Empty;
    private DateTime fechaEvento = DateTime.Now;
    private string? selloEvento;
    private string? detalleEventoJson;

    private string mensaje = string.Empty;

    public ObservableCollection<DocumentoDte> Documentos { get; } = new();

    public ObservableCollection<EvidenciaFiscal> Evidencias { get; }
        = new();

    public ObservableCollection<EventoDte> Eventos { get; } = new();

    public List<FuenteEvidenciaFiscal> FuentesEvidencia { get; } =
        Enum.GetValues<FuenteEvidenciaFiscal>().ToList();

    public List<EstadoDocumentoDte> EstadosDocumento { get; } =
        Enum.GetValues<EstadoDocumentoDte>().ToList();

    public DocumentoDte? DocumentoSeleccionado
    {
        get => documentoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref documentoSeleccionado, value))
            {
                CargarHistorial();
            }
        }
    }

    public FuenteEvidenciaFiscal FuenteSeleccionada
    {
        get => fuenteSeleccionada;
        set => EstablecerPropiedad(ref fuenteSeleccionada, value);
    }

    public EstadoDocumentoDte EstadoDocumentoSeleccionado
    {
        get => estadoDocumentoSeleccionado;
        set => EstablecerPropiedad(
            ref estadoDocumentoSeleccionado,
            value);
    }

    public string? SelloRecibido
    {
        get => selloRecibido;
        set => EstablecerPropiedad(ref selloRecibido, value);
    }

    public string? EstadoHacienda
    {
        get => estadoHacienda;
        set => EstablecerPropiedad(ref estadoHacienda, value);
    }

    public DateTime? FechaProcesamiento
    {
        get => fechaProcesamiento;
        set => EstablecerPropiedad(ref fechaProcesamiento, value);
    }

    public string? CodigoMensaje
    {
        get => codigoMensaje;
        set => EstablecerPropiedad(ref codigoMensaje, value);
    }

    public string? DescripcionMensaje
    {
        get => descripcionMensaje;
        set => EstablecerPropiedad(ref descripcionMensaje, value);
    }

    public string? Observaciones
    {
        get => observaciones;
        set => EstablecerPropiedad(ref observaciones, value);
    }

    public string? RespuestaJson
    {
        get => respuestaJson;
        set => EstablecerPropiedad(ref respuestaJson, value);
    }

    public string TipoEvento
    {
        get => tipoEvento;
        set => EstablecerPropiedad(ref tipoEvento, value);
    }

    public string EstadoEvento
    {
        get => estadoEvento;
        set => EstablecerPropiedad(ref estadoEvento, value);
    }

    public DateTime FechaEvento
    {
        get => fechaEvento;
        set => EstablecerPropiedad(ref fechaEvento, value);
    }

    public string? SelloEvento
    {
        get => selloEvento;
        set => EstablecerPropiedad(ref selloEvento, value);
    }

    public string? DetalleEventoJson
    {
        get => detalleEventoJson;
        set => EstablecerPropiedad(ref detalleEventoJson, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand RegistrarEvidenciaCommand =>
        registrarEvidenciaCommand;

    public ICommand RegistrarEventoCommand =>
        registrarEventoCommand;

    public ICommand CargarCommand => cargarCommand;

    public ValidacionFiscalViewModel()
    {
        fuenteSeleccionada = FuentesEvidencia.FirstOrDefault();
        estadoDocumentoSeleccionado =
            EstadoDocumentoDte.Validado;

        registrarEvidenciaCommand =
            new RelayCommand(RegistrarEvidencia);

        registrarEventoCommand =
            new RelayCommand(RegistrarEvento);

        cargarCommand = new RelayCommand(_ => CargarDocumentos());

        CargarDocumentos();
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
                 ServicioValidacionFiscal.ObtenerDocumentos(
                     SesionActual.EmpresaId.Value))
        {
            Documentos.Add(documento);
        }

        Evidencias.Clear();
        Eventos.Clear();
        DocumentoSeleccionado = null;
    }

    private void CargarHistorial()
    {
        Evidencias.Clear();
        Eventos.Clear();

        if (DocumentoSeleccionado is null)
        {
            return;
        }

        foreach (EvidenciaFiscal evidencia in
                 ServicioValidacionFiscal.ObtenerEvidencias(
                     DocumentoSeleccionado.DocumentoDteId))
        {
            Evidencias.Add(evidencia);
        }

        foreach (EventoDte evento in
                 ServicioValidacionFiscal.ObtenerEventos(
                     DocumentoSeleccionado.DocumentoDteId))
        {
            Eventos.Add(evento);
        }
    }

    private void RegistrarEvidencia(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            DocumentoSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE.";
            return;
        }

        try
        {
            ServicioValidacionFiscal.RegistrarEvidencia(
                SesionActual.EmpresaId.Value,
                DocumentoSeleccionado.DocumentoDteId,
                FuenteSeleccionada,
                SelloRecibido,
                EstadoHacienda,
                FechaProcesamiento,
                CodigoMensaje,
                DescripcionMensaje,
                Observaciones,
                RespuestaJson,
                EstadoDocumentoSeleccionado);

            Mensaje = "Evidencia fiscal registrada correctamente.";

            SelloRecibido = null;
            EstadoHacienda = null;
            CodigoMensaje = null;
            DescripcionMensaje = null;
            Observaciones = null;
            RespuestaJson = null;

            CargarHistorial();
            CargarDocumentos();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void RegistrarEvento(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            DocumentoSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE.";
            return;
        }

        try
        {
            ServicioValidacionFiscal.RegistrarEvento(
                SesionActual.EmpresaId.Value,
                DocumentoSeleccionado.DocumentoDteId,
                TipoEvento,
                EstadoEvento,
                FechaEvento,
                SelloEvento,
                DetalleEventoJson);

            Mensaje = "Evento registrado correctamente.";

            TipoEvento = string.Empty;
            EstadoEvento = string.Empty;
            SelloEvento = null;
            DetalleEventoJson = null;

            CargarHistorial();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}