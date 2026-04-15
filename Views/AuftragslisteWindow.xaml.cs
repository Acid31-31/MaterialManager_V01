using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class AuftragslisteWindow : Window, INotifyPropertyChanged
    {
        private List<Auftrag> _alleAuftraegeAktiv = new();
        private List<Auftrag> _alleAuftraegeArchiv = new();
        private int _selectedYear = DateTime.Now.Year;
        private int _selectedKw = ISOWeek.GetWeekOfYear(DateTime.Now);
        private bool _suppressFilterEvents;
        public ObservableCollection<Auftrag> Auftraege { get; } = new();
        public string? SelectedAuftragsnummer { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AuftragslisteWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializeTimeFilters();
            LoadAuftraege();
        }

        private void InitializeTimeFilters()
        {
            _suppressFilterEvents = true;

            YearFilterBox.Items.Clear();
            var years = AuftragArchivService.GetArchivedYears();
            if (!years.Contains(DateTime.Now.Year))
                years.Insert(0, DateTime.Now.Year);

            foreach (var year in years.Distinct().OrderByDescending(y => y))
                YearFilterBox.Items.Add(new ComboBoxItem { Content = year.ToString(CultureInfo.InvariantCulture), Tag = year });

            YearFilterBox.SelectedItem = YearFilterBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => (i.Tag as int?) == _selectedYear)
                ?? YearFilterBox.Items.OfType<ComboBoxItem>().FirstOrDefault();

            if (YearFilterBox.SelectedItem is ComboBoxItem selectedYearItem && selectedYearItem.Tag is int y)
                _selectedYear = y;

            KwFilterBox.Items.Clear();
            for (var kw = 1; kw <= 53; kw++)
                KwFilterBox.Items.Add(new ComboBoxItem { Content = $"KW {kw:D2}", Tag = kw });

            if (_selectedKw < 1 || _selectedKw > 53)
                _selectedKw = ISOWeek.GetWeekOfYear(DateTime.Now);

            KwFilterBox.SelectedItem = KwFilterBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => (i.Tag as int?) == _selectedKw)
                ?? KwFilterBox.Items.OfType<ComboBoxItem>().FirstOrDefault();

            if (KwFilterBox.SelectedItem is ComboBoxItem selectedKwItem && selectedKwItem.Tag is int kwValue)
                _selectedKw = kwValue;

            _suppressFilterEvents = false;
        }

        private void LoadAuftraege()
        {
            _alleAuftraegeAktiv = AuftragDataService.LoadAllAuftraege();

            try
            {
                AuftragArchivService.BackfillArchiveMetadataForYear(_selectedYear);
            }
            catch
            {
            }

            var archivEintraege = AuftragArchivService.GetArchivedOrdersForYear(_selectedYear);
            _alleAuftraegeArchiv = archivEintraege
                .GroupBy(x => x.Auftragsnummer, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.ArchiviertAm).First())
                .Select(x => new Auftrag
                {
                    Auftragsnummer = x.Auftragsnummer,
                    Arbeitsplatz = "Archiv",
                    Status = AuftragStatus.Abgeschlossen,
                    ErstelltAm = x.ProduktionStartDatum ?? x.ArchiviertAm,
                    GeaendertAm = x.ProduktionEndDatum ?? x.ArchiviertAm,
                    ProduktionStartDatum = x.ProduktionStartDatum,
                    ProduktionEndDatum = x.ProduktionEndDatum,
                    MaterialPositionen = x.MaterialPositionen,
                    GesamtStueckzahl = x.GesamtStueckzahl,
                    GesamtGewichtKg = x.GesamtGewichtKg,
                    AngelegtVon = x.AngelegtVon,
                    GeaendertVon = x.GeaendertVon,
                    PdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(x.Auftragsnummer, x.ErstePdfPfad)
                })
                .OrderByDescending(x => x.GeaendertAm)
                .ToList();

            ApplyFilter();
        }

        private static DateTime GetRelevantDate(Auftrag auftrag)
        {
            return auftrag.GeaendertAm != default ? auftrag.GeaendertAm : auftrag.ErstelltAm;
        }

        private void ApplyFilter()
        {
            var query = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var source = ((SourceFilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Archiviert").Trim();

            List<Auftrag> basis = source switch
            {
                "Aktiv" => _alleAuftraegeAktiv,
                "Archiviert" => _alleAuftraegeArchiv,
                _ => _alleAuftraegeAktiv
                    .Concat(_alleAuftraegeArchiv.Where(a => !_alleAuftraegeAktiv.Any(b => string.Equals(b.Auftragsnummer, a.Auftragsnummer, StringComparison.OrdinalIgnoreCase))))
                    .ToList()
            };

            if (_selectedYear > 0)
                basis = basis.Where(a => GetRelevantDate(a).Year == _selectedYear).ToList();

            if (_selectedKw > 0)
                basis = basis.Where(a => ISOWeek.GetWeekOfYear(GetRelevantDate(a)) == _selectedKw).ToList();

            var filtered = string.IsNullOrWhiteSpace(query)
                ? basis
                : basis.Where(a =>
                    (a.Auftragsnummer ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (a.Arbeitsplatz ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    a.Status.ToString().ToLowerInvariant().Contains(query) ||
                    (a.AngelegtVon ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (a.GeaendertVon ?? string.Empty).ToLowerInvariant().Contains(query))
                    .ToList();

            Auftraege.Clear();
            foreach (var auftrag in filtered)
                Auftraege.Add(auftrag);
        }

        private void UseSelectedAuftrag()
        {
            if (AuftragsGrid.SelectedItem is not Auftrag auftrag)
            {
                MessageBox.Show("Bitte zuerst einen Auftrag auswählen.", "Auftragsliste", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedAuftragsnummer = auftrag.Auftragsnummer;
            DialogResult = true;
            Close();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnSourceFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _suppressFilterEvents)
                return;

            ApplyFilter();
        }

        private void OnYearFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _suppressFilterEvents)
                return;

            if (YearFilterBox.SelectedItem is ComboBoxItem item && item.Tag is int year)
                _selectedYear = year;

            LoadAuftraege();
        }

        private void OnKwFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _suppressFilterEvents)
                return;

            if (KwFilterBox.SelectedItem is ComboBoxItem item && item.Tag is int kw)
                _selectedKw = kw;

            ApplyFilter();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            _selectedKw = ISOWeek.GetWeekOfYear(DateTime.Now);
            InitializeTimeFilters();
            LoadAuftraege();
        }

        private void OnUseSelectedClick(object sender, RoutedEventArgs e)
        {
            UseSelectedAuftrag();
        }

        private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AuftragsGrid.SelectedItem is not Auftrag auftrag)
                return;

            var pdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(auftrag.Auftragsnummer, auftrag.PdfPfad);
            if (string.IsNullOrWhiteSpace(pdfPfad))
            {
                MessageBox.Show("Für diesen Auftrag wurde keine PDF gefunden.", "Auftragsliste", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!File.Exists(pdfPfad))
            {
                MessageBox.Show($"PDF-Datei nicht gefunden:\n{pdfPfad}", "Auftragsliste", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPfad,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF konnte nicht geöffnet werden:\n{ex.Message}", "Auftragsliste", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenPdfClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not Auftrag auftrag)
                return;

            var pdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(auftrag.Auftragsnummer, auftrag.PdfPfad);
            if (string.IsNullOrWhiteSpace(pdfPfad))
            {
                MessageBox.Show("Für diesen Auftrag wurde keine PDF gefunden.", "Auftragsliste", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!File.Exists(pdfPfad))
            {
                MessageBox.Show($"PDF-Datei nicht gefunden:\n{pdfPfad}", "Auftragsliste", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPfad,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF konnte nicht geöffnet werden:\n{ex.Message}", "Auftragsliste", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
