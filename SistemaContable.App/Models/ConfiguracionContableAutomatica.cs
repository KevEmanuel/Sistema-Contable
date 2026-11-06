using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaContableDTE.Models;

public sealed class ConfiguracionContableAutomatica
{
    [Key]
    public int ConfiguracionContableAutomaticaId { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required]
    [StringLength(5)]
    public string TipoDte { get; set; } = string.Empty;

    public OperacionConfiguracionDte Operacion { get; set; } =
        OperacionConfiguracionDte.RevisionManual;

    [NotMapped]
    public string OperacionDescripcion => Operacion switch
    {
        OperacionConfiguracionDte.PorRolEmpresa => "Automática por rol (01/03)",
        OperacionConfiguracionDte.Venta => "Venta",
        OperacionConfiguracionDte.Compra => "Compra",
        OperacionConfiguracionDte.RevisionManual => "Revisión manual",
        _ => "No definida"
    };

    public int? CuentaClientesId { get; set; }
    public CuentaContable? CuentaClientes { get; set; }

    public int? CuentaVentasId { get; set; }
    public CuentaContable? CuentaVentas { get; set; }

    public int? CuentaIvaDebitoId { get; set; }
    public CuentaContable? CuentaIvaDebito { get; set; }

    public int? CuentaProveedoresId { get; set; }
    public CuentaContable? CuentaProveedores { get; set; }

    public int? CuentaComprasId { get; set; }
    public CuentaContable? CuentaCompras { get; set; }

    public int? CuentaIvaAcreditableId { get; set; }
    public CuentaContable? CuentaIvaAcreditable { get; set; }
}
