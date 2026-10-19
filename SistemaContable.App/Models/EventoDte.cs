using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;
//clase para el manejo del DTE
public class EventoDte
{
    [Key]
    public int EventoDteId { get; set; }

    [Required]
    public int DocumentoDteId { get; set; }

    [Required]
    [StringLength(100)]
    public string TipoEvento { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EstadoEvento { get; set; } = string.Empty;

    public DateTime FechaEvento { get; set; }

    [StringLength(100)]
    public string? SelloRecibido { get; set; }

    public string? DetalleJson { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    public DocumentoDte? DocumentoDte { get; set; }
}