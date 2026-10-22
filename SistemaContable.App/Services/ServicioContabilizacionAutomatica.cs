using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;
//modul 06 de contablidad automatica
public static class ServicioContabilizacionAutomatica
{
    public static List<DocumentoDte> ObtenerDtesPendientes(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.DocumentosDte
            .AsNoTracking()
            .Where(documento =>
                documento.EmpresaId == empresaId &&
                documento.Estado == EstadoDocumentoDte.Validado &&
                !contexto.PartidasContables.Any(partida =>
                    partida.DocumentoDteId == documento.DocumentoDteId))
            .OrderBy(documento => documento.FechaEmision)
            .ToList();
    }

    public static List<ReglaContable> ObtenerReglasVigentes(
        int empresaId,
        DateTime fecha)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.ReglasContables
            .AsNoTracking()
            .Include(regla => regla.Cuentas)
            .ThenInclude(linea => linea.CuentaContable)
            .Where(regla =>
                regla.EmpresaId == empresaId &&
                regla.Activa &&
                regla.VigenciDesde <= fecha &&
                (!regla.VigenciaHasta.HasValue ||
                 regla.VigenciaHasta.Value >= fecha))
            .OrderBy(regla => regla.Nombre)
            .ToList();
    }

    public static PartidaContable Contabilizar(
        int empresaId,
        int usuarioId,
        int periodoContableId,
        int documentoDteId,
        int reglaContableId)
    {
        using var contexto = new ContabilidadDbContext();

        DocumentoDte? documento = contexto.DocumentosDte
            .SingleOrDefault(documento =>
                documento.DocumentoDteId == documentoDteId &&
                documento.EmpresaId == empresaId);

        if (documento is null ||
            documento.Estado != EstadoDocumentoDte.Validado)
        {
            throw new InvalidOperationException(
                "Selecciona un DTE validado.");
        }

        bool yaContabilizado = contexto.PartidasContables.Any(partida =>
            partida.DocumentoDteId == documentoDteId);

        if (yaContabilizado)
        {
            throw new InvalidOperationException(
                "El DTE ya posee una partida contable.");
        }

        PeriodoContable? periodo = contexto.PeriodosContables
            .SingleOrDefault(periodo =>
                periodo.PeriodoContableId == periodoContableId &&
                periodo.EmpresaId == empresaId);

        if (periodo is null || periodo.EstaCerrado)
        {
            throw new InvalidOperationException(
                "Selecciona un período abierto.");
        }

        if (documento.FechaEmision.Date < periodo.FechaInicio.Date ||
            documento.FechaEmision.Date > periodo.FechaFin.Date)
        {
            throw new InvalidOperationException(
                "La fecha del DTE no pertenece al período seleccionado.");
        }

        ReglaContable? regla = contexto.ReglasContables
            .Include(regla => regla.Cuentas)
            .ThenInclude(linea => linea.CuentaContable)
            .SingleOrDefault(regla =>
                regla.ReglaContableId == reglaContableId &&
                regla.EmpresaId == empresaId &&
                regla.Activa);

        if (regla is null)
        {
            throw new InvalidOperationException(
                "La regla contable no está activa.");
        }

        if (!string.IsNullOrWhiteSpace(regla.TipoDte) &&
            regla.TipoDte != documento.TipoDte)
        {
            throw new InvalidOperationException(
                "La regla no corresponde al tipo de DTE seleccionado.");
        }

        if (regla.RolEmpresa.HasValue &&
            regla.RolEmpresa.Value != RolEmpresaDte.NoDeterminado &&
            regla.RolEmpresa.Value != documento.RolEmpresa)
        {
            throw new InvalidOperationException(
                "La regla no corresponde al rol de la empresa en este DTE.");
        }

        if (regla.Cuentas.Count < 2)
        {
            throw new InvalidOperationException(
                "La regla debe tener al menos dos líneas contables.");
        }

        Dictionary<string, decimal> valores =
            ObtenerValoresDte(contexto, documentoDteId);

        var detalles = new List<DetallePartida>();

        foreach (ReglaCuenta lineaRegla in regla.Cuentas
                     .OrderBy(linea => linea.Orden))
        {
            if (lineaRegla.CuentaContable is null ||
                !lineaRegla.CuentaContable.Activa ||
                !lineaRegla.CuentaContable.PermiteMovimientos)
            {
                throw new InvalidOperationException(
                    "Una cuenta de la regla está inactiva o no permite movimientos.");
            }

            decimal importe = ResolverFormula(
                lineaRegla.Formula,
                valores);

            if (importe <= 0)
            {
                throw new InvalidOperationException(
                    $"La fórmula '{lineaRegla.Formula}' genera un importe no válido.");
            }

            detalles.Add(new DetallePartida
            {
                CuentaContableId = lineaRegla.CuentaContableId,
                Concepto = $"{regla.Nombre} - {lineaRegla.Formula}",
                Debe = lineaRegla.Lado == LadoContable.Debe
                    ? importe
                    : 0,
                Haber = lineaRegla.Lado == LadoContable.Haber
                    ? importe
                    : 0
            });
        }

        decimal totalDebe = detalles.Sum(detalle => detalle.Debe);
        decimal totalHaber = detalles.Sum(detalle => detalle.Haber);

        if (Math.Round(totalDebe, 2) != Math.Round(totalHaber, 2))
        {
            throw new InvalidOperationException(
                "La regla genera una partida desbalanceada.");
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
            DocumentoDteId = documentoDteId,
            ReglaContableId = reglaContableId,
            NumeroPartida = ultimoNumero + 1,
            Fecha = documento.FechaEmision.Date,
            Glosa = $"DTE {documento.TipoDte} - {documento.NumeroControl}",
            TipoOrigen = TipoOrigenPartida.Dte,
            Estado = EstadoPartidaContable.Contabilizada,
            CreadoPorId = usuarioId,
            AprobadoPorId = usuarioId,
            CreadoEn = DateTime.Now
        };

        foreach (DetallePartida detalle in detalles)
        {
            partidaNueva.Detalles.Add(detalle);
        }

        using var transaccion = contexto.Database.BeginTransaction();

        contexto.PartidasContables.Add(partidaNueva);

        documento.Estado = EstadoDocumentoDte.Contabilizado;

        contexto.SaveChanges();
        transaccion.Commit();

        return partidaNueva;
    }

    private static Dictionary<string, decimal> ObtenerValoresDte(
        ContabilidadDbContext contexto,
        int documentoDteId)
    {
        decimal baseGravada = contexto.DetallesDte
            .Where(detalle => detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaGravada) ?? 0;

        decimal montoExento = contexto.DetallesDte
            .Where(detalle => detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaExenta) ?? 0;

        decimal montoNoSujeto = contexto.DetallesDte
            .Where(detalle => detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaNoSujeta) ?? 0;

        ArchivoDte? archivo = contexto.ArchivosDte
            .AsNoTracking()
            .SingleOrDefault(archivo =>
                archivo.DocumentoDteId == documentoDteId);

        decimal iva = archivo is null
            ? 0
            : LeerResumen(archivo.JsonOriginal, "totalIva");

        decimal totalOperacion = archivo is null
            ? 0
            : LeerResumen(
                archivo.JsonOriginal,
                "montoTotalOperacion");

        if (totalOperacion == 0 && archivo is not null)
        {
            totalOperacion = LeerResumen(
                archivo.JsonOriginal,
                "totalPagar");
        }

        if (totalOperacion == 0)
        {
            totalOperacion =
                baseGravada + montoExento + montoNoSujeto + iva;
        }

        return new Dictionary<string, decimal>
        {
            ["BASEGRAVADA"] = baseGravada,
            ["MONTOEXENTO"] = montoExento,
            ["MONTONOSUJETO"] = montoNoSujeto,
            ["IVA"] = iva,
            ["TOTALOPERACION"] = totalOperacion
        };
    }

    private static decimal ResolverFormula(
        string formula,
        Dictionary<string, decimal> valores)
    {
        string formulaNormalizada = formula
            .Trim()
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        if (valores.TryGetValue(
                formulaNormalizada,
                out decimal valor))
        {
            return valor;
        }

        if (decimal.TryParse(
                formula,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out valor))
        {
            return valor;
        }

        if (decimal.TryParse(
                formula,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out valor))
        {
            return valor;
        }

        throw new InvalidOperationException(
            "Fórmula no reconocida. Usa BaseGravada, MontoExento, " +
            "MontoNoSujeto, IVA, TotalOperacion o un monto fijo.");
    }

    private static decimal LeerResumen(
        string jsonOriginal,
        string nombreCampo)
    {
        using JsonDocument json = JsonDocument.Parse(jsonOriginal);

        if (!json.RootElement.TryGetProperty(
                "resumen",
                out JsonElement resumen) ||
            !resumen.TryGetProperty(
                nombreCampo,
                out JsonElement valor))
        {
            return 0;
        }

        string texto = valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? string.Empty
            : valor.ToString();

        if (decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal resultado))
        {
            return resultado;
        }

        return decimal.TryParse(
            texto,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out resultado)
            ? resultado
            : 0;
    }
}