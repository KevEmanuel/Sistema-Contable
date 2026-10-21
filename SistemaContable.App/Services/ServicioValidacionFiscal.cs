using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

//modulo para la validacion de libro fiscal 05
namespace SistemaContableDTE.Services;

public static class ServicioValidacionFiscal
{
    public static List<DocumentoDte> ObtenerDocumentos(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.DocumentosDte
            .AsNoTracking()
            .Where(documento => documento.EmpresaId == empresaId)
            .OrderByDescending(documento => documento.FechaEmision)
            .ToList();
    }

    public static List<EvidenciaFiscal> ObtenerEvidencias(
        int documentoDteId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.EvidenciasFiscales
            .AsNoTracking()
            .Where(evidencia =>
                evidencia.DocumentoDteId == documentoDteId)
            .OrderByDescending(evidencia =>
                evidencia.FechaProcesamiento)
            .ToList();
    }

    public static List<EventoDte> ObtenerEventos(int documentoDteId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.EventosDte
            .AsNoTracking()
            .Where(evento =>
                evento.DocumentoDteId == documentoDteId)
            .OrderByDescending(evento => evento.FechaEvento)
            .ToList();
    }

    public static void RegistrarEvidencia(
        int empresaId,
        int documentoDteId,
        FuenteEvidenciaFiscal fuente,
        string? selloRecibido,
        string? estadoHacienda,
        DateTime? fechaProcesamiento,
        string? codigoMensaje,
        string? descripcionMensaje,
        string? observaciones,
        string? respuestaJson,
        EstadoDocumentoDte nuevoEstado)
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

        contexto.EvidenciasFiscales.Add(new EvidenciaFiscal
        {
            DocumentoDteId = documentoDteId,
            Fuente = fuente,
            SelloRecibido = Limpiar(selloRecibido),
            EstadoHacienda = Limpiar(estadoHacienda),
            FechaProcesamiento = fechaProcesamiento ?? DateTime.Now,
            CodigoMensaje = Limpiar(codigoMensaje),
            DescripcionMensaje = Limpiar(descripcionMensaje),
            Observaciones = Limpiar(observaciones),
            RespuestaJson = Limpiar(respuestaJson),
            CreadoEn = DateTime.Now
        });

        documento.Estado = nuevoEstado;

        if (nuevoEstado == EstadoDocumentoDte.Invalidado)
        {
            List<RegistroLibroIva> registros = contexto.RegistrosLibroIva
                .Where(registro =>
                    registro.DocumentoDteId == documentoDteId)
                .ToList();

            foreach (RegistroLibroIva registro in registros)
            {
                registro.Estado = EstadoRegistroIva.Invalidado;
            }
        }

        contexto.SaveChanges();
    }

    public static void RegistrarEvento(
        int empresaId,
        int documentoDteId,
        string tipoEvento,
        string estadoEvento,
        DateTime fechaEvento,
        string? selloRecibido,
        string? detalleJson)
    {
        if (string.IsNullOrWhiteSpace(tipoEvento) ||
            string.IsNullOrWhiteSpace(estadoEvento))
        {
            throw new ArgumentException(
                "Ingresa el tipo y estado del evento.");
        }

        using var contexto = new ContabilidadDbContext();

        bool documentoExiste = contexto.DocumentosDte.Any(documento =>
            documento.DocumentoDteId == documentoDteId &&
            documento.EmpresaId == empresaId);

        if (!documentoExiste)
        {
            throw new InvalidOperationException(
                "El DTE seleccionado no pertenece a la empresa.");
        }

        contexto.EventosDte.Add(new EventoDte
        {
            DocumentoDteId = documentoDteId,
            TipoEvento = tipoEvento.Trim(),
            EstadoEvento = estadoEvento.Trim(),
            FechaEvento = fechaEvento,
            SelloRecibido = Limpiar(selloRecibido),
            DetalleJson = Limpiar(detalleJson),
            CreadoEn = DateTime.Now
        });

        contexto.SaveChanges();
    }

    private static string? Limpiar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }
}