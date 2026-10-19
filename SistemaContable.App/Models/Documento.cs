using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class DocumentoDte
{
    [Key]
    public int DocumentoDteId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required]
    [StringLength(36)]
    public string CodigoGeneracion { get; set; } = string.Empty;
    // UUID completo del DTE, incluyendo guiones.

    [Required]
    [StringLength(50)]
    public string NumeroControl { get; set; } = string.Empty;

    [Required]
    [StringLength(5)]
    public string TipoDte { get; set; } = string.Empty;

    public int VersionSchema { get; set; }

    public DateTime FechaEmision { get; set; }

    [StringLength(10)]
    public string? HoraEmision { get; set; }

    public RolEmpresaDte RolEmpresa { get; set; }

    public EstadoDocumentoDte Estado { get; set; } =
        EstadoDocumentoDte.Importado;

    public int? DocumentoRelacionadoId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    public Empresa? Empresa { get; set; }

    public DocumentoDte? DocumentoRelacionado { get; set; }

    public ICollection<DteParticipante> Participantes { get; set; } =
        new List<DteParticipante>();

    public ArchivoDte? Archivo { get; set; }
}