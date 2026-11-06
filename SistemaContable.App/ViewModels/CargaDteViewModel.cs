using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using SistemaContableDTE.Models;
using SistemaContableDTE.Services;

namespace SistemaContableDTE.ViewModels;
// carga del archivo DTES 04
public class CargaDteViewModel : ViewModelBase
{
    private readonly RelayCommand cargarCommand;
    private readonly RelayCommand eliminarCommand;
    private readonly RelayCommand agregarClienteCommand;
    private readonly RelayCommand asignarClienteCommand;
    private readonly RelayCommand registrarEmisorComoClienteCommand;
    private readonly RelayCommand registrarReceptorComoClienteCommand;

    private DocumentoDte? documentoSeleccionado;
    private ClienteContable? clienteSeleccionado;
    private string archivoSeleccionado = string.Empty;
    private string nombreCliente = string.Empty;
    private string nitCliente = string.Empty;
    private string? nrcCliente;
    private string mensaje = string.Empty;

    public ObservableCollection<DocumentoDte> Documentos { get; } = new();
    public ObservableCollection<ClienteContable> Clientes { get; } = new();

    public DocumentoDte? DocumentoSeleccionado
    {
        get => documentoSeleccionado;
        set => EstablecerPropiedad(ref documentoSeleccionado, value);
    }

    public ClienteContable? ClienteSeleccionado
    {
        get => clienteSeleccionado;
        set => EstablecerPropiedad(ref clienteSeleccionado, value);
    }

    public string NombreCliente
    {
        get => nombreCliente;
        set => EstablecerPropiedad(ref nombreCliente, value);
    }

    public string NitCliente
    {
        get => nitCliente;
        set => EstablecerPropiedad(ref nitCliente, value);
    }

    public string? NrcCliente
    {
        get => nrcCliente;
        set => EstablecerPropiedad(ref nrcCliente, value);
    }

    public string ArchivoSeleccionado
    {
        get => archivoSeleccionado;
        private set => EstablecerPropiedad(ref archivoSeleccionado, value);
    }

    public string Mensaje
    {
        get => mensaje;
        private set => EstablecerPropiedad(ref mensaje, value);
    }

    public ICommand CargarCommand => cargarCommand;
    public ICommand EliminarCommand => eliminarCommand;
    public ICommand AgregarClienteCommand => agregarClienteCommand;
    public ICommand AsignarClienteCommand => asignarClienteCommand;
    public ICommand RegistrarEmisorComoClienteCommand =>
        registrarEmisorComoClienteCommand;
    public ICommand RegistrarReceptorComoClienteCommand =>
        registrarReceptorComoClienteCommand;

    public CargaDteViewModel()
    {
        cargarCommand = new RelayCommand(_ => CargarDocumentos());
        eliminarCommand = new RelayCommand(Eliminar);
        agregarClienteCommand = new RelayCommand(_ => AgregarCliente());
        asignarClienteCommand = new RelayCommand(_ => AsignarCliente());
        registrarEmisorComoClienteCommand = new RelayCommand(
            _ => RegistrarParticipanteComoCliente(
                RolParticipanteDte.Emisor));
        registrarReceptorComoClienteCommand = new RelayCommand(
            _ => RegistrarParticipanteComoCliente(
                RolParticipanteDte.Receptor));

        CargarDocumentos();
    }

    public void ImportarArchivo(string rutaArchivo)
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        try
        {
            //modifiaciones de la inserscion del json
            //DocumentoDte documento =
            // ServicioCargaDte.ImportarArchivo(
            //   SesionActual.EmpresaId.Value,
            // rutaArchivo);
            DocumentoDte documento= ServicioCargaDte.ImportarArchivo(
                SesionActual.EmpresaId.Value,
                rutaArchivo,
                SesionActual.Usuario?.UsuarioId,
                ClienteSeleccionado?.ClienteContableId);

            ArchivoSeleccionado = Path.GetFileName(rutaArchivo);

            CargarDocumentos();

            DocumentoSeleccionado = documento;

            Mensaje = documento.ClienteContableId.HasValue
                ? $"DTE importado y clasificado como {documento.OperacionContableDescripcion} para {documento.ClienteContable?.Nombre ?? ClienteSeleccionado?.Nombre ?? "el cliente detectado"}."
                : "DTE importado. Registra o selecciona al cliente y asígnalo para clasificarlo.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void CargarDocumentos()
    {
        if (SesionActual.EmpresaId is null)
        {
            Mensaje = "No hay una empresa activa en la sesión.";
            return;
        }

        Documentos.Clear();
        Clientes.Clear();

        try
        {
            foreach (ClienteContable cliente in
                     ServicioClientesContables.ObtenerPorEmpresa(
                         SesionActual.EmpresaId.Value))
            {
                Clientes.Add(cliente);
            }

            foreach (DocumentoDte documento in
                     ServicioCargaDte.ObtenerPorEmpresa(
                         SesionActual.EmpresaId.Value))
            {
                Documentos.Add(documento);
            }

            if (ClienteSeleccionado is not null)
            {
                ClienteSeleccionado = Clientes.FirstOrDefault(cliente =>
                    cliente.ClienteContableId ==
                    ClienteSeleccionado.ClienteContableId);
            }
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void AgregarCliente()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null)
        {
            Mensaje = "No hay una empresa o un usuario activo en la sesión.";
            return;
        }

        try
        {
            ClienteContable cliente = ServicioClientesContables.Crear(
                empresaId,
                SesionActual.Usuario.UsuarioId,
                NombreCliente,
                NitCliente,
                NrcCliente);
            CargarDocumentos();
            ClienteSeleccionado = Clientes.First(item =>
                item.ClienteContableId == cliente.ClienteContableId);
            NombreCliente = string.Empty;
            NitCliente = string.Empty;
            NrcCliente = string.Empty;
            Mensaje =
                "Cliente registrado. Sus próximos DTE se clasificarán automáticamente si el NIT/NRC coincide.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void AsignarCliente()
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null ||
            DocumentoSeleccionado is null ||
            ClienteSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE y el cliente al que pertenece.";
            return;
        }

        try
        {
            int documentoId = DocumentoSeleccionado.DocumentoDteId;
            ServicioClientesContables.AsignarCliente(
                empresaId,
                documentoId,
                ClienteSeleccionado.ClienteContableId,
                SesionActual.Usuario.UsuarioId);

            CargarDocumentos();
            DocumentoSeleccionado = Documentos.First(item =>
                item.DocumentoDteId == documentoId);
            Mensaje =
                $"Cliente asignado. El DTE fue identificado como {DocumentoSeleccionado.OperacionContableDescripcion}.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void RegistrarParticipanteComoCliente(
        RolParticipanteDte rolCliente)
    {
        if (SesionActual.EmpresaId is not int empresaId ||
            SesionActual.Usuario is null ||
            DocumentoSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE para registrar su cliente.";
            return;
        }

        try
        {
            int documentoId = DocumentoSeleccionado.DocumentoDteId;
            var asignacion =
                ServicioClientesContables.CrearOAsignarDesdeDte(
                    empresaId,
                    documentoId,
                    rolCliente,
                    SesionActual.Usuario.UsuarioId);
            ClienteContable cliente = asignacion.Cliente;

            CargarDocumentos();
            ClienteSeleccionado = Clientes.First(item =>
                item.ClienteContableId == cliente.ClienteContableId);
            DocumentoSeleccionado = Documentos.First(item =>
                item.DocumentoDteId == documentoId);
            Mensaje =
                $"Cliente {cliente.Nombre} asociado. Este DTE quedó clasificado como {DocumentoSeleccionado.OperacionContableDescripcion}; los siguientes se detectarán por NIT/NRC.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }

    private void Eliminar(object? parametro)
    {
        if (SesionActual.EmpresaId is null ||
            DocumentoSeleccionado is null)
        {
            Mensaje = "Selecciona un DTE para eliminar.";
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            "Se eliminará el DTE, el archivo original y sus resultados de validación. Solo se permite cuando ya no tenga evidencias ni eventos y no esté ligado a libros fiscales o partidas. ¿Deseas continuar?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmacion != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            ServicioCargaDte.Eliminar(
                SesionActual.EmpresaId.Value,
                DocumentoSeleccionado.DocumentoDteId,
                SesionActual.Usuario?.UsuarioId);
            DocumentoSeleccionado = null;
            CargarDocumentos();
            Mensaje = "DTE eliminado. Puedes importar el archivo corregido.";
        }
        catch (Exception error)
        {
            Mensaje = error.Message;
        }
    }
}