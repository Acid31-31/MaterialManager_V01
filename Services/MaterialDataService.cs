using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MaterialManager_V01.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManager_V01.Services
{
    public static class MaterialDataService
    {
        public static string? LastExcelSyncError { get; private set; }
        public static DateTime? LastExcelSyncUtc { get; private set; }
        public static string? LastExcelSyncPath { get; private set; }

        public static List<MaterialItem> LoadAllMaterials()
        {
            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();

            var dbItems = db.Materialien
                .AsNoTracking()
                .OrderBy(m => m.Id)
                .ToList();

            if (!NetzwerkService.IsNetzwerkModus)
                return dbItems;

            try
            {
                var savePath = NetzwerkService.GetSavePath();

                if (File.Exists(savePath))
                {
                    var excelWriteUtc = GetFileWriteTimeUtcSafe(savePath);
                    var dbWriteUtc = GetFileWriteTimeUtcSafe(PathService.DatabasePath);

                    // Wenn Excel manuell gepflegt wurde und neuer ist, aus Excel neu einlesen.
                    if (dbItems.Count == 0 || (excelWriteUtc.HasValue && dbWriteUtc.HasValue && excelWriteUtc.Value > dbWriteUtc.Value.AddSeconds(2)))
                    {
                        var sharedItems = LoadFromExcelFile(savePath);
                        if (sharedItems.Count > 0)
                        {
                            PersistToDatabase(sharedItems);
                            return sharedItems;
                        }
                    }
                }

                // DB ist führend, wenn keine neuere Excel-Änderung vorliegt.
                if (dbItems.Count > 0)
                {
                    if (!File.Exists(savePath))
                        TrySyncExcel(dbItems);

                    return dbItems;
                }
            }
            catch
            {
            }

            return dbItems;
        }

        public static List<MaterialItem> LoadFromExcelFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return new List<MaterialItem>();

            return ExcelService.Import(filePath)?.ToList() ?? new List<MaterialItem>();
        }

        public static Task<List<MaterialItem>> LoadAllMaterialsAsync()
        {
            return Task.Run(LoadAllMaterials);
        }

        public static void SaveAllMaterials(IEnumerable<MaterialItem> materialien, bool syncExcel = true)
        {
            var snapshot = materialien?.Select(CloneMaterial).ToList() ?? new List<MaterialItem>();
            PersistToDatabase(snapshot);

            if (syncExcel)
                TrySyncExcel(snapshot);
        }

        private static void PersistToDatabase(IEnumerable<MaterialItem> materialien)
        {
            var snapshot = materialien.Select(CloneMaterial).ToList();

            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();
            using var transaction = db.Database.BeginTransaction();

            db.Materialien.RemoveRange(db.Materialien);
            db.SaveChanges();

            db.Materialien.AddRange(snapshot.Select(CloneMaterial));
            db.SaveChanges();

            SyncAuftraege(db, snapshot);
            db.SaveChanges();
            transaction.Commit();
        }

        private static void SyncAuftraege(MaterialManagerDbContext db, IEnumerable<MaterialItem> materialien)
        {
            var existingByNumber = db.Auftraege
                .AsNoTracking()
                .Where(a => !string.IsNullOrWhiteSpace(a.Auftragsnummer))
                .ToDictionary(a => a.Auftragsnummer.Trim(), StringComparer.OrdinalIgnoreCase);

            db.Auftraege.RemoveRange(db.Auftraege);

            var auftraege = materialien
                .Where(m => !string.IsNullOrWhiteSpace(m.AuftragNr))
                .GroupBy(m => m.AuftragNr.Trim())
                .Select(gruppe =>
                {
                    var items = gruppe.ToList();
                    var first = items[0];
                    var hatAngefangeneTafel = items.Any(i =>
                        string.Equals(i.Lagerort, "Angefangene Tafel", StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(i.PdfPfadAngefangeneTafel));

                    existingByNumber.TryGetValue(gruppe.Key, out var existing);
                    var status = existing?.Status ?? (hatAngefangeneTafel ? AuftragStatus.InBearbeitung : AuftragStatus.Offen);

                    return new Auftrag
                    {
                        Auftragsnummer = gruppe.Key,
                        Status = status,
                        ErstelltAm = existing?.ErstelltAm ?? first.Datum ?? DateTime.Now,
                        GeaendertAm = items.Max(i => i.AenderungsDatum ?? i.Datum ?? DateTime.Now),
                        AngelegtVon = string.IsNullOrWhiteSpace(existing?.AngelegtVon) ? first.AngelegtVon : existing.AngelegtVon,
                        GeaendertVon = items
                            .OrderByDescending(i => i.AenderungsDatum ?? i.Datum ?? DateTime.MinValue)
                            .Select(i => i.GeaendertVon)
                            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
                        MaterialPositionen = items.Count,
                        GesamtStueckzahl = items.Sum(i => i.Stueckzahl),
                        GesamtGewichtKg = Math.Round(items.Sum(i => i.GewichtKg), 2),
                        PdfPfad = items.Select(i => i.PdfPfad).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
                        PdfPfadAngefangeneTafel = items.Select(i => i.PdfPfadAngefangeneTafel).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
                        ProduktionStartDatum = existing?.ProduktionStartDatum,
                        ProduktionEndDatum = existing?.ProduktionEndDatum
                    };
                })
                .ToList();

            db.Auftraege.AddRange(auftraege);
        }

        private static void TrySyncExcel(IEnumerable<MaterialItem> materialien)
        {
            try
            {
                SyncExcel(materialien);
                LastExcelSyncError = null;
                LastExcelSyncUtc = DateTime.UtcNow;
                LastExcelSyncPath = NetzwerkService.GetSavePath();
            }
            catch (Exception ex)
            {
                LastExcelSyncError = ex.Message;
                LastExcelSyncUtc = DateTime.UtcNow;
                LastExcelSyncPath = NetzwerkService.GetSavePath();
            }
        }

        private static void SyncExcel(IEnumerable<MaterialItem> materialien)
        {
            var savePath = NetzwerkService.GetSavePath();
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            ExcelService.Export(savePath, materialien);
            CreateNetworkBackupCopy(savePath);
        }

        private static void CreateNetworkBackupCopy(string savePath)
        {
            if (!NetzwerkService.IsNetzwerkModus || !File.Exists(savePath))
                return;

            var saveDir = Path.GetDirectoryName(savePath);
            if (string.IsNullOrWhiteSpace(saveDir))
                return;

            var backupRoot = Path.Combine(saveDir, "Backups");
            var dayFolder = Path.Combine(backupRoot, DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dayFolder);

            var backupFile = Path.Combine(dayFolder, $"materialbestand_{DateTime.Now:HHmmss}.xlsx");
            File.Copy(savePath, backupFile, overwrite: true);

            var keepAfter = DateTime.Now.Date.AddDays(-30);
            foreach (var folder in Directory.GetDirectories(backupRoot))
            {
                try
                {
                    var name = Path.GetFileName(folder);
                    if (DateTime.TryParse(name, out var date) && date.Date < keepAfter)
                        Directory.Delete(folder, true);
                }
                catch
                {
                }
            }
        }

        private static DateTime? GetFileWriteTimeUtcSafe(string? path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return File.GetLastWriteTimeUtc(path);
            }
            catch
            {
            }

            return null;
        }

        private static MaterialItem CloneMaterial(MaterialItem source)
        {
            return new MaterialItem
            {
                Id = source.Id,
                Kategorie = source.Kategorie,
                MaterialArt = source.MaterialArt,
                Legierung = source.Legierung,
                Oberflaeche = source.Oberflaeche,
                Guete = source.Guete,
                SuchTrefferArt = source.SuchTrefferArt,
                Form = source.Form,
                Staerke = source.Staerke,
                Mass = source.Mass,
                Durchmesser = source.Durchmesser,
                Laenge = source.Laenge,
                ProfilTyp = source.ProfilTyp,
                ProfilHoehe = source.ProfilHoehe,
                ProfilBreite = source.ProfilBreite,
                Stueckzahl = source.Stueckzahl,
                Restnummer = source.Restnummer,
                Datum = source.Datum,
                AenderungsDatum = source.AenderungsDatum,
                Lagerort = source.Lagerort,
                AngelegtVon = source.AngelegtVon,
                GeaendertVon = source.GeaendertVon,
                Lieferant = source.Lieferant,
                LieferscheinNr = source.LieferscheinNr,
                AuftragNr = source.AuftragNr,
                PdfPfad = source.PdfPfad,
                PdfPfadAngefangeneTafel = source.PdfPfadAngefangeneTafel,
                PreisProKg = source.PreisProKg,
                IsHighlighted = source.IsHighlighted,
                IsSelected = source.IsSelected
            };
        }
    }
}
