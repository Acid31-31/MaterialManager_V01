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
    public class InventurDialog : Window
    {
        private readonly IEnumerable<MaterialItem> _materialien;
        private StackPanel _contentStack = new();
        private Dictionary<string, int> _istBestaende = new();

        public InventurDialog(IEnumerable<MaterialItem> materialien)
        {
            _materialien = materialien;
            Title = "📋 Inventur";
            Width = 800;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            BuildUI();
        }

        private void BuildUI()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new StackPanel { Margin = new Thickness(16, 12, 16, 4) };
            header.Children.Add(new TextBlock
            {
                Text = "📋 Inventur - Soll/Ist Vergleich",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            
            // ✅ Info mit "Material hinzufügen" Button
            var infoPanel = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
            
            var addBtn = new Button
            {
                Content = "➕ Neues Material hinzufügen",
                Padding = new Thickness(12, 4, 12, 4),
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            addBtn.Click += (s, e) => AddNewMaterialDuringInventory();
            DockPanel.SetDock(addBtn, Dock.Right);
            infoPanel.Children.Add(addBtn);
            
            var infoText = new TextBlock
            {
                Text = $"Stand: {DateTime.Now:dd.MM.yyyy HH:mm} | Tragen Sie den tatsächlichen Bestand ein",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            infoPanel.Children.Add(infoText);
            
            header.Children.Add(infoPanel);
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Inhalt
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(16, 4, 16, 4) };
            _contentStack = new StackPanel();
            scroll.Content = _contentStack;
            Grid.SetRow(scroll, 1);
            mainGrid.Children.Add(scroll);

            // Regale auflisten
            var regale = _materialien
                .GroupBy(m => m.Lagerort)
                .OrderBy(g => g.Key);

            foreach (var regal in regale)
            {
                AddRegalSection(regal.Key, regal.ToList());
            }

            // Footer Buttons
            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16, 8, 16, 12) };

            var berichtBtn = new Button
            {
                Content = "📊 Inventur-Bericht drucken",
                Padding = new Thickness(16, 8, 16, 8),
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            berichtBtn.Click += (s, e) => ZeigeBericht();

            var closeBtn = new Button
            {
                Content = "Schließen",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeBtn.Click += (s, e) => Close();

            footer.Children.Add(berichtBtn);
            footer.Children.Add(closeBtn);
            Grid.SetRow(footer, 2);
            mainGrid.Children.Add(footer);

            Content = mainGrid;
        }

        private void AddRegalSection(string regalName, List<MaterialItem> items)
        {
            var load = RegalService.CalculateCurrentLoad(items, regalName);
            var cap = RegalService.GetCapacity(regalName);

            _contentStack.Children.Add(new TextBlock
            {
                Text = $"📦 {regalName} ({items.Count} Positionen | {load:N0} kg)",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                Margin = new Thickness(0, 12, 0, 4)
            });

            // Header
            var headerPanel = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            AddHeaderCell(headerPanel, "Material", 0);
            AddHeaderCell(headerPanel, "Soll", 1);
            AddHeaderCell(headerPanel, "Ist", 2);
            AddHeaderCell(headerPanel, "Diff", 3);
            AddHeaderCell(headerPanel, "Status", 4);

            _contentStack.Children.Add(headerPanel);

            // Items
            foreach (var item in items.OrderBy(m => m.MaterialArt).ThenBy(m => m.Legierung))
            {
                var key = $"{regalName}|{item.MaterialArt}|{item.Legierung}|{item.Staerke}|{item.Mass}|{item.Restnummer}";
                AddInventurRow(item, key);
            }
        }

        private void AddHeaderCell(Grid grid, string text, int col)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void AddInventurRow(MaterialItem item, string key)
        {
            var panel = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            var name = $"{item.MaterialArt} {item.Legierung} {item.Staerke:0.0}mm";
            if (!string.IsNullOrEmpty(item.Restnummer)) name += $" ({item.Restnummer})";

            var nameBlock = new TextBlock { Text = name, Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };
            Grid.SetColumn(nameBlock, 0);
            panel.Children.Add(nameBlock);

            var sollBlock = new TextBlock { Text = item.Stueckzahl.ToString(), Foreground = Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };
            Grid.SetColumn(sollBlock, 1);
            panel.Children.Add(sollBlock);

            var istBox = new TextBox
            {
                Text = item.Stueckzahl.ToString(),
                Width = 50,
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Padding = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var diffBlock = new TextBlock { Text = "0", Foreground = Brushes.Green, FontSize = 11, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };
            var statusBlock = new TextBlock { Text = "✓ OK", Foreground = Brushes.Green, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };

            istBox.TextChanged += (s, e) =>
            {
                if (int.TryParse(istBox.Text, out var ist))
                {
                    _istBestaende[key] = ist;
                    var diff = ist - item.Stueckzahl;
                    diffBlock.Text = diff >= 0 ? $"+{diff}" : diff.ToString();

                    if (diff == 0)
                    {
                        diffBlock.Foreground = Brushes.Green;
                        statusBlock.Text = "✓ OK";
                        statusBlock.Foreground = Brushes.Green;
                    }
                    else if (diff < 0)
                    {
                        diffBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                        statusBlock.Text = "⚠ FEHLT";
                        statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    }
                    else
                    {
                        diffBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                        statusBlock.Text = "↑ Mehr";
                        statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    }
                }
            };

            Grid.SetColumn(istBox, 2);
            panel.Children.Add(istBox);
            Grid.SetColumn(diffBlock, 3);
            panel.Children.Add(diffBlock);
            Grid.SetColumn(statusBlock, 4);
            panel.Children.Add(statusBlock);

            _contentStack.Children.Add(panel);
        }

        private void ZeigeBericht()
        {
            var differenzen = _istBestaende.Where(kv =>
            {
                var parts = kv.Key.Split('|');
                var soll = _materialien.FirstOrDefault(m =>
                    m.Lagerort == parts[0] && m.MaterialArt == parts[1] &&
                    m.Legierung == parts[2] && m.Staerke.ToString("0.0") == parts[3]);
                return soll != null && kv.Value != soll.Stueckzahl;
            }).Count();

            MessageBox.Show(
                $"Inventur-Ergebnis:\n\n" +
                $"Geprüfte Positionen: {_istBestaende.Count}\n" +
                $"Davon mit Differenz: {differenzen}\n" +
                $"Ohne Differenz: {_istBestaende.Count - differenzen}\n\n" +
                $"Inventur am {DateTime.Now:dd.MM.yyyy HH:mm}",
                "Inventur-Bericht",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AddNewMaterialDuringInventory()
        {
            // Öffne MaterialDialog um neues Material hinzuzufügen
            var dlg = new MaterialDialog(new System.Collections.ObjectModel.ObservableCollection<MaterialItem>(_materialien)) 
            { 
                Owner = this 
            };
            
            if (dlg.ShowDialog() == true)
            {
                // Neues Material wurde erstellt
                var newItem = dlg.Material;
                
                // Füge zu _istBestaende hinzu
                var key = $"{newItem.Lagerort}|{newItem.MaterialArt}|{newItem.Legierung}|{newItem.Staerke}|{newItem.Mass}|{newItem.Restnummer}";
                _istBestaende[key] = newItem.Stueckzahl;
                
                // Baue UI neu auf
                _contentStack.Children.Clear();
                
                var regale = _materialien.Cast<MaterialItem>().Concat(new[] { newItem })
                    .GroupBy(m => m.Lagerort)
                    .OrderBy(g => g.Key);

                foreach (var regal in regale)
                {
                    AddRegalSection(regal.Key, regal.ToList());
                }
                
                MessageBox.Show(
                    $"✅ Neues Material hinzugefügt:\n\n" +
                    $"{newItem.MaterialArt} {newItem.Legierung}\n" +
                    $"Lagerort: {newItem.Lagerort}\n" +
                    $"Stückzahl: {newItem.Stueckzahl}\n\n" +
                    $"Material wird jetzt in der Inventur aufgenommen.",
                    "Material hinzugefügt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
