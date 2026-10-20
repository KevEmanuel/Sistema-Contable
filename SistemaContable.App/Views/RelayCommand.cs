using System;
using System.Windows.Input;

namespace SistemaContableDTE.ViewModels;
//clase que permite lac comunicación entre ServicioConfiguracionInicial.cs
public class RelayCommand : ICommand
{
    private readonly Action<object?> ejecutar;
    private readonly Func<object?, bool>? puedeEjecutar;

    public RelayCommand(
        Action<object?> ejecutar,
        Func<object?, bool>? puedeEjecutar = null)
    {
        this.ejecutar = ejecutar;
        this.puedeEjecutar = puedeEjecutar;
    }

    public bool CanExecute(object? parametro)
    {
        return puedeEjecutar?.Invoke(parametro) ?? true;
    }

    public void Execute(object? parametro)
    {
        ejecutar(parametro);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void ActualizarEstado()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}