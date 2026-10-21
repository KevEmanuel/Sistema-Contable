//clase para el servicio de las reglas de contabilidad
//21/10/2026
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

//modulos para el manejo de la reglas contables

namespace SistemaContableDTE.Services;

public static class ServicioReglasContables
{
    public static List<ReglaContable> ObtenerPorEmpresa(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.ReglasContables
            .AsNoTracking()
            .Include(regla => regla.Cuentas)
            .ThenInclude(linea => linea.CuentaContable)
            .Where(regla => regla.EmpresaId == empresaId)
            .OrderByDescending(regla => regla.Activa)
            .ThenBy(regla => regla.Nombre)
            .ThenByDescending(regla => regla.Version)
            .ToList();
    }

    public static ReglaContable Crear(
        int empresaId,
        string nombre,
        string? tipoDte,
        RolEmpresaDte? rolEmpresa,
        string condicionJson,
        DateTime vigenciaDesde,
        DateTime? vigenciaHasta)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "Ingresa el nombre de la regla contable.");
        }

        if (string.IsNullOrWhiteSpace(condicionJson))
        {
            throw new ArgumentException(
                "Ingresa la condición de la regla.");
        }

        if (vigenciaHasta.HasValue &&
            vigenciaHasta.Value.Date < vigenciaDesde.Date)
        {
            throw new ArgumentException(
                "La vigencia final no puede ser menor que la inicial.");
        }

        using var contexto = new ContabilidadDbContext();

        string nombreLimpio = nombre.Trim();

        int ultimaVersion = contexto.ReglasContables
            .Where(regla =>
                regla.EmpresaId == empresaId &&
                regla.Nombre == nombreLimpio)
            .Select(regla => (int?)regla.Version)
            .Max() ?? 0;

        var reglaNueva = new ReglaContable
        {
            EmpresaId = empresaId,
            Nombre = nombreLimpio,
            TipoDte = string.IsNullOrWhiteSpace(tipoDte)
                ? null
                : tipoDte.Trim(),
            RolEmpresa = rolEmpresa,
            CondicionJson = condicionJson.Trim(),
            VigenciDesde = vigenciaDesde.Date,
            VigenciaHasta = vigenciaHasta?.Date,
            Version = ultimaVersion + 1,
            Activa = true
        };

        contexto.ReglasContables.Add(reglaNueva);
        contexto.SaveChanges();

        return reglaNueva;
    }

    public static ReglaCuenta AgregarCuenta(
        int empresaId,
        int reglaContableId,
        int cuentaContableId,
        LadoContable lado,
        string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new ArgumentException(
                "Ingresa la fórmula o importe de la línea contable.");
        }

        using var contexto = new ContabilidadDbContext();

        bool reglaExiste = contexto.ReglasContables.Any(regla =>
            regla.ReglaContableId == reglaContableId &&
            regla.EmpresaId == empresaId);

        if (!reglaExiste)
        {
            throw new InvalidOperationException(
                "La regla seleccionada no pertenece a la empresa activa.");
        }

        bool cuentaValida = contexto.CuentasContables.Any(cuenta =>
            cuenta.CuentaContableId == cuentaContableId &&
            cuenta.EmpresaId == empresaId &&
            cuenta.Activa);

        if (!cuentaValida)
        {
            throw new InvalidOperationException(
                "Selecciona una cuenta activa del catálogo.");
        }

        int ultimoOrden = contexto.ReglasCuentas
            .Where(linea => linea.ReglaContableId == reglaContableId)
            .Select(linea => (int?)linea.Orden)
            .Max() ?? 0;

        var lineaNueva = new ReglaCuenta
        {
            ReglaContableId = reglaContableId,
            CuentaContableId = cuentaContableId,
            Lado = lado,
            Formula = formula.Trim(),
            Orden = ultimoOrden + 1
        };

        contexto.ReglasCuentas.Add(lineaNueva);
        contexto.SaveChanges();

        return contexto.ReglasCuentas
            .AsNoTracking()
            .Include(linea => linea.CuentaContable)
            .Single(linea => linea.ReglaCuentaId == lineaNueva.ReglaCuentaId);
    }

    public static void CambiarEstado(
        int empresaId,
        int reglaContableId)
    {
        using var contexto = new ContabilidadDbContext();

        ReglaContable? regla = contexto.ReglasContables
            .SingleOrDefault(regla =>
                regla.ReglaContableId == reglaContableId &&
                regla.EmpresaId == empresaId);

        if (regla is null)
        {
            throw new InvalidOperationException(
                "No se encontró la regla contable.");
        }

        regla.Activa = !regla.Activa;
        contexto.SaveChanges();
    }
}