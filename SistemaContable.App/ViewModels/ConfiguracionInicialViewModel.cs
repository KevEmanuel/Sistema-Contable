using System;
using System.Windows.Input;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;

public class ConfiguracionInicialViewModel : ViewModelBase
{
    private readonly RelayCommand guardarCommand;
    private string mensaje = string.Empty;
    private bool configuracionRealizada;

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

    public string NombrePeriodo { get; set; }
    public DateTime FechaInicioPeriodo { get; set; }
    public DateTime FechaFinPeriodo { get; set; }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public bool ConfiguracionRealizada
    {
        get => configuracionRealizada;
        private set
        {
            if (EstablecerPropiedad(
                ref configuracionRealizada,
                value))
            {
                guardarCommand.ActualizarEstado();
            }
        }
    }

    public ICommand GuardarCommand => guardarCommand;

    public ConfiguracionInicialViewModel()
    {
        int anioActual = DateTime.Today.Year;

        NombrePeriodo = $"Ejercicio {anioActual}";
        FechaInicioPeriodo = new DateTime(anioActual, 1, 1);
        FechaFinPeriodo = new DateTime(anioActual, 12, 31);
        guardarCommand = new RelayCommand(
            Guardar,
            _ => !ConfiguracionRealizada);

        if (ServicioConfiguracionInicial.EstaConfigurada())
        {
            ConfiguracionRealizada = true;
            Mensaje = "La configuración inicial ya fue realizada.";
        }
    }

    private void Guardar(object? parametro)
    {
        string contrasena = parametro?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(NombrePeriodo))
        {
            Mensaje = "Ingresa el nombre del período contable.";
            return;
        }

        if (string.IsNullOrWhiteSpace(contrasena))
        {
            Mensaje = "Ingresa la contraseña del administrador.";
            return;
        }

        try
        {
            var datos = new DatosConfiguracionInicial
            {
                NombreRazonSocial = NombreRazonSocial,
                NombreComercial = NombreComercial,
                Nit = Nit,
                Nrc = Nrc,
                CodigoActividad = CodigoActividad,
                DescripcionActividad = DescripcionActividad,
                Departamento = Departamento,
                Municipio = Municipio,
                Direccion = Direccion,
                Telefono = Telefono,
                CorreoEmpresa = CorreoEmpresa,

                NombreAdministrador = NombreAdministrador,
                CorreoAdministrador = CorreoAdministrador,
                ContrasenaAdministrador = contrasena,

                NombrePeriodo = NombrePeriodo,
                FechaInicioPeriodo = FechaInicioPeriodo,
                FechaFinPeriodo = FechaFinPeriodo
            };

            ServicioConfiguracionInicial.Crear(datos);

            ConfiguracionRealizada = true;
            Mensaje = "Configuración guardada correctamente.";
        }
        catch (Exception error)
        {

            Mensaje = error.Message;
            System.Windows.MessageBox.Show("Error al  guardar: " + error.Message);
        }
    }
}