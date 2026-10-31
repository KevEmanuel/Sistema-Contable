/*
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
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

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
        using var transaccion = contexto.Database.BeginTransaction();

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

        RegistrarAuditoria(
            contexto,
            empresaId,
            usuarioId,
            partidaNueva.PartidaContableId.ToString(),
            contabilizar
                ? "Crear y contabilizar partida manual"
                : "Crear partida manual",
            null,
            new
            {
                partidaNueva.NumeroPartida,
                partidaNueva.Fecha,
                partidaNueva.Glosa,
                TipoOrigen = partidaNueva.TipoOrigen.ToString(),
                Estado = partidaNueva.Estado.ToString(),
                TotalDebe = totalDebe,
                TotalHaber = totalHaber,
                CantidadLineas = partidaNueva.Detalles.Count
            });

        contexto.SaveChanges();
        transaccion.Commit();

        return partidaNueva;
    }

    public static void ActualizarBorradorManual(
        int empresaId,
        int partidaContableId,
        int periodoContableId,
        int usuarioId,
        DateTime fecha,
        string glosa,
        List<DetallePartida> detalles)
    {
        ValidarDetallesManual(glosa, detalles);

        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        PartidaContable? partida = contexto.PartidasContables
            .Include(item => item.Detalles)
            .Include(item => item.PeriodoContable)
            .SingleOrDefault(item =>
                item.PartidaContableId == partidaContableId &&
                item.EmpresaId == empresaId);

        if (partida is null ||
            partida.TipoOrigen != TipoOrigenPartida.Manual ||
            partida.Estado != EstadoPartidaContable.Borrador)
        {
            throw new InvalidOperationException(
                "Solo se pueden actualizar partidas manuales en borrador.");
        }

        PeriodoContable? periodo = contexto.PeriodosContables
            .SingleOrDefault(item =>
                item.PeriodoContableId == periodoContableId &&
                item.EmpresaId == empresaId);

        if (periodo is null || periodo.EstaCerrado)
        {
            throw new InvalidOperationException(
                "Selecciona un período abierto de la empresa.");
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
        int cuentasValidas = contexto.CuentasContables.Count(cuenta =>
            cuentasSeleccionadas.Contains(cuenta.CuentaContableId) &&
            cuenta.EmpresaId == empresaId &&
            cuenta.Activa &&
            cuenta.PermiteMovimientos);
        if (cuentasValidas != cuentasSeleccionadas.Count)
        {
            throw new InvalidOperationException(
                "Una o más cuentas no están activas o no permiten movimientos.");
        }

        var valoresAnteriores = new
        {
            partida.Fecha,
            partida.Glosa,
            TotalDebe = partida.Detalles.Sum(detalle => detalle.Debe),
            TotalHaber = partida.Detalles.Sum(detalle => detalle.Haber)
        };

        contexto.DetallesPartida.RemoveRange(partida.Detalles);
        partida.PeriodoContableId = periodoContableId;
        partida.Fecha = fecha.Date;
        partida.Glosa = glosa.Trim();
        partida.Detalles = detalles.Select(detalle => new DetallePartida
        {
            CuentaContableId = detalle.CuentaContableId,
            Concepto = string.IsNullOrWhiteSpace(detalle.Concepto)
                ? null
                : detalle.Concepto.Trim(),
            Debe = detalle.Debe,
            Haber = detalle.Haber
        }).ToList();

        contexto.SaveChanges();
        RegistrarAuditoria(
            contexto,
            empresaId,
            usuarioId,
            partidaContableId.ToString(),
            "Actualizar borrador de partida manual",
            valoresAnteriores,
            new
            {
                partida.Fecha,
                partida.Glosa,
                TotalDebe = detalles.Sum(detalle => detalle.Debe),
                TotalHaber = detalles.Sum(detalle => detalle.Haber)
            });
        contexto.SaveChanges();
        transaccion.Commit();
    }

    public static void EliminarBorradorManual(
        int empresaId,
        int partidaContableId,
        int usuarioId)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        PartidaContable? partida = contexto.PartidasContables
            .Include(item => item.Detalles)
            .SingleOrDefault(item =>
                item.PartidaContableId == partidaContableId &&
                item.EmpresaId == empresaId);

        if (partida is null ||
            partida.TipoOrigen != TipoOrigenPartida.Manual ||
            partida.Estado != EstadoPartidaContable.Borrador)
        {
            throw new InvalidOperationException(
                "Solo se pueden eliminar partidas manuales en borrador.");
        }

        var valoresAnteriores = new
        {
            partida.NumeroPartida,
            partida.Fecha,
            partida.Glosa,
            TotalDebe = partida.Detalles.Sum(detalle => detalle.Debe),
            TotalHaber = partida.Detalles.Sum(detalle => detalle.Haber)
        };

        contexto.DetallesPartida.RemoveRange(partida.Detalles);
        contexto.PartidasContables.Remove(partida);
        contexto.SaveChanges();
        RegistrarAuditoria(
            contexto,
            empresaId,
            usuarioId,
            partidaContableId.ToString(),
            "Eliminar borrador de partida manual",
            valoresAnteriores,
            null);
        contexto.SaveChanges();
        transaccion.Commit();
    }

    private static void ValidarDetallesManual(
        string glosa,
        List<DetallePartida> detalles)
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

        if (Math.Round(detalles.Sum(detalle => detalle.Debe), 2) !=
            Math.Round(detalles.Sum(detalle => detalle.Haber), 2))
        {
            throw new ArgumentException(
                "La partida no está balanceada: Debe y Haber deben ser iguales.");
        }
    }

    public static PartidaContable ContabilizarBorrador(
        int empresaId,
        int partidaContableId,
        int usuarioId)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        PartidaContable? partida = contexto.PartidasContables
            .Include(item => item.PeriodoContable)
            .Include(item => item.Detalles)
            .SingleOrDefault(item =>
                item.PartidaContableId == partidaContableId &&
                item.EmpresaId == empresaId);

        if (partida is null)
        {
            throw new InvalidOperationException(
                "No se encontró la partida seleccionada.");
        }

        if (partida.Estado != EstadoPartidaContable.Borrador)
        {
            throw new InvalidOperationException(
                "Solo se pueden contabilizar partidas en borrador.");
        }

        if (partida.PeriodoContable is null ||
            partida.PeriodoContable.EstaCerrado)
        {
            throw new InvalidOperationException(
                "No se puede contabilizar una partida de un período cerrado.");
        }

        decimal totalDebe = partida.Detalles.Sum(x => x.Debe);
        decimal totalHaber = partida.Detalles.Sum(x => x.Haber);

        if (Math.Round(totalDebe, 2) != Math.Round(totalHaber, 2))
        {
            throw new InvalidOperationException(
                "La partida no está balanceada.");
        }

        partida.Estado = EstadoPartidaContable.Contabilizada;
        partida.AprobadoPorId = usuarioId;

        contexto.SaveChanges();

        RegistrarAuditoria(
            contexto,
            empresaId,
            usuarioId,
            partida.PartidaContableId.ToString(),
            "Contabilizar partida",
            new
            {
                EstadoAnterior = EstadoPartidaContable.Borrador.ToString()
            },
            new
            {
                EstadoNuevo = partida.Estado.ToString(),
                partida.NumeroPartida,
                TotalDebe = totalDebe,
                TotalHaber = totalHaber
            });

        contexto.SaveChanges();
        transaccion.Commit();

        return partida;
    }

    public static PartidaContable Reversar(
        int empresaId,
        int partidaContableId,
        int usuarioId,
        DateTime fechaReversa,
        string glosa)
    {
        if (string.IsNullOrWhiteSpace(glosa))
        {
            throw new ArgumentException(
                "Ingresa la glosa de la reversión.");
        }

        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        PartidaContable? partidaOriginal = contexto.PartidasContables
            .Include(partida => partida.PeriodoContable)
            .Include(partida => partida.Detalles)
            .SingleOrDefault(partida =>
                partida.PartidaContableId == partidaContableId &&
                partida.EmpresaId == empresaId);

        if (partidaOriginal is null)
        {
            throw new InvalidOperationException(
                "No se encontró la partida que deseas reversar.");
        }

        if (partidaOriginal.TipoOrigen != TipoOrigenPartida.Manual)
        {
            throw new InvalidOperationException(
                "Las partidas originadas por DTE se revierten mediante el documento fiscal correspondiente.");
        }

        if (partidaOriginal.Estado != EstadoPartidaContable.Contabilizada)
        {
            throw new InvalidOperationException(
                "Solo se pueden reversar partidas contabilizadas.");
        }

        if (partidaOriginal.PeriodoContable is null ||
            partidaOriginal.PeriodoContable.EstaCerrado)
        {
            throw new InvalidOperationException(
                "No se puede reversar una partida de un período cerrado.");
        }

        if (fechaReversa.Date < partidaOriginal.PeriodoContable.FechaInicio.Date ||
            fechaReversa.Date > partidaOriginal.PeriodoContable.FechaFin.Date)
        {
            throw new ArgumentException(
                "La fecha de reversión debe estar dentro del período contable.");
        }

        bool yaFueReversada = contexto.PartidasContables.Any(partida =>
            partida.PartidaReversaId == partidaOriginal.PartidaContableId);

        if (yaFueReversada)
        {
            throw new InvalidOperationException(
                "Esta partida ya posee una reversión registrada.");
        }

        int ultimoNumero = contexto.PartidasContables
            .Where(partida =>
                partida.EmpresaId == empresaId &&
                partida.PeriodoContableId ==
                    partidaOriginal.PeriodoContableId)
            .Select(partida => (int?)partida.NumeroPartida)
            .Max() ?? 0;

        var partidaReversa = new PartidaContable
        {
            EmpresaId = empresaId,
            PeriodoContableId = partidaOriginal.PeriodoContableId,
            PartidaReversaId = partidaOriginal.PartidaContableId,
            NumeroPartida = ultimoNumero + 1,
            Fecha = fechaReversa.Date,
            Glosa = $"Reversión de partida #{partidaOriginal.NumeroPartida}: {glosa.Trim()}",
            TipoOrigen = TipoOrigenPartida.Reverso,
            Estado = EstadoPartidaContable.Contabilizada,
            CreadoPorId = usuarioId,
            AprobadoPorId = usuarioId,
            CreadoEn = DateTime.Now
        };

        foreach (DetallePartida detalle in partidaOriginal.Detalles)
        {
            partidaReversa.Detalles.Add(new DetallePartida
            {
                CuentaContableId = detalle.CuentaContableId,
                Concepto = detalle.Concepto,
                Debe = detalle.Haber,
                Haber = detalle.Debe
            });
        }

        partidaOriginal.Estado = EstadoPartidaContable.Reversada;

        contexto.PartidasContables.Add(partidaReversa);
        contexto.SaveChanges();

        decimal totalReversa = partidaReversa.Detalles.Sum(x => x.Debe);

        RegistrarAuditoria(
            contexto,
            empresaId,
            usuarioId,
            partidaOriginal.PartidaContableId.ToString(),
            "Reversar partida",
            new
            {
                EstadoAnterior = EstadoPartidaContable.Contabilizada.ToString(),
                partidaOriginal.NumeroPartida
            },
            new
            {
                partidaReversa.PartidaContableId,
                NumeroPartidaReversa = partidaReversa.NumeroPartida,
                partidaReversa.Fecha,
                EstadoOriginal = partidaOriginal.Estado.ToString(),
                TotalReversa = totalReversa
            });

        contexto.SaveChanges();
        transaccion.Commit();

        return partidaReversa;
    }

    private static void RegistrarAuditoria(
        ContabilidadDbContext contexto,
        int empresaId,
        int usuarioId,
        string idEntidad,
        string accion,
        object? valoresAnteriores,
        object? valoresNuevos)
    {
        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = "PartidaContable",
            IdEntidad = idEntidad,
            Accion = accion,
            ValoresAnteriores = valoresAnteriores is null
                ? null
                : JsonSerializer.Serialize(valoresAnteriores),
            ValoresNuevos = JsonSerializer.Serialize(valoresNuevos),
            FechaHora = DateTime.Now
        });
    }
}