using MaterialManager_V01.Models;
using System;
using System.Linq;
using System.Windows.Controls;

namespace MaterialManager_V01
{
    public partial class MainWindow
    {
        private string GetSelectedMainFilter()
        {
            var selected = (FormFilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
                return selected.Trim();

            if (!string.IsNullOrWhiteSpace(FormFilterBox?.Text))
                return FormFilterBox.Text.Trim();

            return "Alle";
        }

        private void ApplyMainFilter()
        {
            if (MaterialGrid == null)
                return;

            var selectedFilter = GetSelectedMainFilter();
            var query = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            var filtered = Materialien.Where(m =>
            {
                var filterMatch = selectedFilter switch
                {
                    "Alle" => true,
                    "Blech" => m.Kategorie == MaterialKategorie.Blech,
                    "Rohr" => m.Kategorie == MaterialKategorie.Rohr,
                    "Profil" => m.Kategorie == MaterialKategorie.Profil,
                    "GF" or "MF" or "KF" or "Rest" => string.Equals(m.Form, selectedFilter, StringComparison.OrdinalIgnoreCase),
                    _ => true
                };

                if (!filterMatch)
                    return false;

                if (string.IsNullOrWhiteSpace(query))
                    return true;

                return (m.MaterialArt ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Legierung ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Oberflaeche ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Guete ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Form ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       m.Kategorie.ToString().ToLowerInvariant().Contains(query) ||
                       m.Staerke.ToString("0.0").ToLowerInvariant().Contains(query) ||
                       (m.Mass ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       m.Stueckzahl.ToString().Contains(query) ||
                       (m.Lagerort ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Restnummer ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.AuftragNr ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.Lieferant ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.LieferscheinNr ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.AngelegtVon ?? string.Empty).ToLowerInvariant().Contains(query) ||
                       (m.GeaendertVon ?? string.Empty).ToLowerInvariant().Contains(query);
            }).ToList();

            MaterialGrid.ItemsSource = filtered;
        }

        private void OnMainFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyMainFilter();
        }

        private void OnMainSearchChanged(object sender, TextChangedEventArgs e)
        {
            ApplyMainFilter();
        }
    }
}
