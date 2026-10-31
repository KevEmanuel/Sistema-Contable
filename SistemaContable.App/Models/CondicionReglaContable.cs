namespace SistemaContableDTE.Models;

public enum CampoCondicionRegla
{
    TotalOperacion,
    BaseGravada,
    Iva,
    MontoExento,
    MontoNoSujeto
}

public enum OperadorCondicionRegla
{
    Igual,
    MayorQue,
    MayorOIgual,
    MenorQue,
    MenorOIgual
}

public sealed class CondicionReglaContable
{
    public CampoCondicionRegla Campo { get; set; }
    public OperadorCondicionRegla Operador { get; set; }
    public decimal Valor { get; set; }
}

public sealed class DefinicionCondicionesRegla
{
    public int Version { get; set; } = 1;
    public string OperadorLogico { get; set; } = "Y";
    public List<CondicionReglaContable> Condiciones { get; set; } = new();
}
