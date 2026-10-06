using Microsoft.EntityFrameworkCore;
using SistemAlokasiAnggaran.Models.Entities;

namespace SistemAlokasiAnggaran.Data
{
    public static class DatabaseSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Ensure DB created
            context.Database.EnsureCreated();

            // Migrasi kolom dinamis SQLite jika belum ada
            try
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE program_anggaran ADD COLUMN StatusProgram TEXT NOT NULL DEFAULT 'Aktif';");
            }
            catch { /* Kolom sudah ada */ }

            // ── Seed Roles ───────────────────────────────────────────────
            if (!context.Roles.Any(r => r.NamaRole == "Admin"))
                context.Roles.Add(new Role { Id = 1, NamaRole = "Admin", Deskripsi = "Administrator sistem dengan akses penuh" });

            if (!context.Roles.Any(r => r.NamaRole == "Guest"))
                context.Roles.Add(new Role { Id = 2, NamaRole = "Guest", Deskripsi = "Tamu dengan akses terbatas" });

            if (!context.Roles.Any(r => r.NamaRole == "Manajer Perencanaan"))
                context.Roles.Add(new Role { NamaRole = "Manajer Perencanaan", Deskripsi = "Manajer Perencanaan dengan akses alokasi anggaran" });

            context.SaveChanges();

            // Ambil RoleId
            var roleAdmin = context.Roles.First(r => r.NamaRole == "Admin");
            var roleGuest = context.Roles.First(r => r.NamaRole == "Guest");
            var roleManajer = context.Roles.First(r => r.NamaRole == "Manajer Perencanaan");

            // ── Seed MenuRoles ───────────────────────────────────────────
            void SeedMenu(int roleId, string namaMenu, string icon, string url, int urutan)
            {
                if (!context.MenuRoles.Any(m => m.RoleId == roleId && (m.NamaMenu == namaMenu || m.Url == url)))
                    context.MenuRoles.Add(new MenuRole { RoleId = roleId, NamaMenu = namaMenu, Icon = icon, Url = url, Urutan = urutan });
            }

            // Update existing menu name 'Halaman Depan' to 'Dashboard' if exists
            var existingHalamanDepanMenus = context.MenuRoles.Where(m => m.NamaMenu == "Halaman Depan").ToList();
            foreach (var m in existingHalamanDepanMenus)
            {
                m.NamaMenu = "Dashboard";
                m.Icon = "fas fa-tachometer-alt";
            }

            // Admin menus (semua menu dialihkan ke Admin)
            SeedMenu(roleAdmin.Id, "Dashboard",        "fas fa-tachometer-alt",  "/Admin/Dashboard",            1);
            SeedMenu(roleAdmin.Id, "Alokasi Anggaran", "fas fa-money-bill-wave", "/AlokasiAnggaran/Index",     2);
            SeedMenu(roleAdmin.Id, "History Anggaran", "fas fa-history",         "/AlokasiAnggaran/History",   3);
            SeedMenu(roleAdmin.Id, "Database",         "fas fa-database",        "/Admin/Database",             4);

            // Guest menus
            SeedMenu(roleGuest.Id, "Dashboard",        "fas fa-tachometer-alt",  "/Home/Index",                 1);

            // Remove any duplicate menus per role
            var allMenus = context.MenuRoles.ToList();
            var duplicates = allMenus
                .GroupBy(m => new { m.RoleId, m.Url })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Skip(1))
                .ToList();
            if (duplicates.Any())
            {
                context.MenuRoles.RemoveRange(duplicates);
            }

            context.SaveChanges();

            // ── Seed Users ───────────────────────────────────────────────
            if (!context.Users.Any(u => u.Email == "admin@example.com"))
            {
                context.Users.Add(new User
                {
                    NamaLengkap = "Administrator",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                    RoleId = roleAdmin.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!context.Users.Any(u => u.Email == "guest@example.com"))
            {
                context.Users.Add(new User
                {
                    NamaLengkap = "Tamu",
                    Email = "guest@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                    RoleId = roleGuest.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!context.Users.Any(u => u.Email == "manajer@example.com"))
            {
                context.Users.Add(new User
                {
                    NamaLengkap = "Manajer Perencanaan",
                    Email = "manajer@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                    RoleId = roleManajer.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            context.SaveChanges();

            // ── Seed Sample Program Data (9 Aktif & 1 Ditunda) HANYA jika belum ada data alokasi/program ──
            if (!context.AlokasiAnggarans.Any() && !context.ProgramAnggarans.Any())
            {
                var userAdmin = context.Users.FirstOrDefault(u => u.Email == "admin@example.com");
                var adminId = userAdmin?.Id ?? 1;

                var draft = new AlokasiAnggaran
                {
                    Catatan = "Rencana Alokasi Anggaran Belanja Modal Strategis",
                    TotalDana = 2500000000m, // Plafon Rp 2.500.000.000
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminId
                };
                context.AlokasiAnggarans.Add(draft);
                context.SaveChanges();

                var programs10 = new[]
                {
                    // 1. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Transformasi Digital Cloud & Infrastruktur Data Center", AnggaranDiajukan = 400000000m, AnggaranMinimum = 250000000m, AnggaranMaksimum = 450000000m, TingkatPrioritas = 5, EstimasiManfaat = 95, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 2. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Audit Keamanan Siber & Pembaruan Sistem Enkripsi ISO 27001", AnggaranDiajukan = 250000000m, AnggaranMinimum = 150000000m, AnggaranMaksimum = 280000000m, TingkatPrioritas = 5, EstimasiManfaat = 92, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 3. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Pengembangan Modul Enterprise CRM & Helpdesk AI", AnggaranDiajukan = 350000000m, AnggaranMinimum = 200000000m, AnggaranMaksimum = 380000000m, TingkatPrioritas = 4, EstimasiManfaat = 88, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 4. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Kampanye & Pemasaran Multi-Channel Terpadu", AnggaranDiajukan = 300000000m, AnggaranMinimum = 180000000m, AnggaranMaksimum = 320000000m, TingkatPrioritas = 4, EstimasiManfaat = 85, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 5. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Pelatihan Kepemimpinan & Sertifikasi Kompetensi SDM", AnggaranDiajukan = 180000000m, AnggaranMinimum = 100000000m, AnggaranMaksimum = 200000000m, TingkatPrioritas = 3, EstimasiManfaat = 78, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 6. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Riset Inovasi Produk & Analisis Pasar Komprehensif", AnggaranDiajukan = 220000000m, AnggaranMinimum = 120000000m, AnggaranMaksimum = 250000000m, TingkatPrioritas = 3, EstimasiManfaat = 80, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 7. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Pengadaan Workstation & Perangkat Keras Tim Operasional", AnggaranDiajukan = 200000000m, AnggaranMinimum = 100000000m, AnggaranMaksimum = 220000000m, TingkatPrioritas = 2, EstimasiManfaat = 70, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 8. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Otomasi Pelaporan Keuangan & Rekonsiliasi Real-Time", AnggaranDiajukan = 260000000m, AnggaranMinimum = 160000000m, AnggaranMaksimum = 290000000m, TingkatPrioritas = 4, EstimasiManfaat = 86, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 9. Aktif
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Modernisasi Jaringan SD-WAN Antar Kantor Cabang", AnggaranDiajukan = 340000000m, AnggaranMinimum = 200000000m, AnggaranMaksimum = 360000000m, TingkatPrioritas = 4, EstimasiManfaat = 84, StatusProgram = "Aktif", CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                    // 10. Ditunda
                    new ProgramAnggaran { AlokasiAnggaranId = draft.Id, NamaProgram = "Renovasi Interior & Desain Ergonomis Ruang Kerja", AnggaranDiajukan = 150000000m, AnggaranMinimum = 80000000m, AnggaranMaksimum = 180000000m, TingkatPrioritas = 2, EstimasiManfaat = 65, StatusProgram = "Ditunda", CreatedAt = DateTime.UtcNow, CreatedBy = adminId }
                };

                context.ProgramAnggarans.AddRange(programs10);
                context.SaveChanges();
            }
        }
    }
}
