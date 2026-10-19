namespace SistemaContableDTE.Models;

public enum RolEmpresaDte
{
    Emisor,
    Receptor,
    NoDeterminado
}

public enum EstadoDocumentoDte
{
    Importado,
    PendienteValidacion,
    Validado,
    PendienteRevision,
    Contabilizado,
    Invalidado,
    Rechazado
}

public enum RolParticipanteDte
{
    Emisor,
    Receptor
}

public enum FuenteArchivoDte
{
    CargaManual,
    Api,
    Webhook,
    Importacion
}

public enum FuenteEvidenciaFiscal
{
    RespuestaRecepcio,ConsultaPublica, CargarRespaldo
}
