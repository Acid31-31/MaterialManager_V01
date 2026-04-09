using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    public static class AuftragRulesService
    {
        public static List<Auftrag> FilterByIsoCalendarWeek(IEnumerable<Auftrag> auftraege, int jahr, int kalenderWoche)
        {
            return (auftraege ?? Enumerable.Empty<Auftrag>())
                .Where(a =>
                {
                    var relevantDate = a.GeaendertAm != default ? a.GeaendertAm : a.ErstelltAm;
                    return relevantDate.Year == jahr
                        && System.Globalization.ISOWeek.GetWeekOfYear(relevantDate) == kalenderWoche;
                })
                .OrderBy(a => a.SortIndex)
                .ThenBy(a => a.Auftragsnummer)
                .ToList();
        }

        public static List<MaterialItem> GetMaterialsWithoutValidPdf(IEnumerable<MaterialItem> materialien)
        {
            return (materialien ?? Enumerable.Empty<MaterialItem>())
                .Where(m => !HasExistingPdf(m))
                .ToList();
        }

        public static bool HasExistingPdf(MaterialItem material)
        {
            if (material == null)
                return false;

            var pfad = !string.IsNullOrWhiteSpace(material.PdfPfadAngefangeneTafel)
                ? material.PdfPfadAngefangeneTafel
                : material.PdfPfad;

            return !string.IsNullOrWhiteSpace(pfad) && File.Exists(pfad);
        }
    }
}
