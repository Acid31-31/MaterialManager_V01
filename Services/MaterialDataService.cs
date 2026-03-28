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
        public static List<MaterialItem> LoadAllMaterials()
        {
            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();

            return db.Materialien
                .AsNoTracking()
                .OrderBy(m => m.Id)
                .ToList();
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

            if (syncExcel)
                SyncExcel(snapshot);
        }

        private static void SyncAuftraege(MaterialManagerDbContext db, IEnumerable<MaterialItem> materialien)
        {
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

                    return new Auftrag
                    {
                        Auftragsnummer = gruppe.Key,
                        Status = hatAngefangeneTafel ? AuftragStatus.InBearbeitung : AuftragStatus.Offen,
                        ErstelltAm = first.Datum ?? DateTime.Now,
                        GeaendertAm = items.Max(i => i.AenderungsDatum ?? i.Datum ?? DateTime.Now),
                        AngelegtVon = first.AngelegtVon,
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
        }

        private static void SyncExcel(IEnumerable<MaterialItem> materialien)
        {
            var savePath = NetzwerkService.GetSavePath();
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            ExcelService.Export(savePath, materialien);
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
