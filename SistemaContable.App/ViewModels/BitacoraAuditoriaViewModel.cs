using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

// modulo para gestion de contabilidad  auditoria 08
namespace SistemaContableDTE.ViewModels;

public class BitacoraAuditoriaViewModel : ViewModelBase
{
    private readonly RelayCommand cargarCommand;
    private readonly RelayCommand limpiarFiltrosCommand;

    private string entidadFiltro = string.Empty;
    private string accionFiltro = string.Empty;
    private DateTime? fechaDesde;
    private DateTime? fechaHasta;
    private string mensaje = string.Empty;

    public ObservableCollection<BitacoraAuditoria> Registros { get; } = new();

    public string EntidadFiltro
    {
        get => entidadFiltro;
        set => EstablecerPropiedad(ref entidadFiltro, value);
    }

    public string AccionFiltro
    {
        get => accionFiltro;
        set => EstablecerPropiedad(ref accionFiltro, value);
    }

    public DateTime? FechaDesde
    {
        get => fechaDesde;
        set => EstablecerPropiedad(ref fechaDesde, value);
    }

    public DateTime? FechaHasta
    {
        get => fechaHasta;
        set => EstablecerPropiedad(ref fechaHasta, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand CargarCommand => cargarCommand;
    public ICommand LimpiarFiltrosCommand => limpiarFiltrosCommand;

    public BitacoraAuditoriaViewModel()
    {
        fechaDesde = DateTime.Today.AddDays(-30);
        fechaHasta = DateTime.Today;

        cargarCommand = new RelayCommand(_ => CargarRegistros());
        limpiarFiltrosCommand = new RelayCommand(_ => LimpiarFiltros());

        CargarRegistros();
    }

    private void CargarRegistros()
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            List<BitacoraAuditoria> registros =
                ServicioBitacoraAuditoria.ObtenerPorEmpresa(
                    SesionActual.EmpresaId.Value,
                    EntidadFiltro,
                    AccionFiltro,
                    FechaDesde,
                    FechaHasta);

            Registros.Clear();

            foreach (BitacoraAuditoria registro in registros)
            {
                Registros.Add(registro);
            }

            Mensaje = $"{Registros.Count} registro(s) encontrado(s).";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void LimpiarFiltros()
    {
        EntidadFiltro = string.Empty;
        AccionFiltro = string.Empty;
        FechaDesde = DateTime.Today.AddDays(-30);
        FechaHasta = DateTime.Today;

        CargarRegistros();
    }
}