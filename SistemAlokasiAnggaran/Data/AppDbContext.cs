using Microsoft.EntityFrameworkCore;
using SistemAlokasiAnggaran.Models.Entities;

namespace SistemAlokasiAnggaran.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<MenuRole> MenuRoles { get; set; }

        // Tabel bisnis Alokasi Anggaran
        public DbSet<AlokasiAnggaran> AlokasiAnggarans { get; set; }
        public DbSet<ProgramAnggaran> ProgramAnggarans { get; set; }
        public DbSet<HasilAlokasi> HasilAlokasis { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, NamaRole = "Admin", Deskripsi = "Administrator sistem dengan akses penuh" },
                new Role { Id = 2, NamaRole = "Guest", Deskripsi = "Tamu dengan akses terbatas" },
                new Role { Id = 3, NamaRole = "Manajer Perencanaan", Deskripsi = "Manajer Perencanaan dengan akses alokasi anggaran" }
            );

            // Seed MenuRoles (Semua menu ada di Admin)
            modelBuilder.Entity<MenuRole>().HasData(
                new MenuRole { Id = 1, RoleId = 1, NamaMenu = "Dashboard", Icon = "fas fa-tachometer-alt", Url = "/Admin/Dashboard", Urutan = 1 },
                new MenuRole { Id = 5, RoleId = 1, NamaMenu = "Alokasi Anggaran", Icon = "fas fa-money-bill-wave", Url = "/AlokasiAnggaran/Index", Urutan = 2 },
                new MenuRole { Id = 6, RoleId = 1, NamaMenu = "History Anggaran", Icon = "fas fa-history", Url = "/AlokasiAnggaran/History", Urutan = 3 },
                new MenuRole { Id = 2, RoleId = 1, NamaMenu = "Database", Icon = "fas fa-database", Url = "/Admin/Database", Urutan = 4 },
                new MenuRole { Id = 3, RoleId = 2, NamaMenu = "Dashboard", Icon = "fas fa-tachometer-alt", Url = "/Home/Index", Urutan = 1 }
            );

            // Konfigurasi relasi HasilAlokasi - ProgramAnggaran (1:1)
            modelBuilder.Entity<HasilAlokasi>()
                .HasOne(h => h.ProgramAnggaran)
                .WithOne(p => p.HasilAlokasi)
                .HasForeignKey<HasilAlokasi>(h => h.ProgramAnggaranId);

            // Konfigurasi relasi ProgramAnggaran - AlokasiAnggaran (N:1)
            modelBuilder.Entity<ProgramAnggaran>()
                .HasOne(p => p.AlokasiAnggaran)
                .WithMany(a => a.Programs)
                .HasForeignKey(p => p.AlokasiAnggaranId);
        }
    }
}
