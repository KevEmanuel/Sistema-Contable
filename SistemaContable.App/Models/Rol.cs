using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class Rol
{
    [Key]
    public int RolId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<UsuarioRol> Usuarios { get; set; } =
        new List<UsuarioRol>();
}