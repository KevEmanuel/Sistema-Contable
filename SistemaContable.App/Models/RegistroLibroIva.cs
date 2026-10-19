using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class RegistroLibroIva
{
    [Key]
    public int RegistroLibroIvaId { get; set; }

    [Required]
    public int DocumentoDteId { get; set; }

    [Required]
    public int PeriodoContableId { get; set; }

    public TipoLibroIva TipoLibro { get; set; }

    public DateTime FechaRegistro { get; set; }

    public decimal BaseGravada { get; set; } = 0;

    public decimal MontoExento { get; set; } = 0;

    public decimal MontoNoSujeto { get; set; } = 0;

    public decimal CreditoFiscal { get; set; } = 0;

    public decimal DebitoFiscal { get; set; } = 0;

    public decimal TotalOperacion { get; set; } = 0;

    public EstadoRegistroIva Estado { get; set; } =
        EstadoRegistroIva.Vigente;

    public DocumentoDte? DocumentoDte { get; set; }

    public periodoContable? periodoContable { get; set; }
}