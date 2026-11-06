using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

public static class ServicioClientesContables
{
    public static List<ClienteContable> ObtenerPorEmpresa(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.ClientesContables
            .AsNoTracking()
            .Where(cliente => cliente.EmpresaId == empresaId)
            .OrderBy(cliente => cliente.Nombre)
            .ToList();
    }

    public static ClienteContable Crear(
        int empresaId,
        int usuarioId,
        string nombre,
        string nit,
        string? nrc)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(nit) ||
            NormalizarIdentificacion(nit).Length == 0)
        {
            throw new ArgumentException(
                "El nombre y el NIT del cliente son obligatorios.");
        }

        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        string nitNormalizado = NormalizarIdentificacion(nit);
        string nrcNormalizado = NormalizarIdentificacion(nrc);
        bool existe = contexto.ClientesContables
            .AsNoTracking()
            .Where(cliente => cliente.EmpresaId == empresaId)
            .AsEnumerable()
            .Any(cliente =>
                NormalizarIdentificacion(cliente.Nit) == nitNormalizado ||
                (nrcNormalizado.Length > 0 &&
                 NormalizarIdentificacion(cliente.Nrc) == nrcNormalizado));

        if (existe)
        {
            throw new InvalidOperationException(
                "Ya existe un cliente registrado con ese NIT o NRC.");
        }

        var clienteNuevo = new ClienteContable
        {
            EmpresaId = empresaId,
            Nombre = nombre.Trim(),
            Nit = nit.Trim(),
            Nrc = string.IsNullOrWhiteSpace(nrc) ? null : nrc.Trim()
        };

        contexto.ClientesContables.Add(clienteNuevo);
        contexto.SaveChanges();

        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = nameof(ClienteContable),
            IdEntidad = clienteNuevo.ClienteContableId.ToString(),
            Accion = "Registrar cliente contable",
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                clienteNuevo.Nombre,
                clienteNuevo.Nit,
                clienteNuevo.Nrc
            }),
            FechaHora = DateTime.Now
        });
        contexto.SaveChanges();
        transaccion.Commit();

        return clienteNuevo;
    }

    public static DocumentoDte AsignarCliente(
        int empresaId,
        int documentoDteId,
        int clienteContableId,
        int usuarioId)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        DocumentoDte? documento = contexto.DocumentosDte
            .Include(item => item.Participantes)
            .SingleOrDefault(item =>
                item.DocumentoDteId == documentoDteId &&
                item.EmpresaId == empresaId);
        ClienteContable? cliente = contexto.ClientesContables
            .SingleOrDefault(item =>
                item.ClienteContableId == clienteContableId &&
                item.EmpresaId == empresaId);

        if (documento is null || cliente is null)
        {
            throw new InvalidOperationException(
                "No se encontró el DTE o el cliente seleccionado.");
        }

        if (contexto.PartidasContables.Any(partida =>
                partida.DocumentoDteId == documentoDteId) ||
            contexto.RegistrosLibroIva.Any(registro =>
                registro.DocumentoDteId == documentoDteId))
        {
            throw new InvalidOperationException(
                "No se puede cambiar el cliente de un DTE que ya tiene actividad contable o fiscal.");
        }

        bool coincideEmisor = Coincide(
            cliente,
            documento.Participantes.SingleOrDefault(item =>
                item.Rol == RolParticipanteDte.Emisor));
        bool coincideReceptor = Coincide(
            cliente,
            documento.Participantes.SingleOrDefault(item =>
                item.Rol == RolParticipanteDte.Receptor));

        if (coincideEmisor == coincideReceptor)
        {
            throw new InvalidOperationException(
                coincideEmisor
                    ? "El cliente coincide con el emisor y el receptor; verifica los identificadores del cliente."
                    : "El NIT del cliente no coincide con el emisor ni el receptor de este DTE.");
        }

        var valoresAnteriores = new
        {
            documento.ClienteContableId,
            RolEmpresa = documento.RolEmpresa.ToString()
        };

        documento.ClienteContableId = cliente.ClienteContableId;
        documento.RolEmpresa = coincideEmisor
            ? RolEmpresaDte.Emisor
            : RolEmpresaDte.Receptor;

        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = nameof(DocumentoDte),
            IdEntidad = documento.DocumentoDteId.ToString(),
            Accion = "Asignar cliente y clasificar rol del DTE",
            ValoresAnteriores = JsonSerializer.Serialize(valoresAnteriores),
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                Cliente = cliente.Nombre,
                documento.ClienteContableId,
                RolEmpresa = documento.RolEmpresa.ToString()
            }),
            FechaHora = DateTime.Now
        });

        contexto.SaveChanges();
        transaccion.Commit();
        return documento;
    }

    public static (ClienteContable Cliente, DocumentoDte Documento)
        CrearOAsignarDesdeDte(
            int empresaId,
            int documentoDteId,
            RolParticipanteDte rolCliente,
            int usuarioId)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        DocumentoDte? documento = contexto.DocumentosDte
            .Include(item => item.Participantes)
            .SingleOrDefault(item =>
                item.DocumentoDteId == documentoDteId &&
                item.EmpresaId == empresaId);
        if (documento is null)
        {
            throw new InvalidOperationException(
                "No se encontró el DTE seleccionado.");
        }

        if (contexto.PartidasContables.Any(partida =>
                partida.DocumentoDteId == documentoDteId) ||
            contexto.RegistrosLibroIva.Any(registro =>
                registro.DocumentoDteId == documentoDteId))
        {
            throw new InvalidOperationException(
                "No se puede cambiar el cliente de un DTE que ya tiene actividad contable o fiscal.");
        }

        DteParticipante participante = documento.Participantes
            .SingleOrDefault(item => item.Rol == rolCliente)
            ?? throw new InvalidOperationException(
                "El DTE no contiene los datos del emisor o receptor seleccionado.");
        if (string.IsNullOrWhiteSpace(participante.NitSnapshot))
        {
            throw new InvalidOperationException(
                "El participante seleccionado no tiene NIT en el DTE; no se puede registrar un cliente sin identificador.");
        }

        if (string.IsNullOrWhiteSpace(participante.NombreSnapshot))
        {
            throw new InvalidOperationException(
                "El participante seleccionado no tiene nombre en el DTE; no se puede registrar un cliente sin nombre.");
        }

        List<ClienteContable> clientesCoincidentes = contexto.ClientesContables
            .Where(item => item.EmpresaId == empresaId)
            .AsEnumerable()
            .Where(item => Coincide(item, participante))
            .ToList();
        if (clientesCoincidentes.Count > 1)
        {
            throw new InvalidOperationException(
                "Más de un cliente coincide con los identificadores del DTE. Selecciona o corrige el cliente antes de continuar.");
        }

        ClienteContable? cliente = clientesCoincidentes.SingleOrDefault();

        if (cliente is null)
        {
            cliente = new ClienteContable
            {
                EmpresaId = empresaId,
                Nombre = participante.NombreSnapshot,
                Nit = participante.NitSnapshot,
                Nrc = participante.NrcSnapshot
            };
            contexto.ClientesContables.Add(cliente);
            contexto.SaveChanges();

            contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
            {
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                Entidad = nameof(ClienteContable),
                IdEntidad = cliente.ClienteContableId.ToString(),
                Accion = "Registrar cliente desde participante del DTE",
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    cliente.Nombre,
                    cliente.Nit,
                    cliente.Nrc
                }),
                FechaHora = DateTime.Now
            });
        }

        documento.ClienteContableId = cliente.ClienteContableId;
        documento.ClienteContable = cliente;
        documento.RolEmpresa = rolCliente == RolParticipanteDte.Emisor
            ? RolEmpresaDte.Emisor
            : RolEmpresaDte.Receptor;

        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = nameof(DocumentoDte),
            IdEntidad = documento.DocumentoDteId.ToString(),
            Accion = "Asociar cliente desde participante del DTE",
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                Cliente = cliente.Nombre,
                documento.ClienteContableId,
                RolEmpresa = documento.RolEmpresa.ToString()
            }),
            FechaHora = DateTime.Now
        });

        contexto.SaveChanges();
        transaccion.Commit();
        return (cliente, documento);
    }

    internal static string NormalizarIdentificacion(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? string.Empty
            : new string(valor.Where(char.IsLetterOrDigit).ToArray())
                .ToUpperInvariant();

    internal static bool Coincide(
        ClienteContable cliente,
        DteParticipante? participante)
    {
        if (participante is null)
        {
            return false;
        }

        string nitCliente = NormalizarIdentificacion(cliente.Nit);
        string nitDte = NormalizarIdentificacion(participante.NitSnapshot);
        if (nitCliente.Length > 0 && nitCliente == nitDte)
        {
            return true;
        }

        string nrcCliente = NormalizarIdentificacion(cliente.Nrc);
        string nrcDte = NormalizarIdentificacion(participante.NrcSnapshot);
        return nrcCliente.Length > 0 && nrcCliente == nrcDte;
    }
}
