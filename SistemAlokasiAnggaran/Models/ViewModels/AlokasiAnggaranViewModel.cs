namespace SistemAlokasiAnggaran.Models.ViewModels
{
    public class ProgramInputViewModel
    {
        public string NamaProgram { get; set; } = string.Empty;
        public decimal? AnggaranDiajukan { get; set; }
        public decimal? AnggaranMinimum { get; set; }
        public decimal? AnggaranMaksimum { get; set; }
        public int TingkatPrioritas { get; set; }
        public int EstimasiManfaat { get; set; }
    }

    public class AlokasiAnggaranInputViewModel
    {
        public string? Catatan { get; set; }
        public decimal TotalDana { get; set; }
        public List<ProgramInputViewModel> Programs { get; set; } = new();
    }

    public class HasilProgramViewModel
    {
        public string NamaProgram { get; set; } = string.Empty;
        public decimal? AnggaranDiajukan { get; set; }
        public decimal? AnggaranMinimum { get; set; }
        public decimal? AnggaranMaksimum { get; set; }
        public int TingkatPrioritas { get; set; }
        public int EstimasiManfaat { get; set; }
        public decimal AlokasiOptimal { get; set; }
    }

    public class HasilAlokasiViewModel
    {
        public string? Catatan { get; set; }
        public decimal TotalDana { get; set; }
        public decimal TotalDialokasikan { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime TanggalProses { get; set; }
        public List<HasilProgramViewModel> Programs { get; set; } = new();
    }

    public class HistoryItemViewModel
    {
        public int Id { get; set; }
        public string? Catatan { get; set; }
        public decimal TotalDana { get; set; }
        public DateTime TanggalProses { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DetailHistoryViewModel
    {
        public int Id { get; set; }
        public string? Catatan { get; set; }
        public decimal TotalDana { get; set; }
        public DateTime TanggalProses { get; set; }
        public string Status { get; set; } = string.Empty;
        public string NamaUser { get; set; } = string.Empty;
        public List<HasilProgramViewModel> Programs { get; set; } = new();
    }

    public class HalamanDepanViewModel
    {
        public bool AdaData { get; set; }
        public string? Catatan { get; set; }
        public decimal TotalDana { get; set; }
        public int JumlahProgram { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime TanggalProses { get; set; }

        // Core Dashboard Metric Cards (Sistem Informasi Anggaran)
        public int TotalHistoryCount { get; set; }
        public decimal TotalDanaDialokasikan { get; set; }
        public decimal TotalDanaTerserap { get; set; }
        public double RataRataPenyerapan { get; set; }
        public int TotalProgramCount { get; set; }
        public int TotalOptimalCount { get; set; }

        // History 1 minggu kebelakang
        public List<HistoryItemViewModel> RiwayatSatuMinggu { get; set; } = new();
    }
}
