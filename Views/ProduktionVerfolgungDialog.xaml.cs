using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class ProduktionVerfolgungDialog : Window
    {
        private readonly Auftrag _auftrag;
        private readonly int _aktuellesJahr;
        private int _ausgewaehlteKalenderWoche;

        public ProduktionVerfolgungDialog(Auftrag auftrag)
        {
            InitializeComponent();
            _auftrag = auftrag;
            _aktuellesJahr = DateTime.Now.Year;
            _ausgewaehlteKalenderWoche = ISOWeek.GetWeekOfYear(DateTime.Now);
            AuftragTextBlock.Text = _auftrag.Auftragsnummer;
            UpdateKwButtonText();

            UpdateDisplay();
            if (_auftrag.ProduktionStartDatum.HasValue)
            {
                StartButton.IsEnabled = false;
                EndButton.IsEnabled = true;
            }
        }

        private void UpdateDisplay()
        {
            if (_auftrag.ProduktionStartDatum.HasValue)
            {
                StartZeitTextBlock.Text = _auftrag.ProduktionStartDatum.Value.ToString("dd.MM.yyyy HH:mm:ss");
                StartZeitTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }

            if (_auftrag.ProduktionEndDatum.HasValue)
            {
                EndZeitTextBlock.Text = _auftrag.ProduktionEndDatum.Value.ToString("dd.MM.yyyy HH:mm:ss");
                EndZeitTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                DauerTextBlock.Text = $"Dauer: {_auftrag.ProduktionsDauer}";
            }
        }

        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            if (_auftrag.ProduktionStartDatum != null)
            {
                MessageBox.Show("Produktion wurde bereits gestartet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!CanStartProductionWithCompletePdfs(out var fehlendePdfsText))
            {
                MessageBox.Show(
                    $"Produktion kann nicht gestartet werden. Für diesen Auftrag fehlen PDF-Dateien:\n\n{fehlendePdfsText}",
                    "PDF-Pflicht vor Start",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _auftrag.ProduktionStartDatum = DateTime.Now;
            _auftrag.Status = AuftragStatus.InBearbeitung;
            StartButton.IsEnabled = false;
            EndButton.IsEnabled = true;
            UpdateDisplay();
            SaveChanges();

            AuditLogService.LogAction(
                OperatorIdentityService.CurrentOperatorName,
                "START",
                "Auftrag",
                _auftrag.Auftragsnummer,
                oldValue: AuftragStatus.Offen.ToString(),
                newValue: AuftragStatus.InBearbeitung.ToString(),
                reason: "Produktion gestartet");
        }

        private void OnEndClick(object sender, RoutedEventArgs e)
        {
            if (_auftrag.ProduktionStartDatum == null)
            {
                MessageBox.Show("Bitte zuerst die Produktion starten.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _auftrag.ProduktionEndDatum = DateTime.Now;
            _auftrag.Status = AuftragStatus.Abgeschlossen;
            EndButton.IsEnabled = false;
            UpdateDisplay();
            SaveChanges();

            var archivResult = AuftragArchivService.ArchiveCompletedOrder(_auftrag, _ausgewaehlteKalenderWoche, _aktuellesJahr);
            if (!archivResult.Success)
            {
                MessageBox.Show(archivResult.Message, "Archivierung", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            AuditLogService.LogAction(
                OperatorIdentityService.CurrentOperatorName,
                "STOP",
                "Auftrag",
                _auftrag.Auftragsnummer,
                oldValue: AuftragStatus.InBearbeitung.ToString(),
                newValue: AuftragStatus.Abgeschlossen.ToString(),
                reason: archivResult.Success
                    ? $"Produktion abgeschlossen. {archivResult.Message}"
                    : $"Produktion abgeschlossen, Archivierung fehlgeschlagen: {archivResult.Message}");
        }

        private void SaveChanges()
        {
            try
            {
                using (var context = new MaterialManagerDbContext())
                {
                    var existingAuftrag = context.Auftraege.Find(_auftrag.Id);
                    if (existingAuftrag == null && !string.IsNullOrWhiteSpace(_auftrag.Auftragsnummer))
                    {
                        existingAuftrag = context.Auftraege
                            .FirstOrDefault(a => a.Auftragsnummer == _auftrag.Auftragsnummer);
                    }

                    if (existingAuftrag == null)
                    {
                        existingAuftrag = new Auftrag
                        {
                            Auftragsnummer = _auftrag.Auftragsnummer,
                            ErstelltAm = _auftrag.ErstelltAm,
                            AngelegtVon = _auftrag.AngelegtVon
                        };
                        context.Auftraege.Add(existingAuftrag);
                    }

                    existingAuftrag.ProduktionStartDatum = _auftrag.ProduktionStartDatum;
                    existingAuftrag.ProduktionEndDatum = _auftrag.ProduktionEndDatum;
                    existingAuftrag.Status = _auftrag.Status;
                    existingAuftrag.GeaendertAm = DateTime.Now;
                    existingAuftrag.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                    context.SaveChanges();
                    _auftrag.Id = existingAuftrag.Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnKwAuswahlClick(object sender, RoutedEventArgs e)
        {
            var archivDialog = new ArchivAuftraegeDialog(_ausgewaehlteKalenderWoche, _aktuellesJahr)
            {
                Owner = this
            };
            archivDialog.ShowDialog();
        }

        private void OnKwAuswahlItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not int kw)
                return;

            _ausgewaehlteKalenderWoche = kw;
            UpdateKwButtonText();

            var archivDialog = new ArchivAuftraegeDialog(_ausgewaehlteKalenderWoche, _aktuellesJahr)
            {
                Owner = this
            };
            archivDialog.ShowDialog();
        }

        private void UpdateKwButtonText()
        {
            KwAuswahlButton.Content = $"KW {_ausgewaehlteKalenderWoche:D2} ▾";
            KwAuswahlButton.ToolTip = $"Archivansicht für Kalenderwoche {_ausgewaehlteKalenderWoche:D2} im Jahr {_aktuellesJahr}";
        }

        private bool CanStartProductionWithCompletePdfs(out string fehlendePdfsText)
        {
            fehlendePdfsText = string.Empty;

            var materialien = MaterialDataService.LoadAllMaterials()
                .Where(m => string.Equals(m.AuftragNr, _auftrag.Auftragsnummer, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (materialien.Count == 0)
            {
                fehlendePdfsText = "Keine zugeordneten Materialpositionen gefunden.";
                return false;
            }

            var fehlende = AuftragRulesService.GetMaterialsWithoutValidPdf(materialien)
                .Select(m => $"- {m.MaterialArt} {m.Mass} (Restnummer: {m.Restnummer})")
                .ToList();

            if (fehlende.Count == 0)
                return true;

            fehlendePdfsText = string.Join("\n", fehlende);
            return false;
        }

        private static bool HasExistingPdf(MaterialItem material)
        {
            return AuftragRulesService.HasExistingPdf(material);
        }
    }
}
