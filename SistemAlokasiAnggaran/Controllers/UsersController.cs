using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemAlokasiAnggaran.Interfaces;
using SistemAlokasiAnggaran.Models.Entities;

namespace SistemAlokasiAnggaran.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserRepository userRepo, IRoleRepository roleRepo, ILogger<UsersController> logger)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllAsync();
            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string ConfirmPassword)
        {
            ViewBag.Roles = await _roleRepo.GetAllAsync();

            if (user.RoleId <= 0)
            {
                ModelState.AddModelError(nameof(user.RoleId), "Pilih role terlebih dahulu.");
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(nameof(user.Email), "Email wajib diisi.");
            }
            else if (!user.Email.Contains('@'))
            {
                ModelState.AddModelError(nameof(user.Email), "Format email tidak valid.");
            }
            if (string.IsNullOrWhiteSpace(user.NamaLengkap))
            {
                ModelState.AddModelError(nameof(user.NamaLengkap), "Nama lengkap wajib diisi.");
            }
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                ModelState.AddModelError(nameof(user.PasswordHash), "Password wajib diisi.");
            }
            else if (user.PasswordHash.Length < 6)
            {
                ModelState.AddModelError(nameof(user.PasswordHash), "Password minimal 6 karakter.");
            }
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Konfirmasi password wajib diisi.");
            }
            else if (ConfirmPassword != user.PasswordHash)
            {
                ModelState.AddModelError(string.Empty, "Password dan konfirmasi password tidak cocok.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var existing = await _userRepo.GetByEmailAsync(user.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(user.Email), "Email sudah terdaftar.");
                return View(user);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            TempData["StatusMessage"] = "Berhasil menambah user baru.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                TempData["StatusMessage"] = "Data user tidak ditemukan.";
                TempData["StatusType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user, string? Password)
        {
            ViewBag.Roles = await _roleRepo.GetAllAsync();

            if (id != user.Id)
            {
                return RedirectToAction(nameof(Index));
            }

            var current = await _userRepo.GetByIdAsync(id);
            if (current == null)
            {
                TempData["StatusMessage"] = "Data user tidak ditemukan.";
                TempData["StatusType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(user.NamaLengkap))
            {
                ModelState.AddModelError(nameof(user.NamaLengkap), "Nama lengkap wajib diisi.");
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(nameof(user.Email), "Email wajib diisi.");
            }
            else if (!user.Email.Contains('@'))
            {
                ModelState.AddModelError(nameof(user.Email), "Format email tidak valid.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var emailConflict = await _userRepo.GetByEmailAsync(user.Email);
            if (emailConflict != null && emailConflict.Id != id)
            {
                ModelState.AddModelError(nameof(user.Email), "Email sudah digunakan user lain.");
                return View(user);
            }

            current.NamaLengkap = user.NamaLengkap;
            current.Email = user.Email;
            current.RoleId = user.RoleId;
            current.IsActive = user.IsActive;

            if (!string.IsNullOrWhiteSpace(Password) && Password.Length >= 6)
            {
                current.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
            }

            await _userRepo.UpdateAsync(current);
            await _userRepo.SaveChangesAsync();

            TempData["StatusMessage"] = "Berhasil memperbarui data user.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                TempData["StatusMessage"] = "Data user tidak ditemukan.";
                TempData["StatusType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            await _userRepo.DeleteAsync(id);
            await _userRepo.SaveChangesAsync();

            TempData["StatusMessage"] = "Berhasil menghapus user.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Index));
        }
    }
}
