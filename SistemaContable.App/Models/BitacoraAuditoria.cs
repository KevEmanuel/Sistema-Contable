using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class BitacoraAuditoria
{
    [Key]
    public int BitacoraAuditoriaId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    public int? UsuarioId { get; set; }

    [Required]
    [StringLength(100)]
    public string Entidad { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string IdEntidad { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Accion { get; set; } = string.Empty;

    public string? ValoresAnteriores { get; set; }

    public string? ValoresNuevos { get; set; }

    [StringLength(50)]
    public string? IpOrigen { get; set; }

    public DateTime FechaHora { get; set; } = DateTime.Now;

    public Empresa? Empresa { get; set; }

    public Usuario? Usuario { get; set; }
}