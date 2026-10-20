using System;
using System.Data;
using  System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;
using Microsoft.EntityFrameworkCore; 

namespace SistemaContableDTE.Services;

//clase que servira de recepcion temporal de los datos del formulario
public class DatosConfiguracionInicial
{
    
    public string NombreRazonSocial {get; set;}= string.Empty;

    public string? NombreComercial {get; set;}
    public string Nit {get; set;}=string.Empty;
    public string? Ncr{get;set;}
    public string? CodigoActividad {get; set;}
    public string? DescripcionActividad {get;set;}
    public string? Departamento {get;set;}
    public string? Municipio { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoEmpresa { get; set; }

    //datos relacionados con el administrador
    public string NombreAdministrador { get; set; } = string.Empty;
    public string CorreoAdministrador { get; set; } = string.Empty;
    public string ContrasenaAdministrador { get; set; } = string.Empty;

    //periodo contable
    public string NombrePeriodo { get; set; } = string.Empty;
    public DateTime FechaInicioPeriodo { get; set; }
    public DateTime FechaFinPeriodo { get; set; }

    public static    class ServiciosConfiguracionInicial
    {
        public static void Crear(DatosConfiguracionInicial datos)
        {
            if(string.IsNullOrWhiteSpace(datos.NombreRazonSocial)||string.IsNullOrWhiteSpace(datos.Nit)||string.IsNullOrWhiteSpace(datos.NombreAdministrador)||string.IsNullOrWhiteSpace(datos.ContrasenaAdministrador)||string.IsNullOrWhiteSpace(datos.ContrasenaAdministrador))
            {
                throw new ArgumentException("Complete los datos obligatorios de la empresa y Administrador");
            }

            if (datos.FechaFinPeriodo < datos.FechaInicioPeriodo)
            {
                throw  new ArgumentException("La fecha final del periodo no puede ser menor que la inicial");

            }
            using var contexto = new ContabilidadDbContext();

            if (contexto.Empresas.Any())
            {
                throw new InvalidOperationException("la configuración inicial ya fue realizada");
    
            }

            Rol? rolAdministrador = contexto.Roles.SingleOrDefault(rol=> rol.Nombre == "Administrador");
            if(rolAdministrador is null)
            {
                throw new InvalidOperationException("No existe un rol Administrador");
            }

            //captura de los campos ingresados
            using var transaccion = contexto.Database.BeginTransaction();
            var empresa = new Empresa
            {
                NombreRazonSocial=datos.NombreRazonSocial,
                NombreComercial = datos.NombreComercial,
                Nit = datos.Nit,
                NRC = datos.Ncr,
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

            //obtencio de los datos de un usuario con rol admi

            var administrador = new Usuario
            {
                Nombre = datos.NombreAdministrador,
                Correo = datos.ContrasenaAdministrador,
                HashContrasena = ServicioContrasena.CrearHash(datos.ContrasenaAdministrador),
                Activo = true,
                CreadoEn = DateTime.Now
            };

            contexto.Usuarios.Add(administrador);
            contexto.SaveChanges();

            //Asignacion de un nuevo usuario con rol admi
             var usuarioRol = new UsuarioRol
             {
                 UsuarioId = administrador.UsuarioId,
                 RolId = rolAdministrador.RolId,
                 EmpresaId = empresa.EmpresaId
             };

             contexto.UsuarioRoles.Add(usuarioRol);

             //inicializacion de un periodo contable
             var periodo = new periodoContable
             {
                 EmpresaId = empresa.EmpresaId,
                 Nombre =datos.NombrePeriodo,
                 FechaInicio = datos.FechaInicioPeriodo,
                 FechaFin = datos.FechaFinPeriodo,
                 EstaCerrado = false
             };

             contexto.PeriodosContables.Add(periodo);
             contexto.SaveChanges();
             transaccion.Commit();

        }
    }


}