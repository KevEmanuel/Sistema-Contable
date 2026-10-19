using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class Tercero
{
    [Key]
    public int TerceroId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [StringLength(17)]
    public string? Nit { get; set; }

    [StringLength(20)]
    public string? Nrc { get; set; }

    [Required]
    [StringLength(250)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(10)]
    public string? CodigoActividad { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? Correo { get; set; }

    [StringLength(25)]
    public string? Telefono { get; set; }

    [StringLength(500)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;

    public Empresa? Empresa { get; set; }
}