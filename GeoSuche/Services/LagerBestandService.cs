using System.IO;
using ClosedXML.Excel;
using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public class LagerBestandService : ILagerBestandService
{
    public IReadOnlyList<LagerBestandItem> SucheNachZeichnungsnummer(string excelPfad, string suchbegriff)
    {
        if (!File.Exists(excelPfad))
            throw new FileNotFoundException("Excel-Datei nicht gefunden.", excelPfad);

        var term = (suchbegriff ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(term))
            return [];

        using var wb = new XLWorkbook(excelPfad);
        var ws = wb.Worksheets.FirstOrDefault()
                 ?? throw new InvalidOperationException("Die Excel-Datei enthält kein Arbeitsblatt.");

        var usedRange = ws.RangeUsed()
                        ?? throw new InvalidOperationException("Die Excel-Datei ist leer.");

        var headerRow = usedRange.FirstRow();
        var drawingCol = FindColumn(headerRow, "zeichnungsnummer", "zeichnung", "drawing");
        if (drawingCol == 0)
            throw new InvalidOperationException("Spalte für Zeichnungsnummer wurde nicht gefunden.");

        var materialCol = FindColumn(headerRow, "material");
        var bestandCol = FindColumn(headerRow, "bestand", "lagerbestand", "menge", "qty");

        var list = new List<LagerBestandItem>();

        foreach (var row in usedRange.RowsUsed().Skip(1))
        {
            var zeichnung = row.Cell(drawingCol).GetString().Trim();
            if (string.IsNullOrWhiteSpace(zeichnung))
                continue;

            if (!zeichnung.Contains(term, StringComparison.OrdinalIgnoreCase))
                continue;

            var material = materialCol > 0 ? row.Cell(materialCol).GetString().Trim() : string.Empty;
            var bestandText = bestandCol > 0 ? row.Cell(bestandCol).GetString().Trim() : string.Empty;

            var status = "Nicht vorhanden";
            if (TryParseDecimal(bestandText, out var qty) && qty > 0)
                status = "Vorhanden";

            list.Add(new LagerBestandItem
            {
                Zeichnungsnummer = zeichnung,
                Material = material,
                Bestand = bestandText,
                Status = status,
            });
        }

        return list;
    }

    private static int FindColumn(IXLRangeRow headerRow, params string[] candidates)
    {
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(header))
                continue;

            foreach (var c in candidates)
            {
                if (header.Contains(c, StringComparison.OrdinalIgnoreCase))
                    return cell.Address.ColumnNumber;
            }
        }

        return 0;
    }

    private static bool TryParseDecimal(string input, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
