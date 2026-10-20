using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaContableDTE.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool EstablecerPropiedad<T>(
        ref T campo,
        T valor,
        [CallerMemberName] string? nombrePropiedad = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;

        campo = valor;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(nombrePropiedad));

        return true;
    }
}