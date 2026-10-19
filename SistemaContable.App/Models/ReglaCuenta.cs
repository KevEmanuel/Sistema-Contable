using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;

public class ReglaCuenta
{
    [Key]
    public int ReglaCuentaId{get;set;}

    [Required]

    public int ReglaContableId{get;set;}

    [Required]
    public int CuentaContableId{get;set;}
    public LadoContable Lado{get;set;}

    [Required]
    [StringLength(500)]
    public string Formula{get;set}= string.Empty;

    public int Orden{get;set;}

    public ReglaContable? ReglaContable{get;set;}
    public CuentaContable? CuentaContable {get;set;}

}