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
    public static List<DocumentoDte> ObtenerDtesValidados(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.DocumentosDte
            .AsNoTracking()
            .Include(documento => documento.Participantes)
            .Where(documento =>
                documento.EmpresaId == empresaId &&
                documento.Estado == EstadoDocumentoDte.Validado)
            .OrderByDescending(documento => documento.FechaEmision)
            .ThenBy(documento => documento.TipoDte)
            .ToList();
    }

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
        DateTime vigenciaDesde,
        DateTime? vigenciaHasta,
        string condicionJson)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "Ingresa el nombre de la regla contable.");
        }

        if (vigenciaHasta.HasValue &&
            vigenciaHasta.Value.Date < vigenciaDesde.Date)
        {
            throw new ArgumentException(
                "La vigencia final no puede ser menor que la inicial.");
        }

        string condicionesValidadas =
            ServicioCondicionesReglaContable.ValidarYNormalizarJson(
                condicionJson);

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
            CondicionJson = condicionesValidadas,
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
        formula = NormalizarFormula(formula);

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

    public static void ActualizarDatos(
        int empresaId,
        int reglaContableId,
        string nombre,
        string? tipoDte,
        RolEmpresaDte rolEmpresa,
        DateTime vigenciaDesde,
        DateTime? vigenciaHasta,
        string condicionJson)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "Ingresa el nombre de la regla contable.");
        }

        if (vigenciaHasta.HasValue &&
            vigenciaHasta.Value.Date < vigenciaDesde.Date)
        {
            throw new ArgumentException(
                "La vigencia final no puede ser menor que la inicial.");
        }

        using var contexto = new ContabilidadDbContext();
        ReglaContable? regla = contexto.ReglasContables
            .SingleOrDefault(item =>
                item.ReglaContableId == reglaContableId &&
                item.EmpresaId == empresaId);

        if (regla is null)
        {
            throw new InvalidOperationException(
                "No se encontró la regla contable seleccionada.");
        }

        if (contexto.PartidasContables.Any(partida =>
                partida.ReglaContableId == reglaContableId))
        {
            throw new InvalidOperationException(
                "No se puede modificar una regla utilizada en partidas; crea una nueva versión para conservar el historial contable.");
        }

        regla.CondicionJson =
            ServicioCondicionesReglaContable.ValidarYNormalizarJson(
                condicionJson);
        regla.Nombre = nombre.Trim();
        regla.TipoDte = string.IsNullOrWhiteSpace(tipoDte)
            ? null
            : tipoDte.Trim();
        regla.RolEmpresa = rolEmpresa == RolEmpresaDte.NoDeterminado
            ? null
            : rolEmpresa;
        regla.VigenciDesde = vigenciaDesde.Date;
        regla.VigenciaHasta = vigenciaHasta?.Date;

        contexto.SaveChanges();
    }

    public static void ActualizarCuenta(
        int empresaId,
        int reglaContableId,
        int reglaCuentaId,
        int cuentaContableId,
        LadoContable lado,
        string formula)
    {
        formula = NormalizarFormula(formula);
        using var contexto = new ContabilidadDbContext();
        ReglaCuenta? linea = contexto.ReglasCuentas
            .Include(item => item.ReglaContable)
            .SingleOrDefault(item =>
                item.ReglaCuentaId == reglaCuentaId &&
                item.ReglaContableId == reglaContableId &&
                item.ReglaContable != null &&
                item.ReglaContable.EmpresaId == empresaId);

        if (linea is null)
        {
            throw new InvalidOperationException(
                "No se encontró la línea seleccionada.");
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

        linea.CuentaContableId = cuentaContableId;
        linea.Lado = lado;
        linea.Formula = formula;
        contexto.SaveChanges();
    }

    public static void EliminarCuenta(
        int empresaId,
        int reglaContableId,
        int reglaCuentaId)
    {
        using var contexto = new ContabilidadDbContext();
        ReglaCuenta? linea = contexto.ReglasCuentas
            .Include(item => item.ReglaContable)
            .SingleOrDefault(item =>
                item.ReglaCuentaId == reglaCuentaId &&
                item.ReglaContableId == reglaContableId &&
                item.ReglaContable != null &&
                item.ReglaContable.EmpresaId == empresaId);

        if (linea is null)
        {
            throw new InvalidOperationException(
                "No se encontró la línea seleccionada.");
        }

        contexto.ReglasCuentas.Remove(linea);
        contexto.SaveChanges();
    }

    public static void Eliminar(
        int empresaId,
        int reglaContableId)
    {
        using var contexto = new ContabilidadDbContext();
        ReglaContable? regla = contexto.ReglasContables
            .Include(item => item.Cuentas)
            .SingleOrDefault(item =>
                item.ReglaContableId == reglaContableId &&
                item.EmpresaId == empresaId);

        if (regla is null)
        {
            throw new InvalidOperationException(
                "No se encontró la regla contable seleccionada.");
        }

        if (contexto.PartidasContables.Any(partida =>
                partida.ReglaContableId == reglaContableId))
        {
            throw new InvalidOperationException(
                "No se puede eliminar una regla utilizada por partidas contables.");
        }

        contexto.ReglasCuentas.RemoveRange(regla.Cuentas);
        contexto.ReglasContables.Remove(regla);
        contexto.SaveChanges();
    }

    private static string NormalizarFormula(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new ArgumentException(
                "Selecciona el importe que debe usar esta línea.");
        }

        string normalizada = formula
            .Trim()
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        if (normalizada is "BASEGRAVADA" or
            "MONTOEXENTO" or
            "MONTONOSUJETO" or
            "IVA" or
            "TOTALOPERACION")
        {
            return normalizada;
        }

        if ((decimal.TryParse(
                 formula,
                 System.Globalization.NumberStyles.Number,
                 System.Globalization.CultureInfo.InvariantCulture,
                 out decimal importe) ||
             decimal.TryParse(
                 formula,
                 System.Globalization.NumberStyles.Number,
                 System.Globalization.CultureInfo.CurrentCulture,
                 out importe)) &&
            importe > 0)
        {
            return importe.ToString(
                "0.00",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        throw new ArgumentException(
            "Selecciona una base del DTE o ingresa un importe fijo mayor que cero.");
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