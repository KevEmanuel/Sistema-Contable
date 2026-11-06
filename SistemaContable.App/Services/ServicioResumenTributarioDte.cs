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

        if (!resumen.TryGetProperty("tributos", out JsonElement tributos) ||
            tributos.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        decimal total = 0;
        bool encontroIva = false;
        foreach (JsonElement tributo in tributos.EnumerateArray())
        {
            if (!tributo.TryGetProperty("codigo", out JsonElement codigo) ||
                !string.Equals(
                    codigo.ToString().Trim(),
                    "20",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (!tributo.TryGetProperty("valor", out JsonElement valor) ||
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

    private static decimal? LeerImporte(JsonElement objeto, string nombre)
    {
        return objeto.TryGetProperty(nombre, out JsonElement valor) &&
               TryLeerImporte(valor, out decimal importe)
            ? importe
            : null;
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
