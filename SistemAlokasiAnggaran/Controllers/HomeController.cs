using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemAlokasiAnggaran.Data;
using SistemAlokasiAnggaran.Models;
using SistemAlokasiAnggaran.Models.ViewModels;
using System.Diagnostics;

namespace SistemAlokasiAnggaran.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Redirect based on auth
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");

                // Ambil data ringkasan proses terakhir
                var last = await _context.AlokasiAnggarans
                    .Include(a => a.Programs)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                var vm = new HalamanDepanViewModel();
                if (last != null)
                {
                    vm.AdaData = true;
                    vm.Catatan = last.Catatan;
                    vm.TotalDana = last.TotalDana;
                    vm.JumlahProgram = last.Programs.Count;
                    vm.Status = last.Status;
                    vm.TanggalProses = last.CreatedAt.ToLocalTime();
                }

                // Core Financial Information Metrics
                vm.TotalHistoryCount = await _context.AlokasiAnggarans.CountAsync(a => a.Status != "Draft");
                var listTotalDana = await _context.AlokasiAnggarans.Where(a => a.Status != "Draft").Select(a => a.TotalDana).ToListAsync();
                vm.TotalDanaDialokasikan = listTotalDana.Sum();

                var listTerserap = await _context.HasilAlokasis.Select(h => h.AlokasiOptimal).ToListAsync();
                vm.TotalDanaTerserap = listTerserap.Sum();
                vm.RataRataPenyerapan = vm.TotalDanaDialokasikan > 0 
                    ? Math.Min(100.0, Math.Round((double)(vm.TotalDanaTerserap / vm.TotalDanaDialokasikan) * 100.0, 1)) 
                    : 0.0;

                vm.TotalProgramCount = await _context.ProgramAnggarans.CountAsync();
                vm.TotalOptimalCount = await _context.AlokasiAnggarans.CountAsync(a => a.Status == "Optimal");

                // History 1 minggu kebelakang (past 7 days)
                var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
                var weeklyHistory = await _context.AlokasiAnggarans
                    .Where(a => a.CreatedAt >= oneWeekAgo && a.Status != "Draft")
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

                // Fallback: Jika dalam 7 hari terakhir belum ada sesi, tampilkan 5 riwayat alokasi terbaru agar tabel tidak kosong begitu saja
                if (!weeklyHistory.Any())
                {
                    weeklyHistory = await _context.AlokasiAnggarans
                        .Where(a => a.Status != "Draft")
                        .OrderByDescending(a => a.CreatedAt)
                        .Take(5)
                        .Select(a => new HistoryItemViewModel
                        {
                            Id = a.Id,
                            Catatan = a.Catatan,
                            TotalDana = a.TotalDana,
                            TanggalProses = a.CreatedAt,
                            Status = a.Status
                        })
                        .ToListAsync();
                }

                vm.RiwayatSatuMinggu = weeklyHistory;

                ViewData["Title"] = "Dashboard";
                return View(vm);
            }
            return RedirectToAction("Login", "Account");
        }

        public IActionResult HalamanDepan()
        {
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

