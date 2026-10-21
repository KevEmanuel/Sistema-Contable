using System.Windows.Controls;
using SistemaContableDTE.ViewModels;

namespace SistemaContableDTE.Views;

public partial class ConfiguracionInicialView : UserControl
{
    public ConfiguracionInicialView()
    {
        InitializeComponent();
    }

    private void Guardar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionInicialViewModel viewModel)
        {
            if (viewModel.GuardarCommand.CanExecute(ContrasenaAdministrador.Password))
            {
                viewModel.GuardarCommand.Execute(ContrasenaAdministrador.Password);
            }
        }
    }
}