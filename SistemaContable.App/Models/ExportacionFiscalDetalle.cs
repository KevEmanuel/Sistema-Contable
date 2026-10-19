using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class ExportacionFiscalDetalle
{
    [Key]
    public int ExportacionFiscalDetalleId { get; set; }

    [Required]
    public int ExportacionFiscalId { get; set; }

    [Required]
    public int RegistroLibroIvaId { get; set; }

    public int Linea { get; set; }

    [Required]
    public string ContenidoLinea { get; set; } = string.Empty;

    public ExportacionFiscal? ExportacionFiscal { get; set; }

    public RegistroLibroIva? RegistroLibroIva { get; set; }
}