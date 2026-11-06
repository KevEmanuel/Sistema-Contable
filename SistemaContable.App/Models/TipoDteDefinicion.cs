namespace SistemaContableDTE.Models;

public enum OperacionContableDte
{
    RequiereRevision,
    Venta,
    Compra
}

public sealed record TipoDteDefinicion(
    string Codigo,
    string Descripcion);

public static class CatalogoTiposDte
{
    public static IReadOnlyList<TipoDteDefinicion> ParaReglas { get; } =
    [
        new(string.Empty, "Todos los tipos de DTE"),
        new("01", "Factura"),
        new("03", "Comprobante de crédito fiscal"),
        new("04", "Nota de remisión"),
        new("05", "Nota de crédito"),
        new("06", "Nota de débito"),
        new("07", "Comprobante de retención"),
        new("08", "Comprobante de liquidación"),
        new("09", "Documento contable de liquidación"),
        new("11", "Factura de exportación"),
        new("14", "Factura de sujeto excluido"),
        new("15", "Comprobante de donación")
    ];

    public static TipoDteDefinicion? Obtener(string? codigo) =>
        string.IsNullOrWhiteSpace(codigo)
            ? null
            : ParaReglas.FirstOrDefault(tipo =>
                !string.IsNullOrEmpty(tipo.Codigo) &&
                string.Equals(
                    tipo.Codigo,
                    codigo.Trim(),
                    StringComparison.Ordinal));

    public static OperacionContableDte ClasificarOperacion(
        string? tipoDte,
        RolEmpresaDte rolEmpresa)
    {
        if (tipoDte?.Trim() is not ("01" or "03"))
        {
            return OperacionContableDte.RequiereRevision;
        }

        return rolEmpresa switch
        {
            RolEmpresaDte.Emisor => OperacionContableDte.Venta,
            RolEmpresaDte.Receptor => OperacionContableDte.Compra,
            _ => OperacionContableDte.RequiereRevision
        };
    }
}
