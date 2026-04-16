using System.IO;
using System.ComponentModel.DataAnnotations.Schema;

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
        public bool IsEilt { get; set; }
        public int SortIndex { get; set; }

        [NotMapped]
        public string MaterialArtStaerkeText { get; set; } = string.Empty;

        [NotMapped]
        public string MaterialAnzeige => string.IsNullOrWhiteSpace(MaterialArtStaerkeText)
            ? MaterialPositionen.ToString()
            : MaterialArtStaerkeText;

        [NotMapped]
        public string PdfPfadKantzeichnung { get; set; } = string.Empty;

        [NotMapped]
        public string PdfDateinameKantzeichnung => string.IsNullOrWhiteSpace(PdfPfadKantzeichnung)
            ? "Keine Kant-PDF"
            : Path.GetFileName(PdfPfadKantzeichnung);

        [NotMapped]
        public string Arbeitsplatz { get; set; } = "Beides";

        public string PdfDateiname
        {
            get
            {
                var pfad = !string.IsNullOrWhiteSpace(PdfPfadAngefangeneTafel)
                    ? PdfPfadAngefangeneTafel
                    : PdfPfad;

                return string.IsNullOrWhiteSpace(pfad)
                    ? "Keine PDF"
                    : Path.GetFileName(pfad);
            }
        }

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

        [NotMapped]
        public string EiltText => IsEilt ? "EILT" : string.Empty;

        [NotMapped]
        public string ProduktionStartText => ProduktionStartDatum?.ToString("dd.MM.yyyy HH:mm") ?? "–";

        [NotMapped]
        public string ProduktionEndText => ProduktionEndDatum?.ToString("dd.MM.yyyy HH:mm") ?? "–";

        [NotMapped]
        public string ProduktionsBegruendung { get; set; } = string.Empty;
    }

    public enum AuftragStatus
    {
        Offen,
        InBearbeitung,
        Abgeschlossen
    }
}
