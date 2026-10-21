using System.Windows;
using System.Windows.Controls;
using SistemaContableDTE.ViewModels;

namespace SistemaContableDTE.Views;

//clase pare enlazar el archivo de vsita LoginView.xaml
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        //modificaciones del inicio de sesion 
        if(DataContext is LoginViewModel viewModel)
        {
            viewModel.InicioSesionExitoso += MostrarPanelPrincipal;
        }
    }

    private void MostrarPanelPrincipal()
    {
        if(Application.Current.MainWindow is not null)
        {
            Application.Current.MainWindow.Content = new PanelPrincipalView();
        }
    }
    /*
    private void IniciarSesion_Click(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel &&
            viewModel.IniciarSesionCommand.CanExecute(Contrasena.Password))
        {
            viewModel.IniciarSesionCommand.Execute(Contrasena.Password);
        }
    }
    */
}