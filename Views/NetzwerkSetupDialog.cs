using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace MaterialManager_V01.Views
{
    public partial class NetzwerkSetupDialog : Window
    {
        private class NetzwerkConfig
        {
            public bool Aktiviert { get; set; }
            public string NetzwerkPfad { get; set; } = "";
        }

        public NetzwerkSetupDialog()
        {
            InitializeComponent();
            LoadCurrentConfig();
        }

        private void LoadCurrentConfig()
        {
            try
            {
                var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MaterialManager_V01");
                var configFile = Path.Combine(configDir, "netzwerk_config.json");

                if (File.Exists(configFile))
                {
                    var json = File.ReadAllText(configFile);
                    var config = JsonSerializer.Deserialize<NetzwerkConfig>(json);
                    if (config != null)
                    {
                        NetzwerkModeCheckBox.IsChecked = config.Aktiviert;
                        NetzwerkPfadBox.Text = config.NetzwerkPfad;
                    }
                }
            }
            catch { }
        }

        private void OnSpeichern(object sender, RoutedEventArgs e)
        {
            try
            {
                var netzwerkPfad = NetzwerkPfadBox.Text.Trim();
                var aktiviert = NetzwerkModeCheckBox.IsChecked == true;

                if (aktiviert && string.IsNullOrWhiteSpace(netzwerkPfad))
                {
                    MessageBox.Show("Bitte Netzwerkpfad eingeben!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (aktiviert && !Directory.Exists(netzwerkPfad))
                {
                    var result = MessageBox.Show($"Der Pfad existiert nicht:\n{netzwerkPfad}\n\nMöchten Sie ihn erstellen?", 
                        "Pfad erstellen?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.CreateDirectory(netzwerkPfad);
                    }
                    else
                    {
                        return;
                    }
                }

                var config = new NetzwerkConfig
                {
                    Aktiviert = aktiviert,
                    NetzwerkPfad = netzwerkPfad
                };

                var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MaterialManager_V01");
                Directory.CreateDirectory(configDir);
                var configFile = Path.Combine(configDir, "netzwerk_config.json");

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFile, json);

                MessageBox.Show("✅ Konfiguration gespeichert!\n\nBitte starten Sie die Anwendung neu.", 
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnAbbrechen(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnDurchsuchen(object sender, RoutedEventArgs e)
        {
            // Einfacher Text-Input statt FolderBrowser
            MessageBox.Show("Geben Sie den Pfad direkt ein:\n\nBeispiele:\n• \\\\SERVER\\MaterialManager\n• Z:\\MaterialManager\n• C:\\MaterialManager_Shared", 
                "Pfad eingeben", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
