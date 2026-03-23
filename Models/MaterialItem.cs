namespace MaterialManager_V01.Models
{
    public class MaterialItem : System.ComponentModel.INotifyPropertyChanged
    {
        // ── Kategorie ──────────────────────────────────────────────────────────
        public MaterialKategorie Kategorie { get; set; } = MaterialKategorie.Blech;

        public string MaterialArt { get; set; } = "";
        public string Legierung { get; set; } = "";
        public string Oberflaeche { get; set; } = "";
        public string Guete { get; set; } = "";

        // ── Blech-spezifisch ───────────────────────────────────────────────────
        public string Form { get; set; } = "";
        public double Staerke { get; set; }
        public string Mass { get; set; } = "";      // Format "LängexBreite" in mm

        // ── Rohr-spezifisch ────────────────────────────────────────────────────
        public double Durchmesser { get; set; }     // Außen-Ø in mm
        // Wandstärke teilt die Staerke-Property mit Blechen
        // Rohrlänge und Profillänge nutzen Laenge
        public double Laenge { get; set; }          // Länge in mm (Rohr + Profil)

        // ── Profil-spezifisch ──────────────────────────────────────────────────
        public string ProfilTyp { get; set; } = ""; // z. B. "IPE", "U-Profil", "L-Winkel"
        public double ProfilHoehe { get; set; }     // Höhe h in mm
        public double ProfilBreite { get; set; }    // Breite b / Flanschbreite in mm

        // ── Gemeinsam ──────────────────────────────────────────────────────────
        public int Stueckzahl { get; set; } = 1;
        public string Restnummer { get; set; } = "";
        public DateTime? Datum { get; set; }
        public DateTime? AenderungsDatum { get; set; }
        public string Lagerort { get; set; } = "";
        public string AngelegtVon { get; set; } = "";
        public string GeaendertVon { get; set; } = "";
        public string Lieferant { get; set; } = "";
        public string LieferscheinNr { get; set; } = "";
        public string AuftragNr { get; set; } = "";
        public bool IstReserviert => !string.IsNullOrWhiteSpace(AuftragNr);
        public string PdfPfad { get; set; } = "";
        public string PdfDateiname => string.IsNullOrWhiteSpace(PdfPfad) ? "" : System.IO.Path.GetFileName(PdfPfad);
        public bool HasPdf => !string.IsNullOrWhiteSpace(PdfPfad);
        public string LaengeAnzeige => (Kategorie == MaterialKategorie.Rohr || Kategorie == MaterialKategorie.Profil) && Laenge > 0
            ? Laenge.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
            : "";
        public decimal PreisProKg { get; set; } = 0m;
        public decimal Gesamtwert => (decimal)GewichtKg * PreisProKg;

        private bool _isHighlighted;
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set { _isHighlighted = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsHighlighted))); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public double GewichtKg
        {
            get
            {
                double dichte = MaterialArt switch
                {
                    "Stahl"     => 7850,
                    "Edelstahl" => 8000,
                    "Aluminium" => 2700,
                    _           => 0
                };
                if (dichte == 0) return 0;

                try
                {
                    switch (Kategorie)
                    {
                        case MaterialKategorie.Blech:
                        {
                            if (string.IsNullOrWhiteSpace(Mass)) return 0;
                            var teile = Mass.Split('x');
                            if (teile.Length != 2) return 0;
                            double laenge = double.Parse(teile[0]);
                            double breite = double.Parse(teile[1]);
                            double gewicht = (laenge / 1000.0) * (breite / 1000.0) * (Staerke / 1000.0) * dichte * Stueckzahl;
                            return Math.Round(gewicht, 2);
                        }
                        case MaterialKategorie.Rohr:
                        {
                            if (Durchmesser <= 0 || Staerke <= 0 || Laenge <= 0) return 0;
                            double ra = Durchmesser / 2.0 / 1000.0;       // Außenradius in m
                            double ri = (Durchmesser / 2.0 - Staerke) / 1000.0; // Innenradius in m
                            double gewicht = Math.PI * (ra * ra - ri * ri) * (Laenge / 1000.0) * dichte * Stueckzahl;
                            return Math.Round(gewicht, 2);
                        }
                        case MaterialKategorie.Profil:
                        {
                            // Vereinfacht: Gewicht pro Meter als Schätzwert über Querschnitt
                            // Für genaue Berechnung wäre ein Profilgewicht/m-Wert nötig;
                            // hier wird ein generisches Kastenquerschnittmodell verwendet.
                            if (ProfilHoehe <= 0 || Staerke <= 0 || Laenge <= 0) return 0;
                            double b = ProfilBreite > 0 ? ProfilBreite : ProfilHoehe;
                            double querschnitt = 2 * ((ProfilHoehe + b) * Staerke) / 1e6; // m²
                            double gewicht = querschnitt * (Laenge / 1000.0) * dichte * Stueckzahl;
                            return Math.Round(gewicht, 2);
                        }
                        default:
                            return 0;
                    }
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}
