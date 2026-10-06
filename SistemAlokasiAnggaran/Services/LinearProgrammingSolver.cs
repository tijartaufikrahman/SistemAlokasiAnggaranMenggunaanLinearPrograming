namespace SistemAlokasiAnggaran.Services
{
    /// <summary>
    /// Continuous Linear Programming Solver untuk alokasi anggaran.
    /// Memaksimalkan total manfaat tertimbang dengan kendala:
    ///   - Sum(Alokasi) = TotalDana
    ///   - Setiap program wajib mendapat alokasi dana (tidak ada alokasi Rp 0)
    ///   - Menggunakan Anggaran Diajukan sebagai acuan baseline & batas atas alami jika Maksimum tidak diisi
    ///   - Mengabaikan alokasi biner (non-MILP / continuous allocation)
    /// </summary>
    public class LinearProgrammingSolver
    {
        public class Program
        {
            public string NamaProgram { get; set; } = string.Empty;
            public decimal? AnggaranDiajukan { get; set; }
            public decimal? Minimum { get; set; }
            public decimal? Maksimum { get; set; }
            /// <summary>Koefisien fungsi tujuan (nilai manfaat tertimbang)</summary>
            public double ObjectiveCoeff { get; set; }
        }

        public class SolverResult
        {
            public bool IsOptimal { get; set; }
            public decimal[] Allocations { get; set; } = Array.Empty<decimal>();
        }

        public static SolverResult Solve(IList<Program> programs, decimal totalDana)
        {
            int n = programs.Count;
            if (n == 0 || totalDana <= 0)
                return new SolverResult { IsOptimal = false };

            var lb = new decimal[n]; // Lower bounds
            var ub = new decimal[n]; // Upper bounds

            // 1. Tentukan Lower Bound (agar SEMUA program kebagian dana)
            for (int i = 0; i < n; i++)
            {
                var p = programs[i];
                if (p.Minimum.HasValue && p.Minimum.Value > 0)
                {
                    lb[i] = p.Minimum.Value;
                }
                else if (p.AnggaranDiajukan.HasValue && p.AnggaranDiajukan.Value > 0)
                {
                    // Minimal 10% dari diajukan atau porsi proporsional agar pasti kebagian dana
                    decimal minProp = Math.Min(p.AnggaranDiajukan.Value, totalDana / (n * 2m));
                    lb[i] = Math.Max(1m, Math.Round(minProp, 0));
                }
                else
                {
                    // Minimal dana simbolis
                    lb[i] = Math.Max(1m, Math.Round(totalDana / (n * 10m), 0));
                }
            }

            // 2. Tentukan Upper Bound
            for (int i = 0; i < n; i++)
            {
                var p = programs[i];
                if (p.Maksimum.HasValue && p.Maksimum.Value > 0)
                {
                    ub[i] = p.Maksimum.Value;
                }
                else if (p.AnggaranDiajukan.HasValue && p.AnggaranDiajukan.Value > 0)
                {
                    // Anggaran Diajukan menjadi batas atas alami jika Maksimum kosong
                    ub[i] = Math.Max(lb[i], p.AnggaranDiajukan.Value);
                }
                else
                {
                    ub[i] = Math.Max(lb[i], totalDana);
                }

                // Pastikan ub >= lb
                if (ub[i] < lb[i])
                    ub[i] = lb[i];
            }

            // 3. Penyesuaian Jika Sum(lb) > totalDana (Dana kurang untuk memenuhi minimum)
            decimal sumLb = lb.Sum();
            if (sumLb > totalDana)
            {
                // Skalakan lb secara proporsional agar total = totalDana dan semua tetap kebagian
                decimal scale = totalDana / sumLb;
                for (int i = 0; i < n; i++)
                {
                    lb[i] = Math.Round(lb[i] * scale, 0);
                    if (ub[i] < lb[i]) ub[i] = lb[i];
                }
                sumLb = lb.Sum();
            }

            // 4. Penyesuaian Jika Sum(ub) < totalDana (Dana melimpah, naikkan ub)
            decimal sumUb = ub.Sum();
            if (sumUb < totalDana)
            {
                // Perluas upper bound agar seluruh dana terserap habis
                decimal scale = totalDana / sumUb;
                for (int i = 0; i < n; i++)
                {
                    ub[i] = Math.Round(ub[i] * scale, 0);
                }
            }

            // 5. Alokasikan awal sebesar lb
            decimal[] alloc = lb.ToArray();
            decimal sisaDana = totalDana - alloc.Sum();

            // 6. Urutkan berdasarkan bobot manfaat tertimbang (Greedy LP)
            var indices = Enumerable.Range(0, n)
                .OrderByDescending(i => programs[i].ObjectiveCoeff)
                .ToArray();

            // Distribusikan sisa dana ke program berbobot tinggi hingga mencapai ub
            foreach (int i in indices)
            {
                if (sisaDana <= 0m) break;
                decimal ruangTersedia = ub[i] - alloc[i];
                if (ruangTersedia <= 0m) continue;

                decimal alokasiTambahan = Math.Min(ruangTersedia, sisaDana);
                alloc[i] += alokasiTambahan;
                sisaDana -= alokasiTambahan;
            }

            // Jika masih ada sisa (misal semua program sudah mencapai ub)
            if (sisaDana > 0m)
            {
                // Bagikan sisa secara proporsional ke semua program
                for (int i = 0; i < n; i++)
                {
                    int idx = indices[i];
                    alloc[idx] += sisaDana / n;
                }
            }

            // 7. Pembulatan & Koreksi Presisi Rupiah
            decimal[] allocFinal = alloc.Select(a => Math.Round(a, 0)).ToArray();
            decimal diff = totalDana - allocFinal.Sum();
            if (diff != 0 && n > 0)
            {
                // Berikan selisih pembulatan ke program prioritas tertinggi
                allocFinal[indices[0]] += diff;
            }

            return new SolverResult
            {
                IsOptimal = true,
                Allocations = allocFinal
            };
        }
    }
}
