using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class PartidaContable
{
    [Key]
    public int PartidaContableId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required]
    public int PeriodoContableId { get; set; }

    public int? DocumentoDteId { get; set; }

    public int? ReglaContableId { get; set; }

    public int NumeroPartida { get; set; }

    public DateTime Fecha { get; set; }

    [Required]
    [StringLength(1000)]
    public string Glosa { get; set; } = string.Empty;

    public TipoOrigenPartida TipoOrigen { get; set; }

    public EstadoPartidaContable Estado { get; set; } =
        EstadoPartidaContable.Borrador;

    public int? PartidaReversaId { get; set; }

    [Required]
    public int CreadoPorId { get; set; }

    public int? AprobadoPorId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    public Empresa? Empresa { get; set; }

    public periodoContable? periodoContable { get; set; }

    public DocumentoDte? DocumentoDte { get; set; }

    public ReglaContable? ReglaContable { get; set; }

    public PartidaContable? PartidaReversa { get; set; }

//relacion con la clase detallePartida 
    public ICollection<DetallePartida> Detalles { get; set; } =
        new List<DetallePartida>();
}