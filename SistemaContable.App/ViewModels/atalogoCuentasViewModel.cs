/*using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;
//clase para entrelazar la el servicio de ServicioCatalogoCuentasViewModels.cs
public class CatalogoCuentasViewModel : ViewModelBase
{
    private readonly RelayCommand guardarCommand;
    private readonly RelayCommand cargarCommand;

    private string codigo = string.Empty;
    private string nombre = string.Empty;
    private string tipoCuenta = "Activo";
    private bool permiteMovimientos = true;
    private string mensaje = string.Empty;

    //cuenta contable
    private CuentaContable? cuentaPadreSeleccionada;

    public ObservableCollection<CuentaContable> Cuentas { get; } = new();

    public List<string> TiposCuenta { get; } = new()
    {
        "Activo",
        "Pasivo",
        "Patrimonio",
        "Ingreso",
        "Costo",
        "Gasto"
    };

    public string Codigo
    {
        get => codigo;
        set => EstablecerPropiedad(ref codigo, value);
    }

    public string Nombre
    {
        get => nombre;
        set => EstablecerPropiedad(ref nombre, value);
    }

    public string TipoCuenta
    {
        get => tipoCuenta;
        set => EstablecerPropiedad(ref tipoCuenta, value);
    }

    public bool PermiteMovimientos
    {
        get => permiteMovimientos;
        set => EstablecerPropiedad(ref permiteMovimientos, value);
    }
    public CuentaContable? CuentaPadreSeleccionada
    {
        get => cuentaPadreSeleccionada;
        set => EstablecerPropiedad(
            ref cuentaPadreSeleccionada,
            value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand GuardarCommand => guardarCommand;
    public ICommand CargarCommand => cargarCommand;

    public CatalogoCuentasViewModel()
    {
        guardarCommand = new RelayCommand(Guardar);
        cargarCommand = new RelayCommand(_ => CargarCuentas());

        CargarCuentas();
    }

    private void CargarCuentas()
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        Cuentas.Clear();

        List<CuentaContable> cuentas =
            ServicioCatalogoCuentas.ObtenerPorEmpresa(
                SesionActual.EmpresaId.Value);

        foreach (CuentaContable cuenta in cuentas)
        {
            Cuentas.Add(cuenta);
        }
    }

    private void Guardar(object? parametro)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            CuentaContable cuentaNueva =
                ServicioCatalogoCuentas.Crear(
                    SesionActual.EmpresaId.Value,
                    Codigo,
                    Nombre,
                    TipoCuenta,
                    CuentaPadreSeleccionada?.CuentaContableId,
                    CuentaPadreSeleccionada is null ? 1
                    : CuentaPadreSeleccionada.Nivel + 1,
                    PermiteMovimientos);

            Cuentas.Add(cuentaNueva);

            Codigo = string.Empty;
            Nombre = string.Empty;
            TipoCuenta = "Activo";
            PermiteMovimientos = true;
            //cuentas catalos de cuentas
            CuentaPadreSeleccionada = null;

            Mensaje = "Cuenta registrada correctamente.";
        }
        catch (System.Exception error)
        {
            Mensaje = error.Message;
        }
    }
}*/
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;

public class CatalogoCuentasViewModel : ViewModelBase
{
    private readonly RelayCommand guardarCommand;
    private readonly RelayCommand actualizarCommand;
    private readonly RelayCommand cambiarEstadoCommand;
    private readonly RelayCommand cargarCommand;

    private string codigo = string.Empty;
    private string nombre = string.Empty;
    private string tipoCuenta = "Activo";
    private bool permiteMovimientos = true;
    private string mensaje = string.Empty;

    private CuentaContable? cuentaPadreSeleccionada;
    private CuentaContable? cuentaSeleccionada;

    public ObservableCollection<CuentaContable> Cuentas { get; } = new();

    public List<string> TiposCuenta { get; } = new()
    {
        "Activo",
        "Pasivo",
        "Patrimonio",
        "Ingreso",
        "Costo",
        "Gasto"
    };

    public string Codigo
    {
        get => codigo;
        set => EstablecerPropiedad(ref codigo, value);
    }

    public string Nombre
    {
        get => nombre;
        set => EstablecerPropiedad(ref nombre, value);
    }

    public string TipoCuenta
    {
        get => tipoCuenta;
        set => EstablecerPropiedad(ref tipoCuenta, value);
    }

    public bool PermiteMovimientos
    {
        get => permiteMovimientos;
        set => EstablecerPropiedad(ref permiteMovimientos, value);
    }

    public CuentaContable? CuentaPadreSeleccionada
    {
        get => cuentaPadreSeleccionada;
        set => EstablecerPropiedad(
            ref cuentaPadreSeleccionada,
            value);
    }

    public CuentaContable? CuentaSeleccionada
    {
        get => cuentaSeleccionada;
        set
        {
            if (EstablecerPropiedad(
                ref cuentaSeleccionada,
                value) && value is not null)
            {
                Codigo = value.Codigo;
                Nombre = value.Nombre;
                TipoCuenta = value.TipoCuenta;
                PermiteMovimientos = value.PermiteMovimientos;

                // La cuenta padre no se modifica en esta primera versión.
                CuentaPadreSeleccionada = null;

                Mensaje = "Cuenta seleccionada. Puede actualizar sus datos o cambiar su estado.";
            }
        }
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand GuardarCommand => guardarCommand;
    public ICommand ActualizarCommand => actualizarCommand;
    public ICommand CambiarEstadoCommand => cambiarEstadoCommand;
    public ICommand CargarCommand => cargarCommand;

    public CatalogoCuentasViewModel()
    {
        guardarCommand = new RelayCommand(Guardar);
        actualizarCommand = new RelayCommand(Actualizar);
        cambiarEstadoCommand = new RelayCommand(CambiarEstado);
        cargarCommand = new RelayCommand(_ => CargarCuentas());

        CargarCuentas();
    }

    private void CargarCuentas()
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        Cuentas.Clear();

        List<CuentaContable> cuentas =
            ServicioCatalogoCuentas.ObtenerPorEmpresa(
                SesionActual.EmpresaId.Value);

        foreach (CuentaContable cuenta in cuentas)
        {
            Cuentas.Add(cuenta);
        }
    }

    private void Guardar(object? parametro)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            CuentaContable cuentaNueva =
                ServicioCatalogoCuentas.Crear(
                    SesionActual.EmpresaId.Value,
                    Codigo,
                    Nombre,
                    TipoCuenta,
                    CuentaPadreSeleccionada?.CuentaContableId,
                    CuentaPadreSeleccionada is null
                        ? 1
                        : CuentaPadreSeleccionada.Nivel + 1,
                    PermiteMovimientos);

            Cuentas.Add(cuentaNueva);

            LimpiarFormulario();
            Mensaje = "Cuenta registrada correctamente.";
        }
        catch (System.Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void Actualizar(object? parametro)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        if (CuentaSeleccionada is null)
        {
            Mensaje = "Seleccione una cuenta de la tabla para actualizar.";
            return;
        }

        try
        {
            ServicioCatalogoCuentas.ActualizarDatos(
                SesionActual.EmpresaId.Value,
                CuentaSeleccionada.CuentaContableId,
                Codigo,
                Nombre,
                TipoCuenta,
                PermiteMovimientos);

            CargarCuentas();
            LimpiarFormulario();

            Mensaje = "Cuenta actualizada correctamente.";
        }
        catch (System.Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void CambiarEstado(object? parametro)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        if (CuentaSeleccionada is null)
        {
            Mensaje = "Seleccione una cuenta de la tabla para activar o desactivar.";
            return;
        }

        try
        {
            ServicioCatalogoCuentas.CambiarEstado(
                SesionActual.EmpresaId.Value,
                CuentaSeleccionada.CuentaContableId);

            CargarCuentas();
            LimpiarFormulario();

            Mensaje = "Estado de la cuenta actualizado.";
        }
        catch (System.Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void LimpiarFormulario()
    {
        CuentaSeleccionada = null;
        CuentaPadreSeleccionada = null;
        Codigo = string.Empty;
        Nombre = string.Empty;
        TipoCuenta = "Activo";
        PermiteMovimientos = true;
    }
}