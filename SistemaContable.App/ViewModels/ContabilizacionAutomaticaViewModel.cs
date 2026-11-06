using System.Collections.ObjectModel;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;

public class ContabilizacionAutomaticaViewModel : ViewModelBase
{
    private readonly RelayCommand guardarConfiguracionCommand;
    private readonly RelayCommand nuevaConfiguracionCommand;
    private readonly RelayCommand generarBorradorCommand;
    private readonly RelayCommand generarBorradoresTodosCommand;
    private readonly RelayCommand contabilizarBorradorCommand;
    private readonly RelayCommand eliminarBorradorCommand;
    private readonly RelayCommand cargarCommand;

    private DocumentoDte? documentoSeleccionado;
    private PartidaContable? borradorSeleccionado;
    private PeriodoContable? periodoSeleccionado;
    private CuentaContable? cuentaClientesSeleccionada;
    private CuentaContable? cuentaVentasSeleccionada;
    private CuentaContable? cuentaIvaDebitoSeleccionada;
    private CuentaContable? cuentaProveedoresSeleccionada;
    private CuentaContable? cuentaComprasSeleccionada;
    private CuentaContable? cuentaIvaAcreditableSeleccionada;
    private ConfiguracionContableAutomatica? configuracionSeleccionada;
    private OperacionConfiguracionDte operacionSeleccionada =
        OperacionConfiguracionDte.RevisionManual;
    private string tipoDteCodigo = string.Empty;
    private bool codigoDteBloqueado;
    private string mensaje = string.Empty;

    public ObservableCollection<DocumentoDte> DtesPendientes { get; } = new();
    public ObservableCollection<PartidaContable> Borradores { get; } = new();
    public ObservableCollection<PeriodoContable> Periodos { get; } = new();
    public ObservableCollection<CuentaContable> Cuentas { get; } = new();
    public ObservableCollection<ConfiguracionContableAutomatica> Configuraciones { get; } = new();
    public ObservableCollection<string> TiposDteDisponibles { get; } = new();

    public DocumentoDte? DocumentoSeleccionado
    {
        get => documentoSeleccionado;
        set => EstablecerPropiedad(ref documentoSeleccionado, value);
    }

    public bool CodigoDteBloqueado
    {
        get => codigoDteBloqueado;
        private set => EstablecerPropiedad(ref codigoDteBloqueado, value);
    }

    public PartidaContable? BorradorSeleccionado
    {
        get => borradorSeleccionado;
        set => EstablecerPropiedad(ref borradorSeleccionado, value);
    }

    public PeriodoContable? PeriodoSeleccionado
    {
        get => periodoSeleccionado;
        set => EstablecerPropiedad(ref periodoSeleccionado, value);
    }

    public CuentaContable? CuentaClientesSeleccionada
    {
        get => cuentaClientesSeleccionada;
        set => EstablecerPropiedad(ref cuentaClientesSeleccionada, value);
    }

    public CuentaContable? CuentaVentasSeleccionada
    {
        get => cuentaVentasSeleccionada;
        set => EstablecerPropiedad(ref cuentaVentasSeleccionada, value);
    }

    public CuentaContable? CuentaIvaDebitoSeleccionada
    {
        get => cuentaIvaDebitoSeleccionada;
        set => EstablecerPropiedad(ref cuentaIvaDebitoSeleccionada, value);
    }

    public CuentaContable? CuentaProveedoresSeleccionada
    {
        get => cuentaProveedoresSeleccionada;
        set => EstablecerPropiedad(ref cuentaProveedoresSeleccionada, value);
    }

    public CuentaContable? CuentaComprasSeleccionada
    {
        get => cuentaComprasSeleccionada;
        set => EstablecerPropiedad(ref cuentaComprasSeleccionada, value);
    }

    public CuentaContable? CuentaIvaAcreditableSeleccionada
    {
        get => cuentaIvaAcreditableSeleccionada;
        set => EstablecerPropiedad(ref cuentaIvaAcreditableSeleccionada, value);
    }

    public ConfiguracionContableAutomatica? ConfiguracionSeleccionada
    {
        get => configuracionSeleccionada;
        set
        {
            EstablecerPropiedad(ref configuracionSeleccionada, value);
            CodigoDteBloqueado = value is not null;
            CargarCamposConfiguracion(value);
        }
    }

    public OperacionConfiguracionDte OperacionSeleccionada
    {
        get => operacionSeleccionada;
        set => EstablecerPropiedad(ref operacionSeleccionada, value);
    }

    public string TipoDteCodigo
    {
        get => tipoDteCodigo;
        set => EstablecerPropiedad(ref tipoDteCodigo, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand GuardarConfiguracionCommand => guardarConfiguracionCommand;
    public ICommand NuevaConfiguracionCommand => nuevaConfiguracionCommand;
    public ICommand GenerarBorradorCommand => generarBorradorCommand;
    public ICommand GenerarBorradoresTodosCommand =>
        generarBorradoresTodosCommand;
    public ICommand ContabilizarBorradorCommand => contabilizarBorradorCommand;
    public ICommand EliminarBorradorCommand => eliminarBorradorCommand;
    public ICommand CargarCommand => cargarCommand;

    public ContabilizacionAutomaticaViewModel()
    {
        guardarConfiguracionCommand =
            new RelayCommand(_ => GuardarConfiguracion());
        nuevaConfiguracionCommand =
            new RelayCommand(_ => NuevaConfiguracion());
        generarBorradorCommand = new RelayCommand(_ => GenerarBorrador());
        generarBorradoresTodosCommand =
            new RelayCommand(_ => GenerarBorradoresTodos());
        contabilizarBorradorCommand =
            new RelayCommand(_ => ContabilizarBorrador());
        eliminarBorradorCommand = new RelayCommand(_ => EliminarBorrador());
        cargarCommand = new RelayCommand(_ => CargarDatos());

        CargarDatos();
    }

    private void CargarDatos()
    {
        if (SesionActual.EmpresaId is not int empresaId)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            Cuentas.Clear();
            foreach (CuentaContable cuenta in
                     ServicioCatalogoCuentas.ObtenerPorEmpresa(empresaId)
                         .Where(cuenta =>
                             cuenta.Activa &&
                             cuenta.PermiteMovimientos))
            {
                Cuentas.Add(cuenta);
            }

            Periodos.Clear();
            foreach (PeriodoContable periodo in
                     ServicioPartidasContables.ObtenerPeriodosAbiertos(
                         empresaId))
            {
                Periodos.Add(periodo);
            }

            PeriodoSeleccionado = Periodos.FirstOrDefault(periodo =>
                periodo.FechaInicio.Date <= DateTime.Today &&
                periodo.FechaFin.Date >= DateTime.Today)
                ?? Periodos.FirstOrDefault();

            CargarConfiguraciones(empresaId);
            CargarDtes(empresaId);
            CargarBorradores(empresaId);

            if (Periodos.Count == 0)
            {
                Mensaje = "Crea un período contable abierto para continuar.";
            }
            else if (DtesPendientes.Count == 0 && Borradores.Count == 0)
            {
                Mensaje =
                    "No hay DTE validados pendientes ni borradores por revisar.";
            }
            else
            {
                Mensaje = string.Empty;
            }
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void CargarConfiguraciones(int empresaId)
    {
        TiposDteDisponibles.Clear();
        foreach (string tipoDte in
                 ServicioContabilizacionDteSencilla.ObtenerTiposDteDisponibles(
                     empresaId))
        {
            TiposDteDisponibles.Add(tipoDte);
        }

        Configuraciones.Clear();
        foreach (ConfiguracionContableAutomatica configuracion in
                 ServicioContabilizacionDteSencilla.ObtenerConfiguraciones(
                     empresaId))
        {
            Configuraciones.Add(configuracion);
        }

        ConfiguracionSeleccionada = Configuraciones.FirstOrDefault();
        if (ConfiguracionSeleccionada is null)
        {
            NuevaConfiguracion();
        }
    }

    private void CargarCamposConfiguracion(
        ConfiguracionContableAutomatica? configuracion)
    {
        if (configuracion is null)
        {
            return;
        }

        TipoDteCodigo = configuracion.TipoDte;
        OperacionSeleccionada = configuracion.Operacion;
        CuentaClientesSeleccionada = ObtenerCuenta(
            configuracion.CuentaClientesId);
        CuentaVentasSeleccionada = ObtenerCuenta(
            configuracion.CuentaVentasId);
        CuentaIvaDebitoSeleccionada = ObtenerCuenta(
            configuracion.CuentaIvaDebitoId);
        CuentaProveedoresSeleccionada = ObtenerCuenta(
            configuracion.CuentaProveedoresId);
        CuentaComprasSeleccionada = ObtenerCuenta(
            configuracion.CuentaComprasId);
        CuentaIvaAcreditableSeleccionada = ObtenerCuenta(
            configuracion.CuentaIvaAcreditableId);
    }

    private void NuevaConfiguracion()
    {
        ConfiguracionSeleccionada = null;
        TipoDteCodigo = string.Empty;
        OperacionSeleccionada = OperacionConfiguracionDte.RevisionManual;
        CuentaClientesSeleccionada = null;
        CuentaVentasSeleccionada = null;
        CuentaIvaDebitoSeleccionada = null;
        CuentaProveedoresSeleccionada = null;
        CuentaComprasSeleccionada = null;
        CuentaIvaAcreditableSeleccionada = null;
    }

    private CuentaContable? ObtenerCuenta(int? cuentaId) =>
        cuentaId is null
            ? null
            : Cuentas.FirstOrDefault(cuenta =>
                cuenta.CuentaContableId == cuentaId.Value);

    private void CargarDtes(int empresaId)
    {
        DtesPendientes.Clear();
        foreach (DocumentoDte documento in
                 ServicioContabilizacionAutomatica.ObtenerDtesPendientes(
                     empresaId))
        {
            DtesPendientes.Add(documento);
        }

        DocumentoSeleccionado = DtesPendientes.FirstOrDefault();
    }

    private void CargarBorradores(int empresaId)
    {
        Borradores.Clear();
        foreach (PartidaContable partida in
                 ServicioContabilizacionDteSencilla.ObtenerBorradores(
                     empresaId))
        {
            Borradores.Add(partida);
        }

        BorradorSeleccionado = Borradores.FirstOrDefault();
    }

    private void GuardarConfiguracion()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null)
        {
            Mensaje = "No hay una empresa o un usuario activo en la sesión.";
            return;
        }

        try
        {
            string tipoDte = TipoDteCodigo.Trim().ToUpperInvariant();
            ServicioContabilizacionDteSencilla.GuardarConfiguracion(
                empresaId,
                SesionActual.Usuario.UsuarioId,
                new ConfiguracionContableAutomatica
                {
                    TipoDte = tipoDte,
                    Operacion = OperacionSeleccionada,
                    CuentaClientesId =
                        CuentaClientesSeleccionada?.CuentaContableId,
                    CuentaVentasId = CuentaVentasSeleccionada?.CuentaContableId,
                    CuentaIvaDebitoId =
                        CuentaIvaDebitoSeleccionada?.CuentaContableId,
                    CuentaProveedoresId =
                        CuentaProveedoresSeleccionada?.CuentaContableId,
                    CuentaComprasId =
                        CuentaComprasSeleccionada?.CuentaContableId,
                    CuentaIvaAcreditableId =
                        CuentaIvaAcreditableSeleccionada?.CuentaContableId
                });

            CargarConfiguraciones(empresaId);
            ConfiguracionSeleccionada = Configuraciones.FirstOrDefault(item =>
                item.TipoDte == tipoDte);
            Mensaje = $"Configuración guardada para el tipo DTE {tipoDte}.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void GenerarBorrador()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null ||
            DocumentoSeleccionado is null ||
            PeriodoSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE validado y un período contable.";
            return;
        }

        try
        {
            PartidaContable borrador =
                ServicioContabilizacionDteSencilla.CrearBorrador(
                    empresaId,
                    SesionActual.Usuario.UsuarioId,
                    PeriodoSeleccionado.PeriodoContableId,
                    DocumentoSeleccionado.DocumentoDteId);

            string mensajeExito =
                $"Borrador #{borrador.NumeroPartida} creado. Revísalo y confírmalo para incluirlo en los libros.";
            CargarDatos();
            Mensaje = mensajeExito;
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void ContabilizarBorrador()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null ||
            BorradorSeleccionado is null)
        {
            Mensaje = "Selecciona un borrador para revisar y confirmar.";
            return;
        }

        try
        {
            PartidaContable partida = ServicioPartidasContables
                .ContabilizarBorrador(
                    empresaId,
                    BorradorSeleccionado.PartidaContableId,
                    SesionActual.Usuario.UsuarioId);

            string mensajeExito =
                $"Partida #{partida.NumeroPartida} confirmada. Se agregó a Diario y Mayor y, para Factura/Crédito fiscal, al libro de ventas o compras.";
            CargarDatos();
            Mensaje = mensajeExito;
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void GenerarBorradoresTodos()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null ||
            PeriodoSeleccionado is null)
        {
            Mensaje = "Selecciona un período contable para continuar.";
            return;
        }

        List<DocumentoDte> pendientes = DtesPendientes.ToList();
        if (pendientes.Count == 0)
        {
            Mensaje = "No hay DTE pendientes para preparar.";
            return;
        }

        int generados = 0;
        var observaciones = new List<string>();
        try
        {
            foreach (DocumentoDte documento in pendientes)
            {
                try
                {
                    ServicioContabilizacionDteSencilla.CrearBorrador(
                        empresaId,
                        SesionActual.Usuario.UsuarioId,
                        PeriodoSeleccionado.PeriodoContableId,
                        documento.DocumentoDteId);
                    generados++;
                }
                catch (InvalidOperationException error)
                {
                    observaciones.Add(
                        $"{documento.NumeroControl}: {error.Message}");
                }
            }
        }
        catch (Exception error)
        {
            CargarDatos();
            Mensaje =
                $"Se prepararon {generados} borradores antes de interrumpir el proceso. Error: {error.Message}";
            return;
        }

        CargarDatos();
        Mensaje = observaciones.Count == 0
            ? $"Se prepararon {generados} borradores. Revísalos antes de confirmarlos."
            : $"Borradores preparados: {generados}. Requieren revisión: " +
              string.Join(" | ", observaciones);
    }

    private void EliminarBorrador()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null ||
            BorradorSeleccionado is null)
        {
            Mensaje = "Selecciona un borrador para eliminar.";
            return;
        }

        try
        {
            ServicioContabilizacionDteSencilla.EliminarBorrador(
                empresaId,
                SesionActual.Usuario.UsuarioId,
                BorradorSeleccionado.PartidaContableId);
            CargarDatos();
            Mensaje =
                "Borrador eliminado. El DTE seguirá disponible para generar otro.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}
