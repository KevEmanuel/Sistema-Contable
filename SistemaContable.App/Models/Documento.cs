using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    [NotMapped]
    public OperacionContableDte OperacionContable =>
        CatalogoTiposDte.ClasificarOperacion(TipoDte, RolEmpresa);

    [NotMapped]
    public string TipoDteDescripcion =>
        CatalogoTiposDte.Obtener(TipoDte)?.Descripcion ??
        $"DTE tipo {TipoDte}";

    [NotMapped]
    public string ClasificacionContableDescripcion =>
        RolEmpresa == RolEmpresaDte.NoDeterminado
            ? "Requiere revisar emisor/receptor"
            : OperacionContable switch
            {
                OperacionContableDte.Venta => "Venta",
                OperacionContableDte.Compra => "Compra",
                _ => "DTE especial - revisar"
            };

    [NotMapped]
    public string OperacionContableDescripcion
    {
        get
        {
            if (RolEmpresa == RolEmpresaDte.NoDeterminado)
            {
                return "Asociar cliente";
            }

            return OperacionContable switch
            {
                OperacionContableDte.Venta => "Venta",
                OperacionContableDte.Compra => "Compra",
                _ => "DTE especial - revisar"
            };
        }
    }

    [NotMapped]
    public string EmisorDescripcion =>
        CrearDescripcionParticipante(RolParticipanteDte.Emisor);

    [NotMapped]
    public string ReceptorDescripcion =>
        CrearDescripcionParticipante(RolParticipanteDte.Receptor);

    public int VersionSchema { get; set; }

    public DateTime FechaEmision { get; set; }

    [StringLength(10)]
    public string? HoraEmision { get; set; }

    public RolEmpresaDte RolEmpresa { get; set; }

    public EstadoDocumentoDte Estado { get; set; } =
        EstadoDocumentoDte.Importado;

    public int? DocumentoRelacionadoId { get; set; }

    public int? ClienteContableId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.Now;

    public Empresa? Empresa { get; set; }

    public ClienteContable? ClienteContable { get; set; }

    public DocumentoDte? DocumentoRelacionado { get; set; }

    public ICollection<DteParticipante> Participantes { get; set; } =
        new List<DteParticipante>();

    public ArchivoDte? Archivo { get; set; }

    public ResumenTributario? ResumenTributario { get; set; }

    public ICollection<ResultadoValidacionDte> ResultadosValidacion { get; set; } =
        new List<ResultadoValidacionDte>();

    private string CrearDescripcionParticipante(RolParticipanteDte rol)
    {
        DteParticipante? participante = Participantes
            .FirstOrDefault(item => item.Rol == rol);
        if (participante is null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(participante.NitSnapshot)
            ? participante.NombreSnapshot
            : $"{participante.NombreSnapshot} ({participante.NitSnapshot})";
    }
}