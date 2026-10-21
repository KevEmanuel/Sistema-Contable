using System.Security.Cryptography.X509Certificates;
using SistemaContableDTE.Models;
 namespace SistemaContableDTE.Services;

//clase que valida la sesion actual de usuario
 public static class SesionActual
{
    public static Usuario? Usuario {get;set;}

    //cambios para asiganar a una empresa activa
    public static int? EmpresaId {get;set;}
    public static bool EstaAutentificado => Usuario is not null && EmpresaId is not null;

    public static void Iniciar(Usuario usuario, int empresaId)
    {
        Usuario = usuario;
        EmpresaId= empresaId;
    }
    
    public static void Cerrar()
    {
        Usuario = null;
        EmpresaId = null;
    }


}