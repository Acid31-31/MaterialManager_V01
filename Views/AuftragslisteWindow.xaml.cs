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
        private List<Auftrag> _alleAuftraege = new();
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
            _alleAuftraege = AuftragDataService.LoadAllAuftraege();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _alleAuftraege
                : _alleAuftraege.Where(a =>
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
