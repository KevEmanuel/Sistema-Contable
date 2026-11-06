using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

namespace SistemaContableDTE.Services;

public static class ServicioContabilizacionDteSencilla
{
    public static List<string> ObtenerTiposDteDisponibles(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.DocumentosDte
            .AsNoTracking()
            .Where(documento => documento.EmpresaId == empresaId)
            .Select(documento => documento.TipoDte)
            .Union(contexto.ConfiguracionesContablesAutomaticas
                .Where(configuracion => configuracion.EmpresaId == empresaId)
                .Select(configuracion => configuracion.TipoDte))
            .OrderBy(tipoDte => tipoDte)
            .ToList();
    }

    public static List<ConfiguracionContableAutomatica> ObtenerConfiguraciones(
        int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.ConfiguracionesContablesAutomaticas
            .AsNoTracking()
            .Include(configuracion => configuracion.CuentaClientes)
            .Include(configuracion => configuracion.CuentaVentas)
            .Include(configuracion => configuracion.CuentaIvaDebito)
            .Include(configuracion => configuracion.CuentaProveedores)
            .Include(configuracion => configuracion.CuentaCompras)
            .Include(configuracion => configuracion.CuentaIvaAcreditable)
            .Where(configuracion =>
                configuracion.EmpresaId == empresaId)
            .OrderBy(configuracion => configuracion.TipoDte)
            .ToList();
    }

    public static void GuardarConfiguracion(
        int empresaId,
        int usuarioId,
        ConfiguracionContableAutomatica configuracion)
    {
        configuracion.TipoDte = configuracion.TipoDte.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(configuracion.TipoDte) ||
            configuracion.TipoDte.Length > 5)
        {
            throw new InvalidOperationException(
                "Ingresa un código de tipo DTE de entre 1 y 5 caracteres.");
        }

        if (configuracion.Operacion == OperacionConfiguracionDte.PorRolEmpresa &&
            configuracion.TipoDte is not ("01" or "03"))
        {
            throw new InvalidOperationException(
                "La clasificación automática por rol solo está disponible para los tipos 01 y 03.");
        }

        int[] cuentasRequeridas = configuracion.Operacion switch
        {
            OperacionConfiguracionDte.Venta =>
            [
                configuracion.CuentaClientesId ?? 0,
                configuracion.CuentaVentasId ?? 0
            ],
            OperacionConfiguracionDte.Compra =>
            [
                configuracion.CuentaProveedoresId ?? 0,
                configuracion.CuentaComprasId ?? 0
            ],
            OperacionConfiguracionDte.PorRolEmpresa =>
            [
                configuracion.CuentaClientesId ?? 0,
                configuracion.CuentaVentasId ?? 0,
                configuracion.CuentaProveedoresId ?? 0,
                configuracion.CuentaComprasId ?? 0
            ],
            OperacionConfiguracionDte.RevisionManual => [],
            _ => throw new InvalidOperationException(
                "Selecciona una operación válida para el tipo de DTE.")
        };
        int[] cuentasSeleccionadas = cuentasRequeridas
            .Concat(new[]
            {
                configuracion.CuentaIvaDebitoId ?? 0,
                configuracion.CuentaIvaAcreditableId ?? 0
            })
            .Where(id => id != 0)
            .Distinct()
            .ToArray();

        if (cuentasRequeridas.Any(id => id == 0))
        {
            throw new InvalidOperationException(
                "Completa las cuentas requeridas para la operación seleccionada.");
        }

        using var contexto = new ContabilidadDbContext();

        int cuentasValidas = contexto.CuentasContables.Count(cuenta =>
            cuentasSeleccionadas.Contains(cuenta.CuentaContableId) &&
            cuenta.EmpresaId == empresaId &&
            cuenta.Activa &&
            cuenta.PermiteMovimientos);

        if (cuentasValidas != cuentasSeleccionadas.Length)
        {
            throw new InvalidOperationException(
                "Todas las cuentas seleccionadas deben estar activas y permitir movimientos.");
        }

        ConfiguracionContableAutomatica? existente =
            contexto.ConfiguracionesContablesAutomaticas
                .SingleOrDefault(item =>
                    item.EmpresaId == empresaId &&
                    item.TipoDte == configuracion.TipoDte);
        object? valoresAnteriores = existente is null
            ? null
            : new
            {
                existente.TipoDte,
                existente.Operacion,
                existente.CuentaClientesId,
                existente.CuentaVentasId,
                existente.CuentaIvaDebitoId,
                existente.CuentaProveedoresId,
                existente.CuentaComprasId,
                existente.CuentaIvaAcreditableId
            };

        if (existente is null)
        {
            configuracion.EmpresaId = empresaId;
            contexto.ConfiguracionesContablesAutomaticas.Add(configuracion);
        }
        else
        {
            existente.Operacion = configuracion.Operacion;
            existente.CuentaClientesId = configuracion.CuentaClientesId;
            existente.CuentaVentasId = configuracion.CuentaVentasId;
            existente.CuentaIvaDebitoId = configuracion.CuentaIvaDebitoId;
            existente.CuentaProveedoresId = configuracion.CuentaProveedoresId;
            existente.CuentaComprasId = configuracion.CuentaComprasId;
            existente.CuentaIvaAcreditableId =
                configuracion.CuentaIvaAcreditableId;
        }

        contexto.SaveChanges();
        ConfiguracionContableAutomatica guardada = existente ??
            configuracion;
        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = nameof(ConfiguracionContableAutomatica),
            IdEntidad = guardada.ConfiguracionContableAutomaticaId.ToString(),
            Accion = "Guardar configuración contable sencilla",
            ValoresAnteriores = valoresAnteriores is null
                ? null
                : JsonSerializer.Serialize(valoresAnteriores),
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                guardada.CuentaClientesId,
                guardada.CuentaVentasId,
                guardada.CuentaIvaDebitoId,
                guardada.CuentaProveedoresId,
                guardada.CuentaComprasId,
                guardada.CuentaIvaAcreditableId,
                guardada.TipoDte,
                guardada.Operacion
            }),
            FechaHora = DateTime.Now
        });
        contexto.SaveChanges();
    }

    public static List<PartidaContable> ObtenerBorradores(int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.PartidasContables
            .AsNoTracking()
            .Include(partida => partida.DocumentoDte)
            .Include(partida => partida.Detalles)
            .ThenInclude(detalle => detalle.CuentaContable)
            .Where(partida =>
                partida.EmpresaId == empresaId &&
                partida.TipoOrigen == TipoOrigenPartida.Dte &&
                partida.Estado == EstadoPartidaContable.Borrador &&
                partida.DocumentoDteId.HasValue)
            .OrderBy(partida => partida.Fecha)
            .ThenBy(partida => partida.NumeroPartida)
            .ToList();
    }

    public static void EliminarBorrador(
        int empresaId,
        int usuarioId,
        int partidaContableId)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        PartidaContable? partida = contexto.PartidasContables
            .Include(item => item.Detalles)
            .SingleOrDefault(item =>
                item.PartidaContableId == partidaContableId &&
                item.EmpresaId == empresaId);

        if (partida is null ||
            partida.TipoOrigen != TipoOrigenPartida.Dte ||
            partida.Estado != EstadoPartidaContable.Borrador ||
            !partida.DocumentoDteId.HasValue)
        {
            throw new InvalidOperationException(
                "Solo se pueden eliminar borradores generados desde DTE.");
        }

        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = nameof(PartidaContable),
            IdEntidad = partida.PartidaContableId.ToString(),
            Accion = "Eliminar borrador contable generado desde DTE",
            ValoresAnteriores = JsonSerializer.Serialize(new
            {
                partida.NumeroPartida,
                partida.DocumentoDteId,
                partida.Estado,
                TotalDebe = partida.Detalles.Sum(item => item.Debe),
                TotalHaber = partida.Detalles.Sum(item => item.Haber)
            }),
            FechaHora = DateTime.Now
        });
        contexto.DetallesPartida.RemoveRange(partida.Detalles);
        contexto.PartidasContables.Remove(partida);
        contexto.SaveChanges();
        transaccion.Commit();
    }

    public static PartidaContable CrearBorrador(
        int empresaId,
        int usuarioId,
        int periodoContableId,
        int documentoDteId)
    {
        using var contexto = new ContabilidadDbContext();
        using var transaccion = contexto.Database.BeginTransaction();

        DocumentoDte? documento = contexto.DocumentosDte
            .Include(item => item.ResumenTributario)
            .Include(item => item.Participantes)
            .SingleOrDefault(item =>
                item.DocumentoDteId == documentoDteId &&
                item.EmpresaId == empresaId);

        if (documento is null ||
            documento.Estado != EstadoDocumentoDte.Validado)
        {
            throw new InvalidOperationException(
                "Solo se pueden preparar DTE validados.");
        }

        if (contexto.PartidasContables.Any(partida =>
                partida.DocumentoDteId == documentoDteId))
        {
            throw new InvalidOperationException(
                "Este DTE ya tiene una partida o borrador contable.");
        }

        PeriodoContable? periodo = contexto.PeriodosContables
            .SingleOrDefault(item =>
                item.PeriodoContableId == periodoContableId &&
                item.EmpresaId == empresaId);

        if (periodo is null ||
            periodo.EstaCerrado ||
            documento.FechaEmision.Date < periodo.FechaInicio.Date ||
            documento.FechaEmision.Date > periodo.FechaFin.Date)
        {
            throw new InvalidOperationException(
                "Selecciona un período abierto que incluya la fecha del DTE.");
        }

        ConfiguracionContableAutomatica? configuracion =
            contexto.ConfiguracionesContablesAutomaticas
                .SingleOrDefault(item =>
                    item.EmpresaId == empresaId &&
                    item.TipoDte == documento.TipoDte.Trim().ToUpperInvariant());

        if (configuracion is null)
        {
            throw new InvalidOperationException(
                $"Configura las cuentas y la operación para el tipo DTE {documento.TipoDte} antes de generar borradores.");
        }

        bool esVenta = configuracion.Operacion switch
        {
            OperacionConfiguracionDte.Venta => true,
            OperacionConfiguracionDte.Compra => false,
            OperacionConfiguracionDte.PorRolEmpresa =>
                documento.OperacionContable switch
                {
                    OperacionContableDte.Venta => true,
                    OperacionContableDte.Compra => false,
                    _ => throw new InvalidOperationException(
                        "El rol de este DTE no permite determinar si es compra o venta; selecciona una operación explícita o deja el tipo para revisión manual.")
                },
            OperacionConfiguracionDte.RevisionManual =>
                throw new InvalidOperationException(
                    $"El tipo DTE {documento.TipoDte} está configurado para revisión manual."),
            _ => throw new InvalidOperationException(
                "La operación configurada para este tipo de DTE no es válida.")
        };

        if (configuracion.Operacion == OperacionConfiguracionDte.PorRolEmpresa &&
            documento.TipoDte.Trim() is not ("01" or "03"))
        {
            throw new InvalidOperationException(
                "La clasificación automática por rol solo está disponible para los tipos 01 y 03. Selecciona compra, venta o revisión manual.");
        }

        List<DetalleDTe> lineasDte = contexto.DetallesDte
            .Where(item => item.DocumentoDteId == documentoDteId)
            .ToList();

        if (lineasDte.Count == 0)
        {
            throw new InvalidOperationException(
                "El DTE no contiene líneas importadas para calcular el asiento.");
        }

        decimal baseOperacion = lineasDte.Sum(item =>
            item.VentaGravada + item.VentaExenta + item.VentaNoSujeta);

        ResumenTributario? resumen = documento.ResumenTributario;
        decimal? totalIva = resumen?.TotalIva;
        if (!totalIva.HasValue && resumen is not null)
        {
            using JsonDocument resumenJson =
                JsonDocument.Parse(resumen.JsonOriginal);
            totalIva = ServicioResumenTributarioDte.LeerTotalIva(
                resumenJson.RootElement);
        }

        if (!totalIva.HasValue && lineasDte.Any(item =>
                item.VentaGravada != 0))
        {
            throw new InvalidOperationException(
                "No se encontró el importe de IVA en el resumen del DTE (totalIva o tributo código 20). Revisa el JSON del documento antes de contabilizarlo.");
        }

        decimal iva = totalIva ?? 0;
        decimal? totalDocumento = resumen?.TotalPagar ??
                                 resumen?.MontoTotalOperacion;

        if (totalDocumento is null || totalDocumento <= 0)
        {
            throw new InvalidOperationException(
                "El DTE no contiene un total válido para generar la partida.");
        }

        decimal total = decimal.Round(totalDocumento.Value, 2);
        decimal totalComponentes = decimal.Round(baseOperacion + iva, 2);

        if (decimal.Round(baseOperacion, 2) <= 0 ||
            decimal.Round(iva, 2) < 0 ||
            totalComponentes != total)
        {
            throw new InvalidOperationException(
                "Los importes del DTE incluyen componentes que esta configuración sencilla no puede identificar. El documento requiere revisión manual.");
        }

        int? cuentaTerceroId = esVenta
            ? configuracion.CuentaClientesId
            : configuracion.CuentaProveedoresId;
        int? cuentaOperacionId = esVenta
            ? configuracion.CuentaVentasId
            : configuracion.CuentaComprasId;
        int? cuentaIvaId = esVenta
            ? configuracion.CuentaIvaDebitoId
            : configuracion.CuentaIvaAcreditableId;

        if (cuentaTerceroId is null || cuentaOperacionId is null ||
            (iva > 0 && cuentaIvaId is null))
        {
            throw new InvalidOperationException(
                "Falta asignar una cuenta necesaria para este asiento. Revisa la configuración contable sencilla.");
        }

        var detalles = new List<DetallePartida>();

        detalles.Add(new DetallePartida
        {
            CuentaContableId = cuentaTerceroId.Value,
            Concepto = esVenta ? "Cliente según DTE" : "Proveedor según DTE",
            Debe = esVenta ? total : 0,
            Haber = esVenta ? 0 : total
        });

        detalles.Add(new DetallePartida
        {
            CuentaContableId = cuentaOperacionId.Value,
            Concepto = esVenta ? "Venta según DTE" : "Compra según DTE",
            Debe = esVenta ? 0 : baseOperacion,
            Haber = esVenta ? baseOperacion : 0
        });

        if (iva > 0)
        {
            detalles.Add(new DetallePartida
            {
                CuentaContableId = cuentaIvaId!.Value,
                Concepto = esVenta ? "IVA débito según DTE" : "IVA crédito según DTE",
                Debe = esVenta ? 0 : iva,
                Haber = esVenta ? iva : 0
            });
        }

        int[] cuentasSeleccionadas = detalles
            .Select(detalle => detalle.CuentaContableId)
            .Distinct()
            .ToArray();
        int cuentasValidas = contexto.CuentasContables.Count(cuenta =>
            cuentasSeleccionadas.Contains(cuenta.CuentaContableId) &&
            cuenta.EmpresaId == empresaId &&
            cuenta.Activa &&
            cuenta.PermiteMovimientos);

        if (cuentasValidas != cuentasSeleccionadas.Length)
        {
            throw new InvalidOperationException(
                "Una de las cuentas configuradas ya no está activa o no permite movimientos.");
        }

        if (decimal.Round(detalles.Sum(item => item.Debe), 2) !=
            decimal.Round(detalles.Sum(item => item.Haber), 2))
        {
            throw new InvalidOperationException(
                "Los importes no producen una partida balanceada; el DTE requiere revisión.");
        }

        int ultimoNumero = contexto.PartidasContables
            .Where(item =>
                item.EmpresaId == empresaId &&
                item.PeriodoContableId == periodoContableId)
            .Select(item => (int?)item.NumeroPartida)
            .Max() ?? 0;

        var partida = new PartidaContable
        {
            EmpresaId = empresaId,
            PeriodoContableId = periodoContableId,
            DocumentoDteId = documentoDteId,
            NumeroPartida = ultimoNumero + 1,
            Fecha = documento.FechaEmision.Date,
            Glosa = $"DTE {documento.TipoDte} - {documento.NumeroControl}",
            TipoOrigen = TipoOrigenPartida.Dte,
            Estado = EstadoPartidaContable.Borrador,
            CreadoPorId = usuarioId,
            CreadoEn = DateTime.Now,
            Detalles = detalles
        };

        contexto.PartidasContables.Add(partida);
        contexto.SaveChanges();
        contexto.BitacorasAuditoria.Add(new BitacoraAuditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Entidad = nameof(PartidaContable),
            IdEntidad = partida.PartidaContableId.ToString(),
            Accion = "Crear borrador contable desde DTE",
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                partida.NumeroPartida,
                partida.DocumentoDteId,
                documento.CodigoGeneracion,
                documento.TipoDte,
                Operacion = documento.OperacionContable.ToString(),
                Total = total,
                Debe = detalles.Sum(item => item.Debe),
                Haber = detalles.Sum(item => item.Haber)
            }),
            FechaHora = DateTime.Now
        });
        contexto.SaveChanges();
        transaccion.Commit();

        return partida;
    }
}
