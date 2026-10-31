using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;
//models, para control de la fiscalizacion de del libro de iva 05
public class LibroIvaViewModel : ViewModelBase
{
    private readonly RelayCommand generarCommand;
    private readonly RelayCommand cargarCommand;

    private DocumentoDte? documentoSeleccionado;
    private DocumentoDte? documentoRelacionadoSeleccionado;
    private PeriodoContable? periodoSeleccionado;
    private bool esNotaSeleccionada;
    private string tipoLibroSugerido = "Selecciona un DTE para ver su libro.";
    private string mensaje = string.Empty;

    public ObservableCollection<DocumentoDte> DocumentosValidados { get; }
        = new();

    public ObservableCollection<PeriodoContable> Periodos { get; }
        = new();

    public ObservableCollection<DocumentoDte> DocumentosRelacionables { get; }
        = new();

    public ObservableCollection<RegistroLibroIva> Registros { get; }
        = new();

    public DocumentoDte? DocumentoSeleccionado
    {
        get => documentoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref documentoSeleccionado, value))
            {
                EsNotaSeleccionada =
                    value?.TipoDte.Trim() is "05" or "06";
                DocumentoRelacionadoSeleccionado = null;
                CargarDocumentosRelacionables();
                ActualizarTipoLibroSugerido();
            }
        }
    }

    public DocumentoDte? DocumentoRelacionadoSeleccionado
    {
        get => documentoRelacionadoSeleccionado;
        set
        {
            if (EstablecerPropiedad(
                    ref documentoRelacionadoSeleccionado,
                    value))
            {
                ActualizarTipoLibroSugerido();
            }
        }
    }

    public bool EsNotaSeleccionada
    {
        get => esNotaSeleccionada;
        private set => EstablecerPropiedad(ref esNotaSeleccionada, value);
    }

    public string TipoLibroSugerido
    {
        get => tipoLibroSugerido;
        private set => EstablecerPropiedad(ref tipoLibroSugerido, value);
    }

    public PeriodoContable? PeriodoSeleccionado
    {
        get => periodoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref periodoSeleccionado, value))
            {
                CargarRegistros();
            }
        }
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand GenerarCommand => generarCommand;
    public ICommand CargarCommand => cargarCommand;

    public LibroIvaViewModel()
    {
        generarCommand = new RelayCommand(GenerarRegistro);
        cargarCommand = new RelayCommand(_ => CargarDatos());

        CargarDatos();
    }

    private void CargarDatos()
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        int empresaId = SesionActual.EmpresaId.Value;

        DocumentosValidados.Clear();
        DocumentoSeleccionado = null;

        foreach (DocumentoDte documento in
                 ServicioLibroIva.ObtenerDocumentosValidados(empresaId))
        {
            DocumentosValidados.Add(documento);
        }

        Periodos.Clear();

        foreach (PeriodoContable periodo in
                 ServicioPartidasContables.ObtenerPeriodosAbiertos(
                     empresaId))
        {
            Periodos.Add(periodo);
        }

        PeriodoSeleccionado = Periodos.FirstOrDefault();
        CargarRegistros();
    }

    private void CargarRegistros()
    {
        Registros.Clear();

        if (SesionActual.EmpresaId is null ||
            PeriodoSeleccionado is null)
        {
            return;
        }

        foreach (RegistroLibroIva registro in
                 ServicioLibroIva.ObtenerPorPeriodo(
                     SesionActual.EmpresaId.Value,
                     PeriodoSeleccionado.PeriodoContableId))
        {
            Registros.Add(registro);
        }
    }

    private void GenerarRegistro(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            DocumentoSeleccionado is null ||
            PeriodoSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE y un período.";
            return;
        }

        try
        {
            ServicioLibroIva.Generar(
                SesionActual.EmpresaId.Value,
                DocumentoSeleccionado.DocumentoDteId,
                PeriodoSeleccionado.PeriodoContableId,
                DocumentoRelacionadoSeleccionado?.DocumentoDteId);

            Mensaje = "Registro de Libro IVA generado correctamente.";

            DocumentoSeleccionado = null;

            CargarDatos();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void CargarDocumentosRelacionables()
    {
        DocumentosRelacionables.Clear();

        if (!EsNotaSeleccionada ||
            SesionActual.EmpresaId is not int empresaId ||
            DocumentoSeleccionado is null)
        {
            return;
        }

        foreach (DocumentoDte documento in
                 ServicioLibroIva.ObtenerDocumentosRelacionables(
                     empresaId,
                     DocumentoSeleccionado.DocumentoDteId))
        {
            DocumentosRelacionables.Add(documento);
        }

        if (DocumentoSeleccionado.DocumentoRelacionadoId is int relacionadoId)
        {
            DocumentoRelacionadoSeleccionado =
                DocumentosRelacionables.FirstOrDefault(documento =>
                    documento.DocumentoDteId == relacionadoId);
        }
    }

    private void ActualizarTipoLibroSugerido()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            DocumentoSeleccionado is null)
        {
            TipoLibroSugerido = "Selecciona un DTE para ver su libro.";
            return;
        }

        try
        {
            TipoLibroIva tipoLibro =
                ServicioLibroIva.ObtenerTipoLibroSugerido(
                    empresaId,
                    DocumentoSeleccionado.DocumentoDteId,
                    DocumentoRelacionadoSeleccionado?.DocumentoDteId);

            TipoLibroSugerido = $"Se registrará en: {tipoLibro}.";
        }
        catch (InvalidOperationException error)
        {
            TipoLibroSugerido = error.Message;
        }
    }
}