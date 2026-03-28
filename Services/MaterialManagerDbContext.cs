using MaterialManager_V01.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManager_V01.Services
{
    public sealed class MaterialManagerDbContext : DbContext
    {
        public DbSet<MaterialItem> Materialien => Set<MaterialItem>();
        public DbSet<Auftrag> Auftraege => Set<Auftrag>();
        public DbSet<User> Benutzer => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlite($"Data Source={PathService.DatabasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var material = modelBuilder.Entity<MaterialItem>();
            material.HasKey(m => m.Id);
            material.Property(m => m.Id).ValueGeneratedOnAdd();
            material.Property(m => m.Kategorie).HasConversion<string>();
            material.Ignore(m => m.IstReserviert);
            material.Ignore(m => m.PdfDateiname);
            material.Ignore(m => m.HasPdf);
            material.Ignore(m => m.PdfDateinameAngefangeneTafel);
            material.Ignore(m => m.HasPdfAngefangeneTafel);
            material.Ignore(m => m.LaengeAnzeige);
            material.Ignore(m => m.Gesamtwert);
            material.Ignore(m => m.GewichtKg);
            material.Ignore(m => m.IsHighlighted);
            material.Ignore(m => m.IsSelected);

            var auftrag = modelBuilder.Entity<Auftrag>();
            auftrag.HasKey(a => a.Id);
            auftrag.Property(a => a.Id).ValueGeneratedOnAdd();
            auftrag.Property(a => a.Status).HasConversion<string>();
            auftrag.HasIndex(a => a.Auftragsnummer).IsUnique();

            var user = modelBuilder.Entity<User>();
            user.HasKey(u => u.Id);
            user.Property(u => u.Role).HasConversion<string>();
        }
    }
}
