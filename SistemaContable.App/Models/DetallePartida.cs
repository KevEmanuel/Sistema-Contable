using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class DetallePartida
{
    [Key]
    public int DetallePartidaId { get; set; }

    [Required]
    public int PartidaContableId { get; set; }

    [Required]
    public int CuentaContableId { get; set; }

    [StringLength(500)]
    public string? Concepto { get; set; }

    public decimal Debe { get; set; } = 0;

    public decimal Haber { get; set; } = 0;

    public PartidaContable? PartidaContable { get; set; }

    public CuentaContable? CuentaContable { get; set; }
}