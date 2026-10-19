using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;

public class Empresa
{
    [Key]
    public int EmpresaId{get; set;}
    [Required]
    [StringLength(250)]
    public string NombreRazonSocial
    {get;set;}= string.Empty;

    [StringLength(250)]
    public string? NombreComercial{get;set;}
    
    [Required]
    [StringLengthAttribute(17)]
    public string Nit{get;set;}=string.Empty;
    [StringLength(10)]
    public string?NRC{get;set;}

    [StringLength(10)]
    public string? CodigoActividad{get;set;}
    [StringLength(250)]
    public string? DescripcionActividad {get;set;}
    [StringLength(100)]
    public string? Departamento{get;set;}

    [StringLength(500)]
    public string? Direccion {get;set;}

    [StringLength(25)]
    public string? Telegono {get;set;}
    [EmailAddress]
    [StringLength(150)]
    public string? Correo{get;set;}

    public bool Activa {get;set;} = true;

    public DateTime fechaCreacion {get;set;} = DateTime.Now;

}
