using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class Usuario
{
    [Key]
    public int UsuarioId { get; set; }

    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string HashContrasena { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    public ICollection<UsuarioRol> Roles { get; set; } =
        new List<UsuarioRol>();
}