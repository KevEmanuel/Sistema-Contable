using System;
using System.Linq;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

using Microsoft.EntityFrameworkCore;

namespace SistemaContableDTE.Services;

public class DatosConfiguracionInicial
{
    public string NombreRazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string? Nrc { get; set; }
    public string? CodigoActividad { get; set; }
    public string? DescripcionActividad { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoEmpresa { get; set; }

    public string NombreAdministrador { get; set; } = string.Empty;
    public string CorreoAdministrador { get; set; } = string.Empty;
    public string ContrasenaAdministrador { get; set; } = string.Empty;

    public string NombrePeriodo { get; set; } = string.Empty;
    public DateTime FechaInicioPeriodo { get; set; }
    public DateTime FechaFinPeriodo { get; set; }
}

public static class ServicioConfiguracionInicial
{
    public static void Crear(DatosConfiguracionInicial datos)
    {
        if (string.IsNullOrWhiteSpace(datos.NombreRazonSocial) ||
            string.IsNullOrWhiteSpace(datos.Nit) ||
            string.IsNullOrWhiteSpace(datos.NombreAdministrador) ||
            string.IsNullOrWhiteSpace(datos.CorreoAdministrador) ||
            string.IsNullOrWhiteSpace(datos.ContrasenaAdministrador))
        {
            throw new ArgumentException(
                "Completa los datos obligatorios de empresa y administrador.");
        }

        if (datos.FechaFinPeriodo < datos.FechaInicioPeriodo)
        {
            throw new ArgumentException(
                "La fecha final del período no puede ser menor que la inicial.");
        }

        using var contexto = new ContabilidadDbContext();

        if (contexto.Empresas.Any())
        {
            throw new InvalidOperationException(
                "La configuración inicial ya fue realizada.");
        }

        Rol? rolAdministrador = contexto.Roles
            .SingleOrDefault(rol => rol.Nombre == "Administrador");

        if (rolAdministrador is null)
        {
            throw new InvalidOperationException(
                "No existe el rol Administrador.");
        }

        using var transaccion = contexto.Database.BeginTransaction();

        var empresa = new Empresa
        {
            NombreRazonSocial = datos.NombreRazonSocial,
            NombreComercial = datos.NombreComercial,
            Nit = datos.Nit,
            NRC = datos.Nrc,
            CodigoActividad = datos.CodigoActividad,
            DescripcionActividad = datos.DescripcionActividad,
            Departamento = datos.Departamento,
            Municipio = datos.Municipio,
            Direccion = datos.Direccion,
            Telefono = datos.Telefono,
            Correo = datos.CorreoEmpresa,
            Activa = true,
            FechaCreacion = DateTime.Now
        };

        contexto.Empresas.Add(empresa);
        contexto.SaveChanges();

        var administrador = new Usuario
        {
            Nombre = datos.NombreAdministrador,
            Correo = datos.CorreoAdministrador,
            HashContrasena = ServicioContrasena.CrearHash(
                datos.ContrasenaAdministrador),
            Activo = true,
            CreadoEn = DateTime.Now
        };

        contexto.Usuarios.Add(administrador);
        contexto.SaveChanges();

        var usuarioRol = new UsuarioRol
        {
            UsuarioId = administrador.UsuarioId,
            RolId = rolAdministrador.RolId,
            EmpresaId = empresa.EmpresaId
        };

        contexto.UsuarioRoles.Add(usuarioRol);

        //var periodo = new PeriodoContable
        var periodo = new PeriodoContable
        {
            EmpresaId = empresa.EmpresaId,
            Nombre = datos.NombrePeriodo,
            FechaInicio = datos.FechaInicioPeriodo,
            FechaFin = datos.FechaFinPeriodo,
            EstaCerrado = false
        };

        contexto.PeriodosContables.Add(periodo);

        contexto.SaveChanges();
        transaccion.Commit();
    }

    //refencia a la clase configuracionInicialViewModels.css
    //verificacion del metodo
    public static bool EstaConfigurada()
    {
    using var contexto = new ContabilidadDbContext();
    return contexto.Empresas.Any();
    }


}