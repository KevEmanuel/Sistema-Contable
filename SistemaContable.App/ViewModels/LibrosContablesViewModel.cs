using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;

public class LibrosContablesViewModel : ViewModelBase
{
    private readonly RelayCommand cargarCommand;
    private PeriodoContable? periodoSeleccionado;
    private DateTime? fechaDesde;
    private DateTime? fechaHasta;
    private decimal totalDebeDiario;
    private decimal totalHaberDiario;
    private decimal totalDebeMayor;
    private decimal totalHaberMayor;
    private string mensaje = string.Empty;

    public ObservableCollection<PeriodoContable> Periodos { get; } = new();
    public ObservableCollection<LibroDiarioLinea> LibroDiario { get; } = new();
    public ObservableCollection<LibroMayorLinea> LibroMayor { get; } = new();

    public PeriodoContable? PeriodoSeleccionado
    {
        get => periodoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref periodoSeleccionado, value))
            {
                FechaDesde = value?.FechaInicio.Date;
                FechaHasta = value?.FechaFin.Date;
            }
        }
    }

    public DateTime? FechaDesde
    {
        get => fechaDesde;
        set => EstablecerPropiedad(ref fechaDesde, value);
    }

    public DateTime? FechaHasta
    {
        get => fechaHasta;
        set => EstablecerPropiedad(ref fechaHasta, value);
    }

    public decimal TotalDebeDiario
    {
        get => totalDebeDiario;
        private set => EstablecerPropiedad(ref totalDebeDiario, value);
    }

    public decimal TotalHaberDiario
    {
        get => totalHaberDiario;
        private set => EstablecerPropiedad(ref totalHaberDiario, value);
    }

    public decimal TotalDebeMayor
    {
        get => totalDebeMayor;
        private set => EstablecerPropiedad(ref totalDebeMayor, value);
    }

    public decimal TotalHaberMayor
    {
        get => totalHaberMayor;
        private set => EstablecerPropiedad(ref totalHaberMayor, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand CargarCommand => cargarCommand;

    public LibrosContablesViewModel()
    {
        cargarCommand = new RelayCommand(_ => CargarLibros());
        CargarPeriodos();
    }

    public string CrearCsvDiario()
    {
        var lineas = new List<string>
        {
            "Fecha;Partida;Estado;Glosa;Código de cuenta;Cuenta;Concepto;Debe;Haber"
        };

        lineas.AddRange(LibroDiario.Select(linea => string.Join(";",
            linea.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            linea.NumeroPartida,
            linea.EstadoPartida,
            EscaparCsv(linea.Glosa),
            EscaparCsv(linea.CodigoCuenta),
            EscaparCsv(linea.NombreCuenta),
            EscaparCsv(linea.Concepto ?? string.Empty),
            FormatearMonto(linea.Debe),
            FormatearMonto(linea.Haber))));

        return string.Join(Environment.NewLine, lineas);
    }

    public string CrearCsvMayor()
    {
        var lineas = new List<string>
        {
            "Código de cuenta;Cuenta;Fecha;Partida;Estado;Glosa;Concepto;Debe;Haber;Saldo deudor;Saldo acreedor"
        };

        lineas.AddRange(LibroMayor.Select(linea => string.Join(";",
            EscaparCsv(linea.CodigoCuenta),
            EscaparCsv(linea.NombreCuenta),
            linea.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            linea.NumeroPartida?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            linea.EstadoPartida?.ToString() ?? string.Empty,
            EscaparCsv(linea.Glosa),
            EscaparCsv(linea.Concepto ?? string.Empty),
            FormatearMonto(linea.Debe),
            FormatearMonto(linea.Haber),
            FormatearMonto(linea.SaldoDeudor),
            FormatearMonto(linea.SaldoAcreedor))));

        return string.Join(Environment.NewLine, lineas);
    }

    private void CargarPeriodos()
    {
        if (SesionActual.EmpresaId is not int empresaId)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            Periodos.Clear();

            foreach (PeriodoContable periodo in
                     ServicioLibrosContables.ObtenerPeriodos(empresaId))
            {
                Periodos.Add(periodo);
            }

            PeriodoSeleccionado = Periodos.FirstOrDefault(periodo =>
                periodo.FechaInicio.Date <= DateTime.Today &&
                periodo.FechaFin.Date >= DateTime.Today)
                ?? Periodos.FirstOrDefault();

            if (PeriodoSeleccionado is null)
            {
                Mensaje = "La empresa no tiene períodos contables configurados.";
                LimpiarLibros();
                return;
            }

            CargarLibros();
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
            LimpiarLibros();
        }
    }

    private void CargarLibros()
    {
        LimpiarLibros();

        if (SesionActual.EmpresaId is not int empresaId ||
            PeriodoSeleccionado is null ||
            FechaDesde is null ||
            FechaHasta is null)
        {
            Mensaje = "Selecciona una empresa, un período y un rango de fechas.";
            return;
        }

        try
        {
            List<LibroDiarioLinea> diario =
                ServicioLibrosContables.ObtenerLibroDiario(
                    empresaId,
                    PeriodoSeleccionado.PeriodoContableId,
                    FechaDesde.Value,
                    FechaHasta.Value);

            List<LibroMayorLinea> mayor =
                ServicioLibrosContables.ObtenerLibroMayor(
                    empresaId,
                    PeriodoSeleccionado.PeriodoContableId,
                    FechaDesde.Value,
                    FechaHasta.Value);

            foreach (LibroDiarioLinea linea in diario)
            {
                LibroDiario.Add(linea);
            }

            foreach (LibroMayorLinea linea in mayor)
            {
                LibroMayor.Add(linea);
            }

            TotalDebeDiario = diario.Sum(linea => linea.Debe);
            TotalHaberDiario = diario.Sum(linea => linea.Haber);
            TotalDebeMayor = mayor
                .Where(linea => !linea.EsSaldoInicial)
                .Sum(linea => linea.Debe);
            TotalHaberMayor = mayor
                .Where(linea => !linea.EsSaldoInicial)
                .Sum(linea => linea.Haber);

            Mensaje = diario.Count == 0
                ? "No hay partidas contabilizadas en el rango seleccionado."
                : string.Empty;
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
            LimpiarLibros();
        }
    }

    private void LimpiarLibros()
    {
        LibroDiario.Clear();
        LibroMayor.Clear();
        TotalDebeDiario = 0;
        TotalHaberDiario = 0;
        TotalDebeMayor = 0;
        TotalHaberMayor = 0;
    }

    private static string FormatearMonto(decimal monto)
    {
        return monto.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string EscaparCsv(string valor)
    {
        return valor.Contains(';') ||
               valor.Contains('"') ||
               valor.Contains('\r') ||
               valor.Contains('\n')
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;
    }
}
