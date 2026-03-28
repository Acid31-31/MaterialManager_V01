using System;
using System.Windows;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class ProduktionVerfolgungDialog : Window
    {
        private Auftrag _auftrag;

        public ProduktionVerfolgungDialog(Auftrag auftrag)
        {
            InitializeComponent();
            _auftrag = auftrag;
            AuftragTextBlock.Text = _auftrag.Auftragsnummer;

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
            StartButton.IsEnabled = false;
            EndButton.IsEnabled = true;
            UpdateDisplay();
            SaveChanges();
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
        }

        private void SaveChanges()
        {
            try
            {
                using (var context = new MaterialManagerDbContext())
                {
                    var existingAuftrag = context.Auftraege.Find(_auftrag.Id);
                    if (existingAuftrag != null)
                    {
                        existingAuftrag.ProduktionStartDatum = _auftrag.ProduktionStartDatum;
                        existingAuftrag.ProduktionEndDatum = _auftrag.ProduktionEndDatum;
                        existingAuftrag.Status = _auftrag.Status;
                        existingAuftrag.GeaendertAm = DateTime.Now;
                        existingAuftrag.GeaendertVon = OperatorIdentityService.CurrentOperatorName;
                        context.SaveChanges();
                    }
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
    }
}
