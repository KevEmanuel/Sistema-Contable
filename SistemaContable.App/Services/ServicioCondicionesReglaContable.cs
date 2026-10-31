using System.Text.Json;
using System.Text.Json.Serialization;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

public static class ServicioCondicionesReglaContable
{
    private static readonly JsonSerializerOptions JsonOptions = CrearOpcionesJson();

    public static string CrearJson(
        IEnumerable<CondicionReglaContable> condiciones)
    {
        List<CondicionReglaContable> lista = condiciones.ToList();

        if (lista.Count == 0)
        {
            return "{}";
        }

        ValidarCondiciones(lista);

        return JsonSerializer.Serialize(
            new DefinicionCondicionesRegla
            {
                Version = 1,
                OperadorLogico = "Y",
                Condiciones = lista
            },
            JsonOptions);
    }

    public static string ValidarYNormalizarJson(string json)
    {
        DefinicionCondicionesRegla definicion = LeerDefinicion(json);
        ValidarMetadatos(definicion);

        if (definicion.Condiciones.Count == 0)
        {
            return "{}";
        }

        ValidarDefinicion(definicion);
        return CrearJson(definicion.Condiciones);
    }

    public static bool CumpleCondiciones(
        string json,
        IReadOnlyDictionary<string, decimal> valores)
    {
        DefinicionCondicionesRegla definicion = LeerDefinicion(json);
        ValidarMetadatos(definicion);

        if (definicion.Condiciones.Count == 0)
        {
            return true;
        }

        ValidarDefinicion(definicion);

        return definicion.Condiciones.All(condicion =>
        {
            string clave = condicion.Campo.ToString().ToUpperInvariant();

            if (!valores.TryGetValue(clave, out decimal valorDte))
            {
                throw new InvalidOperationException(
                    $"No se pudo evaluar la condición porque falta el importe '{clave}' del DTE.");
            }

            return condicion.Operador switch
            {
                OperadorCondicionRegla.Igual =>
                    valorDte == condicion.Valor,
                OperadorCondicionRegla.MayorQue =>
                    valorDte > condicion.Valor,
                OperadorCondicionRegla.MayorOIgual =>
                    valorDte >= condicion.Valor,
                OperadorCondicionRegla.MenorQue =>
                    valorDte < condicion.Valor,
                OperadorCondicionRegla.MenorOIgual =>
                    valorDte <= condicion.Valor,
                _ => throw new InvalidOperationException(
                    "La regla contiene un operador de condición no compatible.")
            };
        });
    }

    private static DefinicionCondicionesRegla LeerDefinicion(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                "La regla no contiene una configuración de condiciones válida.");
        }

        try
        {
            return JsonSerializer.Deserialize<DefinicionCondicionesRegla>(
                       json,
                       JsonOptions)
                   ?? throw new InvalidOperationException(
                       "La configuración de condiciones de la regla está vacía.");
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException(
                "No se pudo leer la configuración de condiciones de la regla.",
                error);
        }
    }

    private static void ValidarDefinicion(
        DefinicionCondicionesRegla definicion)
    {
        ValidarMetadatos(definicion);

        if (definicion.Condiciones is null ||
            definicion.Condiciones.Count == 0)
        {
            throw new InvalidOperationException(
                "La definición lógica de la regla no contiene condiciones.");
        }

        ValidarCondiciones(definicion.Condiciones);
    }

    private static void ValidarMetadatos(
        DefinicionCondicionesRegla definicion)
    {
        if (definicion.Condiciones is null)
        {
            throw new InvalidOperationException(
                "La regla no contiene una lista de condiciones válida.");
        }

        if (definicion.Version != 1)
        {
            throw new InvalidOperationException(
                $"La versión {definicion.Version} de condiciones no es compatible.");
        }

        if (!string.Equals(
                definicion.OperadorLogico,
                "Y",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La regla contiene una combinación lógica no compatible.");
        }
    }

    private static void ValidarCondiciones(
        IReadOnlyCollection<CondicionReglaContable> condiciones)
    {
        foreach (CondicionReglaContable? condicion in condiciones)
        {
            if (condicion is null ||
                !Enum.IsDefined(condicion.Campo) ||
                !Enum.IsDefined(condicion.Operador))
            {
                throw new InvalidOperationException(
                    "La regla contiene una condición desconocida.");
            }

            if (condicion.Valor < 0)
            {
                throw new InvalidOperationException(
                    "El valor de una condición monetaria no puede ser negativo.");
            }
        }
    }

    private static JsonSerializerOptions CrearOpcionesJson()
    {
        var opciones = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        opciones.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return opciones;
    }
}
