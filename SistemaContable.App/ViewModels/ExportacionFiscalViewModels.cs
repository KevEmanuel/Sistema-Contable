using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;
//Modulo de exportacion fiscal 07
namespace SistemaContableDTE.ViewModels;

public class ExportacionFiscalViewModel : ViewModelBase
{
    private readonly RelayCommand generarCommand;
    private readonly RelayCommand cargarCommand;

    private PeriodoContable? periodoSeleccionado;
    private TipoLibroIva tipoLibroSeleccionado;
    private ExportacionFiscal? exportacionSeleccionada;
    private string versionFormato = "F07v14";
    private string mensaje = string.Empty;

    public ObservableCollection<PeriodoContable> Periodos { get; } = new();

    public ObservableCollection<ExportacionFiscal> Exportaciones { get; }
        = new();

    public List<TipoLibroIva> TiposLibro { get; } =
        Enum.GetValues<TipoLibroIva>().ToList();

    public PeriodoContable? PeriodoSeleccionado
    {
        get => periodoSeleccionado;
        set => EstablecerPropiedad(ref periodoSeleccionado, value);
    }

    public TipoLibroIva TipoLibroSeleccionado
    {
        get => tipoLibroSeleccionado;
        set => EstablecerPropiedad(ref tipoLibroSeleccionado, value);
    }

    public string VersionFormato
    {
        get => versionFormato;
        set => EstablecerPropiedad(ref versionFormato, value);
    }

    public ExportacionFiscal? ExportacionSeleccionada
    {
        get => exportacionSeleccionada;
        set => EstablecerPropiedad(ref exportacionSeleccionada, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand GenerarCommand => generarCommand;
    public ICommand CargarCommand => cargarCommand;

    public ExportacionFiscalViewModel()
    {
        tipoLibroSeleccionado = TipoLibroIva.Compras;

        generarCommand = new RelayCommand(Generar);
        cargarCommand = new RelayCommand(_ => CargarDatos());

        CargarDatos();
    }

    public void GuardarArchivo(string rutaArchivo)
    {
        if (SesionActual.EmpresaId is null ||
            ExportacionSeleccionada is null)
        {
            Mensaje = "Selecciona una exportación fiscal.";
            return;
        }

        try
        {
            string contenido =
                ServicioExportacionFiscal.ObtenerContenido(
                    SesionActual.EmpresaId.Value,
                    ExportacionSeleccionada.ExportacionFiscalId);

            File.WriteAllText(
                rutaArchivo,
                contenido,
                new UTF8Encoding(false));

            Mensaje = "Archivo CSV guardado correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void CargarDatos()
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        int empresaId = SesionActual.EmpresaId.Value;

        Periodos.Clear();

        foreach (PeriodoContable periodo in
                 ServicioPartidasContables.ObtenerPeriodosAbiertos(
                     empresaId))
        {
            Periodos.Add(periodo);
        }

        PeriodoSeleccionado = Periodos.FirstOrDefault();

        Exportaciones.Clear();

        foreach (ExportacionFiscal exportacion in
                 ServicioExportacionFiscal.ObtenerPorEmpresa(empresaId))
        {
            Exportaciones.Add(exportacion);
        }
    }

    private void Generar(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            SesionActual.Usuario is null ||
            PeriodoSeleccionado is null)
        {
            Mensaje = "Selecciona un período contable.";
            return;
        }

        try
        {
            ExportacionFiscal exportacion =
                ServicioExportacionFiscal.Generar(
                    SesionActual.EmpresaId.Value,
                    SesionActual.Usuario.UsuarioId,
                    PeriodoSeleccionado.PeriodoContableId,
                    TipoLibroSeleccionado,
                    VersionFormato);

            Exportaciones.Insert(0, exportacion);
            ExportacionSeleccionada = exportacion;

            Mensaje =
                "Exportación fiscal generada y almacenada correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}