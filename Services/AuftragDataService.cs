using MaterialManager_V01.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManager_V01.Services
{
    public static class AuftragDataService
    {
        public static List<Auftrag> LoadAllAuftraege()
        {
            using var db = new MaterialManagerDbContext();
            db.Database.EnsureCreated();

            return db.Auftraege
                .AsNoTracking()
                .OrderBy(a => a.Auftragsnummer)
                .ToList();
        }
    }
}
