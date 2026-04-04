using MaterialManager_V01.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManager_V01.Services
{
    public static class AuftragDataService
    {
        public static List<Auftrag> LoadAllAuftraege()
        {
            EnsureAuftraegeFromMaterialienIfMissing();

            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();

            var list = db.Auftraege
                .AsNoTracking()
                .OrderBy(a => a.Auftragsnummer)
                .ToList();

            foreach (var auftrag in list)
                auftrag.Arbeitsplatz = AuftragArbeitsplatzService.GetArbeitsplatz(auftrag.Auftragsnummer);

            return list;
        }

        private static void EnsureAuftraegeFromMaterialienIfMissing()
        {
            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();

            var hasAuftraege = db.Auftraege.AsNoTracking().Any();
            if (hasAuftraege)
                return;

            var reservierte = db.Materialien
                .AsNoTracking()
                .Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr))
                .ToList();

            if (!reservierte.Any())
                return;

            var auftraege = reservierte
                .GroupBy(m => m.AuftragNr.Trim())
                .Select(gruppe =>
                {
                    var items = gruppe.ToList();
                    var first = items[0];
                    var hatAngefangeneTafel = items.Any(i =>
                        string.Equals(i.Lagerort, "Angefangene Tafel", StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(i.PdfPfadAngefangeneTafel));

                    return new Auftrag
                    {
                        Auftragsnummer = gruppe.Key,
                        Status = hatAngefangeneTafel ? AuftragStatus.InBearbeitung : AuftragStatus.Offen,
                        ErstelltAm = first.Datum ?? DateTime.Now,
                        GeaendertAm = items.Max(i => i.AenderungsDatum ?? i.Datum ?? DateTime.Now),
                        AngelegtVon = first.AngelegtVon ?? string.Empty,
                        GeaendertVon = items
                            .OrderByDescending(i => i.AenderungsDatum ?? i.Datum ?? DateTime.MinValue)
                            .Select(i => i.GeaendertVon)
                            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
                        MaterialPositionen = items.Count,
                        GesamtStueckzahl = items.Sum(i => i.Stueckzahl),
                        GesamtGewichtKg = Math.Round(items.Sum(i => i.GewichtKg), 2),
                        PdfPfad = items.Select(i => i.PdfPfad).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
                        PdfPfadAngefangeneTafel = items.Select(i => i.PdfPfadAngefangeneTafel).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty
                    };
                })
                .ToList();

            db.Auftraege.AddRange(auftraege);
            db.SaveChanges();
        }
    }
}
