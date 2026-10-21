//clase pare enlazar  la clase ServicioReglasContables.cs, en Services carpeta.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

//modulo para gestioanar las reglas contables

namespace SistemaContableDTE.ViewModels;

public class ReglasContablesViewModel : ViewModelBase
{
    private readonly RelayCommand crearReglaCommand;
    private readonly RelayCommand agregarCuentaCommand;
    private readonly RelayCommand cambiarEstadoCommand;
    private readonly RelayCommand cargarCommand;

    private ReglaContable? reglaSeleccionada;
    private CuentaContable? cuentaSeleccionada;

    private string nombreRegla = string.Empty;
    private string? tipoDte;
    private RolEmpresaDte rolEmpresaSeleccionado =
        RolEmpresaDte.NoDeterminado;
    private string condicionJson = "{}";
    private DateTime vigenciaDesde = DateTime.Today;
    private DateTime? vigenciaHasta;

    private LadoContable ladoSeleccionado = LadoContable.Debe;
    private string formula = string.Empty;
    private string mensaje = string.Empty;

    public ObservableCollection<ReglaContable> Reglas { get; } = new();

    public ObservableCollection<CuentaContable> CuentasCatalogo { get; }
        = new();

    public ObservableCollection<ReglaCuenta> LineasRegla { get; }
        = new();

    public List<RolEmpresaDte> RolesEmpresa { get; } =
        Enum.GetValues<RolEmpresaDte>().ToList();

    public List<LadoContable> LadosContables { get; } =
        Enum.GetValues<LadoContable>().ToList();

    public string NombreRegla
    {
        get => nombreRegla;
        set => EstablecerPropiedad(ref nombreRegla, value);
    }

    public string? TipoDte
    {
        get => tipoDte;
        set => EstablecerPropiedad(ref tipoDte, value);
    }

    public RolEmpresaDte RolEmpresaSeleccionado
    {
        get => rolEmpresaSeleccionado;
        set => EstablecerPropiedad(
            ref rolEmpresaSeleccionado,
            value);
    }

    public string CondicionJson
    {
        get => condicionJson;
        set => EstablecerPropiedad(ref condicionJson, value);
    }

    public DateTime VigenciaDesde
    {
        get => vigenciaDesde;
        set => EstablecerPropiedad(ref vigenciaDesde, value);
    }

    public DateTime? VigenciaHasta
    {
        get => vigenciaHasta;
        set => EstablecerPropiedad(ref vigenciaHasta, value);
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

    public CuentaContable? CuentaSeleccionada
    {
        get => cuentaSeleccionada;
        set => EstablecerPropiedad(ref cuentaSeleccionada, value);
    }

    public LadoContable LadoSeleccionado
    {
        get => ladoSeleccionado;
        set => EstablecerPropiedad(ref ladoSeleccionado, value);
    }

    public string Formula
    {
        get => formula;
        set => EstablecerPropiedad(ref formula, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand CrearReglaCommand => crearReglaCommand;
    public ICommand AgregarCuentaCommand => agregarCuentaCommand;
    public ICommand CambiarEstadoCommand => cambiarEstadoCommand;
    public ICommand CargarCommand => cargarCommand;

    public ReglasContablesViewModel()
    {
        crearReglaCommand = new RelayCommand(CrearRegla);
        agregarCuentaCommand = new RelayCommand(AgregarCuenta);
        cambiarEstadoCommand = new RelayCommand(CambiarEstado);
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

        Reglas.Clear();

        foreach (ReglaContable regla in
                 ServicioReglasContables.ObtenerPorEmpresa(empresaId))
        {
            Reglas.Add(regla);
        }

        CuentasCatalogo.Clear();

        foreach (CuentaContable cuenta in
                 ServicioCatalogoCuentas.ObtenerPorEmpresa(empresaId)
                     .Where(cuenta => cuenta.Activa))
        {
            CuentasCatalogo.Add(cuenta);
        }

        LineasRegla.Clear();
        ReglaSeleccionada = null;
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

    private void CrearRegla(object? parametro)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            ReglaContable reglaNueva =
                ServicioReglasContables.Crear(
                    SesionActual.EmpresaId.Value,
                    NombreRegla,
                    TipoDte,
                    RolEmpresaSeleccionado,
                    CondicionJson,
                    VigenciaDesde,
                    VigenciaHasta);

            Reglas.Add(reglaNueva);
            ReglaSeleccionada = reglaNueva;

            NombreRegla = string.Empty;
            TipoDte = null;
            RolEmpresaSeleccionado = RolEmpresaDte.NoDeterminado;
            CondicionJson = "{}";
            VigenciaDesde = DateTime.Today;
            VigenciaHasta = null;

            Mensaje = "Regla contable creada correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void AgregarCuenta(object? parametro)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        if (ReglaSeleccionada is null)
        {
            Mensaje = "Primero selecciona o crea una regla contable.";
            return;
        }

        if (CuentaSeleccionada is null)
        {
            Mensaje = "Selecciona una cuenta contable.";
            return;
        }

        try
        {
            ReglaCuenta lineaNueva =
                ServicioReglasContables.AgregarCuenta(
                    SesionActual.EmpresaId.Value,
                    ReglaSeleccionada.ReglaContableId,
                    CuentaSeleccionada.CuentaContableId,
                    LadoSeleccionado,
                    Formula);

            LineasRegla.Add(lineaNueva);
            //modificacion para agregar una linea en las reglas de contabilidad
            ReglaSeleccionada.Cuentas.Add(lineaNueva);


            CuentaSeleccionada = null;
            LadoSeleccionado = LadoContable.Debe;
            Formula = string.Empty;

            Mensaje = "Línea contable agregada a la regla.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void CambiarEstado(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            ReglaSeleccionada is null)
        {
            Mensaje = "Selecciona una regla contable.";
            return;
        }

        try
        {
            ServicioReglasContables.CambiarEstado(
                SesionActual.EmpresaId.Value,
                ReglaSeleccionada.ReglaContableId);

            Mensaje = "Se actualizó el estado de la regla.";
            CargarDatos();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}