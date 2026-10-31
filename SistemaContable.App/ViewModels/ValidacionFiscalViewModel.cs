using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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
    private readonly RelayCommand actualizarEvidenciaCommand;
    private readonly RelayCommand eliminarEvidenciaCommand;
    private readonly RelayCommand actualizarEventoCommand;
    private readonly RelayCommand eliminarEventoCommand;

    private DocumentoDte? documentoSeleccionado;
    private EvidenciaFiscal? evidenciaSeleccionada;
    private EventoDte? eventoSeleccionado;

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

    public ObservableCollection<ResultadoValidacionDte> ResultadosValidacion { get; } =
        new();

    public List<FuenteEvidenciaFiscal> FuentesEvidencia { get; } =
        Enum.GetValues<FuenteEvidenciaFiscal>().ToList();

    public List<EstadoDocumentoDte> EstadosDocumento { get; } =
        Enum.GetValues<EstadoDocumentoDte>()
            .Where(estado => estado != EstadoDocumentoDte.Contabilizado)
            .ToList();

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

    public EvidenciaFiscal? EvidenciaSeleccionada
    {
        get => evidenciaSeleccionada;
        set
        {
            if (EstablecerPropiedad(ref evidenciaSeleccionada, value) &&
                value is not null)
            {
                FuenteSeleccionada = value.Fuente;
                SelloRecibido = value.SelloRecibido;
                EstadoHacienda = value.EstadoHacienda;
                FechaProcesamiento = value.FechaProcesamiento;
                CodigoMensaje = value.CodigoMensaje;
                DescripcionMensaje = value.DescripcionMensaje;
                Observaciones = value.Observaciones;
                RespuestaJson = value.RespuestaJson;
                Mensaje = "Evidencia seleccionada. Corrige los datos y pulsa Actualizar evidencia.";
            }
        }
    }

    public EventoDte? EventoSeleccionado
    {
        get => eventoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref eventoSeleccionado, value) &&
                value is not null)
            {
                TipoEvento = value.TipoEvento;
                EstadoEvento = value.EstadoEvento;
                FechaEvento = value.FechaEvento;
                SelloEvento = value.SelloRecibido;
                DetalleEventoJson = value.DetalleJson;
                Mensaje = "Evento seleccionado. Corrige los datos y pulsa Actualizar evento.";
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
    public ICommand ActualizarEvidenciaCommand =>
        actualizarEvidenciaCommand;
    public ICommand EliminarEvidenciaCommand =>
        eliminarEvidenciaCommand;
    public ICommand ActualizarEventoCommand => actualizarEventoCommand;
    public ICommand EliminarEventoCommand => eliminarEventoCommand;

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
        actualizarEvidenciaCommand =
            new RelayCommand(ActualizarEvidencia);
        eliminarEvidenciaCommand = new RelayCommand(EliminarEvidencia);
        actualizarEventoCommand = new RelayCommand(ActualizarEvento);
        eliminarEventoCommand = new RelayCommand(EliminarEvento);

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
        ResultadosValidacion.Clear();
        DocumentoSeleccionado = null;
    }

    private void CargarHistorial()
    {
        EvidenciaSeleccionada = null;
        EventoSeleccionado = null;
        Evidencias.Clear();
        Eventos.Clear();
        ResultadosValidacion.Clear();

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

        foreach (ResultadoValidacionDte resultado in
                 ServicioValidacionFiscal.ObtenerResultadosValidacion(
                     DocumentoSeleccionado.DocumentoDteId))
        {
            ResultadosValidacion.Add(resultado);
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
            int documentoId = DocumentoSeleccionado.DocumentoDteId;
            List<ResultadoValidacionDte> resultados =
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
                EstadoDocumentoSeleccionado,
                SesionActual.Usuario?.UsuarioId);

            bool resultadoAprobado = resultados.All(resultado =>
                resultado.Resultado == EstadoResultadoValidacionDte.Valida);

            Mensaje = EstadoDocumentoSeleccionado == EstadoDocumentoDte.Validado &&
                      !resultadoAprobado
                ? "Evidencia guardada, pero el DTE no se validó. Revisa los resultados registrados."
                : "Evidencia y resultados de validación registrados correctamente.";

            SelloRecibido = null;
            EstadoHacienda = null;
            CodigoMensaje = null;
            DescripcionMensaje = null;
            Observaciones = null;
            RespuestaJson = null;

            CargarDocumentos();
            DocumentoSeleccionado = Documentos.SingleOrDefault(documento =>
                documento.DocumentoDteId == documentoId);
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

    private void ActualizarEvidencia(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            EvidenciaSeleccionada is null)
        {
            Mensaje = "Selecciona una evidencia para actualizar.";
            return;
        }

        try
        {
            ServicioValidacionFiscal.ActualizarEvidencia(
                SesionActual.EmpresaId.Value,
                EvidenciaSeleccionada.EvidenciaFiscalId,
                FuenteSeleccionada,
                SelloRecibido,
                EstadoHacienda,
                FechaProcesamiento,
                CodigoMensaje,
                DescripcionMensaje,
                Observaciones,
                RespuestaJson,
                SesionActual.Usuario?.UsuarioId);

            Mensaje = "Evidencia actualizada y DTE validado nuevamente.";
            EvidenciaSeleccionada = null;
            CargarHistorial();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void EliminarEvidencia(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            EvidenciaSeleccionada is null)
        {
            Mensaje = "Selecciona una evidencia para eliminar.";
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            "Se eliminará esta evidencia. El DTE se validará con la evidencia restante y se conservará el historial de resultados anteriores. ¿Deseas continuar?",
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
            ServicioValidacionFiscal.EliminarEvidencia(
                SesionActual.EmpresaId.Value,
                EvidenciaSeleccionada.EvidenciaFiscalId,
                SesionActual.Usuario?.UsuarioId);
            EvidenciaSeleccionada = null;
            CargarHistorial();
            Mensaje = "Evidencia eliminada y DTE actualizado.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void ActualizarEvento(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            EventoSeleccionado is null)
        {
            Mensaje = "Selecciona un evento para actualizar.";
            return;
        }

        try
        {
            ServicioValidacionFiscal.ActualizarEvento(
                SesionActual.EmpresaId.Value,
                EventoSeleccionado.EventoDteId,
                TipoEvento,
                EstadoEvento,
                FechaEvento,
                SelloEvento,
                DetalleEventoJson);
            EventoSeleccionado = null;
            CargarHistorial();
            Mensaje = "Evento actualizado correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void EliminarEvento(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            EventoSeleccionado is null)
        {
            Mensaje = "Selecciona un evento para eliminar.";
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            "¿Deseas eliminar el evento seleccionado?",
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
            ServicioValidacionFiscal.EliminarEvento(
                SesionActual.EmpresaId.Value,
                EventoSeleccionado.EventoDteId);
            EventoSeleccionado = null;
            CargarHistorial();
            Mensaje = "Evento eliminado correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}