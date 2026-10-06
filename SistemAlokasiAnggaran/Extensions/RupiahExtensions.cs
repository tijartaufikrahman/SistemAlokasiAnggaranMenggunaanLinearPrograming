namespace SistemAlokasiAnggaran
{
    /// <summary>
    /// Extension methods untuk formatting angka ke format Rupiah Indonesia.
    /// </summary>
    public static class RupiahExtensions
    {
        public static string ToRupiah(this decimal value)
        {
            return "Rp" + value.ToString("N0", new System.Globalization.CultureInfo("id-ID"));
        }

        public static string ToRupiah(this decimal? value)
        {
            if (!value.HasValue) return "-";
            return "Rp" + value.Value.ToString("N0", new System.Globalization.CultureInfo("id-ID"));
        }
    }
}
