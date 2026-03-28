using System;
using System.IO;
using System.Linq;
using MaterialManager_V01.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManager_V01.Services
{
    public static class DatabaseBootstrapService
    {
        public static void Initialize()
        {
            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();
            EnsureSchemaUpToDate(db);

            if (db.Materialien.Any())
                return;

            ImportExistingExcelData(db);
        }

        private static void EnsureSchemaUpToDate(MaterialManagerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""Auftraege"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Auftraege"" PRIMARY KEY AUTOINCREMENT,
    ""Auftragsnummer"" TEXT NOT NULL,
    ""Status"" TEXT NOT NULL,
    ""ErstelltAm"" TEXT NOT NULL,
    ""GeaendertAm"" TEXT NOT NULL,
    ""AngelegtVon"" TEXT NOT NULL,
    ""GeaendertVon"" TEXT NOT NULL,
    ""MaterialPositionen"" INTEGER NOT NULL,
    ""GesamtStueckzahl"" INTEGER NOT NULL,
    ""GesamtGewichtKg"" REAL NOT NULL,
    ""PdfPfad"" TEXT NOT NULL,
    ""PdfPfadAngefangeneTafel"" TEXT NOT NULL
);");

            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Auftraege_Auftragsnummer\" ON \"Auftraege\" (\"Auftragsnummer\");");
        }

        private static void ImportExistingExcelData(MaterialManagerDbContext db)
        {
            try
            {
                var excelPath = NetzwerkService.GetSavePath();
                var materialien = MaterialDataService.LoadFromExcelFile(excelPath);
                if (materialien.Count == 0)
                    return;

                foreach (var material in materialien)
                    material.Id = 0;

                db.Materialien.AddRange(materialien);
                db.SaveChanges();

                File.AppendAllText(PathService.LogPath,
                    $"[DB] Erstimport aus Excel abgeschlossen: {materialien.Count} Materialien{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                File.AppendAllText(PathService.LogPath,
                    $"[DB] Erstimport fehlgeschlagen: {ex.Message}{Environment.NewLine}");
            }
        }
    }
}
