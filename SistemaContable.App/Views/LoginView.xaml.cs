using System.Windows.Controls;
using SistemaContableDTE.ViewModels;

namespace SistemaContableDTE.Views;

//clase pare enlazar el archivo de vsita LoginView.xaml
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

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
}