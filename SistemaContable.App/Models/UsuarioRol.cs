using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class UsuarioRol
{
    [Key]
    public int UsuarioRolId { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int RolId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    public Usuario? Usuario { get; set; }

    public Rol? Rol { get; set; }

    public Empresa? Empresa { get; set; }
}