using System.IO;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaContableDTE.Services;

public static class ExportadorLibros
{
    private const double AnchoPaginaA4Horizontal = 842;

    static ExportadorLibros()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void Guardar(
        string ruta,
        string titulo,
        string nombreHoja,
        IReadOnlyList<string> encabezados,
        IEnumerable<IEnumerable<object?>> filas)
    {
        if (encabezados.Count == 0)
        {
            throw new ArgumentException(
                "La exportación debe incluir al menos una columna.",
                nameof(encabezados));
        }

        List<object?[]> datos = filas
            .Select(fila => fila.ToArray())
            .ToList();

        switch (Path.GetExtension(ruta).ToLowerInvariant())
        {
            case ".xlsx":
                GuardarExcel(ruta, titulo, nombreHoja, encabezados, datos);
                break;
            case ".pdf":
                GuardarPdf(ruta, titulo, encabezados, datos);
                break;
            case ".csv":
                GuardarCsv(ruta, encabezados, datos);
                break;
            default:
                throw new InvalidOperationException(
                    "Selecciona un archivo Excel, PDF o CSV.");
        }
    }

    private static void GuardarExcel(
        string ruta,
        string titulo,
        string nombreHoja,
        IReadOnlyList<string> encabezados,
        IReadOnlyList<object?[]> filas)
    {
        using var libro = new XLWorkbook();
        IXLWorksheet hoja = libro.Worksheets.Add(
            nombreHoja.Length > 31 ? nombreHoja[..31] : nombreHoja);

        hoja.Cell(1, 1).Value = titulo;
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Range(1, 1, 1, encabezados.Count).Merge();

        for (int columna = 0; columna < encabezados.Count; columna++)
        {
            IXLCell celda = hoja.Cell(3, columna + 1);
            celda.Value = encabezados[columna];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int indiceFila = 0; indiceFila < filas.Count; indiceFila++)
        {
            object?[] fila = filas[indiceFila];

            for (int columna = 0;
                 columna < Math.Min(fila.Length, encabezados.Count);
                 columna++)
            {
                IXLCell celda = hoja.Cell(indiceFila + 4, columna + 1);

                switch (fila[columna])
                {
                    case null:
                        celda.Value = string.Empty;
                        break;
                    case DateTime fecha:
                        celda.Value = fecha;
                        celda.Style.DateFormat.Format = "dd/mm/yyyy";
                        break;
                    case decimal monto:
                        celda.Value = monto;
                        celda.Style.NumberFormat.Format = "#,##0.00";
                        break;
                    case int entero:
                        celda.Value = entero;
                        break;
                    case Enum enumeracion:
                        celda.Value = enumeracion.ToString();
                        break;
                    default:
                        celda.Value = Convert.ToString(
                            fila[columna],
                            CultureInfo.InvariantCulture) ?? string.Empty;
                        break;
                }
            }
        }

        hoja.SheetView.FreezeRows(3);
        hoja.Range(3, 1, Math.Max(3, filas.Count + 3), encabezados.Count)
            .SetAutoFilter();
        hoja.Columns(1, encabezados.Count).AdjustToContents();

        foreach (IXLColumn columna in hoja.Columns(1, encabezados.Count))
        {
            columna.Width = Math.Min(columna.Width, 50);
        }

        libro.SaveAs(ruta);
    }

    private static void GuardarCsv(
        string ruta,
        IReadOnlyList<string> encabezados,
        IReadOnlyList<object?[]> filas)
    {
        IEnumerable<string> lineas = new[]
            {
                string.Join(";", encabezados.Select(EscaparCsv))
            }
            .Concat(filas.Select(fila => string.Join(
                ";",
                fila.Take(encabezados.Count).Select(valor =>
                    EscaparCsv(FormatearValor(valor))))));

        File.WriteAllText(
            ruta,
            string.Join(Environment.NewLine, lineas),
            new UTF8Encoding(true));
    }

    private static void GuardarPdf(
        string ruta,
        string titulo,
        IReadOnlyList<string> encabezados,
        IReadOnlyList<object?[]> filas)
    {
        double[] anchosColumnas = CalcularAnchosColumnas(
            encabezados,
            filas);
        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(24);
            pagina.Header().Column(columna =>
            {
                columna.Item().Text(titulo).FontSize(12).Bold();
                columna.Item()
                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(7);
            });
            pagina.Content()
                .PaddingTop(12)
                .Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnas =>
                    {
                        foreach (double ancho in anchosColumnas)
                        {
                            columnas.RelativeColumn((float)ancho);
                        }
                    });

                    tabla.Header(encabezado =>
                    {
                        foreach (string valor in encabezados)
                        {
                            encabezado.Cell()
                                .Element(EstiloEncabezado)
                                .Text(valor)
                                .FontSize(7)
                                .Bold();
                        }
                    });

                    foreach (object?[] fila in filas)
                    {
                        for (int indice = 0;
                             indice < encabezados.Count;
                             indice++)
                        {
                            tabla.Cell()
                                .Element(EstiloCelda)
                                .Text(FormatearValor(
                                    indice < fila.Length
                                        ? fila[indice]
                                        : null))
                                .FontSize(7);
                        }
                    }
                });
            pagina.Footer()
                .AlignCenter()
                .Text(texto =>
                {
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                });
        })).GeneratePdf(ruta);
    }

    private static IContainer EstiloCelda(IContainer contenedor)
    {
        return contenedor
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(3);
    }

    private static IContainer EstiloEncabezado(IContainer contenedor)
    {
        return EstiloCelda(contenedor).Background(Colors.Grey.Lighten2);
    }

    private static double[] CalcularAnchosColumnas(
        IReadOnlyList<string> encabezados,
        IReadOnlyList<object?[]> filas)
    {
        double anchoDisponible = AnchoPaginaA4Horizontal - 48;
        double[] pesos = encabezados
            .Select((encabezado, indice) =>
            {
                int maximo = filas
                    .Select(fila => indice < fila.Length
                        ? FormatearValor(fila[indice]).Length
                        : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                return (double)Math.Clamp(
                    Math.Max(encabezado.Length, Math.Min(maximo, 36)),
                    6,
                    36);
            })
            .ToArray();
        double totalPesos = pesos.Sum();

        return pesos
            .Select(peso => anchoDisponible * peso / totalPesos)
            .ToArray();
    }

    private static string FormatearValor(object? valor)
    {
        return valor switch
        {
            null => string.Empty,
            DateTime fecha => fecha.ToString(
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture),
            decimal monto => monto.ToString(
                "0.00",
                CultureInfo.InvariantCulture),
            Enum enumeracion => enumeracion.ToString(),
            _ => Convert.ToString(valor, CultureInfo.InvariantCulture)
                 ?? string.Empty
        };
    }

    private static string EscaparCsv(string valor)
    {
        return valor.Contains(';') ||
               valor.Contains('"') ||
               valor.Contains('\r') ||
               valor.Contains('\n')
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;
    }
}
