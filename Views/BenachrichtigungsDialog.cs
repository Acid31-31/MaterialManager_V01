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
    public static class BenachrichtigungsService
    {
        /// <summary>
        /// Prüft alle Warnungen und zeigt sie beim Start an
        /// </summary>
        public static void PruefeUndZeige(IEnumerable<MaterialItem> materialien)
        {
            var warnungen = new List<(string Icon, string Text, string Farbe)>();

            // 1. Niedrige Bestände (kritisch)
            var tafeln = materialien.Where(m => m.Form == "GF" || m.Form == "MF" || m.Form == "KF").ToList();
            var kritisch = tafeln.Count(m => m.Stueckzahl <= 1);
            if (kritisch > 0)
                warnungen.Add(("🔴", $"{kritisch} Material(ien) mit kritisch niedrigem Bestand (≤1 Tafel)", "#F44336"));

            var niedrig = tafeln.Count(m => m.Stueckzahl == 2);
            if (niedrig > 0)
                warnungen.Add(("🟡", $"{niedrig} Material(ien) mit niedrigem Bestand (2 Tafeln)", "#FF9800"));

            // 2. Regale fast voll
            var regale = RegalService.GetAllLagerorte();
            foreach (var regal in regale)
            {
                var load = RegalService.CalculateCurrentLoad(materialien, regal);
                var cap = RegalService.GetCapacity(regal);
                var pct = RegalService.ComputeUtilizationPercent(load, cap);
                if (pct >= 90)
                    warnungen.Add(("📦", $"{regal}: {pct:N0}% ausgelastet ({load:N0}/{cap:N0} kg)", "#FF5722"));
            }

            // 3. Lizenz
            var days = LicenseService.GetRemainingTrialDays();
            if (days <= 7)
                warnungen.Add(("⏰", $"Demo-Version: Noch {days} Tage verbleibend!", "#E91E63"));

            // 4. Bestellvorschläge
            var vorschlaege = BestellService.BerechneVorschlaege(materialien);
            var kritischeBestellungen = vorschlaege.Count(v => v.Dringlichkeit == "KRITISCH");
            if (kritischeBestellungen > 0)
                warnungen.Add(("🛒", $"{kritischeBestellungen} kritische Bestellvorschläge", "#F44336"));

            if (warnungen.Count == 0)
                return;

            // Dialog anzeigen
            var dlg = new Window
            {
                Title = "⚠️ Benachrichtigungen",
                Width = 500,
                Height = Math.Min(120 + warnungen.Count * 50, 500),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Topmost = true
            };

            var stack = new StackPanel { Margin = new Thickness(16) };
            stack.Children.Add(new TextBlock
            {
                Text = $"⚠️ {warnungen.Count} Warnung{(warnungen.Count > 1 ? "en" : "")} beim Start",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            });

            foreach (var (icon, text, farbe) in warnungen)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(farbe)!,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 2, 0, 2),
                    CornerRadius = new CornerRadius(4)
                };

                border.Child = new TextBlock
                {
                    Text = $"{icon} {text}",
                    Foreground = Brushes.White,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                };

                stack.Children.Add(border);
            }

            var okBtn = new Button
            {
                Content = "OK - Verstanden",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 12, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            okBtn.Click += (s, e) => dlg.Close();
            stack.Children.Add(okBtn);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            scroll.Content = stack;
            dlg.Content = scroll;
            dlg.ShowDialog();
        }
    }
}
