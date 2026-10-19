using System;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace SistemaContableDTE.Models;
//clase para la Evidencia Fiscal del documento DTE
public class EvidenciaFiscal
{
    [Key]
    public int EvidenciaFiscalId{get;set;}
    [Required]
    public int DocumentoDteId {get;set;}
    public FuenteEvidenciaFiscal Fuente{get;set;}

    [StringLength(100)]
    public string? SelloRecibido{get;set;}
    
    [StringLength(50)]
    public string? EstadoHacienda {get;set;}
    
    public DateTime? FechaProcesamiento{get;set;}

    [StringLength(50)]
    public string? CodigoMensaje {get;set;}

    [StringLength(1000)]
    public string?DescripcionMensaje{get;set;}

    public string? Observaciones{get;set;}
    public string? RespuestaJson{get;set;}

    public DateTime CreadoEn {get;set;}=DateTime.Now;
    public DocumentoDte? DocumentoDte {get;set;}
 
}
