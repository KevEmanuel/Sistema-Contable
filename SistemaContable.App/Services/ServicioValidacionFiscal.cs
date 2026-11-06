using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

//modulo para la validacion de libro fiscal 05
namespace SistemaContableDTE.Services;

public sealed record DatosEvidenciaDte(
    string? SelloRecibido,
    string? EstadoHacienda,
    DateTime? FechaProcesamiento,
    string? CodigoMensaje,
    string? DescripcionMensaje,
    string? Observaciones,
    string? RespuestaJson,
    bool TieneRespuestaHacienda,
    EstadoDocumentoDte? EstadoDocumentoSugerido);

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

    public static DatosEvidenciaDte ObtenerDatosEvidenciaDte(
        int empresaId,
        int documentoDteId)
    {
        using var contexto = new ContabilidadDbContext();

        string? jsonOriginal = contexto.ArchivosDte
            .AsNoTracking()
            .Where(archivo =>
                archivo.DocumentoDteId == documentoDteId &&
                archivo.DocumentoDte!.EmpresaId == empresaId)
            .Select(archivo => archivo.JsonOriginal)
            .SingleOrDefault();

        if (jsonOriginal is null)
        {
            throw new InvalidOperationException(
                "No se encontró el JSON original del DTE seleccionado.");
        }

        using JsonDocument json = JsonDocument.Parse(jsonOriginal);
        JsonElement raiz = json.RootElement;
        if (raiz.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "El JSON original del DTE debe tener un objeto como raíz.");
        }

        JsonElement respuesta = ObtenerObjetoPropiedad(
            raiz,
            "responseMH",
            "json_respuesta",
            "respuestaHacienda",
            "respuestaRecepcion",
            "respuesta",
            "response");
        bool tieneRespuestaHacienda =
            respuesta.ValueKind == JsonValueKind.Object ||
            ContieneCampoRespuesta(raiz);

        if (respuesta.ValueKind != JsonValueKind.Object)
        {
            respuesta = tieneRespuestaHacienda ? raiz : default;
        }

        string? selloRecibido =
            LeerTexto(respuesta, "selloRecibido") ??
            LeerTexto(raiz, "selloRecibido");
        string? estadoHacienda = LeerTexto(respuesta, "estado");
        string? fechaProcesamientoTexto =
            LeerTexto(respuesta, "fhProcesamiento") ??
            LeerTexto(respuesta, "fechaProcesamiento");
        DateTime? fechaProcesamiento =
            LeerFechaProcesamiento(fechaProcesamientoTexto);
        string? codigoMensaje = LeerTexto(respuesta, "codigoMsg");
        string? descripcionMensaje =
            LeerTexto(respuesta, "descripcionMsg");
        string? observaciones = LeerObservaciones(respuesta);
        string? respuestaJson = respuesta.ValueKind == JsonValueKind.Object &&
                                tieneRespuestaHacienda
            ? respuesta.GetRawText()
            : null;

        EstadoDocumentoDte? estadoDocumentoSugerido =
            estadoHacienda?.Trim().ToUpperInvariant() switch
            {
                "PROCESADO" => EstadoDocumentoDte.Validado,
                "RECHAZADO" => EstadoDocumentoDte.Rechazado,
                "INVALIDADO" or "ANULADO" =>
                    EstadoDocumentoDte.Invalidado,
                _ => null
            };

        return new DatosEvidenciaDte(
            selloRecibido,
            estadoHacienda,
            fechaProcesamiento,
            codigoMensaje,
            descripcionMensaje,
            observaciones,
            respuestaJson,
            tieneRespuestaHacienda,
            estadoDocumentoSugerido);
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

    public static List<ResultadoValidacionDte> ObtenerResultadosValidacion(
        int documentoDteId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.ResultadosValidacionDte
            .AsNoTracking()
            .Where(resultado =>
                resultado.DocumentoDteId == documentoDteId)
            .OrderByDescending(resultado => resultado.ValidadoEn)
            .ToList();
    }

    public static List<ResultadoValidacionDte> RegistrarEvidencia(
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
        EstadoDocumentoDte nuevoEstado,
        int? usuarioId = null)
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

        if (nuevoEstado == EstadoDocumentoDte.Contabilizado)
        {
            throw new InvalidOperationException(
                "El estado Contabilizado solo puede asignarse al registrar la partida contable.");
        }

        if (documento.Estado == EstadoDocumentoDte.Contabilizado &&
            nuevoEstado != EstadoDocumentoDte.Contabilizado)
        {
            throw new InvalidOperationException(
                "Un DTE contabilizado no puede cambiar de estado desde la validación fiscal.");
        }

        if (documento.Estado == EstadoDocumentoDte.Invalidado &&
            nuevoEstado != EstadoDocumentoDte.Invalidado)
        {
            throw new InvalidOperationException(
                "Un DTE invalidado no puede volver a otro estado desde la validación fiscal.");
        }

        var evidencia = new EvidenciaFiscal
        {
            DocumentoDteId = documentoDteId,
            Fuente = fuente,
            SelloRecibido = Limpiar(selloRecibido),
            EstadoHacienda = Limpiar(estadoHacienda),
            FechaProcesamiento = fechaProcesamiento,
            CodigoMensaje = Limpiar(codigoMensaje),
            DescripcionMensaje = Limpiar(descripcionMensaje),
            Observaciones = Limpiar(observaciones),
            RespuestaJson = Limpiar(respuestaJson),
            CreadoEn = DateTime.Now
        };

        List<ResultadoValidacionDte> resultados = ValidarDocumento(
            contexto,
            documento,
            evidencia,
            usuarioId);

        EstadoDocumentoDte estadoAnterior = documento.Estado;
        bool validacionAprobada = resultados.All(resultado =>
            resultado.Resultado == EstadoResultadoValidacionDte.Valida);

        contexto.EvidenciasFiscales.Add(evidencia);
        contexto.ResultadosValidacionDte.AddRange(resultados);

        if (nuevoEstado is EstadoDocumentoDte.Importado or
            EstadoDocumentoDte.PendienteValidacion or
            EstadoDocumentoDte.Validado)
        {
            documento.Estado = validacionAprobada
                ? EstadoDocumentoDte.Validado
                : EstadoDocumentoDte.PendienteValidacion;
        }
        else
        {
            documento.Estado = nuevoEstado;
        }

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

        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            Entidad = "DocumentoDte",
            IdEntidad = documentoDteId.ToString(),
            Accion = "Registrar evidencia y validar DTE",
            ValoresAnteriores = estadoAnterior.ToString(),
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                EstadoSolicitado = nuevoEstado.ToString(),
                EstadoAplicado = documento.Estado.ToString(),
                ValidacionAprobada = validacionAprobada,
                Resultados = resultados.Select(resultado => new
                {
                    Tipo = resultado.TipoValidacion.ToString(),
                    Estado = resultado.Resultado.ToString(),
                    Errores = resultado.ErroresJson
                })
            }),
            UsuarioId = usuarioId,
            FechaHora = DateTime.Now
        });

        contexto.SaveChanges();

        return resultados;
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

    public static void ActualizarEvidencia(
        int empresaId,
        int evidenciaFiscalId,
        FuenteEvidenciaFiscal fuente,
        string? selloRecibido,
        string? estadoHacienda,
        DateTime? fechaProcesamiento,
        string? codigoMensaje,
        string? descripcionMensaje,
        string? observaciones,
        string? respuestaJson,
        int? usuarioId = null)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        EvidenciaFiscal? evidencia = contexto.EvidenciasFiscales
            .Include(item => item.DocumentoDte)
            .SingleOrDefault(item =>
                item.EvidenciaFiscalId == evidenciaFiscalId &&
                item.DocumentoDte != null &&
                item.DocumentoDte.EmpresaId == empresaId);

        if (evidencia?.DocumentoDte is null)
        {
            throw new InvalidOperationException(
                "No se encontró la evidencia seleccionada.");
        }

        DocumentoDte documento = evidencia.DocumentoDte;
        if (documento.Estado is EstadoDocumentoDte.Contabilizado or
            EstadoDocumentoDte.Invalidado)
        {
            throw new InvalidOperationException(
                "No se puede modificar evidencia de un DTE contabilizado o invalidado.");
        }

        evidencia.Fuente = fuente;
        evidencia.SelloRecibido = Limpiar(selloRecibido);
        evidencia.EstadoHacienda = Limpiar(estadoHacienda);
        evidencia.FechaProcesamiento = fechaProcesamiento;
        evidencia.CodigoMensaje = Limpiar(codigoMensaje);
        evidencia.DescripcionMensaje = Limpiar(descripcionMensaje);
        evidencia.Observaciones = Limpiar(observaciones);
        evidencia.RespuestaJson = Limpiar(respuestaJson);

        List<ResultadoValidacionDte> resultados = ValidarDocumento(
            contexto,
            documento,
            evidencia,
            usuarioId);

        contexto.ResultadosValidacionDte.AddRange(resultados);
        documento.Estado = resultados.All(resultado =>
                resultado.Resultado == EstadoResultadoValidacionDte.Valida)
            ? EstadoDocumentoDte.Validado
            : EstadoDocumentoDte.PendienteValidacion;

        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            Entidad = nameof(EvidenciaFiscal),
            IdEntidad = evidenciaFiscalId.ToString(),
            Accion = "Actualizar evidencia fiscal",
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                evidencia.Fuente,
                evidencia.EstadoHacienda,
                evidencia.FechaProcesamiento
            }),
            UsuarioId = usuarioId,
            FechaHora = DateTime.Now
        });

        contexto.SaveChanges();
        transaccion.Commit();
    }

    public static void EliminarEvidencia(
        int empresaId,
        int evidenciaFiscalId,
        int? usuarioId = null)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        EvidenciaFiscal? evidencia = contexto.EvidenciasFiscales
            .Include(item => item.DocumentoDte)
            .SingleOrDefault(item =>
                item.EvidenciaFiscalId == evidenciaFiscalId &&
                item.DocumentoDte != null &&
                item.DocumentoDte.EmpresaId == empresaId);

        if (evidencia?.DocumentoDte is null)
        {
            throw new InvalidOperationException(
                "No se encontró la evidencia seleccionada.");
        }

        DocumentoDte documento = evidencia.DocumentoDte;
        if (documento.Estado is EstadoDocumentoDte.Contabilizado or
            EstadoDocumentoDte.Invalidado)
        {
            throw new InvalidOperationException(
                "No se puede eliminar evidencia de un DTE contabilizado o invalidado.");
        }

        contexto.EvidenciasFiscales.Remove(evidencia);
        EvidenciaFiscal? evidenciaRestante = contexto.EvidenciasFiscales
            .Where(item => item.DocumentoDteId == documento.DocumentoDteId)
            .OrderByDescending(item => item.FechaProcesamiento)
            .ThenByDescending(item => item.CreadoEn)
            .FirstOrDefault();

        if (evidenciaRestante is null)
        {
            documento.Estado = EstadoDocumentoDte.PendienteValidacion;
        }
        else
        {
            List<ResultadoValidacionDte> resultados = ValidarDocumento(
                contexto,
                documento,
                evidenciaRestante,
                usuarioId);
            contexto.ResultadosValidacionDte.AddRange(resultados);
            documento.Estado = resultados.All(resultado =>
                    resultado.Resultado ==
                    EstadoResultadoValidacionDte.Valida)
                ? EstadoDocumentoDte.Validado
                : EstadoDocumentoDte.PendienteValidacion;
        }

        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            Entidad = nameof(EvidenciaFiscal),
            IdEntidad = evidenciaFiscalId.ToString(),
            Accion = "Eliminar evidencia fiscal",
            ValoresAnteriores = JsonSerializer.Serialize(new
            {
                evidencia.Fuente,
                evidencia.EstadoHacienda,
                evidencia.FechaProcesamiento
            }),
            UsuarioId = usuarioId,
            FechaHora = DateTime.Now
        });

        contexto.SaveChanges();
        transaccion.Commit();
    }

    public static void ActualizarEvento(
        int empresaId,
        int eventoDteId,
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
        EventoDte? evento = contexto.EventosDte
            .Include(item => item.DocumentoDte)
            .SingleOrDefault(item =>
                item.EventoDteId == eventoDteId &&
                item.DocumentoDte != null &&
                item.DocumentoDte.EmpresaId == empresaId);

        if (evento is null)
        {
            throw new InvalidOperationException(
                "No se encontró el evento seleccionado.");
        }

        if (evento.DocumentoDte?.Estado is EstadoDocumentoDte.Contabilizado or
            EstadoDocumentoDte.Invalidado)
        {
            throw new InvalidOperationException(
                "No se puede modificar un evento de un DTE contabilizado o invalidado.");
        }

        evento.TipoEvento = tipoEvento.Trim();
        evento.EstadoEvento = estadoEvento.Trim();
        evento.FechaEvento = fechaEvento;
        evento.SelloRecibido = Limpiar(selloRecibido);
        evento.DetalleJson = Limpiar(detalleJson);
        contexto.SaveChanges();
    }

    public static void EliminarEvento(int empresaId, int eventoDteId)
    {
        using var contexto = new ContabilidadDbContext();
        EventoDte? evento = contexto.EventosDte
            .Include(item => item.DocumentoDte)
            .SingleOrDefault(item =>
                item.EventoDteId == eventoDteId &&
                item.DocumentoDte != null &&
                item.DocumentoDte.EmpresaId == empresaId);

        if (evento is null)
        {
            throw new InvalidOperationException(
                "No se encontró el evento seleccionado.");
        }

        if (evento.DocumentoDte?.Estado is EstadoDocumentoDte.Contabilizado or
            EstadoDocumentoDte.Invalidado)
        {
            throw new InvalidOperationException(
                "No se puede eliminar un evento de un DTE contabilizado o invalidado.");
        }

        contexto.EventosDte.Remove(evento);
        contexto.SaveChanges();
    }

    private static string? Limpiar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }

    private static List<ResultadoValidacionDte> ValidarDocumento(
        ContabilidadDbContext contexto,
        DocumentoDte documento,
        EvidenciaFiscal evidenciaNueva,
        int? usuarioId)
    {
        ArchivoDte? archivo = contexto.ArchivosDte
            .AsNoTracking()
            .SingleOrDefault(archivo =>
                archivo.DocumentoDteId == documento.DocumentoDteId);

        ResumenTributario? resumen = contexto.ResumenesTributarios
            .AsNoTracking()
            .SingleOrDefault(resumen =>
                resumen.DocumentoDteId == documento.DocumentoDteId);

        List<DetalleDTe> detalles = contexto.DetallesDte
            .AsNoTracking()
            .Where(detalle =>
                detalle.DocumentoDteId == documento.DocumentoDteId)
            .ToList();

        List<DteParticipante> participantes = contexto.DteParticipantes
            .AsNoTracking()
            .Where(participante =>
                participante.DocumentoDteId == documento.DocumentoDteId)
            .ToList();

        List<string> erroresEstructurales = new();
        List<string> erroresAritmeticos = new();
        List<string> erroresFiscales = new();

        JsonElement raiz = default;
        JsonElement identificacion = default;
        JsonElement cuerpoDocumento = default;
        JsonElement resumenJson = default;

        if (archivo is null)
        {
            erroresEstructurales.Add("No se encontró el JSON original del DTE.");
        }
        else
        {
            string hashCalculado = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(archivo.JsonOriginal)));

            if (!string.Equals(
                    hashCalculado,
                    archivo.HashSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                erroresEstructurales.Add(
                    "El hash SHA-256 no coincide con el JSON original.");
            }

            try
            {
                using JsonDocument json = JsonDocument.Parse(archivo.JsonOriginal);
                raiz = json.RootElement.Clone();

                if (raiz.ValueKind != JsonValueKind.Object)
                {
                    erroresEstructurales.Add(
                        "La raíz del JSON DTE debe ser un objeto.");
                }
                else
                {
                    identificacion = ObtenerObjeto(
                        raiz,
                        "identificacion",
                        erroresEstructurales);
                    cuerpoDocumento = ObtenerArreglo(
                        raiz,
                        "cuerpoDocumento",
                        erroresEstructurales);
                    resumenJson = ObtenerObjeto(
                        raiz,
                        "resumen",
                        erroresEstructurales);
                }
            }
            catch (JsonException)
            {
                erroresEstructurales.Add(
                    "El JSON original del DTE no tiene una sintaxis válida.");
            }
        }

        if (documento.VersionSchema <= 0)
        {
            erroresEstructurales.Add(
                "La versión de esquema del DTE debe ser mayor que cero.");
        }

        if (!Guid.TryParseExact(documento.CodigoGeneracion, "D", out _))
        {
            erroresEstructurales.Add(
                "El código de generación debe ser un UUID completo con guiones.");
        }

        if (string.IsNullOrWhiteSpace(documento.NumeroControl))
        {
            erroresEstructurales.Add(
                "El número de control es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(documento.TipoDte))
        {
            erroresEstructurales.Add(
                "El tipo de DTE es obligatorio.");
        }

        if (archivo is not null && raiz.ValueKind == JsonValueKind.Object)
        {
            CompararTextoIdentificacion(
                identificacion,
                "codigoGeneracion",
                documento.CodigoGeneracion,
                erroresEstructurales);
            CompararTextoIdentificacion(
                identificacion,
                "numeroControl",
                documento.NumeroControl,
                erroresEstructurales);
            CompararTextoIdentificacion(
                identificacion,
                "tipoDte",
                documento.TipoDte,
                erroresEstructurales);

            if (identificacion.ValueKind != JsonValueKind.Object ||
                !identificacion.TryGetProperty("version", out JsonElement versionJson) ||
                !int.TryParse(versionJson.ToString(), out int versionLeida) ||
                versionLeida != documento.VersionSchema)
            {
                erroresEstructurales.Add(
                    "La versión de esquema del JSON no coincide con la versión persistida.");
            }
        }

        if (resumen is null)
        {
            erroresEstructurales.Add(
                "No se encontró el resumen tributario persistido.");
        }

        if (detalles.Count == 0)
        {
            erroresEstructurales.Add(
                "El DTE debe tener al menos una línea de detalle.");
        }

        if (participantes.Count != 2 ||
            !participantes.Any(participante =>
                participante.Rol == RolParticipanteDte.Emisor) ||
            !participantes.Any(participante =>
                participante.Rol == RolParticipanteDte.Receptor))
        {
            erroresEstructurales.Add(
                "El DTE debe tener un participante emisor y uno receptor.");
        }

        if (cuerpoDocumento.ValueKind == JsonValueKind.Array &&
            detalles.Count != cuerpoDocumento.GetArrayLength())
        {
            erroresEstructurales.Add(
                "La cantidad de detalles persistidos no coincide con el JSON original.");
        }

        if (resumen is not null)
        {
            if (resumenJson.ValueKind != JsonValueKind.Object)
            {
                erroresEstructurales.Add(
                    "El JSON original no contiene un objeto resumen.");
            }
            else if (!string.Equals(
                         resumen.JsonOriginal,
                         resumenJson.GetRawText(),
                         StringComparison.Ordinal))
            {
                erroresEstructurales.Add(
                    "El resumen persistido no coincide con el objeto resumen del JSON original.");
            }
        }

        if (resumen is not null)
        {
            CompararImporte(
                "totalGravada",
                resumen.TotalGravada,
                detalles.Sum(detalle => detalle.VentaGravada),
                erroresAritmeticos);
            CompararImporte(
                "totalExenta",
                resumen.TotalExenta,
                detalles.Sum(detalle => detalle.VentaExenta),
                erroresAritmeticos);
            CompararImporte(
                "totalNoSuj",
                resumen.TotalNoSujeta,
                detalles.Sum(detalle => detalle.VentaNoSujeta),
                erroresAritmeticos);
        }

        if (string.IsNullOrWhiteSpace(evidenciaNueva.SelloRecibido) ||
            string.IsNullOrWhiteSpace(evidenciaNueva.EstadoHacienda) ||
            !evidenciaNueva.FechaProcesamiento.HasValue)
        {
            erroresFiscales.Add(
                "La evidencia registrada requiere sello recibido, estado de Hacienda y fecha de procesamiento.");
        }

        if (!string.IsNullOrWhiteSpace(evidenciaNueva.RespuestaJson))
        {
            try
            {
                using JsonDocument respuesta =
                    JsonDocument.Parse(evidenciaNueva.RespuestaJson);

                if (respuesta.RootElement.ValueKind != JsonValueKind.Object)
                {
                    erroresFiscales.Add(
                        "La respuesta JSON de Hacienda debe tener un objeto como raíz.");
                }
            }
            catch (JsonException)
            {
                erroresFiscales.Add(
                    "La respuesta JSON de Hacienda no tiene una sintaxis válida.");
            }
        }

        List<ResultadoValidacionDte> resultados = new();
        AgregarResultado(
            resultados,
            documento,
            TipoValidacionDte.Estructural,
            erroresEstructurales,
            usuarioId);
        AgregarResultado(
            resultados,
            documento,
            TipoValidacionDte.Aritmetica,
            erroresAritmeticos,
            usuarioId);
        AgregarResultado(
            resultados,
            documento,
            TipoValidacionDte.Fiscal,
            erroresFiscales,
            usuarioId);

        return resultados;
    }

    private static JsonElement ObtenerObjeto(
        JsonElement objeto,
        string nombre,
        ICollection<string> errores)
    {
        if (objeto.ValueKind == JsonValueKind.Object &&
            objeto.TryGetProperty(nombre, out JsonElement propiedad) &&
            propiedad.ValueKind == JsonValueKind.Object)
        {
            return propiedad;
        }

        errores.Add($"El JSON no contiene el objeto obligatorio '{nombre}'.");
        return default;
    }

    private static JsonElement ObtenerArreglo(
        JsonElement objeto,
        string nombre,
        ICollection<string> errores)
    {
        if (objeto.ValueKind == JsonValueKind.Object &&
            objeto.TryGetProperty(nombre, out JsonElement propiedad) &&
            propiedad.ValueKind == JsonValueKind.Array)
        {
            return propiedad;
        }

        errores.Add($"El JSON no contiene el arreglo obligatorio '{nombre}'.");
        return default;
    }

    private static void CompararTextoIdentificacion(
        JsonElement identificacion,
        string nombre,
        string valorPersistido,
        ICollection<string> errores)
    {
        if (identificacion.ValueKind != JsonValueKind.Object ||
            !identificacion.TryGetProperty(nombre, out JsonElement valorJson) ||
            !string.Equals(
                valorJson.ToString().Trim(),
                valorPersistido,
                StringComparison.OrdinalIgnoreCase))
        {
            errores.Add(
                $"El campo '{nombre}' del JSON no coincide con el dato persistido.");
        }
    }

    private static void CompararImporte(
        string nombre,
        decimal? importeResumen,
        decimal importeDetalle,
        ICollection<string> errores)
    {
        if (!importeResumen.HasValue)
        {
            return;
        }

        if (Math.Round(importeResumen.Value, 2) !=
            Math.Round(importeDetalle, 2))
        {
            errores.Add(
                $"El campo '{nombre}' del resumen no coincide con la suma de los detalles.");
        }
    }

    private static void AgregarResultado(
        ICollection<ResultadoValidacionDte> resultados,
        DocumentoDte documento,
        TipoValidacionDte tipo,
        IReadOnlyCollection<string> errores,
        int? usuarioId)
    {
        resultados.Add(new ResultadoValidacionDte
        {
            DocumentoDteId = documento.DocumentoDteId,
            TipoValidacion = tipo,
            VersionSchema = documento.VersionSchema,
            VersionValidador = "1.0",
            Resultado = errores.Count == 0
                ? EstadoResultadoValidacionDte.Valida
                : EstadoResultadoValidacionDte.Invalida,
            ErroresJson = JsonSerializer.Serialize(errores),
            ValidadoEn = DateTime.Now,
            UsuarioId = usuarioId
        });
    }

    private static JsonElement ObtenerObjetoPropiedad(
        JsonElement objeto,
        params string[] nombres)
    {
        if (objeto.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        foreach (JsonProperty propiedad in objeto.EnumerateObject())
        {
            if (nombres.Contains(
                    propiedad.Name,
                    StringComparer.OrdinalIgnoreCase) &&
                propiedad.Value.ValueKind == JsonValueKind.Object)
            {
                return propiedad.Value;
            }
        }

        return default;
    }

    private static bool ContieneCampoRespuesta(JsonElement objeto)
    {
        if (objeto.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        string[] nombres =
        [
            "estado",
            "fhProcesamiento",
            "fechaProcesamiento",
            "codigoMsg",
            "descripcionMsg",
            "observaciones"
        ];

        return objeto.EnumerateObject().Any(propiedad =>
            nombres.Contains(
                propiedad.Name,
                StringComparer.OrdinalIgnoreCase));
    }

    private static string? LeerTexto(
        JsonElement objeto,
        string nombre)
    {
        if (objeto.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        JsonElement valor = default;
        bool encontrada = false;
        foreach (JsonProperty propiedad in objeto.EnumerateObject())
        {
            if (string.Equals(
                    propiedad.Name,
                    nombre,
                    StringComparison.OrdinalIgnoreCase))
            {
                valor = propiedad.Value;
                encontrada = true;
                break;
            }
        }

        if (!encontrada ||
            valor.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        string texto = valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? string.Empty
            : valor.ToString();

        return string.IsNullOrWhiteSpace(texto)
            ? null
            : texto.Trim();
    }

    private static DateTime? LeerFechaProcesamiento(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        string[] formatos =
        [
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy H:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
            "yyyy-MM-dd HH:mm:ss"
        ];

        return DateTime.TryParseExact(
                   texto,
                   formatos,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AllowWhiteSpaces |
                   DateTimeStyles.AssumeLocal,
                   out DateTime fecha)
            ? fecha
            : DateTime.TryParse(
                texto,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces |
                DateTimeStyles.AssumeLocal,
                out fecha)
                ? fecha
                : null;
    }

    private static string? LeerObservaciones(JsonElement respuesta)
    {
        if (respuesta.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        JsonElement observaciones = default;
        foreach (JsonProperty propiedad in respuesta.EnumerateObject())
        {
            if (string.Equals(
                    propiedad.Name,
                    "observaciones",
                    StringComparison.OrdinalIgnoreCase))
            {
                observaciones = propiedad.Value;
                break;
            }
        }

        if (observaciones.ValueKind is
            JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (observaciones.ValueKind == JsonValueKind.Array)
        {
            string[] elementos = observaciones.EnumerateArray()
                .Select(elemento =>
                    elemento.ValueKind == JsonValueKind.String
                        ? elemento.GetString()
                        : elemento.GetRawText())
                .Where(elemento =>
                    !string.IsNullOrWhiteSpace(elemento))
                .Select(elemento => elemento!)
                .ToArray();

            return elementos.Length == 0
                ? null
                : string.Join(Environment.NewLine, elementos);
        }

        return observaciones.ValueKind == JsonValueKind.String
            ? observaciones.GetString()
            : observaciones.GetRawText();
    }
}