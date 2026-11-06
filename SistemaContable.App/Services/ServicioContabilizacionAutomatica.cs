using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

public sealed record ResumenDtesSinValidar(
    int Importados,
    int PendientesValidacion,
    int Rechazados);

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

    public static ResumenDtesSinValidar ObtenerResumenDtesSinValidar(
        int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        Dictionary<EstadoDocumentoDte, int> conteos = contexto.DocumentosDte
            .AsNoTracking()
            .Where(documento =>
                documento.EmpresaId == empresaId &&
                (documento.Estado == EstadoDocumentoDte.Importado ||
                 documento.Estado == EstadoDocumentoDte.PendienteValidacion ||
                 documento.Estado == EstadoDocumentoDte.Rechazado))
            .GroupBy(documento => documento.Estado)
            .Select(grupo => new
            {
                Estado = grupo.Key,
                Total = grupo.Count()
            })
            .ToDictionary(item => item.Estado, item => item.Total);

        return new ResumenDtesSinValidar(
            conteos.GetValueOrDefault(EstadoDocumentoDte.Importado),
            conteos.GetValueOrDefault(
                EstadoDocumentoDte.PendienteValidacion),
            conteos.GetValueOrDefault(EstadoDocumentoDte.Rechazado));
    }

    public static List<ReglaContable> ObtenerReglasVigentes(
        int empresaId,
        DocumentoDte documento)
    {
        using var contexto = new ContabilidadDbContext();

        List<ReglaContable> reglas = contexto.ReglasContables
            .AsNoTracking()
            .Include(regla => regla.Cuentas)
            .ThenInclude(linea => linea.CuentaContable)
            .Where(regla =>
                regla.EmpresaId == empresaId &&
                regla.Activa &&
                regla.VigenciDesde <= documento.FechaEmision &&
                (!regla.VigenciaHasta.HasValue ||
                 regla.VigenciaHasta.Value >= documento.FechaEmision))
            .OrderBy(regla => regla.Nombre)
            .ToList();

        reglas = reglas
            .Where(regla =>
                (string.IsNullOrWhiteSpace(regla.TipoDte) ||
                 string.Equals(
                     regla.TipoDte.Trim(),
                     documento.TipoDte.Trim(),
                     StringComparison.OrdinalIgnoreCase)) &&
                (!regla.RolEmpresa.HasValue ||
                 regla.RolEmpresa.Value == RolEmpresaDte.NoDeterminado ||
                 regla.RolEmpresa.Value == documento.RolEmpresa))
            .ToList();

        if (reglas.Count == 0)
        {
            return reglas;
        }

        bool requiereEvaluacion = reglas.Any(regla =>
            !string.Equals(
                regla.CondicionJson?.Trim(),
                "{}",
                StringComparison.Ordinal));
        Dictionary<string, decimal> valores = requiereEvaluacion
            ? ObtenerValoresDte(contexto, documento.DocumentoDteId)
            : new Dictionary<string, decimal>();

        return reglas
            .Where(regla =>
                ServicioCondicionesReglaContable.CumpleCondiciones(
                    regla.CondicionJson,
                    valores))
            .ToList();
    }

    public static PartidaContable? CrearBorradorConReglaAplicable(
        int empresaId,
        int usuarioId,
        int periodoContableId,
        int documentoDteId)
    {
        DocumentoDte? documento;
        using (var contexto = new ContabilidadDbContext())
        {
            documento = contexto.DocumentosDte
                .AsNoTracking()
                .SingleOrDefault(item =>
                    item.DocumentoDteId == documentoDteId &&
                    item.EmpresaId == empresaId);
        }

        if (documento is null ||
            documento.Estado != EstadoDocumentoDte.Validado)
        {
            throw new InvalidOperationException(
                "Solo se pueden preparar DTE validados.");
        }

        List<ReglaContable> reglas =
            ObtenerReglasVigentes(empresaId, documento);
        if (reglas.Count == 0)
        {
            return null;
        }

        if (reglas.Count > 1)
        {
            throw new InvalidOperationException(
                $"Hay {reglas.Count} reglas vigentes que aplican al DTE {documento.TipoDte}. Deja una sola regla aplicable antes de generar el borrador.");
        }

        return CrearBorrador(
            empresaId,
            usuarioId,
            periodoContableId,
            documentoDteId,
            reglas[0].ReglaContableId);
    }

    public static PartidaContable CrearBorrador(
        int empresaId,
        int usuarioId,
        int periodoContableId,
        int documentoDteId,
        int reglaContableId) =>
        CrearPartidaDesdeRegla(
            empresaId,
            usuarioId,
            periodoContableId,
            documentoDteId,
            reglaContableId,
            contabilizar: false);

    public static PartidaContable Contabilizar(
        int empresaId,
        int usuarioId,
        int periodoContableId,
        int documentoDteId,
        int reglaContableId) =>
        CrearPartidaDesdeRegla(
            empresaId,
            usuarioId,
            periodoContableId,
            documentoDteId,
            reglaContableId,
            contabilizar: true);

    private static PartidaContable CrearPartidaDesdeRegla(
        int empresaId,
        int usuarioId,
        int periodoContableId,
        int documentoDteId,
        int reglaContableId,
        bool contabilizar)
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

        if (regla.VigenciDesde.Date > documento.FechaEmision.Date ||
            (regla.VigenciaHasta.HasValue &&
             regla.VigenciaHasta.Value.Date < documento.FechaEmision.Date))
        {
            throw new InvalidOperationException(
                "La regla no está vigente para la fecha de emisión del DTE.");
        }

        if (!string.IsNullOrWhiteSpace(regla.TipoDte) &&
            !string.Equals(
                regla.TipoDte.Trim(),
                documento.TipoDte.Trim(),
                StringComparison.OrdinalIgnoreCase))
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

        if (!ServicioCondicionesReglaContable.CumpleCondiciones(
                regla.CondicionJson,
                valores))
        {
            throw new InvalidOperationException(
                "El DTE no cumple las condiciones adicionales de la regla seleccionada.");
        }

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
            Estado = contabilizar
                ? EstadoPartidaContable.Contabilizada
                : EstadoPartidaContable.Borrador,
            CreadoPorId = usuarioId,
            AprobadoPorId = contabilizar ? usuarioId : null,
            CreadoEn = DateTime.Now
        };

        foreach (DetallePartida detalle in detalles)
        {
            partidaNueva.Detalles.Add(detalle);
        }

        using var transaccion = contexto.Database.BeginTransaction();

        contexto.PartidasContables.Add(partidaNueva);

        if (contabilizar)
        {
            documento.Estado = EstadoDocumentoDte.Contabilizado;
            ServicioLibroIva.GenerarDesdePartidaContabilizada(
                contexto,
                documento,
                periodoContableId);
        }

        contexto.SaveChanges();
        if (!contabilizar)
        {
            contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
            {
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                Entidad = nameof(PartidaContable),
                IdEntidad = partidaNueva.PartidaContableId.ToString(),
                Accion = "Crear borrador contable desde regla DTE",
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    partidaNueva.NumeroPartida,
                    partidaNueva.DocumentoDteId,
                    partidaNueva.ReglaContableId,
                    documento.TipoDte,
                    Debe = detalles.Sum(detalle => detalle.Debe),
                    Haber = detalles.Sum(detalle => detalle.Haber)
                }),
                FechaHora = DateTime.Now
            });
            contexto.SaveChanges();
        }

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
            : LeerTotalIva(archivo.JsonOriginal);

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

    private static decimal LeerTotalIva(string jsonOriginal)
    {
        using JsonDocument json = JsonDocument.Parse(jsonOriginal);

        return json.RootElement.TryGetProperty(
                "resumen",
                out JsonElement resumen)
            ? ServicioResumenTributarioDte.LeerTotalIva(resumen) ?? 0
            : 0;
    }
}