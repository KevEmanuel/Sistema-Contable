using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace SistemaContableDTE.Models;
//clase para contabilizar la regla de partidas
public class ReglaContable
{
    [Key]
    public int ReglaContableId{get;set;}

    [Required]
    public int EmpresaId{get;set;}

    [Required]
    [StringLength(200)]
    public string Nombre {get;set;}=string.Empty;

    [StringLength(5)]
    public string? TipoDte{get;set;}
    public RolEmpresaDte? RolEmpresa{get;set;}

    [Required]
    public string CondicionJson{get;set;} =string.Empty;
    public DateTime  VigenciDesde {get;set;}

    public DateTime? VigenciaHasta{get;set;}

    public int Version {get;set;}

    public bool Activa {get;set;}
    public Empresa? Empresa{get;set;}
    //relación con la clase Regla cuenta
    public ICollection<ReglaCuenta> Cuentas{get;set;}= new List<ReglaCuenta>();


}