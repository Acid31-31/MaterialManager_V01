using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialManager_V01.Views
{
    public partial class TafelVerbrauchDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly MaterialItem _tafel;
        private readonly IEnumerable<MaterialItem> _inventory;

        public ObservableCollection<RestEingabe> Reste { get; } = new();
        public List<MaterialItem> ErstellteReste { get; } = new();
        public bool TafelKomplett { get; private set; }

        private string _auftragNr = "";
        public string AuftragNr
        {
            get => _auftragNr;
            set { _auftragNr = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuftragNr))); }
        }

        private string _infoText = "";
        public string InfoText
        {
            get => _infoText;
            set { _infoText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InfoText))); }
        }

        public TafelVerbrauchDialog(MaterialItem tafel, IEnumerable<MaterialItem> inventory)
        {
            _tafel = tafel;
            _inventory = inventory;

            Title = "Tafel verbrauchen → Reste anlegen";
            Width = 550;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            DataContext = this;

            InfoText = $"Tafel: {tafel.MaterialArt} {tafel.Legierung} {tafel.Oberflaeche} {tafel.Staerke:0.0}mm\n" +
                       $"Maß: {tafel.Mass} | Stück: {tafel.Stueckzahl} | Gewicht: {tafel.GewichtKg:N2} kg";

            BuildUI();
        }

        private void BuildUI()
        {
            var mainStack = new StackPanel { Margin = new Thickness(16) };

            // Info
            mainStack.Children.Add(new TextBlock
            {
                Text = "📋 Tafel verbrauchen",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            });

            mainStack.Children.Add(new TextBlock
            {
                Text = InfoText,
                Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176)),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            });

            // Auftrag
            var auftragPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            auftragPanel.Children.Add(new TextBlock { Text = "Auftrag-Nr:", Foreground = Brushes.White, Width = 100, VerticalAlignment = VerticalAlignment.Center });
            var auftragBox = new TextBox { Background = new SolidColorBrush(Color.FromRgb(34, 34, 34)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)), Padding = new Thickness(4) };
            auftragBox.TextChanged += (s, e) => AuftragNr = auftragBox.Text;
            auftragPanel.Children.Add(auftragBox);
            mainStack.Children.Add(auftragPanel);

            // Reste-Bereich
            mainStack.Children.Add(new TextBlock
            {
                Text = "🔧 Reste die entstanden sind (Maß eingeben):",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 8, 0, 4)
            });

            var resteStack = new StackPanel();
            _resteStack = resteStack;
            mainStack.Children.Add(resteStack);

            // Rest hinzufügen Button
            var addBtn = new Button
            {
                Content = "+ Rest hinzufügen",
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 8, 0, 8),
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            addBtn.Click += (s, e) => AddRestRow();
            mainStack.Children.Add(addBtn);

            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };

            var okBtn = new Button
            {
                Content = "✓ Verbrauchen & Reste anlegen",
                Padding = new Thickness(16, 8, 16, 8),
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            okBtn.Click += OnOk;

            var cancelBtn = new Button
            {
                Content = "Abbrechen",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; };

            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            mainStack.Children.Add(btnPanel);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            scroll.Content = mainStack;
            Content = scroll;

            // Standard: 1 Rest-Zeile
            AddRestRow();
        }

        private StackPanel _resteStack = new();
        private int _restCount = 0;

        private void AddRestRow()
        {
            _restCount++;
            var rest = new RestEingabe { Nr = _restCount };
            Reste.Add(rest);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            panel.Children.Add(new TextBlock { Text = $"Rest {_restCount}:", Foreground = Brushes.White, Width = 60, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "Länge:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) });

            var laengeBox = new TextBox { Width = 70, Background = new SolidColorBrush(Color.FromRgb(34, 34, 34)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)), Padding = new Thickness(2) };
            laengeBox.TextChanged += (s, e) => { if (int.TryParse(laengeBox.Text, out var v)) rest.Laenge = v; };
            panel.Children.Add(laengeBox);

            panel.Children.Add(new TextBlock { Text = " x Breite:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) });

            var breiteBox = new TextBox { Width = 70, Background = new SolidColorBrush(Color.FromRgb(34, 34, 34)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)), Padding = new Thickness(2) };
            breiteBox.TextChanged += (s, e) => { if (int.TryParse(breiteBox.Text, out var v)) rest.Breite = v; };
            panel.Children.Add(breiteBox);

            panel.Children.Add(new TextBlock { Text = " mm", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) });

            _resteStack.Children.Add(panel);
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            var gueltigeReste = Reste.Where(r => r.Laenge > 0 && r.Breite > 0).ToList();

            foreach (var rest in gueltigeReste)
            {
                var restnummer = MaterialDefinitions.NeueRestnummer();
                ErstellteReste.Add(new MaterialItem
                {
                    MaterialArt = _tafel.MaterialArt,
                    Legierung = _tafel.Legierung,
                    Oberflaeche = _tafel.Oberflaeche,
                    Guete = _tafel.Guete,
                    Form = "Rest",
                    Staerke = _tafel.Staerke,
                    Mass = $"{rest.Laenge}x{rest.Breite}",
                    Stueckzahl = 1,
                    Restnummer = restnummer,
                    Datum = DateTime.Today,
                    AuftragNr = AuftragNr,
                    Lagerort = Services.RegalService.DetermineLagerort(
                        _tafel.MaterialArt, _tafel.Legierung, "Rest", _tafel.Staerke,
                        $"{rest.Laenge}x{rest.Breite}", _inventory)
                });
            }

            TafelKomplett = true;
            DialogResult = true;
        }
    }

    public class RestEingabe
    {
        public int Nr { get; set; }
        public int Laenge { get; set; }
        public int Breite { get; set; }
    }
}
