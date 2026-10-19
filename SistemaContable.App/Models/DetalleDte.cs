using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
namespace SistemaContableDTE.Models;


//clase que contiene el detalle del DTE
public class DetalleDTe
{
    [Key]
    public  int DetalleDTeId{get;set;}
    [Required]
    public int DocumentoDteId{get;set;}

    public int NumeroItem{get;set;}
    public int? TipoItem {get;set;}

    [Required]
    public decimal Cantidad {get;set;}
    [StringLength(500)]
    public string Descripcion{get;set;}=string.Empty;

    public decimal PrecioUnitario {get;set;}
    public decimal Descuento{get;set;}
    public decimal VentaGravada{get;set;}
    public decimal VentaExenta {get;set;}
    public decimal VentaNoSujeta{get;set;}
    public DocumentoDte? DocumentoDte{get;set;}
}
