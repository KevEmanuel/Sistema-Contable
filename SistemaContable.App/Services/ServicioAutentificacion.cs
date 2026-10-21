using System;
using System.Linq;
using System.Reflection;
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
    public static int? ObtenerEmpresaId(int usuarioId)
    {
        using var contexto = new ContabilidadDbContext();
        return contexto.UsuarioRoles.Where(asignacion => asignacion.UsuarioId == usuarioId).Select(asignacion =>(int?)asignacion.EmpresaId).FirstOrDefault();

    }
}