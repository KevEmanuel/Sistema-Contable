using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class ResultadoValidacionDte
{
    [Key]
    public int ResultadoValidacionDteId { get; set; }

    [Required]
    public int DocumentoDteId { get; set; }

    public required TipoValidacionDte TipoValidacion { get; set; }

    [Required]
    public int VersionSchema { get; set; }

    [Required]
    [StringLength(50)]
    public required string VersionValidador { get; set; }

    public required EstadoResultadoValidacionDte Resultado { get; set; }

    [Required]
    public required string ErroresJson { get; set; }

    public DateTime ValidadoEn { get; set; } = DateTime.Now;

    public int? UsuarioId { get; set; }

    public DocumentoDte? DocumentoDte { get; set; }

    public Usuario? Usuario { get; set; }
}
