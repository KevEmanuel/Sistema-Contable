using System.Globalization;
using System.Text.Json;

namespace SistemaContableDTE.Services;

public static class ServicioResumenTributarioDte
{
    public static decimal? LeerTotalIva(JsonElement resumen)
    {
        decimal? totalIva = LeerImporte(resumen, "totalIva");
        if (totalIva.HasValue)
        {
            return totalIva;
        }

        if (!TryGetProperty(resumen, "tributos", out JsonElement tributos) ||
            tributos.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        decimal total = 0;
        bool encontroIva = false;
        foreach (JsonElement tributo in tributos.EnumerateArray())
        {
            if (!TryGetProperty(tributo, "codigo", out JsonElement codigo) ||
                !string.Equals(
                    codigo.ToString().Trim(),
                    "20",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryGetProperty(tributo, "valor", out JsonElement valor) ||
                !TryLeerImporte(valor, out decimal importe))
            {
                throw new ArgumentException(
                    "El tributo de IVA del resumen no contiene un importe válido.");
            }

            total += importe;
            encontroIva = true;
        }

        return encontroIva ? total : null;
    }

    public static decimal? LeerTotalIva(string jsonOriginal)
    {
        using JsonDocument json = JsonDocument.Parse(jsonOriginal);
        JsonElement resumen = ObtenerResumen(json.RootElement);
        return LeerTotalIva(resumen);
    }

    public static decimal? LeerImporte(
        JsonElement objeto,
        string nombre)
    {
        return TryGetProperty(objeto, nombre, out JsonElement valor) &&
               TryLeerImporte(valor, out decimal importe)
            ? importe
            : null;
    }

    public static decimal? LeerImporte(
        string jsonOriginal,
        string nombre)
    {
        using JsonDocument json = JsonDocument.Parse(jsonOriginal);
        return LeerImporte(ObtenerResumen(json.RootElement), nombre);
    }

    private static JsonElement ObtenerResumen(JsonElement raiz) =>
        TryGetProperty(raiz, "resumen", out JsonElement resumen)
            ? resumen
            : raiz;

    private static bool TryGetProperty(
        JsonElement objeto,
        string nombre,
        out JsonElement valor)
    {
        if (objeto.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty propiedad in objeto.EnumerateObject())
            {
                if (string.Equals(
                        propiedad.Name,
                        nombre,
                        StringComparison.OrdinalIgnoreCase))
                {
                    valor = propiedad.Value;
                    return true;
                }
            }
        }

        valor = default;
        return false;
    }

    private static bool TryLeerImporte(JsonElement valor, out decimal importe)
    {
        importe = 0;
        return (valor.ValueKind is JsonValueKind.Number or JsonValueKind.String) &&
               decimal.TryParse(
                   valor.ToString(),
                   NumberStyles.Number | NumberStyles.AllowExponent,
                   CultureInfo.InvariantCulture,
                   out importe);
    }
}
