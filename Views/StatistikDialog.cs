using MaterialManager_V01.Models;
using MaterialManager_V01.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialManager_V01.Views
{
    public partial class StatistikDialog : Window
    {
        private readonly IEnumerable<MaterialItem> _materialien;

        public StatistikDialog(IEnumerable<MaterialItem> materialien)
        {
            _materialien = materialien;
            InitUI();
            BerechneStatistiken();
        }

        private void InitUI()
        {
            Title = "📊 Statistik & Dashboard";
            Width = 900;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var mainStack = new StackPanel { Margin = new Thickness(20) };
            scroll.Content = mainStack;
            Content = scroll;

            // Titel
            mainStack.Children.Add(new TextBlock
            {
                Text = "📊 Lager-Statistiken",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Button-Panel
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
            var verbrauchBtn = new Button
            {
                Content = "📈 Verbrauchs-Statistik",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            verbrauchBtn.Click += (s, e) =>
            {
                var dlg = new VerbrauchsStatistikDialog(_materialien) { Owner = this };
                dlg.ShowDialog();
            };
            btnPanel.Children.Add(verbrauchBtn);
            mainStack.Children.Add(btnPanel);

            _mainStack = mainStack;
        }

        private StackPanel _mainStack = new();

        private void BerechneStatistiken()
        {
            var alle = _materialien.ToList();
            var tafeln = alle.Where(m => m.Form != "Rest").ToList();
            var reste = alle.Where(m => m.Form == "Rest").ToList();

            // Übersicht
            AddSection("📦 Lager-Übersicht");
            AddStat("Gesamte Positionen", alle.Count.ToString());
            AddStat("Gesamtgewicht", $"{alle.Sum(m => m.GewichtKg):N2} kg");
            AddStat("Tafeln (GF/MF/KF)", $"{tafeln.Count} ({tafeln.Sum(m => m.Stueckzahl)} Stück)");
            AddStat("Reste", $"{reste.Count} ({reste.Sum(m => m.GewichtKg):N2} kg)");
            AddSeparator();

            // Nach Material
            AddSection("🏗️ Nach Materialart");
            var matGruppen = alle.GroupBy(m => m.MaterialArt).OrderBy(g => g.Key);
            foreach (var g in matGruppen)
            {
                AddStat(g.Key, $"{g.Count()} Pos. | {g.Sum(m => m.GewichtKg):N2} kg");
            }
            AddSeparator();

            // Regal-Auslastung
            AddSection("📐 Regal-Auslastung");
            var regale = Services.RegalService.GetAllLagerorte();
            foreach (var regal in regale)
            {
                var load = Services.RegalService.CalculateCurrentLoad(alle, regal);
                var cap = Services.RegalService.GetCapacity(regal);
                var pct = Services.RegalService.ComputeUtilizationPercent(load, cap);
                var desc = Services.RegalService.GetRegalDescription(regal);

                AddStatWithBar(regal, $"{load:N0} / {cap:N0} kg ({pct:N0}%)", pct);
            }
            AddSeparator();

            // Top 10 Legierungen
            AddSection("🔝 Top 10 Legierungen (nach Gewicht)");
            var topLeg = alle.GroupBy(m => $"{m.MaterialArt} {m.Legierung}")
                .Select(g => new { Name = g.Key, Kg = g.Sum(m => m.GewichtKg), Count = g.Count() })
                .OrderByDescending(x => x.Kg)
                .Take(10);
            foreach (var l in topLeg)
            {
                AddStat(l.Name, $"{l.Kg:N2} kg ({l.Count} Pos.)");
            }
            AddSeparator();

            // Durchschnittliche Restgröße
            if (reste.Any())
            {
                AddSection("📏 Reste-Statistik");
                AddStat("Durchschnittliches Restgewicht", $"{reste.Average(m => m.GewichtKg):N2} kg");
                AddStat("Kleinster Rest", $"{reste.Min(m => m.GewichtKg):N2} kg");
                AddStat("Größter Rest", $"{reste.Max(m => m.GewichtKg):N2} kg");
            }

            // Reservierte Materialien
            var reserviert = alle.Where(m => m.IstReserviert).ToList();
            if (reserviert.Any())
            {
                AddSeparator();
                AddSection("🔒 Reservierte Materialien");
                var auftraege = reserviert.GroupBy(m => m.AuftragNr);
                foreach (var a in auftraege)
                {
                    AddStat($"Auftrag {a.Key}", $"{a.Count()} Pos. | {a.Sum(m => m.GewichtKg):N2} kg");
                }
            }

            // Buchungshistorie
            var letzteBuchungen = BuchungsService.GetLetzteBuchungen(5);
            if (letzteBuchungen.Any())
            {
                AddSeparator();
                AddSection("📋 Letzte Buchungen");
                foreach (var b in letzteBuchungen)
                {
                    var icon = b.Typ == "Eingang" ? "📥" : "📤";
                    AddStat($"{icon} {b.Zeitpunkt:dd.MM.yy HH:mm}",
                        $"{b.Typ}: {b.MaterialArt} {b.Legierung} {b.Staerke:0.0}mm | {b.GewichtKg:N2} kg");
                }
            }

            // Schließen-Button
            var closeBtn = new Button
            {
                Content = "Schließen",
                Width = 120,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 20, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeBtn.Click += (s, e) => Close();
            _mainStack.Children.Add(closeBtn);
        }

        private void AddSection(string title)
        {
            _mainStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                Margin = new Thickness(0, 12, 0, 6)
            });
        }

        private void AddStat(string label, string value)
        {
            var panel = new DockPanel { Margin = new Thickness(8, 2, 8, 2) };
            panel.Children.Add(new TextBlock { Text = label, Foreground = Brushes.White, FontSize = 12 });
            var valBlock = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(valBlock, Dock.Right);
            panel.Children.Add(valBlock);
            _mainStack.Children.Add(panel);
        }

        private void AddStatWithBar(string label, string value, double percent)
        {
            var panel = new StackPanel { Margin = new Thickness(8, 2, 8, 2) };
            var topRow = new DockPanel();
            topRow.Children.Add(new TextBlock { Text = label, Foreground = Brushes.White, FontSize = 11 });
            var valBlock = new TextBlock
            {
                Text = value,
                Foreground = Brushes.White,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(valBlock, Dock.Right);
            topRow.Children.Add(valBlock);
            panel.Children.Add(topRow);

            var barColor = percent < 60 ? Color.FromRgb(76, 175, 80)
                : percent < 85 ? Color.FromRgb(255, 152, 0)
                : Color.FromRgb(244, 67, 54);

            var bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = percent,
                Height = 8,
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                Foreground = new SolidColorBrush(barColor)
            };
            panel.Children.Add(bar);
            _mainStack.Children.Add(panel);
        }

        private void AddSeparator()
        {
            _mainStack.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 8, 0, 8)
            });
        }
    }
}
