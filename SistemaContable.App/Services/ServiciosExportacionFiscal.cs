using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

//07 Modulo de exportación editable
namespace SistemaContableDTE.Services;

public static class ServicioExportacionFiscal
{
    public static List<ExportacionFiscal> ObtenerPorEmpresa(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.ExportacionesFiscales
            .AsNoTracking()
            .Include(exportacion => exportacion.PeriodoContable)
            .Where(exportacion => exportacion.EmpresaId == empresaId)
            .OrderByDescending(exportacion => exportacion.GeneradoEn)
            .ToList();
    }

    public static ExportacionFiscal Generar(
        int empresaId,
        int usuarioId,
        int periodoContableId,
        TipoLibroIva tipoLibro,
        string versionFormato)
    {
        if (string.IsNullOrWhiteSpace(versionFormato))
        {
            throw new ArgumentException(
                "Ingresa la versión del formato fiscal.");
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

        List<RegistroLibroIva> registros = contexto.RegistrosLibroIva
            .Include(registro => registro.DocumentoDte)
            .ThenInclude(documento => documento!.Participantes)
            .Where(registro =>
                registro.PeriodoContableId == periodoContableId &&
                registro.TipoLibro == tipoLibro &&
                registro.Estado == EstadoRegistroIva.Vigente &&
                registro.DocumentoDte!.EmpresaId == empresaId)
            .OrderBy(registro => registro.FechaRegistro)
            .ToList();

        if (registros.Count == 0)
        {
            throw new InvalidOperationException(
                "No existen registros vigentes para exportar.");
        }

        ServicioLibroIva.CompletarDatosContraparte(registros);

        string tipoAnexo = ObtenerTipoAnexo(tipoLibro);

        var lineas = new List<string>
        {
            "Fecha;NumeroControl;CodigoGeneracion;TipoDte;TipoLibro;" +
            "NombreTercero;NIT;NRC;BaseGravada;MontoExento;" +
            "MontoNoSujeto;CreditoFiscal;DebitoFiscal;TotalOperacion"
        };

        int numeroLinea = 1;

        var exportacion = new ExportacionFiscal
        {
            EmpresaId = empresaId,
            PeriodoContableId = periodoContableId,
            TipoAnexo = tipoAnexo,
            VersionFormato = versionFormato.Trim(),
            NombreArchivo = CrearNombreArchivo(
                tipoAnexo,
                periodo,
                versionFormato),
            GeneradoPorId = usuarioId,
            GeneradoEn = DateTime.Now
        };

        foreach (RegistroLibroIva registro in registros)
        {
            string contenidoLinea = CrearLinea(registro);

            lineas.Add(contenidoLinea);

            exportacion.Detalles.Add(new ExportacionFiscalDetalle
            {
                RegistroLibroIvaId = registro.RegistroLibroIvaId,
                Linea = numeroLinea,
                ContenidoLinea = contenidoLinea
            });

            numeroLinea++;
        }

        exportacion.Contenido = string.Join(
            Environment.NewLine,
            lineas);

        exportacion.HashSha256 = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(exportacion.Contenido)));

        contexto.ExportacionesFiscales.Add(exportacion);
        contexto.SaveChanges();

        return exportacion;
    }

    public static string ObtenerContenido(
        int empresaId,
        int exportacionFiscalId)
    {
        using var contexto = new ContabilidadDbContext();

        ExportacionFiscal? exportacion = contexto.ExportacionesFiscales
            .SingleOrDefault(exportacion =>
                exportacion.ExportacionFiscalId == exportacionFiscalId &&
                exportacion.EmpresaId == empresaId);

        if (exportacion is null)
        {
            throw new InvalidOperationException(
                "No se encontró la exportación fiscal.");
        }

        return exportacion.Contenido;
    }

    public static string ObtenerTipoAnexo(TipoLibroIva tipoLibro)
    {
        return tipoLibro switch
        {
            TipoLibroIva.VentasContribuyentes =>
                "Anexo 1 Ventas a Contribuyentes",

            TipoLibroIva.VentasConsumidorFinal =>
                "Anexo 2 Ventas a Consumidor Final",

            TipoLibroIva.Compras =>
                "Anexo 3 Compras a Contribuyentes",

            TipoLibroIva.SujetosExcluidos =>
                "Anexo 5 Compras a Sujetos Excluidos",

            _ => throw new ArgumentOutOfRangeException(nameof(tipoLibro))
        };
    }

    private static string CrearNombreArchivo(
        string tipoAnexo,
        PeriodoContable periodo,
        string versionFormato)
    {
        string anexo = tipoAnexo
            .Replace(" ", "_")
            .Replace("á", "a")
            .Replace("ó", "o");

        string version = versionFormato
            .Replace(" ", "_");

        return $"{anexo}_{periodo.FechaInicio:yyyyMM}_{version}.csv";
    }

    private static string CrearLinea(RegistroLibroIva registro)
    {
        DocumentoDte? documento = registro.DocumentoDte;

        return string.Join(";",
            registro.FechaRegistro.ToString("dd/MM/yyyy"),
            Escapar(documento?.NumeroControl ?? string.Empty),
            Escapar(documento?.CodigoGeneracion ?? string.Empty),
            Escapar(documento?.TipoDte ?? string.Empty),
            registro.TipoLibro,
            Escapar(registro.NombreTercero),
            Escapar(registro.NitTercero ?? string.Empty),
            Escapar(registro.NrcTercero ?? string.Empty),
            FormatearMonto(registro.BaseGravada),
            FormatearMonto(registro.MontoExento),
            FormatearMonto(registro.MontoNoSujeto),
            FormatearMonto(registro.CreditoFiscal),
            FormatearMonto(registro.DebitoFiscal),
            FormatearMonto(registro.TotalOperacion));
    }

    private static string FormatearMonto(decimal monto)
    {
        return monto.ToString(
            "0.00",
            CultureInfo.InvariantCulture);
    }

    private static string Escapar(string valor)
    {
        return valor.Contains(';') ||
               valor.Contains('"') ||
               valor.Contains('\n')
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;
    }
}