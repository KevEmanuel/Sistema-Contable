using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class DteParticipante
{
    [Key]
    public int DteParticipanteId { get; set; }

    [Required]
    public int DocumentoDteId { get; set; }

    public int? TerceroId { get; set; }

    public RolParticipanteDte Rol { get; set; }

    [StringLength(17)]
    public string? NitSnapshot { get; set; }

    [StringLength(20)]
    public string? NrcSnapshot { get; set; }

    [Required]
    [StringLength(250)]
    public string NombreSnapshot { get; set; } = string.Empty;

    public DocumentoDte? DocumentoDte { get; set; }

    public Tercero? Tercero { get; set; }
}