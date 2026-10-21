using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Data;
public static class InicializadorBaseDatos
{
    
    public static void Inicializar()
    {
        using var contexto = new ContabilidadDbContext();

        contexto.Database.Migrate();

        var roles = contexto.Roles.ToList();
        var rolAdministrador = roles.FirstOrDefault(
            rol => rol.Nombre == "Administrador");
        var rolAdministradorConError = roles.FirstOrDefault(
            rol => rol.Nombre == "Admministrador");

        if (rolAdministrador is null && rolAdministradorConError is not null)
        {
            rolAdministradorConError.Nombre = "Administrador";
        }
        else if (rolAdministrador is null)
        {
            contexto.Roles.Add(new Rol
            {
                Nombre = "Administrador",
                Descripcion = "Encargado de gestionar información y configuraciones."
            });
        }

        if (!roles.Any(rol => rol.Nombre == "Contador"))
        {
            contexto.Roles.Add(new Rol
            {
                Nombre = "Contador",
                Descripcion = "Encargado de registrar DTE y generar libros contables."
            });
        }

        if (!roles.Any(rol => rol.Nombre == "Auditor"))
        {
            contexto.Roles.Add(new Rol
            {
                Nombre = "Auditor",
                Descripcion = "Consulta libros contables, reportes y bitácoras de auditoría."
            });
        }

        contexto.SaveChanges();
    }
}
