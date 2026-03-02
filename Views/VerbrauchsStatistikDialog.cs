using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialManager_V01.Views
{
    public class VerbrauchsStatistikDialog : Window
    {
        private readonly IEnumerable<MaterialItem> _materialien;
        private ComboBox _zeitraumBox;
        private StackPanel _ergebnisStack;

        public VerbrauchsStatistikDialog(IEnumerable<MaterialItem> materialien)
        {
            _materialien = materialien;
            Title = "📈 Verbrauchs-Statistik";
            Width = 700;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            BuildUI();
            AktualisierenStatistik();
        }

        private void BuildUI()
        {
            var mainStack = new StackPanel { Margin = new Thickness(20) };

            // Header
            mainStack.Children.Add(new TextBlock
            {
                Text = "📈 Verbrauchs-Statistik",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            });

            mainStack.Children.Add(new TextBlock
            {
                Text = "Zeigt welche Materialien am häufigsten verbraucht werden.\nBasierend auf Buchungsdaten (Ein-/Ausgangs-Journal).",
                Foreground = Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            });

            // Zeitraum-Auswahl
            var zeitPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            zeitPanel.Children.Add(new TextBlock
            {
                Text = "Zeitraum:",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });

            _zeitraumBox = new ComboBox
            {
                Width = 180,
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68))
            };
            _zeitraumBox.Items.Add("Letzte 7 Tage");
            _zeitraumBox.Items.Add("Letzte 30 Tage");
            _zeitraumBox.Items.Add("Letzte 90 Tage");
            _zeitraumBox.Items.Add("Dieses Jahr");
            _zeitraumBox.Items.Add("Gesamt (alle Daten)");
            _zeitraumBox.SelectedIndex = 1; // 30 Tage
            _zeitraumBox.SelectionChanged += (s, e) => AktualisierenStatistik();
            zeitPanel.Children.Add(_zeitraumBox);

            var refreshBtn = new Button
            {
                Content = "🔄",
                Width = 36,
                Height = 28,
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Aktualisieren"
            };
            refreshBtn.Click += (s, e) => AktualisierenStatistik();
            zeitPanel.Children.Add(refreshBtn);

            mainStack.Children.Add(zeitPanel);

            // Ergebnis-Bereich
            _ergebnisStack = new StackPanel();
            mainStack.Children.Add(_ergebnisStack);

            // Close Button
            var closeBtn = new Button
            {
                Content = "Schließen",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeBtn.Click += (s, e) => Close();
            mainStack.Children.Add(closeBtn);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            scroll.Content = mainStack;
            Content = scroll;
        }

        private void AktualisierenStatistik()
        {
            _ergebnisStack.Children.Clear();

            // Zeitraum berechnen
            var tage = _zeitraumBox.SelectedIndex switch
            {
                0 => 7,
                1 => 30,
                2 => 90,
                3 => 365,
                _ => int.MaxValue
            };

            var cutoffDate = DateTime.Now.AddDays(-tage);

            // Verbrauch aus Buchungen simulieren (in Realität aus BuchungsService)
            // Für Demo: Materialien mit Änderungsdatum = "verbraucht"
            var verbrauchte = _materialien
                .Where(m => m.AenderungsDatum.HasValue && m.AenderungsDatum.Value >= cutoffDate)
                .ToList();

            // Nach Material/Legierung/Stärke gruppieren
            var grouped = verbrauchte
                .GroupBy(m => new { m.MaterialArt, m.Legierung, m.Staerke, m.Form })
                .Select(g => new
                {
                    Material = $"{g.Key.MaterialArt} {g.Key.Legierung} {g.Key.Staerke:0.0}mm {g.Key.Form}",
                    Anzahl = g.Sum(m => m.Stueckzahl),
                    Gewicht = g.Sum(m => m.GewichtKg),
                    Wert = g.Sum(m => m.Gesamtwert)
                })
                .OrderByDescending(x => x.Gewicht)
                .Take(10)
                .ToList();

            if (grouped.Count == 0)
            {
                _ergebnisStack.Children.Add(new TextBlock
                {
                    Text = "❌ Keine Daten für diesen Zeitraum vorhanden.\n\nHinweis: Statistik basiert auf Änderungen (AenderungsDatum).",
                    Foreground = Brushes.Gray,
                    FontSize = 13,
                    Margin = new Thickness(0, 20, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            // Überschrift
            _ergebnisStack.Children.Add(new TextBlock
            {
                Text = $"📊 Top {grouped.Count} Verbrauch ({_zeitraumBox.SelectedItem})",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Margin = new Thickness(0, 0, 0, 12)
            });

            int rank = 1;
            foreach (var item in grouped)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 4, 0, 4),
                    CornerRadius = new CornerRadius(4)
                };

                var innerStack = new StackPanel();
                
                // Platz + Material
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                headerPanel.Children.Add(new TextBlock
                {
                    Text = $"#{rank}  ",
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                });
                headerPanel.Children.Add(new TextBlock
                {
                    Text = item.Material,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                });
                innerStack.Children.Add(headerPanel);

                // Details
                var detailsText = $"    Anzahl: {item.Anzahl} | Gewicht: {item.Gewicht:N2} kg";
                if (item.Wert > 0)
                    detailsText += $" | Wert: {item.Wert:N2} €";
                
                innerStack.Children.Add(new TextBlock
                {
                    Text = detailsText,
                    Foreground = Brushes.Gray,
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                // Progress Bar (relativ zum höchsten Wert)
                var maxGewicht = grouped.Max(x => x.Gewicht);
                var prozent = maxGewicht > 0 ? (item.Gewicht / maxGewicht) * 100 : 0;
                var progressBar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = prozent,
                    Height = 8,
                    Margin = new Thickness(0, 6, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                    Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                };
                innerStack.Children.Add(progressBar);

                border.Child = innerStack;
                _ergebnisStack.Children.Add(border);
                rank++;
            }

            // Zusammenfassung
            var gesamtGewicht = grouped.Sum(x => x.Gewicht);
            var gesamtWert = grouped.Sum(x => x.Wert);
            var summaryText = $"\n📈 Gesamt: {gesamtGewicht:N2} kg";
            if (gesamtWert > 0)
                summaryText += $" | {gesamtWert:N2} €";

            _ergebnisStack.Children.Add(new TextBlock
            {
                Text = summaryText,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Margin = new Thickness(0, 12, 0, 0)
            });
        }
    }
}
