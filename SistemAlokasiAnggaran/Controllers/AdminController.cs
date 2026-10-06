using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemAlokasiAnggaran.Data;
using SistemAlokasiAnggaran.Interfaces;
using SistemAlokasiAnggaran.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace SistemAlokasiAnggaran.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IMenuRoleRepository _menuRoleRepo;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AppDbContext context,
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IMenuRoleRepository menuRoleRepo,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _menuRoleRepo = menuRoleRepo;
            _logger = logger;
        }

        // ==================== PAGES ====================

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }

        public IActionResult Database()
        {
            ViewData["Title"] = "Database";
            return View();
        }

        // ==================== JSON API ====================

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalRoles = await _context.Roles.CountAsync();
            var totalMenus = await _context.MenuRoles.CountAsync();
            var totalAlokasi = await _context.AlokasiAnggarans.CountAsync();
            var totalProgram = await _context.ProgramAnggarans.CountAsync();

            var last = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            object? lastSession = null;
            if (last != null)
            {
                lastSession = new
                {
                    adaData = true,
                    catatan = last.Catatan,
                    totalDana = last.TotalDana,
                    jumlahProgram = last.Programs.Count,
                    status = last.Status,
                    tanggalProses = last.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm")
                };
            }

            // History 1 minggu kebelakang (past 7 days)
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            var weeklyList = await _context.AlokasiAnggarans
                .Where(a => a.CreatedAt >= oneWeekAgo && a.Status != "Draft")
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Catatan,
                    a.TotalDana,
                    TanggalProses = a.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    a.Status
                })
                .ToListAsync();

            if (!weeklyList.Any())
            {
                weeklyList = await _context.AlokasiAnggarans
                    .Where(a => a.Status != "Draft")
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .Select(a => new
                    {
                        a.Id,
                        a.Catatan,
                        a.TotalDana,
                        TanggalProses = a.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        a.Status
                    })
                    .ToListAsync();
            }

            return Json(new { totalUsers, totalRoles, totalMenus, totalAlokasi, totalProgram, lastSession, weeklyHistory = weeklyList });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.NamaLengkap)
                .Select(u => new {
                    u.Id,
                    u.NamaLengkap,
                    u.Email,
                    u.RoleId,
                    RoleName = u.Role != null ? u.Role.NamaRole : "-",
                    u.IsActive,
                    CreatedAt = u.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();
            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .OrderBy(r => r.NamaRole)
                .Select(r => new {
                    r.Id,
                    r.NamaRole,
                    Deskripsi = r.Deskripsi ?? "-"
                })
                .ToListAsync();
            return Json(roles);
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuRoles()
        {
            var menus = await _context.MenuRoles
                .Include(m => m.Role)
                .OrderBy(m => m.RoleId).ThenBy(m => m.Urutan)
                .Select(m => new {
                    m.Id,
                    m.RoleId,
                    RoleName = m.Role != null ? m.Role.NamaRole : "-",
                    m.NamaMenu,
                    Icon = m.Icon ?? "",
                    m.Url,
                    m.Urutan
                })
                .ToListAsync();
            return Json(menus);
        }

        // ==================== USERS CRUD ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User user, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(user.NamaLengkap))
                return Json(new { success = false, message = "Nama lengkap wajib diisi." });
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
                return Json(new { success = false, message = "Email tidak valid." });
            if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash.Length < 6)
                return Json(new { success = false, message = "Password minimal 6 karakter." });
            if (ConfirmPassword != user.PasswordHash)
                return Json(new { success = false, message = "Password dan konfirmasi tidak cocok." });
            if (user.RoleId <= 0 || !await _context.Roles.AnyAsync(r => r.Id == user.RoleId))
                return Json(new { success = false, message = "Pilih role yang valid." });

            var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.Trim().ToLower());
            if (existing)
                return Json(new { success = false, message = "Email sudah terdaftar." });

            var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? adminId = int.TryParse(currentAdminId, out int aid) ? aid : null;

            user.Email = user.Email.Trim();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;
            user.CreatedBy = adminId;
            user.IsActive = true;

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil menambah user baru." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User user, string? Password)
        {
            if (id != user.Id)
                return Json(new { success = false, message = "ID tidak cocok." });

            var current = await _userRepo.GetByIdAsync(id);
            if (current == null)
                return Json(new { success = false, message = "User tidak ditemukan." });

            if (string.IsNullOrWhiteSpace(user.NamaLengkap))
                return Json(new { success = false, message = "Nama lengkap wajib diisi." });
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
                return Json(new { success = false, message = "Email tidak valid." });
            if (user.RoleId <= 0 || !await _context.Roles.AnyAsync(r => r.Id == user.RoleId))
                return Json(new { success = false, message = "Pilih role yang valid." });

            if (id == 1 && !user.IsActive)
                return Json(new { success = false, message = "Akun Administrator utama tidak boleh dinonaktifkan." });

            var emailConflict = await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.Trim().ToLower() && u.Id != id);
            if (emailConflict)
                return Json(new { success = false, message = "Email sudah digunakan user lain." });

            var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? adminId = int.TryParse(currentAdminId, out int aid) ? aid : null;

            current.NamaLengkap = user.NamaLengkap.Trim();
            current.Email = user.Email.Trim();
            current.RoleId = user.RoleId;
            current.IsActive = user.IsActive;
            current.UpdatedAt = DateTime.UtcNow;
            current.UpdatedBy = adminId;

            if (!string.IsNullOrWhiteSpace(Password) && Password.Length >= 6)
                current.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);

            await _userRepo.UpdateAsync(current);
            await _userRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil memperbarui data user." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id == 1)
                return Json(new { success = false, message = "Akun Administrator utama tidak dapat dihapus." });

            var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentAdminId, out int aid) && aid == id)
                return Json(new { success = false, message = "Tidak dapat menghapus akun Anda sendiri yang sedang aktif." });

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User tidak ditemukan." });

            var hasAlokasi = await _context.AlokasiAnggarans.AnyAsync(a => a.CreatedBy == id);
            if (hasAlokasi)
                return Json(new { success = false, message = "User ini memiliki data riwayat alokasi anggaran sehingga tidak dapat dihapus. Silakan nonaktifkan statusnya sebagai alternatif." });

            await _userRepo.DeleteAsync(id);
            await _userRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil menghapus user." });
        }

        // ==================== ROLES CRUD ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(Role role)
        {
            if (string.IsNullOrWhiteSpace(role.NamaRole))
                return Json(new { success = false, message = "Nama role wajib diisi." });

            var roleName = role.NamaRole.Trim();
            var existing = await _context.Roles.AnyAsync(r => r.NamaRole.ToLower() == roleName.ToLower());
            if (existing)
                return Json(new { success = false, message = "Nama role sudah ada." });

            var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? adminId = int.TryParse(currentAdminId, out int aid) ? aid : null;

            role.NamaRole = roleName;
            role.Deskripsi = role.Deskripsi?.Trim();
            role.CreatedAt = DateTime.UtcNow;
            role.CreatedBy = adminId;

            await _roleRepo.AddAsync(role);
            await _roleRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil menambah role baru." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(int id, Role role)
        {
            if (id != role.Id)
                return Json(new { success = false, message = "ID tidak cocok." });

            var current = await _roleRepo.GetByIdAsync(id);
            if (current == null)
                return Json(new { success = false, message = "Role tidak ditemukan." });

            if (string.IsNullOrWhiteSpace(role.NamaRole))
                return Json(new { success = false, message = "Nama role wajib diisi." });

            var roleName = role.NamaRole.Trim();
            if (id == 1 && !roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Nama role Administrator sistem tidak boleh diubah." });

            var conflict = await _context.Roles.AnyAsync(r => r.NamaRole.ToLower() == roleName.ToLower() && r.Id != id);
            if (conflict)
                return Json(new { success = false, message = "Nama role sudah digunakan." });

            var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? adminId = int.TryParse(currentAdminId, out int aid) ? aid : null;

            current.NamaRole = roleName;
            current.Deskripsi = role.Deskripsi?.Trim();
            current.UpdatedAt = DateTime.UtcNow;
            current.UpdatedBy = adminId;

            await _roleRepo.UpdateAsync(current);
            await _roleRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil memperbarui role." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(int id)
        {
            if (id == 1)
                return Json(new { success = false, message = "Role Administrator sistem tidak dapat dihapus." });

            var role = await _roleRepo.GetByIdAsync(id);
            if (role == null)
                return Json(new { success = false, message = "Role tidak ditemukan." });

            var hasUsers = await _roleRepo.HasUsersAsync(id);
            if (hasUsers)
                return Json(new { success = false, message = "Tidak bisa menghapus role yang masih digunakan oleh user." });

            // Hapus menu role terkait agar tidak terjadi FK constraint error
            var associatedMenus = await _context.MenuRoles.Where(m => m.RoleId == id).ToListAsync();
            if (associatedMenus.Any())
            {
                _context.MenuRoles.RemoveRange(associatedMenus);
            }

            await _roleRepo.DeleteAsync(id);
            await _roleRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil menghapus role." });
        }

        // ==================== MENU ROLES CRUD ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuRole(MenuRole menuRole)
        {
            if (string.IsNullOrWhiteSpace(menuRole.NamaMenu))
                return Json(new { success = false, message = "Nama menu wajib diisi." });
            if (string.IsNullOrWhiteSpace(menuRole.Url))
                return Json(new { success = false, message = "URL wajib diisi." });
            if (menuRole.RoleId <= 0 || !await _context.Roles.AnyAsync(r => r.Id == menuRole.RoleId))
                return Json(new { success = false, message = "Pilih role yang valid." });

            var url = menuRole.Url.Trim();
            if (!url.StartsWith("/") && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "/" + url;

            var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? adminId = int.TryParse(currentAdminId, out int aid) ? aid : null;

            menuRole.NamaMenu = menuRole.NamaMenu.Trim();
            menuRole.Url = url;
            menuRole.Icon = menuRole.Icon?.Trim();
            menuRole.CreatedAt = DateTime.UtcNow;
            menuRole.CreatedBy = adminId;

            await _menuRoleRepo.AddAsync(menuRole);
            await _menuRoleRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil menambah menu role baru." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuRole(int id, MenuRole menuRole)
        {
            if (id != menuRole.Id)
                return Json(new { success = false, message = "ID tidak cocok." });

            var current = await _menuRoleRepo.GetByIdAsync(id);
            if (current == null)
                return Json(new { success = false, message = "Menu role tidak ditemukan." });

            if (string.IsNullOrWhiteSpace(menuRole.NamaMenu))
                return Json(new { success = false, message = "Nama menu wajib diisi." });
            if (string.IsNullOrWhiteSpace(menuRole.Url))
                return Json(new { success = false, message = "URL wajib diisi." });
            if (menuRole.RoleId <= 0 || !await _context.Roles.AnyAsync(r => r.Id == menuRole.RoleId))
                return Json(new { success = false, message = "Pilih role yang valid." });

            var url = menuRole.Url.Trim();
            if (!url.StartsWith("/") && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "/" + url;

            var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? adminId = int.TryParse(currentAdminId, out int aid) ? aid : null;

            current.RoleId = menuRole.RoleId;
            current.NamaMenu = menuRole.NamaMenu.Trim();
            current.Icon = menuRole.Icon?.Trim();
            current.Url = url;
            current.Urutan = menuRole.Urutan;
            current.UpdatedAt = DateTime.UtcNow;
            current.UpdatedBy = adminId;

            await _menuRoleRepo.UpdateAsync(current);
            await _menuRoleRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil memperbarui menu role." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuRole(int id)
        {
            var menuRole = await _menuRoleRepo.GetByIdAsync(id);
            if (menuRole == null)
                return Json(new { success = false, message = "Menu role tidak ditemukan." });

            await _menuRoleRepo.DeleteAsync(id);
            await _menuRoleRepo.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil menghapus menu role." });
        }
    }
}
