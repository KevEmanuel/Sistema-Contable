using System;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;

//Clase para el inicio de sesion o login
public class LoginViewModel : ViewModelBase
{
    private readonly RelayCommand iniciarSesionCommand;
    private string correo = string.Empty;
    private string mensaje = string.Empty;
    private Usuario? usuarioAutenticado;

    public string Correo
    {
        get => correo;
        set => EstablecerPropiedad(ref correo, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public Usuario? UsuarioAutenticado
    {
        get => usuarioAutenticado;
        private set => EstablecerPropiedad(ref usuarioAutenticado, value);
    }

    public ICommand IniciarSesionCommand => iniciarSesionCommand;

    //Inicio de sesion exitoso
    public event Action? InicioSesionExitoso;

    public LoginViewModel()
    {
        iniciarSesionCommand = new RelayCommand(IniciarSesion);
    }

    private void IniciarSesion(object? parametro)
    {
        string contrasena = parametro?.ToString() ?? string.Empty;

        Usuario? usuario = ServicioAutenticacion.Autenticar(
            Correo,
            contrasena);

        if (usuario is null)
        {
            Mensaje = "Correo o contraseña incorrectos.";
            return;
        }

        UsuarioAutenticado = usuario;
        //Modificacion de la sesion actual
        SesionActual.Iniciar(usuario);
        //mensaje de confirmacion con el usuario y nombre del sesion actual
        Mensaje = $"Bienvenido, {usuario.Nombre}.";
        //llamada del evento ,de sesion 
        InicioSesionExitoso?.Invoke();
    }
}