using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;
//modulo para generar partidas contables manuales 02
public static class ServicioPartidasContables
{
    public static List<PeriodoContable> ObtenerPeriodosAbiertos(
        int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.PeriodosContables
            .AsNoTracking()
            .Where(periodo =>
                periodo.EmpresaId == empresaId &&
                !periodo.EstaCerrado)
            .OrderByDescending(periodo => periodo.FechaInicio)
            .ToList();
    }

    public static List<PartidaContable> ObtenerPorEmpresa(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.PartidasContables
            .AsNoTracking()
            .Include(partida => partida.PeriodoContable)
            .Include(partida => partida.Detalles)
            .ThenInclude(detalle => detalle.CuentaContable)
            .Where(partida => partida.EmpresaId == empresaId)
            .OrderByDescending(partida => partida.Fecha)
            .ThenByDescending(partida => partida.NumeroPartida)
            .ToList();
    }

    public static PartidaContable CrearManual(
        int empresaId,
        int periodoContableId,
        int usuarioId,
        DateTime fecha,
        string glosa,
        List<DetallePartida> detalles,
        bool contabilizar)
    {
        if (string.IsNullOrWhiteSpace(glosa))
        {
            throw new ArgumentException(
                "Ingresa la glosa de la partida.");
        }

        if (detalles.Count < 2)
        {
            throw new ArgumentException(
                "La partida debe tener al menos dos líneas.");
        }

        foreach (DetallePartida detalle in detalles)
        {
            bool tieneDebe = detalle.Debe > 0;
            bool tieneHaber = detalle.Haber > 0;

            if (detalle.Debe < 0 || detalle.Haber < 0 ||
                tieneDebe == tieneHaber)
            {
                throw new ArgumentException(
                    "Cada línea debe tener un valor solamente en Debe o en Haber.");
            }
        }

        decimal totalDebe = detalles.Sum(detalle => detalle.Debe);
        decimal totalHaber = detalles.Sum(detalle => detalle.Haber);

        if (Math.Round(totalDebe, 2) != Math.Round(totalHaber, 2))
        {
            throw new ArgumentException(
                "La partida no está balanceada: Debe y Haber deben ser iguales.");
        }

        using var contexto = new ContabilidadDbContext();

        PeriodoContable? periodo = contexto.PeriodosContables
            .SingleOrDefault(periodo =>
                periodo.PeriodoContableId == periodoContableId &&
                periodo.EmpresaId == empresaId);

        if (periodo is null)
        {
            throw new InvalidOperationException(
                "El período seleccionado no pertenece a la empresa.");
        }

        if (periodo.EstaCerrado)
        {
            throw new InvalidOperationException(
                "No se pueden registrar partidas en un período cerrado.");
        }

        if (fecha.Date < periodo.FechaInicio.Date ||
            fecha.Date > periodo.FechaFin.Date)
        {
            throw new ArgumentException(
                "La fecha debe estar dentro del período contable.");
        }

        List<int> cuentasSeleccionadas = detalles
            .Select(detalle => detalle.CuentaContableId)
            .Distinct()
            .ToList();

        int cuentasValidas = contexto.CuentasContables
            .Count(cuenta =>
                cuentasSeleccionadas.Contains(cuenta.CuentaContableId) &&
                cuenta.EmpresaId == empresaId &&
                cuenta.Activa &&
                cuenta.PermiteMovimientos);

        if (cuentasValidas != cuentasSeleccionadas.Count)
        {
            throw new InvalidOperationException(
                "Una o más cuentas no están activas o no permiten movimientos.");
        }

        int ultimoNumero = contexto.PartidasContables
            .Where(partida =>
                partida.EmpresaId == empresaId &&
                partida.PeriodoContableId == periodoContableId)
            .Select(partida => (int?)partida.NumeroPartida)
            .Max() ?? 0;

        var partidaNueva = new PartidaContable
        {
            EmpresaId = empresaId,
            PeriodoContableId = periodoContableId,
            NumeroPartida = ultimoNumero + 1,
            Fecha = fecha.Date,
            Glosa = glosa.Trim(),
            TipoOrigen = TipoOrigenPartida.Manual,
            Estado = contabilizar
                ? EstadoPartidaContable.Contabilizada
                : EstadoPartidaContable.Borrador,
            CreadoPorId = usuarioId,
            AprobadoPorId = contabilizar ? usuarioId : null,
            CreadoEn = DateTime.Now
        };

        foreach (DetallePartida detalle in detalles)
        {
            partidaNueva.Detalles.Add(new DetallePartida
            {
                CuentaContableId = detalle.CuentaContableId,
                Concepto = string.IsNullOrWhiteSpace(detalle.Concepto)
                    ? null
                    : detalle.Concepto.Trim(),
                Debe = detalle.Debe,
                Haber = detalle.Haber
            });
        }

        contexto.PartidasContables.Add(partidaNueva);
        contexto.SaveChanges();

        return partidaNueva;
    }
}