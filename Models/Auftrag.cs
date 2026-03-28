namespace MaterialManager_V01.Models
{
    public class Auftrag
    {
        public int Id { get; set; }
        public string Auftragsnummer { get; set; } = string.Empty;
        public AuftragStatus Status { get; set; } = AuftragStatus.Offen;
        public DateTime ErstelltAm { get; set; } = DateTime.Now;
        public DateTime GeaendertAm { get; set; } = DateTime.Now;
        public string AngelegtVon { get; set; } = string.Empty;
        public string GeaendertVon { get; set; } = string.Empty;
        public int MaterialPositionen { get; set; }
        public int GesamtStueckzahl { get; set; }
        public double GesamtGewichtKg { get; set; }
        public string PdfPfad { get; set; } = string.Empty;
        public string PdfPfadAngefangeneTafel { get; set; } = string.Empty;
        public DateTime? ProduktionStartDatum { get; set; }
        public DateTime? ProduktionEndDatum { get; set; }

        public string ProduktionsDauer
        {
            get
            {
                if (ProduktionStartDatum == null || ProduktionEndDatum == null)
                    return "–";
                
                var duration = ProduktionEndDatum.Value - ProduktionStartDatum.Value;
                if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}h {duration.Minutes}min";
                return $"{duration.Minutes}min";
            }
        }
    }

    public enum AuftragStatus
    {
        Offen,
        InBearbeitung,
        Abgeschlossen
    }
}
