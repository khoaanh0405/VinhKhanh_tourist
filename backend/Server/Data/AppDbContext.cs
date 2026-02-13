using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // =====================
        // DbSets
        // =====================

        public DbSet<User> Users { get; set; }
        public DbSet<POI> POIs { get; set; }
        public DbSet<PoiImage> PoiImages { get; set; }   // 👈 THÊM
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Food> Foods { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Narration> Narrations { get; set; }
        public DbSet<Geofence> Geofences { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // POI - PoiImage (1 - N) 👈 THÊM
            modelBuilder.Entity<POI>()
                .HasMany(p => p.PoiImages)
                .WithOne(i => i.POI)
                .HasForeignKey(i => i.PoiId)
                .OnDelete(DeleteBehavior.Cascade);

            // POI - Restaurant (1 - N)
            modelBuilder.Entity<POI>()
                .HasMany(p => p.Restaurants)
                .WithOne(r => r.POI)
                .HasForeignKey(r => r.PoiId);

            // POI - Narration (1 - N)
            modelBuilder.Entity<POI>()
                .HasMany(p => p.Narrations)
                .WithOne(n => n.POI)
                .HasForeignKey(n => n.PoiId);

            // Restaurant - Food (1 - N)
            modelBuilder.Entity<Restaurant>()
                .HasMany(r => r.Foods)
                .WithOne(f => f.Restaurant)
                .HasForeignKey(f => f.RestaurantId);

            // Language - Narration (1 - N)
            modelBuilder.Entity<Language>()
                .HasMany(l => l.Narrations)
                .WithOne(n => n.Language)
                .HasForeignKey(n => n.LanguageCode);

            // Unique narration per POI + Language
            modelBuilder.Entity<Narration>()
                .HasIndex(n => new { n.PoiId, n.LanguageCode })
                .IsUnique();

            // QRCode unique
            modelBuilder.Entity<QRCode>()
                .HasIndex(q => q.CodeValue)
                .IsUnique();
        }
    }
}
