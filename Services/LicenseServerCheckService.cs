using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Prüft die Lizenz gegen ein GitHub Gist und sendet beim Start eine
    /// Push-Benachrichtigung via ntfy.sh – nur sichtbar für den Entwickler.
    /// </summary>
    public static class LicenseServerCheckService
    {
        // ─────────────────────────────────────────────────────────────────
        //  !! HIER DEINE WERTE EINTRAGEN !!
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raw-URL deines GitHub Gist, z.B.:
        /// https://gist.githubusercontent.com/Acid31-31/GIST_ID/raw/licenses.json
        /// Leer lassen → Online-Prüfung deaktiviert (App läuft immer).
        /// </summary>
        private const string GistRawUrl = "";

        /// <summary>
        /// Dein geheimer ntfy.sh Topic-Name (nur du kennst ihn).
        /// Leer lassen → keine Push-Benachrichtigung.
        /// </summary>
        private const string NtfyTopic = "";

        // ─────────────────────────────────────────────────────────────────

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        /// <summary>
        /// Beim App-Start aufrufen: Ping senden + ggf. Lizenz sperren prüfen.
        /// Gibt null zurück wenn alles OK, sonst eine Fehlermeldung.
        /// </summary>
        public static async Task<string?> CheckAndPingAsync()
        {
            var licenseKey = LicenseService.GetCurrentLicenseKey();
            var pcName     = Environment.MachineName;
            var winUser    = Environment.UserName;
            var zeitpunkt  = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            // 1) Push-Benachrichtigung senden (fire & forget, Fehler ignorieren)
            _ = SendNtfyPingAsync(licenseKey, pcName, winUser, zeitpunkt);

            // 2) Gist-Lizenzprüfung (optional)
            if (string.IsNullOrWhiteSpace(GistRawUrl))
                return null;

            try
            {
                var json = await _http.GetStringAsync(GistRawUrl);
                return CheckLicenseInGist(json, licenseKey);
            }
            catch
            {
                // Kein Internet oder Gist nicht erreichbar → App läuft weiter
                return null;
            }
        }

        private static string? CheckLicenseInGist(string json, string licenseKey)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("licenses", out var licenses))
                    return null;

                // Kein Lizenzschlüssel aktiviert → Testversion → OK
                if (string.IsNullOrWhiteSpace(licenseKey))
                    return null;

                if (!licenses.TryGetProperty(licenseKey, out var entry))
                    return null; // Schlüssel nicht in Gist → OK (unbekannt)

                if (entry.TryGetProperty("valid", out var valid) && !valid.GetBoolean())
                {
                    var grund = entry.TryGetProperty("gesperrt_grund", out var g)
                        ? g.GetString() ?? "Lizenz gesperrt."
                        : "Diese Lizenz wurde gesperrt. Bitte den Hersteller kontaktieren.";
                    return grund;
                }

                // Ablaufdatum prüfen
                if (entry.TryGetProperty("ablauf", out var ablauf)
                    && DateTime.TryParse(ablauf.GetString(), out var expires)
                    && DateTime.Now > expires)
                {
                    return $"Lizenz abgelaufen am {expires:dd.MM.yyyy}. Bitte erneuern.";
                }

                return null; // alles OK
            }
            catch
            {
                return null;
            }
        }

        private static async Task SendNtfyPingAsync(string licenseKey, string pcName, string winUser, string zeitpunkt)
        {
            if (string.IsNullOrWhiteSpace(NtfyTopic))
                return;

            try
            {
                var lizenzInfo = string.IsNullOrWhiteSpace(licenseKey) ? "Testversion" : licenseKey;
                var message =
                    $"MaterialManager V01 gestartet\n" +
                    $"PC: {pcName}\n" +
                    $"Windows-User: {winUser}\n" +
                    $"Lizenz: {lizenzInfo}\n" +
                    $"Zeit: {zeitpunkt}";

                var content = new StringContent(message, Encoding.UTF8, "text/plain");
                content.Headers.Add("Title", "MM-V01 Start");
                content.Headers.Add("Priority", "default");

                await _http.PostAsync($"https://ntfy.sh/{NtfyTopic}", content);
            }
            catch
            {
                // Fehler ignorieren – UI darf nicht blockiert werden
            }
        }
    }
}
