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
    private PeriodoContable? periodoSeleccionado;
    private TipoLibroIva tipoLibroSeleccionado;
    private string mensaje = string.Empty;

    public ObservableCollection<DocumentoDte> DocumentosValidados { get; }
        = new();

    public ObservableCollection<PeriodoContable> Periodos { get; }
        = new();

    public ObservableCollection<RegistroLibroIva> Registros { get; }
        = new();

    public List<TipoLibroIva> TiposLibro { get; } =
        Enum.GetValues<TipoLibroIva>().ToList();

    public DocumentoDte? DocumentoSeleccionado
    {
        get => documentoSeleccionado;
        set => EstablecerPropiedad(ref documentoSeleccionado, value);
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

    public TipoLibroIva TipoLibroSeleccionado
    {
        get => tipoLibroSeleccionado;
        set => EstablecerPropiedad(ref tipoLibroSeleccionado, value);
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
        tipoLibroSeleccionado = TipoLibroIva.Compras;

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
                TipoLibroSeleccionado);

            Mensaje = "Registro de Libro IVA generado correctamente.";

            DocumentoSeleccionado = null;

            CargarDatos();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}