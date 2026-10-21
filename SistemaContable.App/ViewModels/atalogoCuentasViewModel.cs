using System.Collections.Generic;
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
                    null,
                    1,
                    PermiteMovimientos);

            Cuentas.Add(cuentaNueva);

            Codigo = string.Empty;
            Nombre = string.Empty;
            TipoCuenta = "Activo";
            PermiteMovimientos = true;

            Mensaje = "Cuenta registrada correctamente.";
        }
        catch (System.Exception error)
        {
            Mensaje = error.Message;
        }
    }
}