using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemAlokasiAnggaran.Data;
using SistemAlokasiAnggaran.Models.Entities;
using SistemAlokasiAnggaran.Models.ViewModels;
using SistemAlokasiAnggaran.Services;

namespace SistemAlokasiAnggaran.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AlokasiAnggaranController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AlokasiAnggaranController> _logger;

        public AlokasiAnggaranController(AppDbContext context, ILogger<AlokasiAnggaranController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }

        private bool CanAccessAlokasi(AlokasiAnggaran? alokasi, int userId)
        {
            if (alokasi == null) return false;
            // Admin, system root (id 1), creator, unassigned (0), atau akses draft bersama
            if (User.IsInRole("Admin") || userId == 1 || alokasi.CreatedBy == userId || alokasi.CreatedBy == 0)
                return true;
            return false;
        }

        // ==================== HALAMAN UTAMA ====================

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Alokasi Anggaran";
            return View();
        }

        // ==================== DRAFT HEADER ====================

        [HttpGet]
        public async Task<IActionResult> GetDraft()
        {
            int userId = GetCurrentUserId();
            var draft = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                    .ThenInclude(p => p.HasilAlokasi)
                .Where(a => a.Status == "Draft" && (a.CreatedBy == userId || userId == 1 || User.IsInRole("Admin")))
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (draft == null)
            {
                draft = await _context.AlokasiAnggarans
                    .Include(a => a.Programs)
                        .ThenInclude(p => p.HasilAlokasi)
                    .Where(a => a.Status == "Draft")
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            if (draft == null)
                return Json(new { hasDraft = false });

            return Json(new
            {
                hasDraft = true,
                id = draft.Id,
                catatan = draft.Catatan,
                totalDana = draft.TotalDana,
                status = draft.Status,
                programs = draft.Programs.Select(p => new
                {
                    p.Id,
                    p.NamaProgram,
                    p.AnggaranDiajukan,
                    p.AnggaranMinimum,
                    p.AnggaranMaksimum,
                    p.TingkatPrioritas,
                    p.EstimasiManfaat,
                    AlokasiOptimal = p.HasilAlokasi?.AlokasiOptimal ?? 0,
                    StatusProgram = string.IsNullOrEmpty(p.StatusProgram) ? "Aktif" : p.StatusProgram
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveHeader([FromBody] SaveHeaderRequest req)
        {
            if (req.TotalDana <= 0)
                return Json(new { success = false, message = "Total dana tersedia harus lebih besar dari 0." });

            int userId = GetCurrentUserId();

            if (req.Id.HasValue && req.Id > 0)
            {
                var existing = await _context.AlokasiAnggarans.FindAsync(req.Id.Value);
                if (existing != null && existing.Status == "Draft" && CanAccessAlokasi(existing, userId))
                {
                    existing.Catatan = req.Catatan?.Trim();
                    existing.TotalDana = req.TotalDana;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, id = existing.Id });
                }
            }

            var alokasi = new AlokasiAnggaran
            {
                Catatan = req.Catatan?.Trim(),
                TotalDana = req.TotalDana,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            _context.AlokasiAnggarans.Add(alokasi);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = alokasi.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscardDraft([FromBody] IdRequest req)
        {
            int userId = GetCurrentUserId();
            var draft = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                .FirstOrDefaultAsync(a => a.Id == req.Id && a.Status == "Draft");

            if (draft == null || !CanAccessAlokasi(draft, userId))
                return Json(new { success = false, message = "Draft tidak ditemukan atau tidak diizinkan." });

            _context.ProgramAnggarans.RemoveRange(draft.Programs);
            _context.AlokasiAnggarans.Remove(draft);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ==================== PROGRAM CRUD ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProgram([FromBody] AddProgramRequest req)
        {
            int userId = GetCurrentUserId();
            AlokasiAnggaran? alokasi = null;

            if (req.AlokasiId > 0)
            {
                alokasi = await _context.AlokasiAnggarans
                    .FirstOrDefaultAsync(a => a.Id == req.AlokasiId && a.Status == "Draft");
                if (alokasi != null && !CanAccessAlokasi(alokasi, userId))
                {
                    alokasi = null;
                }
            }

            if (alokasi == null)
            {
                // Cek apakah ada draft aktif yang dapat diakses user
                alokasi = await _context.AlokasiAnggarans
                    .Where(a => a.Status == "Draft" && (a.CreatedBy == userId || userId == 1 || User.IsInRole("Admin")))
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (alokasi == null)
                {
                    // Buat draft alokasi baru otomatis untuk menampung program usulan
                    alokasi = new AlokasiAnggaran
                    {
                        Catatan = null,
                        TotalDana = 0,
                        Status = "Draft",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    _context.AlokasiAnggarans.Add(alokasi);
                    await _context.SaveChangesAsync();
                }
            }

            var errs = ValidateProgram(req, 1);
            if (errs.Count > 0)
                return Json(new { success = false, message = string.Join(" | ", errs) });

            var prog = new ProgramAnggaran
            {
                AlokasiAnggaranId = alokasi.Id,
                NamaProgram = req.NamaProgram.Trim(),
                AnggaranDiajukan = req.AnggaranDiajukan,
                AnggaranMinimum = req.AnggaranMinimum,
                AnggaranMaksimum = req.AnggaranMaksimum,
                TingkatPrioritas = req.TingkatPrioritas,
                EstimasiManfaat = req.EstimasiManfaat,
                StatusProgram = req.StatusProgram == "Ditunda" ? "Ditunda" : "Aktif",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            _context.ProgramAnggarans.Add(prog);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                alokasiId = alokasi.Id,
                program = new
                {
                    prog.Id,
                    prog.NamaProgram,
                    prog.AnggaranDiajukan,
                    prog.AnggaranMinimum,
                    prog.AnggaranMaksimum,
                    prog.TingkatPrioritas,
                    prog.EstimasiManfaat,
                    prog.StatusProgram
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgram([FromBody] UpdateProgramRequest req)
        {
            if (req.Id <= 0)
                return Json(new { success = false, message = "ID Program tidak valid." });

            int userId = GetCurrentUserId();
            var prog = await _context.ProgramAnggarans
                .Include(p => p.AlokasiAnggaran)
                .FirstOrDefaultAsync(p => p.Id == req.Id);

            if (prog == null || prog.AlokasiAnggaran == null)
                return Json(new { success = false, message = "Program tidak ditemukan." });
            if (prog.AlokasiAnggaran.Status != "Draft")
                return Json(new { success = false, message = "Hanya bisa mengedit program pada alokasi yang belum diproses." });
            if (!CanAccessAlokasi(prog.AlokasiAnggaran, userId))
                return Json(new { success = false, message = "Tidak diizinkan." });

            var errs = ValidateProgram(req, 1);
            if (errs.Count > 0)
                return Json(new { success = false, message = string.Join(" | ", errs) });

            prog.NamaProgram = req.NamaProgram.Trim();
            prog.AnggaranDiajukan = req.AnggaranDiajukan;
            prog.AnggaranMinimum = req.AnggaranMinimum;
            prog.AnggaranMaksimum = req.AnggaranMaksimum;
            prog.TingkatPrioritas = req.TingkatPrioritas;
            prog.EstimasiManfaat = req.EstimasiManfaat;
            prog.StatusProgram = req.StatusProgram == "Ditunda" ? "Ditunda" : "Aktif";
            prog.UpdatedAt = DateTime.UtcNow;
            prog.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                program = new
                {
                    prog.Id,
                    prog.NamaProgram,
                    prog.AnggaranDiajukan,
                    prog.AnggaranMinimum,
                    prog.AnggaranMaksimum,
                    prog.TingkatPrioritas,
                    prog.EstimasiManfaat,
                    prog.StatusProgram
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgram([FromBody] IdRequest req)
        {
            int userId = GetCurrentUserId();
            var prog = await _context.ProgramAnggarans
                .Include(p => p.AlokasiAnggaran)
                .FirstOrDefaultAsync(p => p.Id == req.Id);

            if (prog == null || prog.AlokasiAnggaran == null)
                return Json(new { success = false, message = "Program tidak ditemukan." });
            if (prog.AlokasiAnggaran.Status != "Draft")
                return Json(new { success = false, message = "Hanya bisa menghapus program pada alokasi yang belum diproses." });
            if (!CanAccessAlokasi(prog.AlokasiAnggaran, userId))
                return Json(new { success = false, message = "Tidak diizinkan." });

            _context.ProgramAnggarans.Remove(prog);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== PROSES ALOKASI & RE-RUN ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Proses([FromBody] ProsesAlokasiRequest req)
        {
            if (req.TotalDana <= 0)
                return Json(new { success = false, message = "Total dana pagu tersedia harus lebih besar dari 0." });

            int userId = GetCurrentUserId();
            var alokasi = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                .FirstOrDefaultAsync(a => a.Id == req.Id && a.Status == "Draft");

            if (alokasi == null || !CanAccessAlokasi(alokasi, userId))
                return Json(new { success = false, message = "Data alokasi tidak ditemukan atau tidak diizinkan." });
            if (!alokasi.Programs.Any())
                return Json(new { success = false, message = "Tambahkan minimal satu program sebelum melakukan proses alokasi." });

            // Perbarui catatan dan total dana pagu dari inputan akhir
            alokasi.Catatan = req.Catatan?.Trim();
            alokasi.TotalDana = req.TotalDana;

            // Filter program yang aktif dihitung (bukan Ditunda)
            var activePrograms = alokasi.Programs.Where(p => p.StatusProgram != "Ditunda").ToList();
            if (!activePrograms.Any())
                return Json(new { success = false, message = "Seluruh program berstatus Ditunda. Aktifkan minimal satu program untuk melakukan perhitungan alokasi." });

            var allErrs = new List<string>();
            for (int i = 0; i < activePrograms.Count; i++)
            {
                var p = activePrograms[i];
                var req2 = new AddProgramRequest
                {
                    NamaProgram = p.NamaProgram,
                    AnggaranDiajukan = p.AnggaranDiajukan,
                    AnggaranMinimum = p.AnggaranMinimum,
                    AnggaranMaksimum = p.AnggaranMaksimum,
                    TingkatPrioritas = p.TingkatPrioritas,
                    EstimasiManfaat = p.EstimasiManfaat,
                    StatusProgram = p.StatusProgram
                };
                var errs = ValidateProgram(req2, i + 1);
                allErrs.AddRange(errs);
            }
            if (allErrs.Count > 0)
                return Json(new { success = false, message = string.Join(" | ", allErrs) });

            // Bentuk LP hanya untuk program aktif
            var lpPrograms = activePrograms.Select(p => new LinearProgrammingSolver.Program
            {
                NamaProgram = p.NamaProgram,
                AnggaranDiajukan = p.AnggaranDiajukan,
                Minimum = p.AnggaranMinimum,
                Maksimum = p.AnggaranMaksimum,
                ObjectiveCoeff = p.TingkatPrioritas * (p.EstimasiManfaat / 100.0)
            }).ToList();

            var result = LinearProgrammingSolver.Solve(lpPrograms, alokasi.TotalDana);

            if (!result.IsOptimal)
            {
                return Json(new
                {
                    success = false,
                    isNoSolution = true,
                    message = "Alokasi tidak dapat dilakukan karena tidak ditemukan solusi yang memenuhi seluruh batasan anggaran untuk program aktif."
                });
            }

            var hasilPrograms = new List<HasilProgramViewModel>();
            for (int i = 0; i < activePrograms.Count; i++)
            {
                var p = activePrograms[i];
                hasilPrograms.Add(new HasilProgramViewModel
                {
                    NamaProgram = p.NamaProgram,
                    AnggaranDiajukan = p.AnggaranDiajukan,
                    AnggaranMinimum = p.AnggaranMinimum,
                    AnggaranMaksimum = p.AnggaranMaksimum,
                    TingkatPrioritas = p.TingkatPrioritas,
                    EstimasiManfaat = p.EstimasiManfaat,
                    AlokasiOptimal = result.Allocations[i]
                });
            }

            // Simpan perubahan catatan & total dana di draft tanpa mengubah status ke Optimal
            alokasi.UpdatedAt = DateTime.UtcNow;
            alokasi.UpdatedBy = userId;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                data = new HasilAlokasiViewModel
                {
                    Catatan = alokasi.Catatan,
                    TotalDana = alokasi.TotalDana,
                    TotalDialokasikan = result.Allocations.Sum(),
                    Status = "Pratinjau",
                    TanggalProses = DateTime.Now,
                    Programs = hasilPrograms
                }
            });
        }

        /// <summary>
        /// POST /AlokasiAnggaran/SimpanHasil
        /// Menyimpan hasil alokasi secara permanen ke database dan riwayat dengan status Optimal.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimpanHasil([FromBody] IdRequest req)
        {
            try
            {
                int userId = GetCurrentUserId();
                var alokasi = await _context.AlokasiAnggarans
                    .Include(a => a.Programs)
                        .ThenInclude(p => p.HasilAlokasi)
                    .FirstOrDefaultAsync(a => a.Id == req.Id);

                if (alokasi == null || !CanAccessAlokasi(alokasi, userId))
                    return Json(new { success = false, message = "Data alokasi tidak ditemukan atau tidak diizinkan." });

                var activePrograms = alokasi.Programs.Where(p => p.StatusProgram != "Ditunda").ToList();
                var ditundaPrograms = alokasi.Programs.Where(p => p.StatusProgram == "Ditunda").ToList();

                if (!activePrograms.Any())
                    return Json(new { success = false, message = "Tidak ada program kegiatan aktif yang dialokasikan." });

                // Jalankan solver sekali lagi untuk program aktif memastikan validitas angka
                var lpPrograms = activePrograms.Select(p => new LinearProgrammingSolver.Program
                {
                    NamaProgram = p.NamaProgram,
                    AnggaranDiajukan = p.AnggaranDiajukan,
                    Minimum = p.AnggaranMinimum,
                    Maksimum = p.AnggaranMaksimum,
                    ObjectiveCoeff = p.TingkatPrioritas * (p.EstimasiManfaat / 100.0)
                }).ToList();

                var result = LinearProgrammingSolver.Solve(lpPrograms, alokasi.TotalDana);
                if (!result.IsOptimal)
                    return Json(new { success = false, message = "Gagal memvalidasi optimasi anggaran." });

                // Hapus hasil alokasi lama jika ada
                foreach (var prog in alokasi.Programs)
                {
                    if (prog.HasilAlokasi != null)
                    {
                        _context.HasilAlokasis.Remove(prog.HasilAlokasi);
                    }
                }

                for (int i = 0; i < activePrograms.Count; i++)
                {
                    var p = activePrograms[i];
                    var hasil = new HasilAlokasi
                    {
                        ProgramAnggaranId = p.Id,
                        AlokasiOptimal = result.Allocations[i],
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    _context.HasilAlokasis.Add(hasil);
                }

                // Jika terdapat program yang berstatus Ditunda, pindahkan ke draft baru
                // agar program yang belum diproses ini tetap muncul di halaman Index untuk alokasi berikutnya
                // dan tidak ikut tersimpan ke dalam riwayat alokasi yang sudah selesai (Optimal).
                if (ditundaPrograms.Any())
                {
                    var nextDraft = new AlokasiAnggaran
                    {
                        Periode = alokasi.Periode ?? DateTime.UtcNow.Year.ToString(),
                        Catatan = null,
                        TotalDana = 0,
                        Status = "Draft",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    _context.AlokasiAnggarans.Add(nextDraft);

                    foreach (var dp in ditundaPrograms)
                    {
                        dp.AlokasiAnggaran = nextDraft;
                        dp.UpdatedAt = DateTime.UtcNow;
                        dp.UpdatedBy = userId;
                    }
                }

                alokasi.Status = "Optimal";
                alokasi.UpdatedAt = DateTime.UtcNow;
                alokasi.UpdatedBy = userId;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Hasil rekomendasi alokasi berhasil disimpan ke Riwayat!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menyimpan hasil alokasi ID {Id}", req.Id);
                return Json(new { success = false, message = $"Gagal menyimpan alokasi: {ex.Message}" });
            }
        }

        /// <summary>
        /// POST /AlokasiAnggaran/ReopenDraft
        /// Kembalikan hasil alokasi dari History / Detail ke status Draft agar dapat diedit & di-run ulang.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenDraft([FromBody] IdRequest req)
        {
            int userId = GetCurrentUserId();
            var alokasi = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                    .ThenInclude(p => p.HasilAlokasi)
                .FirstOrDefaultAsync(a => a.Id == req.Id);

            if (alokasi == null || !CanAccessAlokasi(alokasi, userId))
                return Json(new { success = false, message = "Data alokasi tidak ditemukan atau tidak diizinkan." });

            // Jika ada draft lain yang sedang aktif, pindahkan programnya ke alokasi ini agar tidak hilang
            var otherDrafts = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                .Where(a => a.Status == "Draft" && (a.CreatedBy == userId || userId == 1 || User.IsInRole("Admin")) && a.Id != req.Id)
                .ToListAsync();

            foreach (var od in otherDrafts)
            {
                foreach (var op in od.Programs)
                {
                    op.AlokasiAnggaranId = alokasi.Id;
                    op.UpdatedAt = DateTime.UtcNow;
                    op.UpdatedBy = userId;
                }
                _context.AlokasiAnggarans.Remove(od);
            }

            // Hapus hasil alokasi lama dari alokasi ini
            foreach (var prog in alokasi.Programs)
            {
                if (prog.HasilAlokasi != null)
                {
                    _context.HasilAlokasis.Remove(prog.HasilAlokasi);
                }
            }

            alokasi.Status = "Draft";
            alokasi.UpdatedAt = DateTime.UtcNow;
            alokasi.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            return Json(new { success = true, redirectUrl = "/AlokasiAnggaran/Index" });
        }

        /// <summary>
        /// POST /AlokasiAnggaran/DeleteAllocation
        /// Hapus data alokasi beserta program dan hasilnya.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllocation([FromBody] IdRequest req)
        {
            int userId = GetCurrentUserId();
            var alokasi = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                    .ThenInclude(p => p.HasilAlokasi)
                .FirstOrDefaultAsync(a => a.Id == req.Id);

            if (alokasi == null)
                return Json(new { success = false, message = "Data alokasi tidak ditemukan." });

            foreach (var p in alokasi.Programs)
            {
                if (p.HasilAlokasi != null)
                {
                    _context.HasilAlokasis.Remove(p.HasilAlokasi);
                }
            }

            _context.ProgramAnggarans.RemoveRange(alokasi.Programs);
            _context.AlokasiAnggarans.Remove(alokasi);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Berhasil menghapus alokasi anggaran." });
        }

        // ==================== HISTORY ====================

        [HttpGet]
        public IActionResult History()
        {
            ViewData["Title"] = "History Anggaran";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory(string? catatan, string? startDate, string? endDate)
        {
            var query = _context.AlokasiAnggarans
                .Where(a => a.Status == "Optimal")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(catatan))
                query = query.Where(a => a.Catatan != null && a.Catatan.Contains(catatan));

            TimeZoneInfo wibZone;
            try
            {
                wibZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                try { wibZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta"); }
                catch { wibZone = TimeZoneInfo.CreateCustomTimeZone("WIB", TimeSpan.FromHours(7), "WIB", "WIB"); }
            }

            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var sDate))
            {
                var startWib = new DateTime(sDate.Year, sDate.Month, sDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(startWib, wibZone);
                query = query.Where(a => a.CreatedAt >= startUtc);
            }

            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var eDate))
            {
                var endWib = new DateTime(eDate.Year, eDate.Month, eDate.Day, 23, 59, 59, 999, DateTimeKind.Unspecified);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(endWib, wibZone);
                query = query.Where(a => a.CreatedAt <= endUtc);
            }

            var data = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new HistoryItemViewModel
                {
                    Id = a.Id,
                    Catatan = a.Catatan,
                    TotalDana = a.TotalDana,
                    TanggalProses = a.CreatedAt,
                    Status = a.Status
                })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var alokasi = await _context.AlokasiAnggarans
                .Include(a => a.Programs)
                    .ThenInclude(p => p.HasilAlokasi)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alokasi == null)
            {
                TempData["Error"] = "Data alokasi tidak ditemukan.";
                return RedirectToAction("History");
            }

            var creator = await _context.Users.FindAsync(alokasi.CreatedBy);

            var vm = new DetailHistoryViewModel
            {
                Id = alokasi.Id,
                Catatan = alokasi.Catatan,
                TotalDana = alokasi.TotalDana,
                TanggalProses = alokasi.CreatedAt.ToLocalTime(),
                Status = alokasi.Status,
                NamaUser = creator?.NamaLengkap ?? "-",
                Programs = alokasi.Programs
                    .Where(p => p.StatusProgram != "Ditunda")
                    .Select(p => new HasilProgramViewModel
                    {
                        NamaProgram = p.NamaProgram,
                        AnggaranDiajukan = p.AnggaranDiajukan,
                        AnggaranMinimum = p.AnggaranMinimum,
                        AnggaranMaksimum = p.AnggaranMaksimum,
                        TingkatPrioritas = p.TingkatPrioritas,
                        EstimasiManfaat = p.EstimasiManfaat,
                        AlokasiOptimal = p.HasilAlokasi?.AlokasiOptimal ?? 0
                    }).ToList()
            };

            return View(vm);
        }

        // ==================== VALIDASI ====================

        private List<string> ValidateProgram(AddProgramRequest p, int no)
        {
            var errs = new List<string>();

            if (string.IsNullOrWhiteSpace(p.NamaProgram))
                errs.Add($"Program {no}: Nama program wajib diisi.");
            if (p.AnggaranDiajukan == null && p.AnggaranMinimum == null && p.AnggaranMaksimum == null)
                errs.Add($"Program {no}: Minimal salah satu batas anggaran (Diajukan, Minimum, Maksimum) harus diisi.");
            if (p.AnggaranDiajukan is < 0)
                errs.Add($"Program {no}: Nilai anggaran tidak boleh negatif.");
            if (p.AnggaranMinimum is < 0)
                errs.Add($"Program {no}: Nilai anggaran tidak boleh negatif.");
            if (p.AnggaranMaksimum is < 0)
                errs.Add($"Program {no}: Nilai anggaran tidak boleh negatif.");
            if (p.AnggaranMinimum.HasValue && p.AnggaranMaksimum.HasValue && p.AnggaranMinimum > p.AnggaranMaksimum)
                errs.Add($"Program {no}: Anggaran minimum tidak boleh lebih besar dari anggaran maksimum.");
            if (p.TingkatPrioritas < 1 || p.TingkatPrioritas > 5)
                errs.Add($"Program {no}: Tingkat prioritas harus berada pada nilai 1 sampai 5.");
            if (p.EstimasiManfaat < 0 || p.EstimasiManfaat > 100)
                errs.Add($"Program {no}: Estimasi manfaat harus berada pada nilai 0 sampai 100.");

            return errs;
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────

    public class SaveHeaderRequest
    {
        public int? Id { get; set; }
        public string? Catatan { get; set; }
        public decimal TotalDana { get; set; }
    }

    public class AddProgramRequest
    {
        public int AlokasiId { get; set; }
        public string NamaProgram { get; set; } = string.Empty;
        public decimal? AnggaranDiajukan { get; set; }
        public decimal? AnggaranMinimum { get; set; }
        public decimal? AnggaranMaksimum { get; set; }
        public int TingkatPrioritas { get; set; }
        public int EstimasiManfaat { get; set; }
        public string? StatusProgram { get; set; } = "Aktif";
    }

    public class UpdateProgramRequest : AddProgramRequest
    {
        public int Id { get; set; }
    }

    public class IdRequest
    {
        public int Id { get; set; }
    }

    public class ProsesAlokasiRequest
    {
        public int Id { get; set; }
        public string? Catatan { get; set; }
        public decimal TotalDana { get; set; }
    }
}
