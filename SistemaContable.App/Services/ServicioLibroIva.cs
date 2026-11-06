using Microsoft.EntityFrameworkCore;
using SistemaContableDTE.Data;
using SistemaContableDTE.Models;

//modulo para la validacion fiscal 05

namespace SistemaContableDTE.Services;

public static class ServicioLibroIva
{
    internal static void GenerarDesdePartidaContabilizada(
        ContabilidadDbContext contexto,
        DocumentoDte documento,
        int periodoContableId)
    {
        string tipoDte = documento.TipoDte.Trim();
        if (tipoDte is not ("01" or "03"))
        {
            return;
        }

        List<RegistroLibroIva> registrosExistentes =
            contexto.RegistrosLibroIva
                .Where(registro =>
                    registro.DocumentoDteId == documento.DocumentoDteId)
                .ToList();
        if (registrosExistentes.Any(registro =>
                registro.Estado == EstadoRegistroIva.Vigente))
        {
            return;
        }

        if (registrosExistentes.Count > 0)
        {
            throw new InvalidOperationException(
                "El DTE ya tiene un registro de ventas o compras no vigente; revisa ese registro antes de contabilizarlo.");
        }

        TipoLibroIva tipoLibro = ResolverTipoLibro(
            contexto,
            documento,
            null,
            out _);

        decimal baseGravada = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documento.DocumentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaGravada) ?? 0;
        decimal montoExento = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documento.DocumentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaExenta) ?? 0;
        decimal montoNoSujeto = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documento.DocumentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaNoSujeta) ?? 0;

        ResumenTributario resumen = contexto.ResumenesTributarios
            .SingleOrDefault(item =>
                item.DocumentoDteId == documento.DocumentoDteId)
            ?? throw new InvalidOperationException(
                "El DTE no tiene un resumen tributario para crear su libro de ventas o compras.");

        decimal iva = resumen.TotalIva ?? 0;
        decimal totalOperacion = resumen.MontoTotalOperacion ??
            resumen.TotalPagar ??
            baseGravada + montoExento + montoNoSujeto + iva;

        contexto.RegistrosLibroIva.Add(new RegistroLibroIva
        {
            DocumentoDteId = documento.DocumentoDteId,
            PeriodoContableId = periodoContableId,
            TipoLibro = tipoLibro,
            FechaRegistro = documento.FechaEmision.Date,
            BaseGravada = baseGravada,
            MontoExento = montoExento,
            MontoNoSujeto = montoNoSujeto,
            CreditoFiscal = tipoLibro == TipoLibroIva.Compras ? iva : 0,
            DebitoFiscal = tipoLibro is
                (TipoLibroIva.VentasContribuyentes or
                 TipoLibroIva.VentasConsumidorFinal)
                ? iva
                : 0,
            TotalOperacion = totalOperacion,
            Estado = EstadoRegistroIva.Vigente
        });
    }

    public static List<DocumentoDte> ObtenerDocumentosValidados(
        int empresaId)
    {
        using var contexto = new ContabilidadDbContext();

        return contexto.DocumentosDte
            .AsNoTracking()
            .Where(documento =>
                documento.EmpresaId == empresaId &&
                (documento.Estado == EstadoDocumentoDte.Validado ||
                 documento.Estado == EstadoDocumentoDte.Contabilizado) &&
                !contexto.RegistrosLibroIva.Any(registro =>
                    registro.DocumentoDteId == documento.DocumentoDteId))
            .OrderByDescending(documento => documento.FechaEmision)
            .ToList();
    }

    public static List<DocumentoDte> ObtenerDocumentosRelacionables(
        int empresaId,
        int documentoNotaId)
    {
        using var contexto = new ContabilidadDbContext();

        DocumentoDte? nota = contexto.DocumentosDte
            .AsNoTracking()
            .SingleOrDefault(documento =>
                documento.DocumentoDteId == documentoNotaId &&
                documento.EmpresaId == empresaId);

        if (nota is null || !EsNotaAjuste(nota.TipoDte))
        {
            return new List<DocumentoDte>();
        }

        List<RegistroLibroIva> registrosRelacionables =
            contexto.RegistrosLibroIva
            .AsNoTracking()
            .Include(registro => registro.DocumentoDte)
            .Where(registro =>
                registro.Estado == EstadoRegistroIva.Vigente &&
                registro.DocumentoDte!.EmpresaId == empresaId &&
                registro.DocumentoDte.DocumentoDteId != documentoNotaId &&
                registro.DocumentoDte.RolEmpresa == nota.RolEmpresa &&
                registro.DocumentoDte.TipoDte != "05" &&
                registro.DocumentoDte.TipoDte != "06" &&
                (registro.DocumentoDte.Estado == EstadoDocumentoDte.Validado ||
                 registro.DocumentoDte.Estado == EstadoDocumentoDte.Contabilizado))
            .ToList();

        return registrosRelacionables
            .Select(registro => registro.DocumentoDte!)
            .DistinctBy(documento => documento.DocumentoDteId)
            .OrderByDescending(documento => documento.FechaEmision)
            .ToList();
    }

    public static List<RegistroLibroIva> ObtenerPorPeriodo(
        int empresaId,
        int periodoContableId)
    {
        using var contexto = new ContabilidadDbContext();

        List<RegistroLibroIva> registros = contexto.RegistrosLibroIva
            .AsNoTracking()
            .Include(registro => registro.DocumentoDte)
            .ThenInclude(documento => documento!.Participantes)
            .Where(registro =>
                registro.PeriodoContableId == periodoContableId &&
                registro.DocumentoDte!.EmpresaId == empresaId)
            .OrderBy(registro => registro.FechaRegistro)
            .ToList();

        CompletarDatosContraparte(registros);
        return registros;
    }

    internal static void CompletarDatosContraparte(
        IEnumerable<RegistroLibroIva> registros)
    {
        foreach (RegistroLibroIva registro in registros)
        {
            DocumentoDte documento = registro.DocumentoDte
                ?? throw new InvalidOperationException(
                    $"El registro de IVA {registro.RegistroLibroIvaId} no tiene DTE asociado.");

            RolParticipanteDte rolContraparte = documento.RolEmpresa switch
            {
                RolEmpresaDte.Emisor => RolParticipanteDte.Receptor,
                RolEmpresaDte.Receptor => RolParticipanteDte.Emisor,
                _ => throw new InvalidOperationException(
                    $"No se determinó el rol empresarial del DTE {documento.NumeroControl}.")
            };

            DteParticipante participante = documento.Participantes
                .SingleOrDefault(item => item.Rol == rolContraparte)
                ?? throw new InvalidOperationException(
                    $"El DTE {documento.NumeroControl} no tiene los datos del cliente o proveedor.");

            registro.NombreTercero = participante.NombreSnapshot;
            registro.NitTercero = participante.NitSnapshot;
            registro.NrcTercero = participante.NrcSnapshot;
        }
    }

    public static RegistroLibroIva Generar(
        int empresaId,
        int documentoDteId,
        int periodoContableId,
        int? documentoRelacionadoId = null)
    {
        using var contexto = new ContabilidadDbContext();

        DocumentoDte? documento = contexto.DocumentosDte
            .SingleOrDefault(documento =>
                documento.DocumentoDteId == documentoDteId &&
                documento.EmpresaId == empresaId);

        if (documento is null)
        {
            throw new InvalidOperationException(
                "El DTE seleccionado no pertenece a la empresa.");
        }

        if (documento.Estado is not
            (EstadoDocumentoDte.Validado or
             EstadoDocumentoDte.Contabilizado))
        {
            throw new InvalidOperationException(
                "Solo los DTE validados o contabilizados pueden generar Libro IVA.");
        }

        bool yaRegistrado = contexto.RegistrosLibroIva.Any(registro =>
            registro.DocumentoDteId == documentoDteId);

        if (yaRegistrado)
        {
            throw new InvalidOperationException(
                "Este DTE ya posee un registro de Libro IVA.");
        }

        PeriodoContable? periodo = contexto.PeriodosContables
            .SingleOrDefault(periodo =>
                periodo.PeriodoContableId == periodoContableId &&
                periodo.EmpresaId == empresaId);

        if (periodo is null || periodo.EstaCerrado)
        {
            throw new InvalidOperationException(
                "Selecciona un período contable abierto.");
        }

        if (documento.FechaEmision.Date < periodo.FechaInicio.Date ||
            documento.FechaEmision.Date > periodo.FechaFin.Date)
        {
            throw new InvalidOperationException(
                "La fecha del DTE no pertenece al período seleccionado.");
        }

        TipoLibroIva tipoLibro = ResolverTipoLibro(
            contexto,
            documento,
            documentoRelacionadoId,
            out DocumentoDte? documentoRelacionado);

        decimal baseGravada = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaGravada) ?? 0;

        decimal montoExento = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaExenta) ?? 0;

        decimal montoNoSujeto = contexto.DetallesDte
            .Where(detalle =>
                detalle.DocumentoDteId == documentoDteId)
            .Sum(detalle => (decimal?)detalle.VentaNoSujeta) ?? 0;

        ResumenTributario resumen = contexto.ResumenesTributarios
            .AsNoTracking()
            .SingleOrDefault(resumen =>
                resumen.DocumentoDteId == documentoDteId)
            ?? throw new InvalidOperationException(
                "El DTE no tiene un resumen tributario almacenado.");

        decimal iva = resumen.TotalIva ?? 0;
        decimal totalOperacion =
            resumen.MontoTotalOperacion ??
            resumen.TotalPagar ??
            baseGravada + montoExento + montoNoSujeto + iva;
        decimal signo = documento.TipoDte.Trim() == "05" ? -1 : 1;

        bool esCompra = tipoLibro == TipoLibroIva.Compras;

        bool esVenta =
            tipoLibro == TipoLibroIva.VentasContribuyentes ||
            tipoLibro == TipoLibroIva.VentasConsumidorFinal;

        if (documentoRelacionado is not null)
        {
            documento.DocumentoRelacionadoId =
                documentoRelacionado.DocumentoDteId;

            bool referenciaExiste = contexto.DteReferencias.Any(referencia =>
                referencia.DocumentoDteOrigenId == documentoDteId &&
                referencia.DocumentoDteReferenciadoId ==
                    documentoRelacionado.DocumentoDteId);

            if (!referenciaExiste)
            {
                contexto.DteReferencias.Add(new DteReferencia
                {
                    DocumentoDteOrigenId = documentoDteId,
                    DocumentoDteReferenciadoId =
                        documentoRelacionado.DocumentoDteId,
                    TipoReferencia = documento.TipoDte == "05"
                        ? "Nota de crédito"
                        : "Nota de débito"
                });
            }
        }

        var registroNuevo = new RegistroLibroIva
        {
            DocumentoDteId = documentoDteId,
            PeriodoContableId = periodoContableId,
            TipoLibro = tipoLibro,
            FechaRegistro = documento.FechaEmision.Date,
            BaseGravada = baseGravada * signo,
            MontoExento = montoExento * signo,
            MontoNoSujeto = montoNoSujeto * signo,
            CreditoFiscal = esCompra ? iva * signo : 0,
            DebitoFiscal = esVenta ? iva * signo : 0,
            TotalOperacion = totalOperacion * signo,
            Estado = EstadoRegistroIva.Vigente
        };

        contexto.RegistrosLibroIva.Add(registroNuevo);
        contexto.SaveChanges();

        return registroNuevo;
    }

    public static TipoLibroIva ObtenerTipoLibroSugerido(
        int empresaId,
        int documentoDteId,
        int? documentoRelacionadoId = null)
    {
        using var contexto = new ContabilidadDbContext();

        DocumentoDte? documento = contexto.DocumentosDte
            .AsNoTracking()
            .SingleOrDefault(documento =>
                documento.DocumentoDteId == documentoDteId &&
                documento.EmpresaId == empresaId);

        if (documento is null)
        {
            throw new InvalidOperationException(
                "No se encontró el DTE seleccionado.");
        }

        return ResolverTipoLibro(
            contexto,
            documento,
            documentoRelacionadoId,
            out _);
    }

    private static TipoLibroIva ResolverTipoLibro(
        ContabilidadDbContext contexto,
        DocumentoDte documento,
        int? documentoRelacionadoId,
        out DocumentoDte? documentoRelacionado)
    {
        documentoRelacionado = null;
        string tipoDte = documento.TipoDte.Trim();

        if (EsNotaAjuste(tipoDte))
        {
            int? idRelacionado =
                documentoRelacionadoId ?? documento.DocumentoRelacionadoId;

            if (idRelacionado is null)
            {
                throw new InvalidOperationException(
                    "Selecciona el DTE original registrado antes de agregar esta nota.");
            }

            int relacionadoId = idRelacionado.Value;
            documentoRelacionado = contexto.DocumentosDte
                .SingleOrDefault(original =>
                    original.DocumentoDteId == relacionadoId &&
                    original.EmpresaId == documento.EmpresaId);

            if (documentoRelacionado is null ||
                documentoRelacionado.RolEmpresa != documento.RolEmpresa ||
                EsNotaAjuste(documentoRelacionado.TipoDte))
            {
                throw new InvalidOperationException(
                    "El DTE original debe pertenecer a la misma empresa y tener el mismo rol; no puede ser otra nota de ajuste.");
            }

            int idDocumentoOriginal = documentoRelacionado.DocumentoDteId;
            RegistroLibroIva? registroOriginal =
                contexto.RegistrosLibroIva
                    .AsNoTracking()
                    .SingleOrDefault(registro =>
                        registro.DocumentoDteId ==
                            idDocumentoOriginal &&
                        registro.Estado == EstadoRegistroIva.Vigente);

            if (registroOriginal is null)
            {
                throw new InvalidOperationException(
                    "Registra primero el DTE original en un Libro IVA vigente.");
            }

            return registroOriginal.TipoLibro;
        }

        return (tipoDte, documento.RolEmpresa) switch
        {
            ("01", RolEmpresaDte.Emisor) =>
                TipoLibroIva.VentasConsumidorFinal,
            ("03", RolEmpresaDte.Emisor) =>
                TipoLibroIva.VentasContribuyentes,
            ("14", RolEmpresaDte.Receptor) =>
                TipoLibroIva.SujetosExcluidos,
            ("01" or "03", RolEmpresaDte.Receptor) =>
                TipoLibroIva.Compras,
            (_, RolEmpresaDte.NoDeterminado) =>
                throw new InvalidOperationException(
                    "No se puede clasificar el DTE porque no se determinó si la empresa es emisor o receptor."),
            _ => throw new InvalidOperationException(
                $"El tipo de DTE {tipoDte} no está contemplado para los libros de IVA disponibles.")
        };
    }

    private static bool EsNotaAjuste(string tipoDte)
    {
        return tipoDte.Trim() is "05" or "06";
    }
}