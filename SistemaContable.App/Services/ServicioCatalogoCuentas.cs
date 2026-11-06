using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;
//clase para entrelazar el catalo de cuenta  con el models CuentaContable.cs 
//serviara para mostrar el catalogo y registro de cuentas
public static class ServicioCatalogoCuentas
{
    public static int CargarCatalogoBaseSiEstaVacio(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        if (contexto.CuentasContables.Any(cuenta =>
                cuenta.EmpresaId == empresaId))
        {
            transaccion.Commit();
            return 0;
        }

        string rutaCatalogo = Path.Combine(
            AppContext.BaseDirectory,
            "catalogo_cuentas_comercial.json");
        string json = File.ReadAllText(rutaCatalogo);

        CatalogoCuentaJson catalogo =
            JsonSerializer.Deserialize<CatalogoCuentaJson>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
            ?? throw new InvalidDataException(
                "El archivo del catálogo de cuentas está vacío.");

        ValidarCatalogo(catalogo);

        List<CuentaContable> cuentas = catalogo.Cuentas
            .Select(cuenta => new CuentaContable
            {
                EmpresaId = empresaId,
                Codigo = cuenta.Codigo,
                Nombre = cuenta.Nombre,
                TipoCuenta = DeterminarTipoCuenta(cuenta),
                Nivel = cuenta.Nivel,
                PermiteMovimientos = !cuenta.TieneSubcuentas,
                Activa = true,
                FechaCreacion = DateTime.Now
            })
            .ToList();

        contexto.CuentasContables.AddRange(cuentas);
        contexto.SaveChanges();

        Dictionary<string, CuentaContable> cuentasPorCodigo = cuentas
            .ToDictionary(cuenta => cuenta.Codigo);

        foreach (int indice in Enumerable.Range(0, catalogo.Cuentas.Count))
        {
            string? codigoPadre = catalogo.Cuentas[indice].CodigoPadre;
            if (codigoPadre is not null)
            {
                cuentas[indice].CuentaPadreId =
                    cuentasPorCodigo[codigoPadre].CuentaContableId;
            }
        }

        contexto.SaveChanges();
        transaccion.Commit();

        return cuentas.Count;
    }

    public static List<CuentaContable> ObtenerPorEmpresa(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.CuentasContables
            .AsNoTracking()
            .Where(cuenta => cuenta.EmpresaId == empresaId)
            .OrderBy(cuenta => cuenta.Codigo)
            .ToList();
    }

    public static CuentaContable Crear(
        int empresaId,
        string codigo,
        string nombre,
        string tipoCuenta,
        int? cuentaPadreId,
        int nivel,
        bool permiteMovimientos)
    {
        codigo = codigo.Trim();
        nombre = nombre.Trim();
        tipoCuenta = tipoCuenta.Trim();

        if (string.IsNullOrWhiteSpace(codigo) ||
            string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(tipoCuenta))
        {
            throw new ArgumentException(
                "Código, nombre y tipo de cuenta son obligatorios.");
        }

        if (nivel < 1)
        {
            throw new ArgumentException(
                "El nivel de la cuenta debe ser mayor que cero.");
        }

        using var contexto = new ContabilidadDbContext();

        bool codigoExiste = contexto.CuentasContables.Any(cuenta =>
            cuenta.EmpresaId == empresaId &&
            cuenta.Codigo == codigo);

        if (codigoExiste)
        {
            throw new InvalidOperationException(
                "Ya existe una cuenta con ese código.");
        }

        if (cuentaPadreId is not null)
        {
            bool padreExiste = contexto.CuentasContables.Any(cuenta =>
                cuenta.CuentaContableId == cuentaPadreId &&
                cuenta.EmpresaId == empresaId);

            if (!padreExiste)
            {
                throw new InvalidOperationException(
                    "La cuenta padre seleccionada no pertenece a la empresa.");
            }
        }

        var cuentaNueva = new CuentaContable
        {
            EmpresaId = empresaId,
            Codigo = codigo,
            Nombre = nombre,
            TipoCuenta = tipoCuenta,
            CuentaPadreId = cuentaPadreId,
            Nivel = nivel,
            PermiteMovimientos = permiteMovimientos,
            Activa = true,
            FechaCreacion = DateTime.Now
        };

        contexto.CuentasContables.Add(cuentaNueva);
        contexto.SaveChanges();

        return cuentaNueva;
    }
    public static void ActualizarDatos(
    int empresaId,
    int cuentaContableId,
    string codigo,
    string nombre,
    string tipoCuenta,
    bool permiteMovimientos)
    {
        codigo = codigo.Trim();
        nombre = nombre.Trim();
        tipoCuenta = tipoCuenta.Trim();

        if (string.IsNullOrWhiteSpace(codigo) ||
            string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(tipoCuenta))
        {
            throw new ArgumentException(
                "Código, nombre y tipo de cuenta son obligatorios.");
        }

        using var contexto = new ContabilidadDbContext();

        CuentaContable? cuenta = contexto.CuentasContables
            .SingleOrDefault(cuenta =>
                cuenta.CuentaContableId == cuentaContableId &&
                cuenta.EmpresaId == empresaId);

        if (cuenta is null)
        {
            throw new InvalidOperationException(
                "La cuenta seleccionada no existe.");
        }

        bool codigoExiste = contexto.CuentasContables.Any(otraCuenta =>
            otraCuenta.EmpresaId == empresaId &&
            otraCuenta.Codigo == codigo &&
            otraCuenta.CuentaContableId != cuentaContableId);

        if (codigoExiste)
        {
            throw new InvalidOperationException(
                "Ya existe otra cuenta con ese código.");
        }

        cuenta.Codigo = codigo;
        cuenta.Nombre = nombre;
        cuenta.TipoCuenta = tipoCuenta;
        cuenta.PermiteMovimientos = permiteMovimientos;

        contexto.SaveChanges();
    }

    public static void CambiarEstado(
        int empresaId,
        int cuentaContableId)
    {
        using var contexto = new ContabilidadDbContext();

        CuentaContable? cuenta = contexto.CuentasContables
            .SingleOrDefault(cuenta =>
                cuenta.CuentaContableId == cuentaContableId &&
                cuenta.EmpresaId == empresaId);

        if (cuenta is null)
        {
            throw new InvalidOperationException(
                "La cuenta seleccionada no existe.");
        }

        cuenta.Activa = !cuenta.Activa;

        contexto.SaveChanges();
    }

    public static void Eliminar(int empresaId, int cuentaContableId)
    {
        using var contexto = new ContabilidadDbContext();

        CuentaContable? cuenta = contexto.CuentasContables
            .SingleOrDefault(cuenta =>
                cuenta.CuentaContableId == cuentaContableId &&
                cuenta.EmpresaId == empresaId);

        if (cuenta is null)
        {
            throw new InvalidOperationException(
                "La cuenta seleccionada no existe.");
        }

        if (contexto.CuentasContables.Any(otra =>
                otra.CuentaPadreId == cuentaContableId) ||
            contexto.ReglasCuentas.Any(linea =>
                linea.CuentaContableId == cuentaContableId) ||
            contexto.DetallesPartida.Any(detalle =>
                detalle.CuentaContableId == cuentaContableId))
        {
            throw new InvalidOperationException(
                "No se puede eliminar esta cuenta porque tiene subcuentas o está utilizada en reglas o partidas. Puedes desactivarla.");
        }

        contexto.CuentasContables.Remove(cuenta);
        contexto.SaveChanges();
    }

    private static void ValidarCatalogo(CatalogoCuentaJson catalogo)
    {
        if (catalogo.VersionEsquema != 1 ||
            catalogo.Cuentas.Count == 0 ||
            catalogo.CantidadCuentas != catalogo.Cuentas.Count)
        {
            throw new InvalidDataException(
                "El archivo del catálogo tiene una versión o cantidad de cuentas no válida.");
        }

        var cuentasPorCodigo = new Dictionary<string, CuentaCatalogoJson>(
            StringComparer.Ordinal);

        foreach (CuentaCatalogoJson cuenta in catalogo.Cuentas)
        {
            if (string.IsNullOrWhiteSpace(cuenta.Codigo) ||
                cuenta.Codigo.Length > 30 ||
                string.IsNullOrWhiteSpace(cuenta.Nombre) ||
                cuenta.Nombre.Length > 200 ||
                cuenta.Nivel < 1 ||
                !cuentasPorCodigo.TryAdd(cuenta.Codigo, cuenta))
            {
                throw new InvalidDataException(
                    $"La cuenta '{cuenta.Codigo}' tiene datos incompletos, duplicados o fuera de límite.");
            }
        }

        foreach (CuentaCatalogoJson cuenta in catalogo.Cuentas)
        {
            if (cuenta.CodigoPadre is null)
            {
                if (cuenta.Nivel != 1)
                {
                    throw new InvalidDataException(
                        $"La cuenta raíz '{cuenta.Codigo}' tiene un nivel no válido.");
                }

                continue;
            }

            if (!cuentasPorCodigo.TryGetValue(
                    cuenta.CodigoPadre,
                    out CuentaCatalogoJson? padre) ||
                padre.Nivel != cuenta.Nivel - 1 ||
                padre.CodigoClase != cuenta.CodigoClase)
            {
                throw new InvalidDataException(
                    $"La jerarquía de la cuenta '{cuenta.Codigo}' no es válida.");
            }
        }
    }

    private static string DeterminarTipoCuenta(CuentaCatalogoJson cuenta)
    {
        return cuenta.CodigoClase switch
        {
            "1" => "Activo",
            "2" => "Pasivo",
            "3" => "Patrimonio",
            "4" => cuenta.Codigo.StartsWith(
                    "4101",
                    StringComparison.Ordinal)
                ? "Costo"
                : "Gasto",
            "5" => "Ingreso",
            "6" or "7" => "Patrimonio",
            _ => throw new InvalidDataException(
                $"La cuenta '{cuenta.Codigo}' tiene una clase contable no reconocida.")
        };
    }

    private sealed class CatalogoCuentaJson
    {
        public CatalogoCuentaJson()
        {
        }

        public int VersionEsquema { get; set; }
        public int CantidadCuentas { get; set; }
        public List<CuentaCatalogoJson> Cuentas { get; set; } = new();
    }

    private sealed class CuentaCatalogoJson
    {
        public CuentaCatalogoJson()
        {
        }

        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoPadre { get; set; }
        public string CodigoClase { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public bool TieneSubcuentas { get; set; }
    }
}