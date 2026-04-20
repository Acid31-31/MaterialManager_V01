using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MaterialManager_V01.Services
{
    public enum AppTheme
    {
        Dark,
        Light
    }

    public static class ThemeService
    {
        private sealed class ThemeSettings
        {
            public string Theme { get; set; } = nameof(AppTheme.Dark);
        }

        private static readonly string ThemeSettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01",
            "theme_settings.json");

        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

        public static void Initialize()
        {
            CurrentTheme = LoadTheme();
            ApplyAppResources(CurrentTheme);
        }

        public static void SetTheme(AppTheme theme)
        {
            if (CurrentTheme == theme)
                return;

            CurrentTheme = theme;
            ApplyAppResources(theme);
            SaveTheme(theme);

            foreach (Window window in Application.Current.Windows)
                ApplyThemeToWindow(window);
        }

        public static void ApplyThemeToWindow(Window? window)
        {
            if (window == null)
                return;

            var palette = GetPalette(CurrentTheme);

            if (IsNeutral(window.Background))
                window.Background = palette.WindowBackground;
            if (IsNeutral(window.Foreground, includeWhiteBlack: true))
                window.Foreground = palette.Foreground;

            ApplyToVisualTree(window, palette);
        }

        private static void ApplyAppResources(AppTheme theme)
        {
            var palette = GetPalette(theme);
            var resources = Application.Current.Resources;
            resources["ThemeWindowBackgroundBrush"] = palette.WindowBackground;
            resources["ThemeSurfaceBrush"] = palette.Surface;
            resources["ThemeAltSurfaceBrush"] = palette.AltSurface;
            resources["ThemeBorderBrush"] = palette.Border;
            resources["ThemeForegroundBrush"] = palette.Foreground;
            resources["ThemeMutedForegroundBrush"] = palette.MutedForeground;
        }

        private static void ApplyToVisualTree(DependencyObject root, ThemePalette palette)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                switch (child)
                {
                    case Border border:
                        if (ShouldRecolorBackground(border.Background)) border.Background = MapNeutralBackground(border.Background, palette);
                        if (IsNeutral(border.BorderBrush)) border.BorderBrush = palette.Border;
                        break;

                    case Panel panel:
                        if (ShouldRecolorBackground(panel.Background)) panel.Background = MapNeutralBackground(panel.Background, palette);
                        break;

                    case Menu menu:
                        if (ShouldRecolorBackground(menu.Background)) menu.Background = palette.Surface;
                        if (IsNeutral(menu.Foreground, includeWhiteBlack: true)) menu.Foreground = palette.Foreground;
                        break;

                    case MenuItem menuItem:
                        if (ShouldRecolorBackground(menuItem.Background)) menuItem.Background = palette.Surface;
                        if (IsNeutral(menuItem.Foreground, includeWhiteBlack: true)) menuItem.Foreground = palette.Foreground;
                        if (IsNeutral(menuItem.BorderBrush)) menuItem.BorderBrush = palette.Border;
                        break;

                    case Separator separator:
                        if (ShouldRecolorBackground(separator.Background)) separator.Background = palette.Border;
                        break;

                    case DataGridColumnHeader header:
                        if (ShouldRecolorBackground(header.Background)) header.Background = palette.AltSurface;
                        if (IsNeutral(header.Foreground, includeWhiteBlack: true)) header.Foreground = palette.Foreground;
                        if (IsNeutral(header.BorderBrush)) header.BorderBrush = palette.Border;
                        break;

                    case DataGridCell cell:
                        if (ShouldRecolorBackground(cell.Background)) cell.Background = palette.Surface;
                        if (IsNeutral(cell.Foreground, includeWhiteBlack: true)) cell.Foreground = palette.Foreground;
                        if (IsNeutral(cell.BorderBrush)) cell.BorderBrush = palette.Border;
                        break;

                    case DataGridRow row:
                        if (ShouldRecolorBackground(row.Background)) row.Background = palette.Surface;
                        if (IsNeutral(row.Foreground, includeWhiteBlack: true)) row.Foreground = palette.Foreground;
                        break;

                    case TextBlock textBlock:
                        if (IsNeutral(textBlock.Foreground, includeWhiteBlack: true))
                            textBlock.Foreground = textBlock.Foreground is SolidColorBrush sb && IsMuted(sb.Color)
                                ? palette.MutedForeground
                                : palette.Foreground;
                        break;

                    case TextBox textBox:
                        if (ShouldRecolorBackground(textBox.Background)) textBox.Background = MapNeutralBackground(textBox.Background, palette);
                        if (IsNeutral(textBox.Foreground, includeWhiteBlack: true)) textBox.Foreground = palette.Foreground;
                        if (IsNeutral(textBox.BorderBrush)) textBox.BorderBrush = palette.Border;
                        break;

                    case ComboBox comboBox:
                        if (ShouldRecolorBackground(comboBox.Background)) comboBox.Background = MapNeutralBackground(comboBox.Background, palette);
                        if (IsNeutral(comboBox.Foreground, includeWhiteBlack: true)) comboBox.Foreground = palette.Foreground;
                        if (IsNeutral(comboBox.BorderBrush)) comboBox.BorderBrush = palette.Border;
                        ApplyComboBoxReadability(comboBox, palette);
                        break;

                    case Slider slider:
                        if (ShouldRecolorBackground(slider.Background)) slider.Background = palette.AltSurface;
                        if (IsNeutral(slider.Foreground, includeWhiteBlack: true)) slider.Foreground = palette.Foreground;
                        break;

                    case DataGrid dataGrid:
                        if (ShouldRecolorBackground(dataGrid.Background)) dataGrid.Background = palette.Surface;
                        if (IsNeutral(dataGrid.Foreground, includeWhiteBlack: true)) dataGrid.Foreground = palette.Foreground;
                        if (ShouldRecolorBackground(dataGrid.RowBackground)) dataGrid.RowBackground = palette.Surface;
                        if (ShouldRecolorBackground(dataGrid.AlternatingRowBackground)) dataGrid.AlternatingRowBackground = palette.AltSurface;
                        if (IsNeutral(dataGrid.BorderBrush)) dataGrid.BorderBrush = palette.Border;

                        if (CurrentTheme == AppTheme.Light)
                            ApplyReadableDataGridStyles(dataGrid, palette);
                        break;

                    case Button button:
                        if (ShouldRecolorBackground(button.Background)) button.Background = palette.Button;
                        if (IsNeutral(button.Foreground, includeWhiteBlack: true)) button.Foreground = palette.Foreground;
                        if (IsNeutral(button.BorderBrush)) button.BorderBrush = palette.Border;
                        break;
                }

                ApplyToVisualTree(child, palette);
            }
        }

        private static void ApplyReadableDataGridStyles(DataGrid dataGrid, ThemePalette palette)
        {
            dataGrid.ColumnHeaderStyle = BuildHeaderStyle(dataGrid.ColumnHeaderStyle, palette);
            dataGrid.CellStyle = BuildCellStyle(dataGrid.CellStyle, palette);
            dataGrid.RowStyle = BuildRowStyle(dataGrid.RowStyle, palette);
        }

        private static Style BuildHeaderStyle(Style? baseStyle, ThemePalette palette)
        {
            var style = new Style(typeof(DataGridColumnHeader), baseStyle);
            style.Setters.Add(new Setter(Control.ForegroundProperty, palette.Foreground));
            style.Setters.Add(new Setter(Control.BackgroundProperty, palette.AltSurface));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, palette.Border));
            return style;
        }

        private static Style BuildCellStyle(Style? baseStyle, ThemePalette palette)
        {
            var style = new Style(typeof(DataGridCell), baseStyle);
            style.Setters.Add(new Setter(Control.ForegroundProperty, palette.Foreground));
            style.Setters.Add(new Setter(Control.BackgroundProperty, palette.Surface));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, palette.Border));
            return style;
        }

        private static Style BuildRowStyle(Style? baseStyle, ThemePalette palette)
        {
            var style = new Style(typeof(DataGridRow), baseStyle);
            style.Setters.Add(new Setter(Control.ForegroundProperty, palette.Foreground));

            var selectedTrigger = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            style.Triggers.Add(selectedTrigger);

            return style;
        }

        private static void ApplyComboBoxReadability(ComboBox comboBox, ThemePalette palette)
        {
            if (CurrentTheme != AppTheme.Light)
            {
                // Dunkelmodus: explizit dunkle Werte setzen
                comboBox.Background = palette.Surface;
                comboBox.Foreground = palette.Foreground;
                comboBox.BorderBrush = palette.Border;

                // ItemContainerStyle mit dunklen Farben setzen (NICHT löschen - sonst greift System-Standard mit weißem Hintergrund)
                var baseItemStyle = comboBox.TryFindResource(typeof(ComboBoxItem)) as Style;
                var darkItemStyle = new Style(typeof(ComboBoxItem), baseItemStyle);
                darkItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, palette.Foreground));
                darkItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, palette.Surface));
                comboBox.ItemContainerStyle = darkItemStyle;

                comboBox.Resources.Remove(SystemColors.HighlightTextBrushKey);
                comboBox.Resources.Remove(SystemColors.ControlTextBrushKey);
                comboBox.Resources.Remove(SystemColors.WindowTextBrushKey);
                comboBox.Resources.Remove(SystemColors.HighlightBrushKey);
                comboBox.Resources.Remove(SystemColors.InactiveSelectionHighlightBrushKey);
                return;
            }

            var textBrush = palette.Foreground;
            var backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D8CF"));
            var selectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5B8AA"));

            comboBox.Foreground = textBrush;
            comboBox.Background = backgroundBrush;
            comboBox.Resources[SystemColors.HighlightTextBrushKey] = textBrush;
            comboBox.Resources[SystemColors.ControlTextBrushKey] = textBrush;
            comboBox.Resources[SystemColors.WindowTextBrushKey] = textBrush;
            comboBox.Resources[SystemColors.HighlightBrushKey] = selectedBrush;
            comboBox.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = selectedBrush;

            // BasedOn preserves the custom ControlTemplate defined in Window.Resources
            var baseStyle = comboBox.TryFindResource(typeof(ComboBoxItem)) as Style;
            var itemStyle = new Style(typeof(ComboBoxItem), baseStyle);
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, textBrush));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, backgroundBrush));

            var selectedTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, selectedBrush));
            itemStyle.Triggers.Add(selectedTrigger);

            var highlightedTrigger = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            highlightedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            highlightedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, selectedBrush));
            itemStyle.Triggers.Add(highlightedTrigger);

            comboBox.ItemContainerStyle = itemStyle;
        }

        private static bool ShouldRecolorBackground(Brush? brush)
        {
            if (brush is not SolidColorBrush solid)
                return false;

            if (solid.Color.A == 0)
                return false;

            return IsNeutral(solid);
        }

        private static ThemePalette GetPalette(AppTheme theme)
        {
            if (theme == AppTheme.Light)
            {
                return new ThemePalette(
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AEB7AC")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B9C2B6")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AEB7AC")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7E887E")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#102018")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F3127")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5AEA4"))
                );
            }

            return new ThemePalette(
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B0B0B")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"))
            );
        }

        private static SolidColorBrush MapNeutralBackground(Brush? source, ThemePalette palette)
        {
            if (CurrentTheme != AppTheme.Light || source is not SolidColorBrush solid)
                return palette.Surface;

            var c = solid.Color;
            var luma = (c.R + c.G + c.B) / 3;

            if (luma <= 26)
                return palette.AltSurface;
            if (luma <= 58)
                return palette.Surface;
            if (luma <= 96)
                return palette.AltSurface;

            return palette.WindowBackground;
        }

        private static bool IsNeutral(Brush? brush, bool includeWhiteBlack = false)
        {
            if (brush == null)
                return false;

            if (brush is not SolidColorBrush solid)
                return false;

            var c = solid.Color;
            if (c.A == 0)
                return false;

            var diffRg = Math.Abs(c.R - c.G);
            var diffGb = Math.Abs(c.G - c.B);
            var diffRb = Math.Abs(c.R - c.B);
            var grayscale = diffRg <= 14 && diffGb <= 14 && diffRb <= 14;

            if (grayscale)
                return true;

            if (!includeWhiteBlack)
                return false;

            return (c.R > 245 && c.G > 245 && c.B > 245)
                || (c.R < 20 && c.G < 20 && c.B < 20);
        }

        private static bool IsMuted(Color color)
        {
            return color.R > 120 && color.G > 120 && color.B > 120 && color.R < 200 && color.G < 200 && color.B < 200;
        }

        private static AppTheme LoadTheme()
        {
            try
            {
                if (!File.Exists(ThemeSettingsFile))
                    return AppTheme.Dark;

                var json = File.ReadAllText(ThemeSettingsFile);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                if (settings == null)
                    return AppTheme.Dark;

                return Enum.TryParse<AppTheme>(settings.Theme, true, out var parsed)
                    ? parsed
                    : AppTheme.Dark;
            }
            catch
            {
                return AppTheme.Dark;
            }
        }

        private static void SaveTheme(AppTheme theme)
        {
            try
            {
                var dir = Path.GetDirectoryName(ThemeSettingsFile);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var settings = new ThemeSettings { Theme = theme.ToString() };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ThemeSettingsFile, json);
            }
            catch
            {
            }
        }

        private sealed record ThemePalette(
            SolidColorBrush WindowBackground,
            SolidColorBrush Surface,
            SolidColorBrush AltSurface,
            SolidColorBrush Border,
            SolidColorBrush Foreground,
            SolidColorBrush MutedForeground,
            SolidColorBrush Button);
    }
}
