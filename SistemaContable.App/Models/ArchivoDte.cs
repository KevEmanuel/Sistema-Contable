using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class ArchivoDte
{
    [Key]
    public int ArchivoDteId { get; set; }

    [Required]
    public int DocumentoDteId { get; set; }

    [Required]
    public string JsonOriginal { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string HashSha256 { get; set; } = string.Empty;

    public FuenteArchivoDte Fuente { get; set; } =
        FuenteArchivoDte.CargaManual;

    public DateTime FechaCarga { get; set; } = DateTime.Now;

    public DocumentoDte? DocumentoDte { get; set; }
}