namespace MaterialManager_V01.Models
{
    public class MaterialItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string MaterialArt { get; set; } = "";
        public string Legierung { get; set; } = "";
        public string Oberflaeche { get; set; } = "";
        public string Guete { get; set; } = "";
        public string Form { get; set; } = "";
        public double Staerke { get; set; }
        public string Mass { get; set; } = "";
        public int Stueckzahl { get; set; } = 1;
        public string Restnummer { get; set; } = "";
        public DateTime? Datum { get; set; }  // Erstelldatum (bei Rest = Anlagedatum, bei GF/MF/KF = Lieferdatum)
        public DateTime? AenderungsDatum { get; set; }  // Änderungsdatum (wird bei Bearbeitung gesetzt)
        public string Lagerort { get; set; } = "";

        public string AngelegtVon { get; set; } = "";
        public string GeaendertVon { get; set; } = "";

        // Feature 5: Material-Buchung
        public string Lieferant { get; set; } = "";
        public string LieferscheinNr { get; set; } = "";

        // Feature 7: Auftrags-Zuordnung
        public string AuftragNr { get; set; } = "";
        public bool IstReserviert => !string.IsNullOrWhiteSpace(AuftragNr);

        // Feature 8: Preis & Lagerwert
        public decimal PreisProKg { get; set; } = 0m;  // €/kg vom Lieferanten
        public decimal Gesamtwert => (decimal)GewichtKg * PreisProKg;  // Berechnet

        private bool _isHighlighted;
        public bool IsHighlighted 
        { 
            get => _isHighlighted; 
            set 
            { 
                _isHighlighted = value; 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsHighlighted)));
            } 
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public double GewichtKg
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Mass))
                    return 0;

                try
                {
                    var teile = Mass.Split('x');
                    if (teile.Length != 2)
                        return 0;

                    double laenge = double.Parse(teile[0]);
                    double breite = double.Parse(teile[1]);

                    double dichte = 0;

                    if (MaterialArt == "Stahl") dichte = 7850;
                    if (MaterialArt == "Edelstahl") dichte = 8000;
                    if (MaterialArt == "Aluminium") dichte = 2700;

                    double gewichtProStueck =
                        (laenge / 1000.0) *
                        (breite / 1000.0) *
                        (Staerke / 1000.0) *
                        dichte;

                    // Multipliziere mit Stückzahl
                    double gesamtGewicht = gewichtProStueck * Stueckzahl;

                    return Math.Round(gesamtGewicht, 2);
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}
