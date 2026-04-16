using System.IO;
using System.Text.Json;
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
                .OrderBy(a => a.SortIndex)
                .ThenBy(a => a.Auftragsnummer)
                .ToList();

            ApplySharedAuftragsState(list);

            var materialAnzeigeLookup = BuildMaterialAnzeigeLookup(db);

            foreach (var auftrag in list)
            {
                var normalized = (auftrag.Auftragsnummer ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(normalized) && materialAnzeigeLookup.TryGetValue(normalized, out var anzeige))
                    auftrag.MaterialArtStaerkeText = anzeige;

                auftrag.Arbeitsplatz = AuftragArbeitsplatzService.GetArbeitsplatz(auftrag.Auftragsnummer);
                auftrag.PdfPfad = AuftragArchivService.ResolveAccessiblePdfPath(auftrag.Auftragsnummer, auftrag.PdfPfad);
                auftrag.PdfPfadKantzeichnung = string.Empty;
            }

            return list;
        }

        private static Dictionary<string, string> BuildMaterialAnzeigeLookup(MaterialManagerDbContext db)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var materialByOrder = db.Materialien
                .AsNoTracking()
                .Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr))
                .GroupBy(m => m.AuftragNr!.Trim())
                .ToList();

            foreach (var group in materialByOrder)
            {
                var teile = group
                    .Select(m =>
                    {
                        var art = (m.MaterialArt ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(art))
                            art = "Material";

                        return $"{art}-{m.Staerke:0.0} mm";
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

                if (teile.Count == 0)
                    continue;

                var text = string.Join(", ", teile);
                if (group.Count() > teile.Count)
                    text += ", ...";

                result[group.Key] = text;
            }

            return result;
        }

        public static Dictionary<string, Auftrag> LoadSharedAuftraegeLookup()
        {
            return LoadSharedAuftraege()
                .Where(a => !string.IsNullOrWhiteSpace(a.Auftragsnummer))
                .GroupBy(a => a.Auftragsnummer.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .ToDictionary(a => a.Auftragsnummer.Trim(), CloneAuftrag, StringComparer.OrdinalIgnoreCase);
        }

        public static void TryUpsertSharedAuftrag(Auftrag auftrag)
        {
            if (!NetzwerkService.IsNetzwerkModus || string.IsNullOrWhiteSpace(auftrag?.Auftragsnummer))
                return;

            var list = LoadSharedAuftraege();
            var normalized = auftrag.Auftragsnummer.Trim();
            var existingIndex = list.FindIndex(a => string.Equals((a.Auftragsnummer ?? string.Empty).Trim(), normalized, StringComparison.OrdinalIgnoreCase));
            var snapshot = CloneAuftrag(auftrag);

            if (existingIndex >= 0)
                list[existingIndex] = snapshot;
            else
                list.Add(snapshot);

            SaveSharedAuftraege(list);
        }

        public static void TrySyncSharedAuftraegeFromDatabase()
        {
            if (!NetzwerkService.IsNetzwerkModus)
                return;

            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();
            var list = db.Auftraege
                .AsNoTracking()
                .OrderBy(a => a.SortIndex)
                .ThenBy(a => a.Auftragsnummer)
                .ToList();

            SaveSharedAuftraege(list);
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
                        PdfPfadAngefangeneTafel = items.Select(i => i.PdfPfadAngefangeneTafel).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
                        IsEilt = false,
                        SortIndex = 0
                    };
                })
                .ToList();

            db.Auftraege.AddRange(auftraege);
            db.SaveChanges();
            SaveSharedAuftraege(auftraege);
        }

        public static bool AddAuftrag(Auftrag auftrag)
        {
            if (auftrag == null || string.IsNullOrWhiteSpace(auftrag.Auftragsnummer))
                return false;

            var normalized = auftrag.Auftragsnummer.Trim();

            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();

            var exists = db.Auftraege
                .AsEnumerable()
                .Any(a => string.Equals((a.Auftragsnummer ?? string.Empty).Trim(), normalized, StringComparison.OrdinalIgnoreCase));
            if (exists)
                return false;

            auftrag.Auftragsnummer = normalized;
            db.Auftraege.Add(CloneAuftrag(auftrag));
            db.SaveChanges();
            TryUpsertSharedAuftrag(auftrag);
            return true;
        }

        public static bool UpdateAuftrag(string auftragsnummer, Action<Auftrag> updateAction)
        {
            var normalized = (auftragsnummer ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();

            var auftrag = db.Auftraege
                .AsEnumerable()
                .FirstOrDefault(a => string.Equals((a.Auftragsnummer ?? string.Empty).Trim(), normalized, StringComparison.OrdinalIgnoreCase));

            if (auftrag == null)
                return false;

            updateAction(auftrag);
            db.SaveChanges();
            TryUpsertSharedAuftrag(auftrag);
            return true;
        }

        private static void ApplySharedAuftragsState(List<Auftrag> localAuftraege)
        {
            var sharedByNumber = LoadSharedAuftraegeLookup();
            if (sharedByNumber.Count == 0)
                return;

            foreach (var local in localAuftraege)
            {
                var normalized = (local.Auftragsnummer ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normalized) || !sharedByNumber.TryGetValue(normalized, out var shared))
                    continue;

                local.Status = shared.Status;
                local.ProduktionStartDatum = shared.ProduktionStartDatum;
                local.ProduktionEndDatum = shared.ProduktionEndDatum;
                local.IsEilt = shared.IsEilt;
                local.SortIndex = shared.SortIndex;
                local.ErstelltAm = shared.ErstelltAm;
                local.GeaendertAm = shared.GeaendertAm;
                local.AngelegtVon = shared.AngelegtVon;
                local.GeaendertVon = shared.GeaendertVon;
                if (!string.IsNullOrWhiteSpace(shared.PdfPfad))
                    local.PdfPfad = shared.PdfPfad;
                if (!string.IsNullOrWhiteSpace(shared.PdfPfadAngefangeneTafel))
                    local.PdfPfadAngefangeneTafel = shared.PdfPfadAngefangeneTafel;
            }
        }

        private static void PersistLocalAuftraegeSnapshot(List<Auftrag> auftraege)
        {
            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();
            db.Auftraege.RemoveRange(db.Auftraege);
            db.SaveChanges();
            db.Auftraege.AddRange(auftraege.Select(CloneAuftrag));
            db.SaveChanges();
        }

        private static List<Auftrag> LoadSharedAuftraege()
        {
            try
            {
                var path = GetSharedAuftraegePath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return new List<Auftrag>();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Auftrag>>(json) ?? new List<Auftrag>();
            }
            catch
            {
                return new List<Auftrag>();
            }
        }

        private static void SaveSharedAuftraege(IEnumerable<Auftrag> auftraege)
        {
            if (!NetzwerkService.IsNetzwerkModus)
                return;

            try
            {
                var path = GetSharedAuftraegePath();
                if (string.IsNullOrWhiteSpace(path))
                    return;

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var snapshot = auftraege
                    .Where(a => !string.IsNullOrWhiteSpace(a.Auftragsnummer))
                    .Select(CloneAuftrag)
                    .OrderBy(a => a.SortIndex)
                    .ThenBy(a => a.Auftragsnummer)
                    .ToList();

                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                FileWatcherService.RegisterLocalWrite(path);
                AutoSyncManager.RegisterLocalSave(path);
                File.WriteAllText(path, json);
            }
            catch
            {
            }
        }

        private static string GetSharedAuftraegePath()
        {
            if (!NetzwerkService.IsNetzwerkModus)
                return string.Empty;

            var materialPath = NetzwerkService.GetSavePath();
            var dir = Path.GetDirectoryName(materialPath);
            return string.IsNullOrWhiteSpace(dir) ? string.Empty : Path.Combine(dir, "auftraege.json");
        }

        private static Auftrag CloneAuftrag(Auftrag source)
        {
            return new Auftrag
            {
                Id = source.Id,
                Auftragsnummer = source.Auftragsnummer,
                Arbeitsplatz = source.Arbeitsplatz,
                Status = source.Status,
                ProduktionStartDatum = source.ProduktionStartDatum,
                ProduktionEndDatum = source.ProduktionEndDatum,
                ErstelltAm = source.ErstelltAm,
                GeaendertAm = source.GeaendertAm,
                AngelegtVon = source.AngelegtVon,
                GeaendertVon = source.GeaendertVon,
                MaterialPositionen = source.MaterialPositionen,
                GesamtStueckzahl = source.GesamtStueckzahl,
                GesamtGewichtKg = source.GesamtGewichtKg,
                PdfPfad = source.PdfPfad,
                PdfPfadAngefangeneTafel = source.PdfPfadAngefangeneTafel,
                PdfPfadKantzeichnung = source.PdfPfadKantzeichnung,
                IsEilt = source.IsEilt,
                SortIndex = source.SortIndex
            };
        }
    }
}
