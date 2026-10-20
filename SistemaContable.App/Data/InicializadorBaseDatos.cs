using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Data;
public static class InicializadorBaseDatos
{
    
    public static void Inicializar()
    {
        //generación de un nuevo elemento de la clase ContabilidadDbContex
        using var contexto = new ContabilidadDbContext();

        //Aplica las migraciones pendientes al  iniciar el sistema
        contexto.Database.Migrate();
        //Evita generar los repetidamente, cada ves que se genera la aplicación 
        if(contexto.Roles.Any())
        return;

        contexto.Roles.AddRange(
        new Rol
        {
            Nombre = "Admministrador",
            Descripcion="Encargado de Gestionar, Informacion, configuraciones etc"      
        },
        new Rol
        {
            Nombre = "Contador",
            Descripcion = "Encardado de subir los DTE, para generar los libros contables"
        },
        new Rol
        {
            Nombre = "Auditor",
            Descripcion= "Consulta los libros contabilidad, Reportes Bitacora de auditorias"
        }
        );
        contexto.SaveChanges();
        
    }
    

}

