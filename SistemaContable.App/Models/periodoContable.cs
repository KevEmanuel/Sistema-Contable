using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaContableDTE.Models;
public class periodoContable
{

    [Key]
    public int PeriodoContableId{get;set;}

    [Required]
    public int EmpresaId {get;set;}

    [Required]
    [StringLength(100)]
    public string Nombre {get;set;}=string.Empty;
    public  DateTime FechaInicio { get;set;}
    public  DateTime FechaFin {get;set;}
    public bool EstaCerrado {get;set;}
    public DateTime FechaCreación {get;set;}

    //relación de un periodo pertenece a uma empresa
    public Empresa? Empresa {get;set;}
}
