using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;
//contablizacion automaatica modulo 06
public class ContabilizacionAutomaticaViewModel : ViewModelBase
{
    private readonly RelayCommand contabilizarCommand;
    private readonly RelayCommand cargarCommand;

    private DocumentoDte? documentoSeleccionado;
    private ReglaContable? reglaSeleccionada;
    private PeriodoContable? periodoSeleccionado;
    private string mensaje = string.Empty;

    public ObservableCollection<DocumentoDte> DtesPendientes { get; }
        = new();

    public ObservableCollection<ReglaContable> Reglas { get; }
        = new();

    public ObservableCollection<PeriodoContable> Periodos { get; }
        = new();

    public ObservableCollection<ReglaCuenta> LineasRegla { get; }
        = new();

    public DocumentoDte? DocumentoSeleccionado
    {
        get => documentoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref documentoSeleccionado, value))
            {
                CargarReglas();
            }
        }
    }

    public ReglaContable? ReglaSeleccionada
    {
        get => reglaSeleccionada;
        set
        {
            if (EstablecerPropiedad(ref reglaSeleccionada, value))
            {
                CargarLineasRegla();
            }
        }
    }

    public PeriodoContable? PeriodoSeleccionado
    {
        get => periodoSeleccionado;
        set => EstablecerPropiedad(ref periodoSeleccionado, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand ContabilizarCommand => contabilizarCommand;
    public ICommand CargarCommand => cargarCommand;

    public ContabilizacionAutomaticaViewModel()
    {
        contabilizarCommand = new RelayCommand(Contabilizar);
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

        DtesPendientes.Clear();

        foreach (DocumentoDte documento in
                 ServicioContabilizacionAutomatica.ObtenerDtesPendientes(
                     empresaId))
        {
            DtesPendientes.Add(documento);
        }

        Periodos.Clear();

        foreach (PeriodoContable periodo in
                 ServicioPartidasContables.ObtenerPeriodosAbiertos(
                     empresaId))
        {
            Periodos.Add(periodo);
        }

        PeriodoSeleccionado = Periodos.FirstOrDefault();
        DocumentoSeleccionado = null;
        ReglaSeleccionada = null;
        Reglas.Clear();
        LineasRegla.Clear();
    }

    private void CargarReglas()
    {
        Reglas.Clear();
        LineasRegla.Clear();
        ReglaSeleccionada = null;

        if (SesionActual.EmpresaId is null ||
            DocumentoSeleccionado is null)
        {
            return;
        }

        foreach (ReglaContable regla in
                 ServicioContabilizacionAutomatica.ObtenerReglasVigentes(
                     SesionActual.EmpresaId.Value,
                     DocumentoSeleccionado.FechaEmision)
                     .Where(regla =>
                         string.IsNullOrWhiteSpace(regla.TipoDte) ||
                         regla.TipoDte == DocumentoSeleccionado.TipoDte))
        {
            Reglas.Add(regla);
        }
    }

    private void CargarLineasRegla()
    {
        LineasRegla.Clear();

        if (ReglaSeleccionada is null)
        {
            return;
        }

        foreach (ReglaCuenta linea in ReglaSeleccionada.Cuentas
                     .OrderBy(linea => linea.Orden))
        {
            LineasRegla.Add(linea);
        }
    }

    private void Contabilizar(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            SesionActual.Usuario is null ||
            DocumentoSeleccionado is null ||
            ReglaSeleccionada is null ||
            PeriodoSeleccionado is null)
        {
            Mensaje =
                "Selecciona DTE, regla contable y período.";
            return;
        }

        try
        {
            PartidaContable partida =
                ServicioContabilizacionAutomatica.Contabilizar(
                    SesionActual.EmpresaId.Value,
                    SesionActual.Usuario.UsuarioId,
                    PeriodoSeleccionado.PeriodoContableId,
                    DocumentoSeleccionado.DocumentoDteId,
                    ReglaSeleccionada.ReglaContableId);

            Mensaje =
                $"Partida #{partida.NumeroPartida} generada correctamente.";

            CargarDatos();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}