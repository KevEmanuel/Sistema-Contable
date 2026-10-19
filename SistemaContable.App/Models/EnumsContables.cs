namespace SistemaContableDTE.Models;

public enum LadoContable
{
    Debe,Haber
    
}
public enum TipoOrigenPartida
{
    Dte,Manual,
    Ajuste,
    Apertura,
    Cierre,
    Reverso
}
public enum EstadoPartidaContable
{
    Borrador,
    PendienteAprobacion,
    Contabilizada,
    Reversada,
    Anulada
}
