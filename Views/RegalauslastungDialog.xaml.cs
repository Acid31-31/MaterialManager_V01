using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public partial class RegalauslastungDialog : Window
    {
        private IEnumerable<MaterialItem> _materialien;

        public RegalauslastungDialog(IEnumerable<MaterialItem> materialien)
        {
            InitializeComponent();
            _materialien = materialien;
            LoadRegalData();
        }

        private void LoadRegalData()
        {
            RegalPanel.Children.Clear();

            var regale = RegalService.GetAllLagerorte()
                .Where(r => !string.Equals(r, "EU Palette", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r)
                .ToList();

            foreach (var regal in regale)
            {
                var capacity = RegalService.GetCapacity(regal);
                var currentLoad = RegalService.CalculateCurrentLoad(_materialien, regal);
                var percent = RegalService.ComputeUtilizationPercent(currentLoad, capacity);
                var description = RegalService.GetRegalDescription(regal);

                var border = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var header = new TextBlock
                {
                    Text = $"{regal} - {description}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                Grid.SetRow(header, 0);
                grid.Children.Add(header);

                var progressBar = new ProgressBar
                {
                    Value = percent,
                    Height = 22,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A")),
                    Foreground = GetProgressBarColor(percent),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                Grid.SetRow(progressBar, 1);
                grid.Children.Add(progressBar);

                var detailsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                
                var loadText = new TextBlock
                {
                    Text = $"Belegt: {currentLoad:F2} kg / {capacity:F0} kg",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAA")),
                    Margin = new Thickness(0, 0, 15, 0)
                };
                detailsPanel.Children.Add(loadText);

                var percentText = new TextBlock
                {
                    Text = $"{percent:F1}%",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = GetPercentageColor(percent)
                };
                detailsPanel.Children.Add(percentText);

                var freeSpace = capacity - currentLoad;
                var freeText = new TextBlock
                {
                    Text = $"Frei: {freeSpace:F2} kg",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    Margin = new Thickness(15, 0, 0, 0)
                };
                detailsPanel.Children.Add(freeText);

                Grid.SetRow(detailsPanel, 2);
                grid.Children.Add(detailsPanel);

                border.Child = grid;
                RegalPanel.Children.Add(border);
            }

            var totalLoad = regale.Sum(r => RegalService.CalculateCurrentLoad(_materialien, r));
            var totalCapacity = regale.Sum(r => RegalService.GetCapacity(r));
            var avgPercent = totalCapacity > 0 ? (totalLoad / totalCapacity) * 100 : 0;

            var summaryBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var summaryStack = new StackPanel();
            
            var summaryTitle = new TextBlock
            {
                Text = "📊 Gesamt-Übersicht",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            summaryStack.Children.Add(summaryTitle);

            var summaryText = new TextBlock
            {
                Text = $"Gesamt belegt: {totalLoad:F2} kg / {totalCapacity:F0} kg\n" +
                       $"Durchschnittliche Auslastung: {avgPercent:F1}%\n" +
                       $"Freie Kapazität: {(totalCapacity - totalLoad):F2} kg",
                FontSize = 14,
                Foreground = Brushes.White,
                LineHeight = 24
            };
            summaryStack.Children.Add(summaryText);

            summaryBorder.Child = summaryStack;
            RegalPanel.Children.Add(summaryBorder);
        }

        private Brush GetProgressBarColor(double percent)
        {
            if (percent < 50) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            if (percent < 80) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
        }

        private Brush GetPercentageColor(double percent)
        {
            if (percent < 50) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            if (percent < 80) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            LoadRegalData();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
