using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;
//clase para Refencia del DTE, base 
public class DteReferencia
{
    [Key]
    public int DteReferenciaId { get; set; }

    [Required]
    public int DocumentoDteOrigenId { get; set; }

    [Required]
    public int DocumentoDteReferenciadoId { get; set; }

    [Required]
    [StringLength(50)]
    public string TipoReferencia { get; set; } = string.Empty;

    public DocumentoDte? DocumentoDteOrigen { get; set; }

    public DocumentoDte? DocumentoDteReferenciado { get; set; }
}