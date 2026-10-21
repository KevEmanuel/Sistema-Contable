using System.Security.Cryptography.X509Certificates;
using SistemaContableDTE.Models;
 namespace SistemaContableDTE.Services;

//clase que valida la sesion actual de usuario
 public static class SesionActual
{
    public static Usuario? Usuario {get;set;}
    public static bool EstaAutentificado => Usuario is not null;

    public static void Iniciar(Usuario usuario)
    {
        Usuario = usuario;
    }
    
    public static void Cerrar()
    {
        Usuario = null;
    }


}