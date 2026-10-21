using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

//modulo para la carga del dte 04

public static class ServicioCargaDte
{
    public static List<DocumentoDte> ObtenerPorEmpresa(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.DocumentosDte
            .AsNoTracking()
            .Where(documento => documento.EmpresaId == empresaId)
            .OrderByDescending(documento => documento.FechaEmision)
            .ThenByDescending(documento => documento.CreadoEn)
            .ToList();
    }

    public static DocumentoDte ImportarArchivo(
        int empresaId,
        string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo JSON seleccionado.");
        }

        string jsonOriginal = File.ReadAllText(rutaArchivo);

        return ImportarJson(empresaId, jsonOriginal);
    }

    public static DocumentoDte ImportarJson(
        int empresaId,
        string jsonOriginal)
    {
        if (string.IsNullOrWhiteSpace(jsonOriginal))
        {
            throw new ArgumentException(
                "El archivo JSON está vacío.");
        }

        using JsonDocument json = JsonDocument.Parse(jsonOriginal);

        JsonElement raiz = json.RootElement;
        JsonElement identificacion = ObtenerSeccion(
            raiz,
            "identificacion");

        JsonElement emisor = ObtenerSeccion(raiz, "emisor");
        JsonElement receptor = ObtenerSeccion(raiz, "receptor");

        string codigoGeneracion = LeerTextoRequerido(
            identificacion,
            "codigoGeneracion");

        string numeroControl = LeerTextoRequerido(
            identificacion,
            "numeroControl");

        string tipoDte = LeerTextoRequerido(
            identificacion,
            "tipoDte");

        int versionSchema = LeerEntero(identificacion, "version");

        DateTime fechaEmision = LeerFecha(
            identificacion,
            "fecEmi");

        string? horaEmision = LeerTexto(identificacion, "horEmi");

        using var contexto = new ContabilidadDbContext();

        Empresa? empresa = contexto.Empresas
            .SingleOrDefault(empresa =>
                empresa.EmpresaId == empresaId);

        if (empresa is null)
        {
            throw new InvalidOperationException(
                "No se encontró la empresa activa.");
        }

        bool existeDocumento = contexto.DocumentosDte.Any(documento =>
            documento.EmpresaId == empresaId &&
            documento.CodigoGeneracion == codigoGeneracion);

        if (existeDocumento)
        {
            throw new InvalidOperationException(
                "Este DTE ya fue importado anteriormente.");
        }

        RolEmpresaDte rolEmpresa = DeterminarRolEmpresa(
            empresa,
            emisor,
            receptor);

        string hashSha256 = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(jsonOriginal)));

        using var transaccion = contexto.Database.BeginTransaction();

        var documentoNuevo = new DocumentoDte
        {
            EmpresaId = empresaId,
            CodigoGeneracion = codigoGeneracion,
            NumeroControl = numeroControl,
            TipoDte = tipoDte,
            VersionSchema = versionSchema,
            FechaEmision = fechaEmision,
            HoraEmision = horaEmision,
            RolEmpresa = rolEmpresa,
            Estado = EstadoDocumentoDte.Importado,
            CreadoEn = DateTime.Now
        };

        contexto.DocumentosDte.Add(documentoNuevo);

        Tercero? terceroEmisor = EsLaEmpresa(empresa, emisor)
            ? null
            : ObtenerOCrearTercero(contexto, empresaId, emisor);

        Tercero? terceroReceptor = EsLaEmpresa(empresa, receptor)
            ? null
            : ObtenerOCrearTercero(contexto, empresaId, receptor);

        contexto.DteParticipantes.Add(new DteParticipante
        {
            DocumentoDte = documentoNuevo,
            Tercero = terceroEmisor,
            Rol = RolParticipanteDte.Emisor,
            NitSnapshot = ObtenerNit(emisor),
            NrcSnapshot = LeerTexto(emisor, "nrc"),
            NombreSnapshot = LeerTextoRequerido(emisor, "nombre")
        });

        contexto.DteParticipantes.Add(new DteParticipante
        {
            DocumentoDte = documentoNuevo,
            Tercero = terceroReceptor,
            Rol = RolParticipanteDte.Receptor,
            NitSnapshot = ObtenerNit(receptor),
            NrcSnapshot = LeerTexto(receptor, "nrc"),
            NombreSnapshot = LeerTextoRequerido(receptor, "nombre")
        });

        JsonElement cuerpoDocumento = ObtenerArreglo(
            raiz,
            "cuerpoDocumento");

        int orden = 1;

        foreach (JsonElement item in cuerpoDocumento.EnumerateArray())
        {
            int numeroItem = LeerEntero(item, "numItem");

            contexto.DetallesDte.Add(new DetalleDTe
            {
                DocumentoDte = documentoNuevo,
                NumeroItem = numeroItem == 0 ? orden : numeroItem,
                TipoItem = LeerEnteroOpcional(item, "tipoItem"),
                Cantidad = LeerDecimal(item, "cantidad"),
                Descripcion = LeerTextoRequerido(item, "descripcion"),
                PrecioUnitario = LeerDecimal(item, "precioUni"),
                Descuento = LeerDecimal(item, "montoDescu"),
                VentaGravada = LeerDecimal(item, "ventaGravada"),
                VentaExenta = LeerDecimal(item, "ventaExenta"),
                VentaNoSujeta = LeerDecimal(item, "ventaNoSuj")
            });

            orden++;
        }

        contexto.ArchivosDte.Add(new ArchivoDte
        {
            DocumentoDte = documentoNuevo,
            JsonOriginal = jsonOriginal,
            HashSha256 = hashSha256,
            Fuente = FuenteArchivoDte.CargaManual,
            FechaCarga = DateTime.Now
        });

        contexto.SaveChanges();
        transaccion.Commit();

        return documentoNuevo;
    }

    private static RolEmpresaDte DeterminarRolEmpresa(
        Empresa empresa,
        JsonElement emisor,
        JsonElement receptor)
    {
        if (EsLaEmpresa(empresa, emisor))
        {
            return RolEmpresaDte.Emisor;
        }

        if (EsLaEmpresa(empresa, receptor))
        {
            return RolEmpresaDte.Receptor;
        }

        return RolEmpresaDte.NoDeterminado;
    }

    private static bool EsLaEmpresa(
        Empresa empresa,
        JsonElement participante)
    {
        string nitEmpresa = NormalizarIdentificacion(empresa.Nit);
        string nrcEmpresa = NormalizarIdentificacion(empresa.NRC);

        string nitParticipante = NormalizarIdentificacion(
            ObtenerNit(participante));

        string nrcParticipante = NormalizarIdentificacion(
            LeerTexto(participante, "nrc"));

        return (!string.IsNullOrWhiteSpace(nitEmpresa) &&
                nitEmpresa == nitParticipante)
            || (!string.IsNullOrWhiteSpace(nrcEmpresa) &&
                nrcEmpresa == nrcParticipante);
    }

    private static Tercero ObtenerOCrearTercero(
        ContabilidadDbContext contexto,
        int empresaId,
        JsonElement participante)
    {
        string? nit = ObtenerNit(participante);
        string? nrc = LeerTexto(participante, "nrc");

        Tercero? tercero = null;

        if (!string.IsNullOrWhiteSpace(nit))
        {
            tercero = contexto.Terceros.FirstOrDefault(item =>
                item.EmpresaId == empresaId &&
                item.Nit == nit);
        }

        if (tercero is null && !string.IsNullOrWhiteSpace(nrc))
        {
            tercero = contexto.Terceros.FirstOrDefault(item =>
                item.EmpresaId == empresaId &&
                item.Nrc == nrc);
        }

        if (tercero is not null)
        {
            return tercero;
        }

        var terceroNuevo = new Tercero
        {
            EmpresaId = empresaId,
            Nit = nit,
            Nrc = nrc,
            Nombre = LeerTextoRequerido(participante, "nombre"),
            CodigoActividad = LeerTexto(participante, "codActividad"),
            Correo = LeerTexto(participante, "correo"),
            Telefono = LeerTexto(participante, "telefono"),
            Direccion = LeerDireccion(participante),
            Activo = true
        };

        contexto.Terceros.Add(terceroNuevo);

        return terceroNuevo;
    }

    private static JsonElement ObtenerSeccion(
        JsonElement raiz,
        string nombre)
    {
        if (!raiz.TryGetProperty(nombre, out JsonElement seccion) ||
            seccion.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                $"El JSON no contiene la sección obligatoria '{nombre}'.");
        }

        return seccion;
    }

    private static JsonElement ObtenerArreglo(
        JsonElement raiz,
        string nombre)
    {
        if (!raiz.TryGetProperty(nombre, out JsonElement arreglo) ||
            arreglo.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException(
                $"El JSON no contiene el arreglo obligatorio '{nombre}'.");
        }

        return arreglo;
    }

    private static string? LeerTexto(
        JsonElement objeto,
        string nombre)
    {
        if (!objeto.TryGetProperty(nombre, out JsonElement valor) ||
            valor.ValueKind == JsonValueKind.Null ||
            valor.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return valor.ValueKind == JsonValueKind.String
            ? valor.GetString()?.Trim()
            : valor.ToString().Trim();
    }

    private static string LeerTextoRequerido(
        JsonElement objeto,
        string nombre)
    {
        string? valor = LeerTexto(objeto, nombre);

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                $"El campo '{nombre}' es obligatorio en el JSON.");
        }

        return valor;
    }

    private static int LeerEntero(
        JsonElement objeto,
        string nombre)
    {
        string? texto = LeerTexto(objeto, nombre);

        return int.TryParse(texto, out int valor)
            ? valor
            : 0;
    }

    private static int? LeerEnteroOpcional(
        JsonElement objeto,
        string nombre)
    {
        string? texto = LeerTexto(objeto, nombre);

        return int.TryParse(texto, out int valor)
            ? valor
            : null;
    }

    private static decimal LeerDecimal(
        JsonElement objeto,
        string nombre)
    {
        string? texto = LeerTexto(objeto, nombre);

        if (decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valor))
        {
            return valor;
        }

        if (decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out valor))
        {
            return valor;
        }

        return 0;
    }

    private static DateTime LeerFecha(
        JsonElement objeto,
        string nombre)
    {
        string texto = LeerTextoRequerido(objeto, nombre);

        if (!DateTime.TryParse(
                texto,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fecha))
        {
            throw new ArgumentException(
                $"El campo '{nombre}' no contiene una fecha válida.");
        }

        return fecha.Date;
    }

    private static string? ObtenerNit(JsonElement participante)
    {
        return LeerTexto(participante, "nit")
            ?? LeerTexto(participante, "numDocumento");
    }

    private static string NormalizarIdentificacion(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return new string(
            valor.Where(char.IsLetterOrDigit).ToArray())
            .ToUpperInvariant();
    }

    private static string? LeerDireccion(JsonElement participante)
    {
        if (!participante.TryGetProperty(
                "direccion",
                out JsonElement direccion))
        {
            return null;
        }

        if (direccion.ValueKind == JsonValueKind.String)
        {
            return direccion.GetString()?.Trim();
        }

        if (direccion.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var partes = new List<string?>
        {
            LeerTexto(direccion, "departamento"),
            LeerTexto(direccion, "municipio"),
            LeerTexto(direccion, "complemento")
        };

        string resultado = string.Join(
            ", ",
            partes.Where(parte =>
                !string.IsNullOrWhiteSpace(parte)));

        return string.IsNullOrWhiteSpace(resultado)
            ? null
            : resultado;
    }
}