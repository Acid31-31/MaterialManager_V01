using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Prüft die Lizenz gegen ein GitHub Gist und sendet beim Start eine
    /// E-Mail-Benachrichtigung – nur sichtbar für den Entwickler.
    /// </summary>
    public static class LicenseServerCheckService
    {
        // ─────────────────────────────────────────────────────────────────
        //  !! HIER DEINE WERTE EINTRAGEN !!
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raw-URL deines GitHub Gist, z.B.:
        /// https://gist.githubusercontent.com/Acid31-31/GIST_ID/raw/licenses.json
        /// Leer lassen → Online-Lizenzprüfung deaktiviert (App läuft immer).
        /// </summary>
        private const string GistRawUrl = "";

        // --- E-Mail Einstellungen (Gmail) ---
        /// <summary>
        /// Absender Gmail-Adresse (z.B. eine neue Wegwerf-Adresse nur für diesen Zweck).
        /// Leer lassen → keine E-Mail.
        /// </summary>
        private const string GmailAbsender = "";      // z.B. "mm.monitor2025@gmail.com"

        /// <summary>
        /// Gmail App-Passwort (NICHT dein normales Passwort!).
        /// Erstellen unter: Google-Konto → Sicherheit → App-Passwörter
        /// Format: "xxxx xxxx xxxx xxxx"
        /// </summary>
        private const string GmailAppPasswort = "";   // z.B. "abcd efgh ijkl mnop"

        /// <summary>
        /// Deine private E-Mail-Adresse, an die die Benachrichtigung gesendet wird.
        /// </summary>
        private const string EmpfaengerEmail = "";    // z.B. "deine@email.com"

        // ─────────────────────────────────────────────────────────────────

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

        /// <summary>
        /// Beim App-Start aufrufen: E-Mail senden + Gist-Lizenzprüfung.
        /// Gibt null zurück wenn alles OK, sonst Fehlermeldung zum Anzeigen.
        /// </summary>
        public static async Task<string?> CheckAndPingAsync()
        {
            var licenseKey = LicenseService.GetCurrentLicenseKey();
            var pcName    = Environment.MachineName;
            var winUser   = Environment.UserName;
            var zeitpunkt = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            // 1) E-Mail senden (im Hintergrund, Fehler werden ignoriert)
            _ = SendStartupMailAsync(licenseKey, pcName, winUser, zeitpunkt);

            // 2) Gist-Lizenzprüfung (optional, nur wenn URL gesetzt)
            if (string.IsNullOrWhiteSpace(GistRawUrl))
                return null;

            try
            {
                var json = await _http.GetStringAsync(GistRawUrl);
                return CheckLicenseInGist(json, licenseKey);
            }
            catch
            {
                return null; // Kein Internet → App läuft weiter
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

                if (string.IsNullOrWhiteSpace(licenseKey))
                    return null; // Testversion → immer OK

                if (!licenses.TryGetProperty(licenseKey, out var entry))
                    return null; // Schlüssel unbekannt → OK

                if (entry.TryGetProperty("valid", out var valid) && !valid.GetBoolean())
                {
                    return entry.TryGetProperty("gesperrt_grund", out var g)
                        ? g.GetString() ?? "Diese Lizenz wurde gesperrt."
                        : "Diese Lizenz wurde gesperrt. Bitte den Hersteller kontaktieren.";
                }

                if (entry.TryGetProperty("ablauf", out var ablauf)
                    && DateTime.TryParse(ablauf.GetString(), out var expires)
                    && DateTime.Now > expires)
                {
                    return $"Lizenz abgelaufen am {expires:dd.MM.yyyy}. Bitte erneuern.";
                }

                return null;
            }
            catch { return null; }
        }

        private static async Task SendStartupMailAsync(string licenseKey, string pcName, string winUser, string zeitpunkt)
        {
            if (string.IsNullOrWhiteSpace(GmailAbsender)
                || string.IsNullOrWhiteSpace(GmailAppPasswort)
                || string.IsNullOrWhiteSpace(EmpfaengerEmail))
                return;

            try
            {
                var lizenzInfo = string.IsNullOrWhiteSpace(licenseKey) ? "Testversion (kein Schlüssel)" : licenseKey;

                var betreff = $"MaterialManager V01 gestartet – {pcName} – {zeitpunkt}";

                var text = new StringBuilder();
                text.AppendLine("MaterialManager V01 wurde gestartet.");
                text.AppendLine();
                text.AppendLine($"PC-Name:       {pcName}");
                text.AppendLine($"Windows-User:  {winUser}");
                text.AppendLine($"Lizenzschlüssel: {lizenzInfo}");
                text.AppendLine($"Zeitpunkt:     {zeitpunkt}");
                text.AppendLine();
                text.AppendLine("─────────────────────────────────────");
                text.AppendLine("Diese Mail wurde automatisch von MaterialManager V01 gesendet.");

                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(GmailAbsender, GmailAppPasswort.Replace(" ", ""))
                };

                var mail = new MailMessage(GmailAbsender, EmpfaengerEmail, betreff, text.ToString())
                {
                    IsBodyHtml = false
                };

                await client.SendMailAsync(mail);
            }
            catch
            {
                // E-Mail-Fehler ignorieren – App läuft weiter
            }
        }
    }
}
