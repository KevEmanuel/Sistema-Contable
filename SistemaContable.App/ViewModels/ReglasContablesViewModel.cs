//clase pare enlazar  la clase ServicioReglasContables.cs, en Services carpeta.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

//modulo para gestioanar las reglas contables

namespace SistemaContableDTE.ViewModels;

public sealed record OpcionTipoDte(string Codigo, string Descripcion);

public sealed record OpcionRolEmpresa(
    RolEmpresaDte Valor,
    string Descripcion);

public sealed record OpcionFormulaContable(
    string Valor,
    string Descripcion,
    bool EsImporteFijo = false);

public sealed record OpcionCampoCondicion(
    CampoCondicionRegla Valor,
    string Descripcion);

public sealed record OpcionOperadorCondicion(
    OperadorCondicionRegla Valor,
    string Descripcion);

public sealed class CondicionReglaEditor : ViewModelBase
{
    private CampoCondicionRegla campo = CampoCondicionRegla.TotalOperacion;
    private OperadorCondicionRegla operador =
        OperadorCondicionRegla.MayorOIgual;
    private string valor = string.Empty;

    public List<OpcionCampoCondicion> Campos { get; } =
    [
        new(CampoCondicionRegla.TotalOperacion, "Total de la operación"),
        new(CampoCondicionRegla.BaseGravada, "Base gravada"),
        new(CampoCondicionRegla.Iva, "IVA"),
        new(CampoCondicionRegla.MontoExento, "Monto exento"),
        new(CampoCondicionRegla.MontoNoSujeto, "Monto no sujeto")
    ];

    public List<OpcionOperadorCondicion> Operadores { get; } =
    [
        new(OperadorCondicionRegla.Igual, "es igual a"),
        new(OperadorCondicionRegla.MayorQue, "es mayor que"),
        new(OperadorCondicionRegla.MayorOIgual, "es al menos"),
        new(OperadorCondicionRegla.MenorQue, "es menor que"),
        new(OperadorCondicionRegla.MenorOIgual, "es como máximo")
    ];

    public CampoCondicionRegla Campo
    {
        get => campo;
        set => EstablecerPropiedad(ref campo, value);
    }

    public OperadorCondicionRegla Operador
    {
        get => operador;
        set => EstablecerPropiedad(ref operador, value);
    }

    public string Valor
    {
        get => valor;
        set => EstablecerPropiedad(ref this.valor, value);
    }

    public CondicionReglaContable CrearCondicion()
    {
        const NumberStyles estiloImporte =
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        if (!decimal.TryParse(
                Valor,
                estiloImporte,
                CultureInfo.CurrentCulture,
                out decimal valorCondicion) &&
            !decimal.TryParse(
                Valor,
                estiloImporte,
                CultureInfo.InvariantCulture,
                out valorCondicion))
        {
            throw new InvalidOperationException(
                "Escribe un importe válido para cada condición.");
        }

        return new CondicionReglaContable
        {
            Campo = Campo,
            Operador = Operador,
            Valor = valorCondicion
        };
    }
}

public class ReglasContablesViewModel : ViewModelBase
{
    private readonly RelayCommand crearReglaCommand;
    private readonly RelayCommand agregarCuentaCommand;
    private readonly RelayCommand cambiarEstadoCommand;
    private readonly RelayCommand cargarCommand;
    private readonly RelayCommand agregarCondicionCommand;
    private readonly RelayCommand quitarCondicionCommand;
    private readonly RelayCommand actualizarReglaCommand;
    private readonly RelayCommand eliminarReglaCommand;
    private readonly RelayCommand actualizarCuentaCommand;
    private readonly RelayCommand eliminarCuentaCommand;

    private ReglaContable? reglaSeleccionada;
    private CuentaContable? cuentaSeleccionada;
    private OpcionTipoDte? tipoDteSeleccionado;
    private OpcionFormulaContable? formulaSeleccionada;
    private CondicionReglaEditor? condicionSeleccionada;
    private ReglaCuenta? lineaReglaSeleccionada;

    private string nombreRegla = string.Empty;
    private string condicionJsonVistaPrevia = "{}";
    private RolEmpresaDte rolEmpresaSeleccionado =
        RolEmpresaDte.NoDeterminado;
    private DateTime vigenciaDesde = DateTime.Today;
    private DateTime? vigenciaHasta;

    private LadoContable ladoSeleccionado = LadoContable.Debe;
    private string importeFijo = string.Empty;
    private string mensaje = string.Empty;

    public ObservableCollection<ReglaContable> Reglas { get; } = new();

    public ObservableCollection<CuentaContable> CuentasCatalogo { get; }
        = new();

    public ObservableCollection<ReglaCuenta> LineasRegla { get; }
        = new();

    public ObservableCollection<CondicionReglaEditor> Condiciones { get; }
        = new();

    public List<OpcionTipoDte> TiposDte { get; } =
    [
        new(string.Empty, "Todos los tipos de DTE"),
        new("01", "Factura"),
        new("03", "Comprobante de crédito fiscal"),
        new("04", "Nota de remisión"),
        new("05", "Nota de crédito"),
        new("06", "Nota de débito"),
        new("07", "Comprobante de retención"),
        new("08", "Comprobante de liquidación"),
        new("09", "Documento contable de liquidación"),
        new("11", "Factura de exportación"),
        new("14", "Factura de sujeto excluido"),
        new("15", "Comprobante de donación")
    ];

    public List<OpcionRolEmpresa> RolesEmpresa { get; } =
    [
        new(RolEmpresaDte.NoDeterminado, "Todos los roles"),
        new(RolEmpresaDte.Emisor, "La empresa es emisora"),
        new(RolEmpresaDte.Receptor, "La empresa es receptora")
    ];

    public List<OpcionFormulaContable> FormulasContables { get; } =
    [
        new("BASEGRAVADA", "Base gravada del DTE"),
        new("MONTOEXENTO", "Monto exento"),
        new("MONTONOSUJETO", "Monto no sujeto"),
        new("IVA", "Impuesto (IVA)"),
        new("TOTALOPERACION", "Total de la operación"),
        new(string.Empty, "Importe fijo", true)
    ];

    public List<LadoContable> LadosContables { get; } =
        Enum.GetValues<LadoContable>().ToList();

    public string NombreRegla
    {
        get => nombreRegla;
        set => EstablecerPropiedad(ref nombreRegla, value);
    }

    public OpcionTipoDte? TipoDteSeleccionado
    {
        get => tipoDteSeleccionado;
        set => EstablecerPropiedad(ref tipoDteSeleccionado, value);
    }

    public RolEmpresaDte RolEmpresaSeleccionado
    {
        get => rolEmpresaSeleccionado;
        set => EstablecerPropiedad(
            ref rolEmpresaSeleccionado,
            value);
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
                if (value is not null)
                {
                    NombreRegla = value.Nombre;
                    TipoDteSeleccionado = TiposDte.FirstOrDefault(tipo =>
                        tipo.Codigo == value.TipoDte) ?? TiposDte[0];
                    RolEmpresaSeleccionado = value.RolEmpresa ??
                        RolEmpresaDte.NoDeterminado;
                    VigenciaDesde = value.VigenciDesde;
                    VigenciaHasta = value.VigenciaHasta;
                    CargarCondiciones(value.CondicionJson);
                    Mensaje = "Regla seleccionada. Modifica los datos y pulsa Actualizar regla.";
                }

                CargarLineasRegla();
            }
        }
    }

    public CuentaContable? CuentaSeleccionada
    {
        get => cuentaSeleccionada;
        set => EstablecerPropiedad(ref cuentaSeleccionada, value);
    }

    public ReglaCuenta? LineaReglaSeleccionada
    {
        get => lineaReglaSeleccionada;
        set
        {
            if (EstablecerPropiedad(ref lineaReglaSeleccionada, value) &&
                value is not null)
            {
                CuentaSeleccionada = CuentasCatalogo.FirstOrDefault(cuenta =>
                    cuenta.CuentaContableId == value.CuentaContableId);
                LadoSeleccionado = value.Lado;
                FormulaSeleccionada = FormulasContables.FirstOrDefault(
                    formula => formula.Valor == value.Formula) ??
                    FormulasContables.First(formula =>
                        formula.EsImporteFijo);
                ImporteFijo = FormulaSeleccionada.EsImporteFijo
                    ? value.Formula
                    : string.Empty;
                Mensaje = "Línea seleccionada. Modifica los datos y pulsa Actualizar línea.";
            }
        }
    }

    public LadoContable LadoSeleccionado
    {
        get => ladoSeleccionado;
        set => EstablecerPropiedad(ref ladoSeleccionado, value);
    }

    public OpcionFormulaContable? FormulaSeleccionada
    {
        get => formulaSeleccionada;
        set => EstablecerPropiedad(ref formulaSeleccionada, value);
    }

    public CondicionReglaEditor? CondicionSeleccionada
    {
        get => condicionSeleccionada;
        set => EstablecerPropiedad(ref condicionSeleccionada, value);
    }

    public string CondicionJsonVistaPrevia
    {
        get => condicionJsonVistaPrevia;
        private set => EstablecerPropiedad(
            ref condicionJsonVistaPrevia,
            value);
    }

    public string ImporteFijo
    {
        get => importeFijo;
        set => EstablecerPropiedad(ref importeFijo, value);
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
    public ICommand AgregarCondicionCommand => agregarCondicionCommand;
    public ICommand QuitarCondicionCommand => quitarCondicionCommand;
    public ICommand ActualizarReglaCommand => actualizarReglaCommand;
    public ICommand EliminarReglaCommand => eliminarReglaCommand;
    public ICommand ActualizarCuentaCommand => actualizarCuentaCommand;
    public ICommand EliminarCuentaCommand => eliminarCuentaCommand;

    public ReglasContablesViewModel()
    {
        tipoDteSeleccionado = TiposDte[0];
        crearReglaCommand = new RelayCommand(CrearRegla);
        agregarCuentaCommand = new RelayCommand(AgregarCuenta);
        cambiarEstadoCommand = new RelayCommand(CambiarEstado);
        cargarCommand = new RelayCommand(_ => CargarDatos());
        agregarCondicionCommand = new RelayCommand(AgregarCondicion);
        quitarCondicionCommand = new RelayCommand(
            QuitarCondicion,
            _ => CondicionSeleccionada is not null);
        actualizarReglaCommand = new RelayCommand(ActualizarRegla);
        eliminarReglaCommand = new RelayCommand(EliminarRegla);
        actualizarCuentaCommand = new RelayCommand(ActualizarCuenta);
        eliminarCuentaCommand = new RelayCommand(EliminarCuenta);

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
        LineaReglaSeleccionada = null;

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

    private void CargarCondiciones(string condicionJson)
    {
        Condiciones.Clear();

        try
        {
            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            opciones.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            DefinicionCondicionesRegla definicion =
                JsonSerializer.Deserialize<DefinicionCondicionesRegla>(
                    condicionJson,
                    opciones) ?? new DefinicionCondicionesRegla();

            foreach (CondicionReglaContable condicion in
                     definicion.Condiciones)
            {
                var editor = new CondicionReglaEditor
                {
                    Campo = condicion.Campo,
                    Operador = condicion.Operador,
                    Valor = condicion.Valor.ToString(
                        CultureInfo.InvariantCulture)
                };
                editor.PropertyChanged += (_, _) => ActualizarVistaPrevia();
                Condiciones.Add(editor);
            }

            ActualizarVistaPrevia();
        }
        catch (JsonException error)
        {
            Mensaje = $"No se pudieron cargar las condiciones de la regla: {error.Message}";
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
                    string.IsNullOrWhiteSpace(TipoDteSeleccionado?.Codigo)
                        ? null
                        : TipoDteSeleccionado.Codigo,
                    RolEmpresaSeleccionado,
                    VigenciaDesde,
                        VigenciaHasta,
                        CrearCondicionJson());

            Reglas.Add(reglaNueva);
            ReglaSeleccionada = reglaNueva;

            NombreRegla = string.Empty;
            TipoDteSeleccionado = TiposDte[0];
            RolEmpresaSeleccionado = RolEmpresaDte.NoDeterminado;
            VigenciaDesde = DateTime.Today;
            VigenciaHasta = null;
            Condiciones.Clear();
            CondicionSeleccionada = null;

            Mensaje = "Regla contable creada correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void AgregarCondicion(object? parametro)
    {
        var condicion = new CondicionReglaEditor();
        condicion.PropertyChanged += (_, _) => ActualizarVistaPrevia();
        Condiciones.Add(condicion);
        CondicionSeleccionada = condicion;
        ActualizarVistaPrevia();
    }

    private void QuitarCondicion(object? parametro)
    {
        if (CondicionSeleccionada is null)
        {
            return;
        }

        Condiciones.Remove(CondicionSeleccionada);
        CondicionSeleccionada = null;
        ActualizarVistaPrevia();
    }

    private string CrearCondicionJson()
    {
        return ServicioCondicionesReglaContable.CrearJson(
            Condiciones.Select(condicion => condicion.CrearCondicion()));
    }

    private void ActualizarVistaPrevia()
    {
        try
        {
            CondicionJsonVistaPrevia = CrearCondicionJson();
        }
        catch (InvalidOperationException error)
        {
            CondicionJsonVistaPrevia = error.Message;
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

        if (FormulaSeleccionada is null)
        {
            Mensaje = "Selecciona el importe que debe usar esta línea.";
            return;
        }

        string formula;

        if (FormulaSeleccionada.EsImporteFijo)
        {
            if (!decimal.TryParse(
                    ImporteFijo,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal importe) &&
                !decimal.TryParse(
                    ImporteFijo,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out importe))
            {
                Mensaje = "Escribe un importe fijo válido.";
                return;
            }

            if (importe <= 0)
            {
                Mensaje = "El importe fijo debe ser mayor que cero.";
                return;
            }

            formula = importe.ToString(
                "0.00",
                CultureInfo.InvariantCulture);
        }
        else
        {
            formula = FormulaSeleccionada.Valor;
        }

        try
        {
            ReglaCuenta lineaNueva =
                ServicioReglasContables.AgregarCuenta(
                    SesionActual.EmpresaId.Value,
                    ReglaSeleccionada.ReglaContableId,
                    CuentaSeleccionada.CuentaContableId,
                    LadoSeleccionado,
                    formula);

            LineasRegla.Add(lineaNueva);
            //modificacion para agregar una linea en las reglas de contabilidad
            ReglaSeleccionada.Cuentas.Add(lineaNueva);


            CuentaSeleccionada = null;
            LadoSeleccionado = LadoContable.Debe;
            FormulaSeleccionada = null;
            ImporteFijo = string.Empty;

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

    private void ActualizarRegla(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            ReglaSeleccionada is null)
        {
            Mensaje = "Selecciona una regla para actualizar.";
            return;
        }

        try
        {
            ServicioReglasContables.ActualizarDatos(
                SesionActual.EmpresaId.Value,
                ReglaSeleccionada.ReglaContableId,
                NombreRegla,
                string.IsNullOrWhiteSpace(TipoDteSeleccionado?.Codigo)
                    ? null
                    : TipoDteSeleccionado.Codigo,
                RolEmpresaSeleccionado,
                VigenciaDesde,
                VigenciaHasta,
                CrearCondicionJson());
            CargarDatos();
            Mensaje = "Regla actualizada correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void EliminarRegla(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            ReglaSeleccionada is null)
        {
            Mensaje = "Selecciona una regla para eliminar.";
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            $"¿Deseas eliminar la regla «{ReglaSeleccionada.Nombre}» y sus líneas contables?",
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
            ServicioReglasContables.Eliminar(
                SesionActual.EmpresaId.Value,
                ReglaSeleccionada.ReglaContableId);
            CargarDatos();
            Mensaje = "Regla eliminada correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void ActualizarCuenta(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            ReglaSeleccionada is null ||
            LineaReglaSeleccionada is null ||
            CuentaSeleccionada is null ||
            FormulaSeleccionada is null)
        {
            Mensaje = "Selecciona una línea y completa sus datos para actualizarla.";
            return;
        }

        string formula;
        if (FormulaSeleccionada.EsImporteFijo)
        {
            if (!decimal.TryParse(
                    ImporteFijo,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal importe) &&
                !decimal.TryParse(
                    ImporteFijo,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out importe))
            {
                Mensaje = "Escribe un importe fijo válido.";
                return;
            }

            formula = importe.ToString(
                "0.00",
                CultureInfo.InvariantCulture);
        }
        else
        {
            formula = FormulaSeleccionada.Valor;
        }

        try
        {
            ServicioReglasContables.ActualizarCuenta(
                SesionActual.EmpresaId.Value,
                ReglaSeleccionada.ReglaContableId,
                LineaReglaSeleccionada.ReglaCuentaId,
                CuentaSeleccionada.CuentaContableId,
                LadoSeleccionado,
                formula);
            CargarDatos();
            Mensaje = "Línea contable actualizada correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void EliminarCuenta(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            ReglaSeleccionada is null ||
            LineaReglaSeleccionada is null)
        {
            Mensaje = "Selecciona una línea contable para eliminar.";
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            "¿Deseas eliminar la línea contable seleccionada?",
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
            ServicioReglasContables.EliminarCuenta(
                SesionActual.EmpresaId.Value,
                ReglaSeleccionada.ReglaContableId,
                LineaReglaSeleccionada.ReglaCuentaId);
            CargarDatos();
            Mensaje = "Línea contable eliminada correctamente.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}