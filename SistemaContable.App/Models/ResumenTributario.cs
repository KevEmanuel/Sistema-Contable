using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class ResumenTributario
{
    [Key]
    public int ResumenTributarioId { get; set; }

    [Required]
    public int DocumentoDteId { get; set; }

    public decimal? TotalGravada { get; set; }

    public decimal? TotalExenta { get; set; }

    public decimal? TotalNoSujeta { get; set; }

    public decimal? TotalIva { get; set; }

    public decimal? MontoTotalOperacion { get; set; }

    public decimal? TotalPagar { get; set; }

    [Required]
    public required string JsonOriginal { get; set; }

    public DocumentoDte? DocumentoDte { get; set; }
}
