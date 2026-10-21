using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;
//clase para entrelazar el catalo de cuenta  con el models CuentaContable.cs 
//serviara para mostrar el catalogo y registro de cuentas
public static class ServicioCatalogoCuentas
{
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

}