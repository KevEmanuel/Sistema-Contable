using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaContableDTE.Models;

public sealed class ClienteContable
{
    [Key]
    public int ClienteContableId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required]
    [StringLength(250)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(17)]
    public string Nit { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Nrc { get; set; }

    public Empresa? Empresa { get; set; }

    public ICollection<DocumentoDte> DocumentosDte { get; set; } =
        new List<DocumentoDte>();

    [NotMapped]
    public string Etiqueta => $"{Nombre} (NIT: {Nit})";
}
