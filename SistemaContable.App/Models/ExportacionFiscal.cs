using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class ExportacionFiscal
{
    [Key]
    public int ExportacionFiscalId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required]
    public int PeriodoContableId { get; set; }

    [Required]
    [StringLength(100)]
    public string TipoAnexo { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string VersionFormato { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string NombreArchivo { get; set; } = string.Empty;

    [Required]
    public string Contenido { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string HashSha256 { get; set; } = string.Empty;

    [Required]
    public int GeneradoPorId { get; set; }

    public DateTime GeneradoEn { get; set; } = DateTime.Now;

    public Empresa? Empresa { get; set; }

    public PeriodoContable? PeriodoContable { get; set; }

    public Usuario? GeneradoPor { get; set; }

    public ICollection<ExportacionFiscalDetalle> Detalles { get; set; } =
        new List<ExportacionFiscalDetalle>();
}