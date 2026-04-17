using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
                        if (IsNeutral(border.Background)) border.Background = palette.Surface;
                        if (IsNeutral(border.BorderBrush)) border.BorderBrush = palette.Border;
                        break;

                    case Panel panel:
                        if (IsNeutral(panel.Background)) panel.Background = palette.Surface;
                        break;

                    case TextBlock textBlock:
                        if (IsNeutral(textBlock.Foreground, includeWhiteBlack: true))
                            textBlock.Foreground = textBlock.Foreground is SolidColorBrush sb && IsMuted(sb.Color)
                                ? palette.MutedForeground
                                : palette.Foreground;
                        break;

                    case TextBox textBox:
                        if (IsNeutral(textBox.Background)) textBox.Background = palette.Surface;
                        if (IsNeutral(textBox.Foreground, includeWhiteBlack: true)) textBox.Foreground = palette.Foreground;
                        if (IsNeutral(textBox.BorderBrush)) textBox.BorderBrush = palette.Border;
                        break;

                    case ComboBox comboBox:
                        if (IsNeutral(comboBox.Background)) comboBox.Background = palette.Surface;
                        if (IsNeutral(comboBox.Foreground, includeWhiteBlack: true)) comboBox.Foreground = palette.Foreground;
                        if (IsNeutral(comboBox.BorderBrush)) comboBox.BorderBrush = palette.Border;
                        break;

                    case DataGrid dataGrid:
                        if (IsNeutral(dataGrid.Background)) dataGrid.Background = palette.Surface;
                        if (IsNeutral(dataGrid.Foreground, includeWhiteBlack: true)) dataGrid.Foreground = palette.Foreground;
                        if (IsNeutral(dataGrid.RowBackground)) dataGrid.RowBackground = palette.Surface;
                        if (IsNeutral(dataGrid.AlternatingRowBackground)) dataGrid.AlternatingRowBackground = palette.AltSurface;
                        if (IsNeutral(dataGrid.BorderBrush)) dataGrid.BorderBrush = palette.Border;
                        break;

                    case Button button:
                        if (IsNeutral(button.Background)) button.Background = palette.Button;
                        if (IsNeutral(button.Foreground, includeWhiteBlack: true)) button.Foreground = palette.Foreground;
                        if (IsNeutral(button.BorderBrush)) button.BorderBrush = palette.Border;
                        break;
                }

                ApplyToVisualTree(child, palette);
            }
        }

        private static ThemePalette GetPalette(AppTheme theme)
        {
            if (theme == AppTheme.Light)
            {
                return new ThemePalette(
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F6F8")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF1F4")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C9CED6")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5F6368")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8EBEF"))
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

        private static bool IsNeutral(Brush? brush, bool includeWhiteBlack = false)
        {
            if (brush == null)
                return true;

            if (brush is not SolidColorBrush solid)
                return false;

            var c = solid.Color;
            if (c.A == 0)
                return true;

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
