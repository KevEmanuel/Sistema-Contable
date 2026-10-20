using System.Configuration;
using System.Data;
using System.Windows;
//refencia  alas namespace del la cada clase
using SistemaContableDTE.Data;


namespace SistemaContable.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    //Inicializacon de la base de datos, cada ves que se inicia la aplicación

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        InicializadorBaseDatos.Inicializar();
    }
}

