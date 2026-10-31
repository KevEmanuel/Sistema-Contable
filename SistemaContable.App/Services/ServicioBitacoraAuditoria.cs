using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

//Modulo 8, bitacora para la gestion de consultoria profesiona 08
namespace SistemaContableDTE.Services;

public static class ServicioBitacoraAuditoria
{
    public static BitacoraAuditoria Registrar(
        int empresaId,
        int? usuarioId,
        string entidad,
        string idEntidad,
        string accion,
        string? valoresAnteriores = null,
        string? valoresNuevos = null,
        string? ipOrigen = null)
    {
        if (string.IsNullOrWhiteSpace(entidad) ||
            string.IsNullOrWhiteSpace(idEntidad) ||
            string.IsNullOrWhiteSpace(accion))
        {
            throw new ArgumentException(
                "Entidad, identificador y acción son obligatorios.");
        }

        using var contexto = new ContabilidadDbContext();

        var registro = new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = entidad.Trim(),
            IdEntidad = idEntidad.Trim(),
            Accion = accion.Trim(),
            ValoresAnteriores = valoresAnteriores,
            ValoresNuevos = valoresNuevos,
            IpOrigen = ipOrigen,
            FechaHora = DateTime.Now
        };

        contexto.BitacorasAuditoria.Add(registro);
        contexto.SaveChanges();

        return registro;
    }

    public static List<BitacoraAuditoria> ObtenerPorEmpresa(
        int empresaId,
        string? entidad = null,
        string? accion = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null)
    {
        if (fechaDesde.HasValue &&
            fechaHasta.HasValue &&
            fechaHasta.Value.Date < fechaDesde.Value.Date)
        {
            throw new ArgumentException(
                "La fecha final no puede ser menor que la fecha inicial.");
        }

        using var contexto = new ContabilidadDbContext();

        IQueryable<BitacoraAuditoria> consulta =
            contexto.BitacorasAuditoria
                .Include(x => x.Usuario)
                .Where(x => x.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(entidad))
        {
            consulta = consulta.Where(x =>
                x.Entidad.Contains(entidad.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(accion))
        {
            consulta = consulta.Where(x =>
                x.Accion.Contains(accion.Trim()));
        }

        if (fechaDesde.HasValue)
        {
            consulta = consulta.Where(x =>
                x.FechaHora >= fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            DateTime finDelDia =
                fechaHasta.Value.Date.AddDays(1).AddTicks(-1);

            consulta = consulta.Where(x =>
                x.FechaHora <= finDelDia);
        }

        return consulta
            .OrderByDescending(x => x.FechaHora)
            .ToList();
    }
}