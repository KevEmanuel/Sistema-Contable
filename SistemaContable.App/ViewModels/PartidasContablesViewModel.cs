using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;

//modulo para partidas contables manuales 02
public class PartidasContablesViewModel : ViewModelBase
{
    private readonly RelayCommand agregarLineaCommand;
    private readonly RelayCommand quitarLineaCommand;
    private readonly RelayCommand guardarBorradorCommand;
    private readonly RelayCommand contabilizarCommand;
    private readonly RelayCommand cargarCommand;

    private PeriodoContable? periodoSeleccionado;
    private CuentaContable? cuentaSeleccionada;
    private DetallePartida? lineaSeleccionada;

    private DateTime fecha = DateTime.Today;
    private string glosa = string.Empty;
    private string? concepto;
    private decimal debe;
    private decimal haber;
    private decimal totalDebe;
    private decimal totalHaber;
    private string mensaje = string.Empty;

    public ObservableCollection<PeriodoContable> Periodos { get; } = new();

    public ObservableCollection<CuentaContable> Cuentas { get; } = new();

    public ObservableCollection<DetallePartida> Lineas { get; } = new();

    public ObservableCollection<PartidaContable> Partidas { get; } = new();

    public DateTime Fecha
    {
        get => fecha;
        set => EstablecerPropiedad(ref fecha, value);
    }

    public PeriodoContable? PeriodoSeleccionado
    {
        get => periodoSeleccionado;
        set => EstablecerPropiedad(ref periodoSeleccionado, value);
    }

    public string Glosa
    {
        get => glosa;
        set => EstablecerPropiedad(ref glosa, value);
    }

    public CuentaContable? CuentaSeleccionada
    {
        get => cuentaSeleccionada;
        set => EstablecerPropiedad(ref cuentaSeleccionada, value);
    }

    public string? Concepto
    {
        get => concepto;
        set => EstablecerPropiedad(ref concepto, value);
    }

    public decimal Debe
    {
        get => debe;
        set => EstablecerPropiedad(ref debe, value);
    }

    public decimal Haber
    {
        get => haber;
        set => EstablecerPropiedad(ref haber, value);
    }

    public DetallePartida? LineaSeleccionada
    {
        get => lineaSeleccionada;
        set => EstablecerPropiedad(ref lineaSeleccionada, value);
    }

    public decimal TotalDebe
    {
        get => totalDebe;
        private set => EstablecerPropiedad(ref totalDebe, value);
    }

    public decimal TotalHaber
    {
        get => totalHaber;
        private set => EstablecerPropiedad(ref totalHaber, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand AgregarLineaCommand => agregarLineaCommand;
    public ICommand QuitarLineaCommand => quitarLineaCommand;
    public ICommand GuardarBorradorCommand => guardarBorradorCommand;
    public ICommand ContabilizarCommand => contabilizarCommand;
    public ICommand CargarCommand => cargarCommand;

    public PartidasContablesViewModel()
    {
        agregarLineaCommand = new RelayCommand(AgregarLinea);
        quitarLineaCommand = new RelayCommand(QuitarLinea);
        guardarBorradorCommand =
            new RelayCommand(_ => GuardarPartida(false));
        contabilizarCommand =
            new RelayCommand(_ => GuardarPartida(true));
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

        Periodos.Clear();

        foreach (PeriodoContable periodo in
                 ServicioPartidasContables.ObtenerPeriodosAbiertos(empresaId))
        {
            Periodos.Add(periodo);
        }

        PeriodoSeleccionado = Periodos.FirstOrDefault(periodo =>
            Fecha.Date >= periodo.FechaInicio.Date &&
            Fecha.Date <= periodo.FechaFin.Date)
            ?? Periodos.FirstOrDefault();

        Cuentas.Clear();

        foreach (CuentaContable cuenta in
                 ServicioCatalogoCuentas.ObtenerPorEmpresa(empresaId)
                     .Where(cuenta =>
                         cuenta.Activa &&
                         cuenta.PermiteMovimientos))
        {
            Cuentas.Add(cuenta);
        }

        Partidas.Clear();

        foreach (PartidaContable partida in
                 ServicioPartidasContables.ObtenerPorEmpresa(empresaId))
        {
            Partidas.Add(partida);
        }
    }

    private void AgregarLinea(object? parametro)
    {
        if (CuentaSeleccionada is null)
        {
            Mensaje = "Selecciona una cuenta contable.";
            return;
        }

        bool tieneDebe = Debe > 0;
        bool tieneHaber = Haber > 0;

        if (Debe < 0 || Haber < 0 || tieneDebe == tieneHaber)
        {
            Mensaje =
                "Ingresa un valor únicamente en Debe o únicamente en Haber.";
            return;
        }

        Lineas.Add(new DetallePartida
        {
            CuentaContableId = CuentaSeleccionada.CuentaContableId,
            CuentaContable = CuentaSeleccionada,
            Concepto = Concepto,
            Debe = Debe,
            Haber = Haber
        });

        ActualizarTotales();

        CuentaSeleccionada = null;
        Concepto = null;
        Debe = 0;
        Haber = 0;

        Mensaje = "Línea agregada a la partida.";
    }

    private void QuitarLinea(object? parametro)
    {
        if (LineaSeleccionada is null)
        {
            Mensaje = "Selecciona una línea para quitarla.";
            return;
        }

        Lineas.Remove(LineaSeleccionada);
        LineaSeleccionada = null;
        ActualizarTotales();

        Mensaje = "Línea eliminada antes de guardar la partida.";
    }

    private void ActualizarTotales()
    {
        TotalDebe = Lineas.Sum(linea => linea.Debe);
        TotalHaber = Lineas.Sum(linea => linea.Haber);
    }

    private void GuardarPartida(bool contabilizar)
    {
        if (SesionActual.EmpresaId is null ||
            SesionActual.Usuario is null)
        {
            Mensaje = "No hay una sesión válida.";
            return;
        }

        if (PeriodoSeleccionado is null)
        {
            Mensaje = "Selecciona un período contable.";
            return;
        }

        try
        {
            PartidaContable partidaNueva =
                ServicioPartidasContables.CrearManual(
                    SesionActual.EmpresaId.Value,
                    PeriodoSeleccionado.PeriodoContableId,
                    SesionActual.Usuario.UsuarioId,
                    Fecha,
                    Glosa,
                    Lineas.ToList(),
                    contabilizar);

            Lineas.Clear();
            ActualizarTotales();

            Glosa = string.Empty;
            Concepto = null;
            Debe = 0;
            Haber = 0;

            CargarDatos();

            Mensaje = contabilizar
                ? $"Partida #{partidaNueva.NumeroPartida} contabilizada."
                : $"Partida #{partidaNueva.NumeroPartida} guardada como borrador.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}