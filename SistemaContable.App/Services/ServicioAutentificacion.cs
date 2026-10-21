using System;
using System.Linq;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;
//clase que valida la autentificacion 
public static class ServicioAutenticacion
{
    public static Usuario? Autenticar(string correo, string contrasena)
    {
        if (string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(contrasena))
        {
            return null;
        }

        using var contexto = new ContabilidadDbContext();

        Usuario? usuario = contexto.Usuarios
            .SingleOrDefault(usuario =>
                usuario.Correo == correo.Trim() &&
                usuario.Activo);

        if (usuario is null)
        {
            return null;
        }

        return ServicioContrasena.Verificar(
            contrasena,
            usuario.HashContrasena)
            ? usuario
            : null;
    }
}