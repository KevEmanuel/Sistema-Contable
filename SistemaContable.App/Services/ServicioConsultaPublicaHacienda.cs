using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

//clase para consultar el servicio del estado del Dte
public sealed record ResultadoConsultaPublicaHacienda(
    string EstadoHacienda,
    string CodigoEstado,
    string Descripcion,
    DateTime? FechaProcesamiento,
    string? SelloRecibido,
    string? Observaciones,
    string RespuestaJson,
    EstadoDocumentoDte EstadoDocumentoSugerido);

public static class ServicioConsultaPublicaHacienda
{
    private static readonly HttpClient ClienteHttp = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly Uri UrlPortal =
        new("https://admin.factura.gob.sv/");

    public static async Task<ResultadoConsultaPublicaHacienda> ConsultarAsync(
        int empresaId,
        DocumentoDte documento,
        CancellationToken cancellationToken = default)
    {
        string jsonOriginal = ObtenerJsonDte(empresaId, documento);
        using JsonDocument json = JsonDocument.Parse(jsonOriginal);
        JsonElement identificacion = ObtenerIdentificacion(json.RootElement);

        string codigoGeneracion = LeerTexto(
            identificacion,
            "codigoGeneracion");
        if (!string.Equals(
                codigoGeneracion,
                documento.CodigoGeneracion,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El código de generación del JSON no coincide con el DTE seleccionado.");
        }

        string fechaGeneracionTexto = LeerTexto(
            identificacion,
            "fecEmi");
        if (!DateTime.TryParseExact(
                fechaGeneracionTexto,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fechaGeneracion) ||
            fechaGeneracion.Date != documento.FechaEmision.Date)
        {
            throw new InvalidOperationException(
                "La fecha de generación del JSON no es válida o no coincide con el DTE seleccionado.");
        }

        string ambiente = LeerTexto(identificacion, "ambiente");
        string rutaAmbiente = ambiente switch
        {
            "00" => "test",
            "01" => "prod",
            _ => throw new InvalidOperationException(
                $"El ambiente '{ambiente}' del DTE no es compatible con la consulta pública de Hacienda.")
        };

        string url =
            $"{UrlPortal}{rutaAmbiente}/consultas/publica/simple/1" +
            $"?codigoGeneracion={Uri.EscapeDataString(codigoGeneracion)}" +
            $"&fechaEmi={fechaGeneracion:yyyy-MM-dd}" +
            $"&ambiente={Uri.EscapeDataString(ambiente)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("application/json, text/plain, */*");
        request.Headers.Referrer = new Uri(UrlPortal, "consultaPublica");
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/131.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation(
            "Origin",
            UrlPortal.GetLeftPart(UriPartial.Authority));
        request.Headers.TryAddWithoutValidation(
            "Content-Type",
            "application/json");

        using HttpResponseMessage response = await ClienteHttp.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        string contenido = await response.Content.ReadAsStringAsync(
            cancellationToken);
        using JsonDocument respuesta = JsonDocument.Parse(contenido);
        JsonElement raiz = respuesta.RootElement;
        if (raiz.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "La consulta pública de Hacienda devolvió una respuesta inesperada.");
        }

        string? codigoRespuesta = LeerTextoOpcional(raiz, "codGen");
        if (!string.Equals(
                codigoRespuesta,
                codigoGeneracion,
                StringComparison.OrdinalIgnoreCase))
        {
            string detalle = LeerTextoOpcional(raiz, "descripcionEstado")
                ?? "Hacienda no encontró el DTE con los datos consultados.";
            throw new InvalidOperationException(detalle);
        }

        string codigoEstado =
            LeerTextoOpcional(raiz, "estadoDocInc") ?? string.Empty;
        string estadoHacienda =
            LeerTextoOpcional(raiz, "estadoDoc") ?? string.Empty;
        EstadoDocumentoDte estadoDocumento =
            ObtenerEstadoDocumento(codigoEstado, estadoHacienda);
        string descripcion =
            LeerTextoOpcional(raiz, "descripcionEstado") ??
            estadoHacienda;
        string? fechaProcesamientoTexto =
            LeerTextoOpcional(raiz, "fechaProcesado");
        DateTime? fechaProcesamiento = LeerFechaProcesamiento(
            fechaProcesamientoTexto);
        string? selloRecibido = LeerTextoOpcional(raiz, "selloVal");
        string? observaciones = LeerObservaciones(raiz);

        string respuestaJson = JsonSerializer.Serialize(new
        {
            ambiente,
            codigoGeneracion,
            fechaGeneracion = fechaGeneracion.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            estadoHacienda,
            codigoEstado,
            descripcion,
            fechaProcesamiento,
            selloRecibido,
            observaciones
        });

        return new ResultadoConsultaPublicaHacienda(
            estadoHacienda,
            codigoEstado,
            descripcion,
            fechaProcesamiento,
            selloRecibido,
            observaciones,
            respuestaJson,
            estadoDocumento);
    }

    private static string ObtenerJsonDte(
        int empresaId,
        DocumentoDte documento)
    {
        using var contexto = new ContabilidadDbContext();

        string? jsonOriginal = contexto.ArchivosDte
            .Where(archivo =>
                archivo.DocumentoDteId == documento.DocumentoDteId &&
                archivo.DocumentoDte!.EmpresaId == empresaId)
            .Select(archivo => archivo.JsonOriginal)
            .SingleOrDefault();

        return jsonOriginal
            ?? throw new InvalidOperationException(
                "No se encontró el JSON original del DTE seleccionado.");
    }

    private static JsonElement ObtenerIdentificacion(JsonElement raiz)
    {
        if (raiz.ValueKind == JsonValueKind.Object &&
            raiz.TryGetProperty(
                "identificacion",
                out JsonElement identificacion) &&
            identificacion.ValueKind == JsonValueKind.Object)
        {
            return identificacion;
        }

        throw new InvalidOperationException(
            "El JSON original no contiene la identificación requerida para consultar Hacienda.");
    }

    private static string LeerTexto(JsonElement objeto, string nombre)
    {
        return LeerTextoOpcional(objeto, nombre)
            ?? throw new InvalidOperationException(
                $"El JSON original no contiene el campo '{nombre}'.");
    }

    private static string? LeerTextoOpcional(
        JsonElement objeto,
        string nombre)
    {
        if (objeto.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (JsonProperty propiedad in objeto.EnumerateObject())
        {
            if (string.Equals(
                    propiedad.Name,
                    nombre,
                    StringComparison.OrdinalIgnoreCase) &&
                propiedad.Value.ValueKind != JsonValueKind.Null)
            {
                string? valor = propiedad.Value.ValueKind ==
                                JsonValueKind.String
                    ? propiedad.Value.GetString()
                    : propiedad.Value.ToString();

                return string.IsNullOrWhiteSpace(valor)
                    ? null
                    : valor.Trim();
            }
        }

        return null;
    }

    private static DateTime? LeerFechaProcesamiento(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        string[] formatos =
        [
            "yyyy-MM-dd HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"
        ];

        return DateTime.TryParseExact(
            texto,
            formatos,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out DateTime fecha)
            ? fecha
            : null;
    }

    private static string? LeerObservaciones(JsonElement raiz)
    {
        if (raiz.ValueKind != JsonValueKind.Object ||
            !raiz.TryGetProperty(
                "observaciones",
                out JsonElement observaciones) ||
            observaciones.ValueKind is
                JsonValueKind.Null or JsonValueKind.Undefined)
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

    private static EstadoDocumentoDte ObtenerEstadoDocumento(
        string codigoEstado,
        string estadoHacienda)
    {
        return codigoEstado.Trim().ToUpperInvariant() switch
        {
            "SR" or "SRO" or "SRA" =>
                EstadoDocumentoDte.Validado,
            "RE" => EstadoDocumentoDte.Rechazado,
            "INV" => EstadoDocumentoDte.Invalidado,
            "EE" => throw new InvalidOperationException(
                "Hacienda no pudo confirmar el estado del DTE. No se modificó el registro local."),
            _ => estadoHacienda.Trim().ToUpperInvariant() switch
            {
                "PROCESADO" or
                "TRANSMITIDO" or
                "TRANSMITIDO SATISFACTORIAMENTE" =>
                    EstadoDocumentoDte.Validado,
                "RECHAZADO" => EstadoDocumentoDte.Rechazado,
                "INVALIDADO" or "ANULADO" =>
                    EstadoDocumentoDte.Invalidado,
                _ => throw new InvalidOperationException(
                    $"Hacienda devolvió un estado no reconocido ('{estadoHacienda}', código '{codigoEstado}'). No se modificó el registro local.")
            }
        };
    }
}
