using System;
using System.Security.Cryptography;
using System.Text;

namespace MaterialManager_V01.Tools
{
    /// <summary>
    /// Standalone Lizenzgenerator-Tool für MaterialManager
    /// Verwendung: dotnet run "HardwareID" "Firmenname" [Jahre]
    /// </summary>
    class LicenseGeneratorTool
    {
        private const string MasterSecret = "MM_V01_MASTER_SECRET_2025_PRODUCTION";

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║       MaterialManager V01 - Lizenzgenerator                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            // Validiere Argumente
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            string hardwareId = args[0];
            string registeredTo = args[1];
            int licenseYears = args.Length > 2 && int.TryParse(args[2], out int years) ? years : 1;

            try
            {
                // Generiere Lizenzschlüssel
                string licenseKey = GenerateLicenseKey(hardwareId, registeredTo, licenseYears);

                // Zeige Ergebnis
                Console.WriteLine("✓ Lizenzschlüssel erfolgreich generiert!\n");
                Console.WriteLine($"Hardware-ID:        {hardwareId}");
                Console.WriteLine($"Registriert auf:    {registeredTo}");
                Console.WriteLine($"Lizenzlaufzeit:     {licenseYears} Jahr(e)");
                Console.WriteLine($"Ablaufdatum:        {DateTime.Now.AddYears(licenseYears):dd.MM.yyyy}");
                Console.WriteLine($"\n{'─'.ToString().PadRight(64, '─')}");
                Console.WriteLine($"LIZENZSCHLÜSSEL:    {licenseKey}");
                Console.WriteLine($"{'─'.ToString().PadRight(64, '─')}\n");

                // Versuche Zwischenablage zu kopieren (nur wenn Windows.Forms verfügbar)
                try
                {
                    // Versuche über TextCopy zu kopieren (einfache Alternative)
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo = new System.Diagnostics.ProcessStartInfo("cmd", $"/c echo {licenseKey} | clip")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    p.Start();
                    p.WaitForExit(500);
                    Console.WriteLine("✓ Lizenzschlüssel wurde in die Zwischenablage kopiert!");
                }
                catch
                {
                    Console.WriteLine("(Zwischenablage-Zugriff nicht verfügbar - bitte manuell kopieren)");
                }

                Console.WriteLine("\n◉ Dieser Schlüssel kann sofort im Programm aktiviert werden.");
                Console.WriteLine("\n" + "═".PadRight(64, '═'));
                Console.WriteLine("KONTAKT & SUPPORT");
                Console.WriteLine("═".PadRight(64, '═'));
                Console.WriteLine("E-Mail: support@materialmanager.de");
                Console.WriteLine("Version: MaterialManager V01 (Vollversion)");
                Console.WriteLine("═".PadRight(64, '═'));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FEHLER: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Generiert einen Lizenzschlüssel mit HMAC-SHA256
        /// </summary>
        static string GenerateLicenseKey(string hardwareId, string registeredTo, int licenseYears = 1)
        {
            try
            {
                var expiryDate = DateTime.Now.AddYears(licenseYears).ToString("yyyyMMdd");
                var data = $"{hardwareId}|{registeredTo}|{expiryDate}|{MasterSecret}";

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(MasterSecret)))
                {
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                    var hashString = Convert.ToBase64String(hash)
                        .Replace("+", "")
                        .Replace("/", "")
                        .Replace("=", "")
                        .Substring(0, 16)
                        .ToUpper();
                    
                    return $"MM-{hashString.Substring(0, 4)}-{hashString.Substring(4, 4)}-{hashString.Substring(8, 4)}-{hashString.Substring(12, 4)}";
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Fehler beim Generieren des Lizenzschlüssels", ex);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("VERWENDUNG:");
            Console.WriteLine("  dotnet run <HardwareID> <Firmenname> [Jahre]\n");
            Console.WriteLine("BEISPIELE:");
            Console.WriteLine("  dotnet run \"ABC123DEF456GHI789JKL012\" \"Musterfirma GmbH\"");
            Console.WriteLine("  dotnet run \"ABC123DEF456GHI789JKL012\" \"Musterfirma GmbH\" 3\n");
            Console.WriteLine("PARAMETER:");
            Console.WriteLine("  HardwareID   - Hardware-ID des Kundencomputers (aus dem Dialog kopieren)");
            Console.WriteLine("  Firmenname   - Name der Firma (wird in der Lizenz gespeichert)");
            Console.WriteLine("  Jahre        - Gültigkeit in Jahren (Standard: 1 Jahr)\n");
            Console.WriteLine("HINWEIS:");
            Console.WriteLine("  Die Hardware-ID finden Sie im Programm über: Hilfe → Lizenzinformationen");
            Environment.Exit(1);
        }
    }
}
