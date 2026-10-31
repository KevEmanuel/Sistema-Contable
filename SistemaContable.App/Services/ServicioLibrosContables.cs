using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

public static class ServicioLibrosContables
{
    public static List<PeriodoContable> ObtenerPeriodos(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.PeriodosContables
            .AsNoTracking()
            .Where(periodo => periodo.EmpresaId == empresaId)
            .OrderByDescending(periodo => periodo.FechaInicio)
            .ToList();
    }

    public static List<LibroDiarioLinea> ObtenerLibroDiario(
        int empresaId,
        int periodoContableId,
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        using var contexto = new ContabilidadDbContext();

        ValidarRango(
            contexto,
            empresaId,
            periodoContableId,
            fechaDesde,
            fechaHasta);

        return ObtenerPartidasContabilizadas(
                contexto,
                empresaId,
                periodoContableId,
                fechaDesde.Date,
                fechaHasta.Date.AddDays(1))
            .SelectMany(partida => partida.Detalles.Select(detalle =>
            {
                CuentaContable cuenta = detalle.CuentaContable
                    ?? throw new InvalidOperationException(
                        $"La línea {detalle.DetallePartidaId} no tiene cuenta contable.");

                return new LibroDiarioLinea
                {
                    Fecha = partida.Fecha,
                    NumeroPartida = partida.NumeroPartida,
                    Glosa = partida.Glosa,
                    CodigoCuenta = cuenta.Codigo,
                    NombreCuenta = cuenta.Nombre,
                    Concepto = detalle.Concepto,
                    EstadoPartida = partida.Estado,
                    Debe = detalle.Debe,
                    Haber = detalle.Haber
                };
            }))
            .OrderBy(linea => linea.Fecha)
            .ThenBy(linea => linea.NumeroPartida)
            .ThenBy(linea => linea.CodigoCuenta)
            .ToList();
    }

    public static List<LibroMayorLinea> ObtenerLibroMayor(
        int empresaId,
        int periodoContableId,
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        using var contexto = new ContabilidadDbContext();

        ValidarRango(
            contexto,
            empresaId,
            periodoContableId,
            fechaDesde,
            fechaHasta);

        List<PartidaContable> partidasAnteriores =
            ObtenerPartidasContabilizadas(
            contexto,
            empresaId,
            null,
            null,
            fechaDesde.Date);

        List<PartidaContable> partidasPeriodo =
            ObtenerPartidasContabilizadas(
                contexto,
                empresaId,
                periodoContableId,
                fechaDesde.Date,
                fechaHasta.Date.AddDays(1));

        List<PartidaContable> partidas = partidasAnteriores
            .Concat(partidasPeriodo)
            .ToList();

        var resultado = new List<LibroMayorLinea>();

        foreach (var grupoCuenta in partidas
                     .SelectMany(partida => partida.Detalles.Select(detalle =>
                     {
                         CuentaContable cuenta = detalle.CuentaContable
                             ?? throw new InvalidOperationException(
                                 $"La línea {detalle.DetallePartidaId} no tiene cuenta contable.");

                         return new
                         {
                             Partida = partida,
                             Detalle = detalle,
                             Cuenta = cuenta
                         };
                     }))
                     .GroupBy(linea => new
                     {
                         linea.Cuenta.CuentaContableId,
                         linea.Cuenta.Codigo,
                         linea.Cuenta.Nombre
                     })
                     .OrderBy(grupo => grupo.Key.Codigo))
        {
            var lineasCuenta = grupoCuenta
                .OrderBy(linea => linea.Partida.Fecha)
                .ThenBy(linea => linea.Partida.NumeroPartida)
                .ThenBy(linea => linea.Detalle.DetallePartidaId)
                .ToList();

            decimal saldoNeto = lineasCuenta
                .Where(linea => linea.Partida.Fecha.Date < fechaDesde.Date)
                .Sum(linea => linea.Detalle.Debe - linea.Detalle.Haber);

            var movimientos = lineasCuenta
                .Where(linea =>
                    linea.Partida.Fecha.Date >= fechaDesde.Date &&
                    linea.Partida.PeriodoContableId == periodoContableId)
                .ToList();

            if (saldoNeto != 0 || movimientos.Count > 0)
            {
                resultado.Add(CrearLineaMayor(
                    grupoCuenta.Key.Codigo,
                    grupoCuenta.Key.Nombre,
                    fechaDesde.Date,
                    null,
                    "Saldo inicial",
                    null,
                    true,
                    null,
                    Math.Max(saldoNeto, 0),
                    Math.Max(-saldoNeto, 0),
                    saldoNeto));
            }

            foreach (var movimiento in movimientos)
            {
                PartidaContable partida = movimiento.Partida;
                DetallePartida detalle = movimiento.Detalle;
                saldoNeto += detalle.Debe - detalle.Haber;

                resultado.Add(CrearLineaMayor(
                    grupoCuenta.Key.Codigo,
                    grupoCuenta.Key.Nombre,
                    partida.Fecha,
                    partida.NumeroPartida,
                    partida.Glosa,
                    detalle.Concepto,
                    false,
                    partida.Estado,
                    detalle.Debe,
                    detalle.Haber,
                    saldoNeto));
            }
        }

        return resultado;
    }

    private static LibroMayorLinea CrearLineaMayor(
        string codigoCuenta,
        string nombreCuenta,
        DateTime fecha,
        int? numeroPartida,
        string glosa,
        string? concepto,
        bool esSaldoInicial,
        EstadoPartidaContable? estadoPartida,
        decimal debe,
        decimal haber,
        decimal saldoNeto)
    {
        return new LibroMayorLinea
        {
            CodigoCuenta = codigoCuenta,
            NombreCuenta = nombreCuenta,
            Fecha = fecha,
            NumeroPartida = numeroPartida,
            Glosa = glosa,
            Concepto = concepto,
            EsSaldoInicial = esSaldoInicial,
            EstadoPartida = estadoPartida,
            Debe = debe,
            Haber = haber,
            SaldoDeudor = Math.Max(saldoNeto, 0),
            SaldoAcreedor = Math.Max(-saldoNeto, 0)
        };
    }

    private static List<PartidaContable> ObtenerPartidasContabilizadas(
        ContabilidadDbContext contexto,
        int empresaId,
        int? periodoContableId,
        DateTime? fechaDesde,
        DateTime fechaHastaExclusiva)
    {
        IQueryable<PartidaContable> consulta = contexto.PartidasContables
            .AsNoTracking()
            .Include(partida => partida.Detalles)
            .ThenInclude(detalle => detalle.CuentaContable)
            .Where(partida =>
                partida.EmpresaId == empresaId &&
                (partida.Estado == EstadoPartidaContable.Contabilizada ||
                 partida.Estado == EstadoPartidaContable.Reversada) &&
                partida.Fecha < fechaHastaExclusiva);

        if (periodoContableId.HasValue)
        {
            consulta = consulta.Where(partida =>
                partida.PeriodoContableId == periodoContableId.Value);
        }

        if (fechaDesde.HasValue)
        {
            consulta = consulta.Where(partida =>
                partida.Fecha >= fechaDesde.Value);
        }

        return consulta
            .OrderBy(partida => partida.Fecha)
            .ThenBy(partida => partida.NumeroPartida)
            .ToList();
    }

    private static void ValidarRango(
        ContabilidadDbContext contexto,
        int empresaId,
        int periodoContableId,
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        PeriodoContable? periodo = contexto.PeriodosContables
            .AsNoTracking()
            .SingleOrDefault(periodo =>
                periodo.PeriodoContableId == periodoContableId &&
                periodo.EmpresaId == empresaId);

        if (periodo is null)
        {
            throw new InvalidOperationException(
                "El período seleccionado no pertenece a la empresa.");
        }

        if (fechaDesde.Date > fechaHasta.Date ||
            fechaDesde.Date < periodo.FechaInicio.Date ||
            fechaHasta.Date > periodo.FechaFin.Date)
        {
            throw new ArgumentException(
                "El rango de fechas debe estar dentro del período seleccionado.");
        }
    }
}
