using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public string Formula{get;set;}= string.Empty;

    public int Orden{get;set;}

    public ReglaContable? ReglaContable{get;set;}
    public CuentaContable? CuentaContable {get;set;}

    [NotMapped]
    public string FormulaDescripcion
    {
        get
        {
            string formula = Formula.Trim();

            string? descripcion = formula.ToUpperInvariant() switch
            {
                "BASEGRAVADA" => "Base gravada del DTE",
                "MONTOEXENTO" => "Monto exento",
                "MONTONOSUJETO" => "Monto no sujeto",
                "IVA" => "Impuesto (IVA)",
                "TOTALOPERACION" => "Total de la operación",
                _ => null
            };

            if (descripcion is not null)
            {
                return descripcion;
            }

            if (decimal.TryParse(
                    formula,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal importe))
            {
                return $"Importe fijo: {importe:N2}";
            }

            return "Fórmula heredada no disponible en el formulario";
        }
    }
}