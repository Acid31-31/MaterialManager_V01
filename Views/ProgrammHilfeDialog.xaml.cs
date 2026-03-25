using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MaterialManager_V01.Views
{
    public partial class ProgrammHilfeDialog : Window
    {
        public ProgrammHilfeDialog()
        {
            InitializeComponent();
            HelpTextBox.Text = """
1) PROGRAMMBESCHREIBUNG

MaterialManager V01 verwaltet Bleche, Rohre und Profile in einem gemeinsamen Lagerbestand.
Die wichtigsten Bereiche:
- Hauptprogramm: Gesamtübersicht, Suche, Filter, Bearbeitung, Auswertungen.
- Lager: Wareneingang, Regalauslastung, Inventur, niedrige Bestände.
- Tafelplanung: Material für Aufträge reservieren und freigeben.
- Laser: Nur reservierte Materialien sehen, bearbeiten oder löschen.


2) INSTALLATION AUF MEHREREN PCS

Empfohlene Reihenfolge:
1. Auf einem PC Programm normal installieren (Referenz-PC).
2. Auf jedem weiteren PC die gleiche Programmversion installieren.
3. Auf allen PCs denselben Netzwerk-Speicherpfad für die Excel-Datei konfigurieren
   (Menü: Datei -> Netzwerk-Einstellungen).
4. Alle PCs neu starten und einen Test mit einem neuen Materialeintrag durchführen.

Wichtig:
- Alle PCs müssen Zugriff auf denselben Netzwerkordner haben.
- Benutzer benötigen Lese-/Schreibrechte auf den Ordner.
- Uhrzeit/Datum sollten auf allen PCs korrekt sein.


3) NETZWERKVERBINDUNG – GLEICHZEITIGER BETRIEB

Damit alle Programme gleichzeitig funktionieren:
- Es darf nur EINE gemeinsame Excel-Datei genutzt werden.
- Jeder PC muss in den Netzwerk-Einstellungen exakt denselben Pfad verwenden.
- Beispielpfad: \\SERVER\MaterialManager\Daten\Materialien.xlsx

Empfehlung:
- Ablage auf Server/NAS (nicht lokal auf C: eines einzelnen PCs).
- Netzwerkfreigabe stabil und dauerhaft verfügbar halten.
- Bei Verbindungsabbrüchen zuerst Freigabe und Rechte prüfen.


4) WO SOLL DIE EXCEL-DATEI GESPEICHERT WERDEN?

Richtig:
- In einem zentralen Netzwerkordner, den alle beteiligten PCs erreichen können.
- Beispiel:
  \\SERVER\MaterialManager\Daten\Materialien.xlsx

Nicht empfohlen:
- Lokaler Desktop oder Dokumente eines einzelnen Rechners.
- USB-Stick als dauerhafte Live-Datenquelle.


5) PRAKTISCHER SCHNELLTEST NACH DER EINRICHTUNG

1. PC A: Material anlegen und speichern.
2. PC B: Aktualisieren klicken -> Material muss sichtbar sein.
3. PC C (Laser/Tafel): Reservieren/Ändern.
4. PC A/B: Änderung muss nach Aktualisieren sichtbar sein.

Wenn das funktioniert, ist der Mehr-PC-Betrieb korrekt eingerichtet.


6) FEHLERSUCHE (KURZ)

- Änderungen nicht sichtbar?
  -> Auf "Aktualisieren" klicken und Netzwerkpfad vergleichen.
- Speichern schlägt fehl?
  -> Schreibrechte im Freigabeordner prüfen.
- Unterschiedliche Datenstände?
  -> Prüfen, ob wirklich alle PCs dieselbe Excel-Datei benutzen.


Hinweis:
Diese Hilfe beschreibt den empfohlenen Standardbetrieb mit gemeinsamer Excel-Datei im Netzwerk.
""";
        }

        private FlowDocument CreateHelpDocument(double columnWidth)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12,
                ColumnWidth = columnWidth
            };

            document.Blocks.Add(new Paragraph(new Bold(new Run("MaterialManager V01 – Programm- & Netzwerkanleitung")))
            {
                Margin = new Thickness(0, 0, 0, 14),
                FontSize = 14
            });

            document.Blocks.Add(new Paragraph(new Run(HelpTextBox.Text))
            {
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0)
            });

            return document;
        }

        private void OnSavePdf(object sender, RoutedEventArgs e)
        {
            try
            {
                using var server = new LocalPrintServer();
                var pdfQueue = server.GetPrintQueues()
                    .FirstOrDefault(q => q.Name.Contains("Microsoft Print to PDF", System.StringComparison.OrdinalIgnoreCase));

                if (pdfQueue == null)
                {
                    MessageBox.Show("Der Drucker 'Microsoft Print to PDF' wurde nicht gefunden.", "PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var document = CreateHelpDocument(768);
                var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;

                var writer = PrintQueue.CreateXpsDocumentWriter(pdfQueue);
                writer.Write(paginator);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"PDF-Erstellung fehlgeschlagen:\n{ex.Message}", "PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPrint(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() != true)
                    return;

                var document = CreateHelpDocument(printDialog.PrintableAreaWidth);
                var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                printDialog.PrintDocument(paginator, "Programm- und Netzwerkanleitung");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Drucken fehlgeschlagen:\n{ex.Message}", "Drucken", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
