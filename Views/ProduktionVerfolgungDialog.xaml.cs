using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;
using Microsoft.Win32;

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
            _auftrag.Auftragsnummer = (_auftrag.Auftragsnummer ?? string.Empty).Trim();
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

            var begruendungDialog = new ProduktionsBegruendungDialog { Owner = this };
            if (begruendungDialog.ShowDialog() != true)
                return;

            _auftrag.ProduktionEndDatum = DateTime.Now;
            _auftrag.Status = AuftragStatus.Abgeschlossen;
            EndButton.IsEnabled = false;
            UpdateDisplay();
            SaveChanges();

            var archivResult = AuftragArchivService.ArchiveCompletedOrder(
                _auftrag,
                _ausgewaehlteKalenderWoche,
                _aktuellesJahr,
                begruendungDialog.Kommentar);
            if (!archivResult.Success)
            {
                MessageBox.Show(archivResult.Message, "Archivierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            HandleCompletedOrderMaterials();

            AuditLogService.LogAction(
                OperatorIdentityService.CurrentOperatorName,
                "STOP",
                "Auftrag",
                _auftrag.Auftragsnummer,
                oldValue: AuftragStatus.InBearbeitung.ToString(),
                newValue: AuftragStatus.Abgeschlossen.ToString(),
                reason: $"Produktion abgeschlossen. {archivResult.Message}");
        }

        private void SaveChanges()
        {
            try
            {
                using (var context = new MaterialManagerDbContext())
                {
                    var normalizedOrderNo = (_auftrag.Auftragsnummer ?? string.Empty).Trim();

                    var existingAuftrag = context.Auftraege.Find(_auftrag.Id);
                    if (existingAuftrag == null && !string.IsNullOrWhiteSpace(normalizedOrderNo))
                    {
                        existingAuftrag = context.Auftraege
                            .AsEnumerable()
                            .FirstOrDefault(a => string.Equals((a.Auftragsnummer ?? string.Empty).Trim(), normalizedOrderNo, StringComparison.OrdinalIgnoreCase));
                    }

                    if (existingAuftrag == null)
                    {
                        existingAuftrag = new Auftrag
                        {
                            Auftragsnummer = normalizedOrderNo,
                            ErstelltAm = _auftrag.ErstelltAm,
                            AngelegtVon = _auftrag.AngelegtVon
                        };
                        context.Auftraege.Add(existingAuftrag);
                    }
                    else
                    {
                        existingAuftrag.Auftragsnummer = normalizedOrderNo;
                    }

                    _auftrag.Auftragsnummer = normalizedOrderNo;
                    existingAuftrag.ProduktionStartDatum = _auftrag.ProduktionStartDatum;
                    existingAuftrag.ProduktionEndDatum = _auftrag.ProduktionEndDatum;
                    existingAuftrag.Status = _auftrag.Status;
                    existingAuftrag.PdfPfad = _auftrag.PdfPfad;
                    existingAuftrag.PdfPfadAngefangeneTafel = _auftrag.PdfPfadAngefangeneTafel;
                    existingAuftrag.GeaendertAm = DateTime.Now;
                    existingAuftrag.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                    context.SaveChanges();
                    _auftrag.Id = existingAuftrag.Id;
                    AuftragDataService.TryUpsertSharedAuftrag(existingAuftrag);
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

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnOpenLaserProgramClick(object sender, RoutedEventArgs e)
        {
            var laserWindow = new LaserDemoWindow();
            laserWindow.Show();
            Close();
        }

        private void OnOpenLagerProgramClick(object sender, RoutedEventArgs e)
        {
            var lagerWindow = new LagerDemoWindow();
            lagerWindow.Show();
            Close();
        }

        private void OnAttachOrderPdfClick(object sender, RoutedEventArgs e)
        {
            var orderNo = (_auftrag.Auftragsnummer ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(orderNo))
                return;

            var dlg = new OpenFileDialog
            {
                Title = $"PDF für Auftrag {orderNo} auswählen",
                Filter = "PDF-Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() != true)
                return;

            var archivPdfPfad = AuftragArchivService.TryArchivePdfForOrder(orderNo, dlg.FileName, _ausgewaehlteKalenderWoche, _aktuellesJahr) ?? dlg.FileName;
            var materials = MaterialDataService.LoadAllMaterials();
            var matched = materials
                .Where(m => string.Equals((m.AuftragNr ?? string.Empty).Trim(), orderNo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matched.Count == 0)
            {
                MessageBox.Show("Keine Materialpositionen für diesen Auftrag gefunden.", "Auftrag", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in matched)
            {
                item.PdfPfad = archivPdfPfad;
                item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                item.AenderungsDatum = DateTime.Now;
            }

            _auftrag.PdfPfad = archivPdfPfad;
            SaveChanges();
            MaterialDataService.SaveAllMaterials(materials);

            MessageBox.Show("PDF wurde für den Auftrag übernommen und im KW-Archiv abgelegt.", "Auftrag", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnDeleteOrderClick(object sender, RoutedEventArgs e)
        {
            var orderNo = (_auftrag.Auftragsnummer ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(orderNo))
                return;

            var confirm = MessageBox.Show(
                $"Auftrag '{orderNo}' wirklich löschen?\n\nDie Reservierung wird von allen zugehörigen Materialien entfernt.",
                "Auftrag löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            var materials = MaterialDataService.LoadAllMaterials();
            var matched = materials
                .Where(m => string.Equals((m.AuftragNr ?? string.Empty).Trim(), orderNo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in matched)
            {
                item.AuftragNr = string.Empty;
                if (string.Equals(item.Lagerort, "Gebucht", StringComparison.OrdinalIgnoreCase))
                {
                    item.Lagerort = RegalService.DetermineLagerort(
                        item.MaterialArt,
                        item.Legierung,
                        item.Form,
                        item.Staerke,
                        item.Mass,
                        materials.Where(m => !ReferenceEquals(m, item)).ToList());
                }

                item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                item.AenderungsDatum = DateTime.Now;
            }

            MaterialDataService.SaveAllMaterials(materials);

            using (var context = new MaterialManagerDbContext())
            {
                var dbOrder = context.Auftraege
                    .AsEnumerable()
                    .FirstOrDefault(a => string.Equals((a.Auftragsnummer ?? string.Empty).Trim(), orderNo, StringComparison.OrdinalIgnoreCase));
                if (dbOrder != null)
                {
                    context.Auftraege.Remove(dbOrder);
                    context.SaveChanges();
                }
            }

            DialogResult = true;
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

        private void HandleCompletedOrderMaterials()
        {
            var orderNo = (_auftrag.Auftragsnummer ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(orderNo))
            {
                DialogResult = true;
                Close();
                return;
            }

            var materials = MaterialDataService.LoadAllMaterials();
            var matched = materials
                .Where(m => string.Equals((m.AuftragNr ?? string.Empty).Trim(), orderNo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matched.Count == 0)
            {
                RemoveOrderFromActiveList(orderNo);
                DialogResult = true;
                Close();
                return;
            }

            var actionDialog = new AuftragAbschlussAktionDialog(matched.Count) { Owner = this };
            if (actionDialog.ShowDialog() != true || actionDialog.SelectedAction == AuftragAbschlussAktion.Keine)
                return;

            if (matched.Count == 1)
            {
                var item = matched[0];
                if (actionDialog.SelectedAction == AuftragAbschlussAktion.Bearbeiten)
                {
                    ReleaseMaterialToStock(item, materials);
                    MaterialDataService.SaveAllMaterials(materials);
                    OpenCompletedMaterialEditor(materials, item);
                }
                else if (actionDialog.SelectedAction == AuftragAbschlussAktion.Loeschen)
                {
                    BuchungsService.BucheAusgang(item, orderNo, OperatorIdentityService.CurrentOperatorName);
                    materials.Remove(item);
                    MaterialDataService.SaveAllMaterials(materials);
                }
                else if (actionDialog.SelectedAction == AuftragAbschlussAktion.InsLagerUebernehmen)
                {
                    ReleaseMaterialToStock(item, materials);
                    MaterialDataService.SaveAllMaterials(materials);
                }
            }
            else
            {
                if (actionDialog.SelectedAction == AuftragAbschlussAktion.InsLagerUebernehmen)
                {
                    foreach (var item in matched)
                        ReleaseMaterialToStock(item, materials);

                    MaterialDataService.SaveAllMaterials(materials);
                }
                else if (actionDialog.SelectedAction == AuftragAbschlussAktion.Loeschen)
                {
                    foreach (var item in matched.ToList())
                    {
                        BuchungsService.BucheAusgang(item, orderNo, OperatorIdentityService.CurrentOperatorName);
                        materials.Remove(item);
                    }

                    MaterialDataService.SaveAllMaterials(materials);
                }
            }

            RemoveOrderFromActiveList(orderNo);
            DialogResult = true;
            Close();
        }

        private void OpenCompletedMaterialEditor(List<MaterialItem> materials, MaterialItem item)
        {
            var dlg = new MaterialDialog(materials)
            {
                Owner = this,
                PreserveOriginalAuftragOnEdit = false
            };
            dlg.SetEditMode(item);
            if (dlg.ShowDialog() != true)
                return;

            var index = materials.IndexOf(item);
            if (index < 0)
                return;

            dlg.Material.AuftragNr = string.Empty;
            if (string.IsNullOrWhiteSpace(dlg.Material.Lagerort) || string.Equals(dlg.Material.Lagerort, "Gebucht", StringComparison.OrdinalIgnoreCase))
            {
                dlg.Material.Lagerort = IsAngefangeneTafel(item)
                    ? "Angefangene Tafel"
                    : RegalService.DetermineLagerort(
                        dlg.Material.MaterialArt,
                        dlg.Material.Legierung,
                        dlg.Material.Form,
                        dlg.Material.Staerke,
                        dlg.Material.Mass,
                        materials.Where(m => !ReferenceEquals(m, item)).ToList());
            }

            dlg.Material.IsSelected = false;
            materials[index] = dlg.Material;
            MaterialDataService.SaveAllMaterials(materials);
        }

        private void ReleaseMaterialToStock(MaterialItem item, List<MaterialItem> materials)
        {
            item.AuftragNr = string.Empty;
            item.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
            item.AenderungsDatum = DateTime.Now;
            item.IsSelected = false;

            if (IsAngefangeneTafel(item))
            {
                item.Lagerort = "Angefangene Tafel";
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Lagerort) || string.Equals(item.Lagerort, "Gebucht", StringComparison.OrdinalIgnoreCase))
            {
                item.Lagerort = RegalService.DetermineLagerort(
                    item.MaterialArt,
                    item.Legierung,
                    item.Form,
                    item.Staerke,
                    item.Mass,
                    materials.Where(m => !ReferenceEquals(m, item)).ToList());
            }
        }

        private void RemoveOrderFromActiveList(string orderNo)
        {
            using var context = new MaterialManagerDbContext();
            var dbOrder = context.Auftraege
                .AsEnumerable()
                .FirstOrDefault(a => string.Equals((a.Auftragsnummer ?? string.Empty).Trim(), orderNo, StringComparison.OrdinalIgnoreCase));
            if (dbOrder != null)
            {
                context.Auftraege.Remove(dbOrder);
                context.SaveChanges();
                AuftragDataService.TrySyncSharedAuftraegeFromDatabase();
            }
        }

        private static bool IsAngefangeneTafel(MaterialItem item)
        {
            return item.Kategorie == MaterialKategorie.Blech
                && (string.Equals(item.Lagerort, "Angefangene Tafel", StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrWhiteSpace(item.PdfPfadAngefangeneTafel));
        }

        private bool CanStartProductionWithCompletePdfs(out string fehlendePdfsText)
        {
            fehlendePdfsText = string.Empty;

            var orderNo = (_auftrag.Auftragsnummer ?? string.Empty).Trim();
            var materialien = MaterialDataService.LoadAllMaterials()
                .Where(m => string.Equals((m.AuftragNr ?? string.Empty).Trim(), orderNo, StringComparison.OrdinalIgnoreCase))
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
