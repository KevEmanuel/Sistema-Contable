namespace SistemaContableDTE.Models;

public class LibroMayorLinea
{
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int? NumeroPartida { get; set; }
    public string Glosa { get; set; } = string.Empty;
    public string? Concepto { get; set; }
    public bool EsSaldoInicial { get; set; }
    public EstadoPartidaContable? EstadoPartida { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal SaldoDeudor { get; set; }
    public decimal SaldoAcreedor { get; set; }
}
