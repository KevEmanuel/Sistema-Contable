using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;

public class PanelPrincipalViewModel : ViewModelBase
{
    public string NombreUsuario=>SesionActual.Usuario?.Nombre?? "Usuario";

    public string CorreoUsuario => SesionActual.Usuario?.Correo?? string.Empty;

    public string MesajeBienvenida => $"Bienvendo, {NombreUsuario}.";
}
