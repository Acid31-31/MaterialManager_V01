using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// PDF-Export und Druck-Service
    /// Exportiert Bestandslisten, Reports als PDF
    /// </summary>
    public static class PdfExportService
    {
        /// <summary>
        /// Generiere Bestandslisten-PDF
        /// </summary>
        public static string GenerateBestandslisteAsPdf(
            IEnumerable<MaterialItem> materialien,
            string title = "Materialbestand")
        {
            var items = materialien?.ToList() ?? new List<MaterialItem>();

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 20px;
            background: white;
        }}
        h1 {{
            color: #1976D2;
            text-align: center;
            border-bottom: 3px solid #1976D2;
            padding-bottom: 10px;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
            color: #666;
        }}
        .timestamp {{
            text-align: right;
            font-size: 10pt;
            color: #999;
            margin-bottom: 20px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }}
        th {{
            background-color: #1976D2;
            color: white;
            padding: 10px;
            text-align: left;
            border: 1px solid #ddd;
        }}
        td {{
            padding: 8px;
            border: 1px solid #ddd;
        }}
        tr:nth-child(even) {{
            background-color: #f9f9f9;
        }}
        .total {{
            font-weight: bold;
            background-color: #f0f0f0;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 9pt;
            color: #999;
            text-align: center;
        }}
    </style>
</head>
<body>
    <h1>📊 {title}</h1>
    <div class='timestamp'>
        Erstellt: {DateTime.Now:dd.MM.yyyy HH:mm:ss}
    </div>

    <table>
        <thead>
            <tr>
                <th>Material</th>
                <th>Legierung</th>
                <th>Stärke</th>
                <th>Maß</th>
                <th>Stück</th>
                <th>Gewicht (kg)</th>
                <th>Lagerort</th>
            </tr>
        </thead>
        <tbody>
{string.Join("\n", items.Select(m => $@"
            <tr>
                <td>{m.MaterialArt}</td>
                <td>{m.Legierung}</td>
                <td>{m.Staerke:F1} mm</td>
                <td>{m.Mass}</td>
                <td>{m.Stueckzahl}</td>
                <td>{m.GewichtKg:F2}</td>
                <td>{m.Lagerort}</td>
            </tr>"))}
        </tbody>
        <tfoot>
            <tr class='total'>
                <td colspan='5'><strong>GESAMT:</strong></td>
                <td><strong>{items.Sum(m => m.GewichtKg):F2} kg</strong></td>
                <td></td>
            </tr>
        </tfoot>
    </table>

    <div class='footer'>
        MaterialManager R03 | Professional Bestandsverwaltung
    </div>
</body>
</html>";

            return html;
        }

        /// <summary>
        /// Generiere Lageraustattungs-Report als PDF
        /// </summary>
        public static string GenerateLagerAuslastungAsPdf(
            IEnumerable<MaterialItem> materialien)
        {
            var items = materialien?.ToList() ?? new List<MaterialItem>();

            // Gruppiere nach Lagerort
            var byLagerort = items
                .GroupBy(m => m.Lagerort)
                .OrderBy(g => g.Key)
                .ToList();

            var totalWeight = items.Sum(m => m.GewichtKg);

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Lagerauslastungs-Report</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 20px;
        }}
        h1 {{
            color: #1976D2;
            text-align: center;
            border-bottom: 3px solid #1976D2;
        }}
        .stats {{
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 20px;
            margin: 20px 0;
        }}
        .stat-box {{
            background: #f0f0f0;
            padding: 15px;
            border-radius: 5px;
            border-left: 4px solid #1976D2;
        }}
        .stat-box h3 {{
            margin: 0;
            color: #666;
            font-size: 11pt;
        }}
        .stat-box .value {{
            font-size: 24pt;
            color: #1976D2;
            font-weight: bold;
            margin: 5px 0;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }}
        th {{
            background-color: #1976D2;
            color: white;
            padding: 10px;
            text-align: left;
        }}
        td {{
            padding: 8px;
            border-bottom: 1px solid #ddd;
        }}
        .bar {{
            background: linear-gradient(to right, #4CAF50, #FFC107, #F44336);
            height: 20px;
            border-radius: 3px;
            position: relative;
        }}
        .timestamp {{
            text-align: right;
            font-size: 9pt;
            color: #999;
            margin-bottom: 20px;
        }}
    </style>
</head>
<body>
    <h1>📊 Lagerauslastungs-Report</h1>
    <div class='timestamp'>{DateTime.Now:dd.MM.yyyy HH:mm:ss}</div>

    <div class='stats'>
        <div class='stat-box'>
            <h3>Gesamtgewicht</h3>
            <div class='value'>{totalWeight:F1} kg</div>
        </div>
        <div class='stat-box'>
            <h3>Unterschiedliche Materialien</h3>
            <div class='value'>{items.Count}</div>
        </div>
        <div class='stat-box'>
            <h3>Verwendete Lagerorte</h3>
            <div class='value'>{byLagerort.Count}</div>
        </div>
    </div>

    <h2>Auslastung pro Lagerort:</h2>
    <table>
        <thead>
            <tr>
                <th>Lagerort</th>
                <th>Gewicht (kg)</th>
                <th>% der Gesamtmenge</th>
            </tr>
        </thead>
        <tbody>
{string.Join("\n", byLagerort.Select(g => {
    var weight = g.Sum(m => m.GewichtKg);
    var percent = totalWeight > 0 ? (weight / totalWeight) * 100 : 0;
    return $@"
            <tr>
                <td><strong>{g.Key}</strong></td>
                <td>{weight:F2} kg</td>
                <td>
                    <div class='bar' style='width: {percent:F0}%'></div>
                    {percent:F1}%
                </td>
            </tr>";
}))}
        </tbody>
    </table>
</body>
</html>";

            return html;
        }

        /// <summary>
        /// HTML zu PDF konvertieren (Export als HTML dann manuell zu PDF öffnen)
        /// ODER: iText für PDF direkt
        /// </summary>
        public static void SaveHtmlToPdf(string html, string filePath)
        {
            try
            {
                // Speichere als HTML (Browser kann zu PDF drucken)
                var fileName = Path.ChangeExtension(filePath, ".html");
                File.WriteAllText(fileName, html);

                // Öffne im Browser/PDF-Viewer
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fehler beim Speichern: {ex.Message}");
            }
        }

        /// <summary>
        /// Audit-Log als HTML exportieren
        /// </summary>
        public static string GenerateAuditLogAsHtml(int lastNDays = 30)
        {
            var entries = AuditLogService.GetAuditLog(lastNDays);

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Audit-Log</title>
    <style>
        body {{
            font-family: 'Courier New', monospace;
            margin: 20px;
            background: white;
        }}
        h1 {{
            color: #1976D2;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            font-size: 9pt;
        }}
        th {{
            background-color: #1976D2;
            color: white;
            padding: 8px;
            text-align: left;
            border: 1px solid #ddd;
        }}
        td {{
            padding: 6px;
            border: 1px solid #ddd;
        }}
        tr:nth-child(even) {{
            background-color: #f9f9f9;
        }}
        .CREATE {{ color: green; font-weight: bold; }}
        .UPDATE {{ color: orange; font-weight: bold; }}
        .DELETE {{ color: red; font-weight: bold; }}
        .LOGIN {{ color: blue; }}
    </style>
</head>
<body>
    <h1>📋 Audit-Log (Letzte {lastNDays} Tage)</h1>
    <p><strong>Erstellt:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>

    <table>
        <thead>
            <tr>
                <th>Zeitstempel</th>
                <th>Benutzer</th>
                <th>Aktion</th>
                <th>Tabelle</th>
                <th>Record ID</th>
                <th>Grund</th>
            </tr>
        </thead>
        <tbody>
{string.Join("\n", entries.Take(500).Select(e => $@"
            <tr>
                <td>{e.Timestamp:dd.MM.yyyy HH:mm:ss}</td>
                <td>{e.Username}</td>
                <td><span class='{e.Action}'>{e.Action}</span></td>
                <td>{e.TableName}</td>
                <td>{e.RecordId}</td>
                <td>{e.Reason}</td>
            </tr>"))}
        </tbody>
    </table>
</body>
</html>";

            return html;
        }
    }
}
