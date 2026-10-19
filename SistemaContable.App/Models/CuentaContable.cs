using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class CuentaContable
{
    [Key]
    public int CuentaContableId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required]
    [StringLength(30)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20)]
    public string TipoCuenta { get; set; } = string.Empty;
    // Activo, Pasivo, Patrimonio, Ingreso, Costo o Gasto.

    public int? CuentaPadreId { get; set; }

    public int Nivel { get; set; }

    public bool PermiteMovimientos { get; set; } = true;

    public bool Activa { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Relación con la empresa.
    public Empresa? Empresa { get; set; }

    // Relación consigo misma para formar el catálogo jerárquico.
    public CuentaContable? CuentaPadre { get; set; }
}