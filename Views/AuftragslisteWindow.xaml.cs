using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public ObservableCollection<Auftrag> Auftraege { get; } = new();
        public string? SelectedAuftragsnummer { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AuftragslisteWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadAuftraege();
        }

        private void LoadAuftraege()
        {
            _alleAuftraegeAktiv = AuftragDataService.LoadAllAuftraege();

            var archivEintraege = AuftragArchivService.GetArchivedOrdersForYear(System.DateTime.Now.Year);
            _alleAuftraegeArchiv = archivEintraege
                .GroupBy(x => x.Auftragsnummer, System.StringComparer.OrdinalIgnoreCase)
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
                    MaterialPositionen = 0,
                    GesamtStueckzahl = 0,
                    GesamtGewichtKg = 0,
                    AngelegtVon = string.Empty,
                    GeaendertVon = string.Empty,
                    PdfPfad = x.ErstePdfPfad
                })
                .OrderByDescending(x => x.GeaendertAm)
                .ToList();

            ApplyFilter();
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
                    .Concat(_alleAuftraegeArchiv.Where(a => !_alleAuftraegeAktiv.Any(b => string.Equals(b.Auftragsnummer, a.Auftragsnummer, System.StringComparison.OrdinalIgnoreCase))))
                    .ToList()
            };

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
            if (!IsLoaded)
                return;

            ApplyFilter();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadAuftraege();
        }

        private void OnUseSelectedClick(object sender, RoutedEventArgs e)
        {
            UseSelectedAuftrag();
        }

        private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UseSelectedAuftrag();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
