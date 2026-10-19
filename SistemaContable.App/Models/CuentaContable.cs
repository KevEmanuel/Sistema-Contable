using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;
//clase para contabilizar el periodo contables
public class PeriodoContable
{
    [Key]
    public int PeriodoContableId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public bool EstaCerrado { get; set; } = false;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Relación: un período pertenece a una empresa.
    public Empresa? Empresa { get; set; }
}