using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

//modulo para la validacion fiscal 05

namespace SistemaContableDTE.Services;

public static class ServicioLibroIva
{
    public static List<DocumentoDte> ObtenerDocumentosValidados(
        int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.DocumentosDte
            .AsNoTracking()
            .Where(documento =>
                documento.EmpresaId == empresaId &&
                documento.Estado == EstadoDocumentoDte.Validado)
            .OrderByDescending(documento => documento.FechaEmision)
            .ToList();
    }

    public static List<RegistroLibroIva> ObtenerPorPeriodo(
        int empresaId,
        int periodoContableId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.RegistrosLibroIva
            .AsNoTracking()
            .Include(registro => registro.DocumentoDte)
            .Where(registro =>
                registro.PeriodoContableId == periodoContableId &&
                registro.DocumentoDte!.EmpresaId == empresaId)
            .OrderBy(registro => registro.FechaRegistro)
            .ToList();
    }

    public static RegistroLibroIva Generar(
        int empresaId,
        int documentoDteId,
        int periodoContableId,
        TipoLibroIva tipoLibro)
    {
        using var contexto = new ContabilidadDbContext();

        DocumentoDte? documento = contexto.DocumentosDte
            .SingleOrDefault(documento =>
                documento.DocumentoDteId == documentoDteId &&
                documento.EmpresaId == empresaId);

        if (documento is null)
        {
            throw new InvalidOperationException(
                "El DTE seleccionado no pertenece a la empresa.");
        }

        if (documento.Estado != EstadoDocumentoDte.Validado)
        {
            throw new InvalidOperationException(
                "Solo los DTE validados pueden generar Libro IVA.");
        }

        bool yaRegistrado = contexto.RegistrosLibroIva.Any(registro =>
            registro.DocumentoDteId == documentoDteId);

        if (yaRegistrado)
        {
            throw new InvalidOperationException(
                "Este DTE ya posee un registro de Libro IVA.");
        }

        PeriodoContable? periodo = contexto.PeriodosContables
            .SingleOrDefault(periodo =>
                periodo.PeriodoContableId == periodoContableId &&
                periodo.EmpresaId == empresaId);

        if (periodo is null || periodo.EstaCerrado)
        {
            throw new InvalidOperationException(
                "Selecciona un período contable abierto.");
        }

        if (documento.FechaEmision.Date < periodo.FechaInicio.Date ||
            documento.FechaEmision.Date > periodo.FechaFin.Date)
        {
            throw new InvalidOperationException(
                "La fecha del DTE no pertenece al período seleccionado.");
        }

        decimal baseGravada = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaGravada) ?? 0;

        decimal montoExento = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaExenta) ?? 0;

        decimal montoNoSujeto = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaNoSujeta) ?? 0;

        ArchivoDte? archivo = contexto.ArchivosDte
            .AsNoTracking()
            .SingleOrDefault(archivo =>
                archivo.DocumentoDteId == documentoDteId);

        decimal iva = archivo is null
            ? 0
            : LeerValorResumen(archivo.JsonOriginal, "totalIva");

        decimal totalOperacion = archivo is null
            ? 0
            : LeerValorResumen(
                archivo.JsonOriginal,
                "montoTotalOperacion");

        if (totalOperacion == 0 && archivo is not null)
        {
            totalOperacion = LeerValorResumen(
                archivo.JsonOriginal,
                "totalPagar");
        }

        if (totalOperacion == 0)
        {
            totalOperacion =
                baseGravada + montoExento + montoNoSujeto + iva;
        }

        bool esCompra = tipoLibro == TipoLibroIva.Compras;

        bool esVenta =
            tipoLibro == TipoLibroIva.VentasContribuyentes ||
            tipoLibro == TipoLibroIva.VentasConsumidorFinal;

        var registroNuevo = new RegistroLibroIva
        {
            DocumentoDteId = documentoDteId,
            PeriodoContableId = periodoContableId,
            TipoLibro = tipoLibro,
            FechaRegistro = documento.FechaEmision.Date,
            BaseGravada = baseGravada,
            MontoExento = montoExento,
            MontoNoSujeto = montoNoSujeto,
            CreditoFiscal = esCompra ? iva : 0,
            DebitoFiscal = esVenta ? iva : 0,
            TotalOperacion = totalOperacion,
            Estado = EstadoRegistroIva.Vigente
        };

        contexto.RegistrosLibroIva.Add(registroNuevo);
        contexto.SaveChanges();

        return registroNuevo;
    }

    private static decimal LeerValorResumen(
        string jsonOriginal,
        string nombreCampo)
    {
        using JsonDocument json = JsonDocument.Parse(jsonOriginal);

        if (!json.RootElement.TryGetProperty(
                "resumen",
                out JsonElement resumen) ||
            resumen.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        if (!resumen.TryGetProperty(
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