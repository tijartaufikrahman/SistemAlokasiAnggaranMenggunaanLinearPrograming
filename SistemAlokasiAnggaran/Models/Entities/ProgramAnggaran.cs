using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemAlokasiAnggaran.Models.Entities
{
    [Table("program_anggaran")]
    public class ProgramAnggaran
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("AlokasiAnggaran")]
        public int AlokasiAnggaranId { get; set; }

        [Required]
        [MaxLength(200)]
        public string NamaProgram { get; set; } = string.Empty;

        [Column(TypeName = "REAL")]
        public decimal? AnggaranDiajukan { get; set; }

        [Column(TypeName = "REAL")]
        public decimal? AnggaranMinimum { get; set; }

        [Column(TypeName = "REAL")]
        public decimal? AnggaranMaksimum { get; set; }

        /// <summary>Skala 1-5</summary>
        [Required]
        public int TingkatPrioritas { get; set; }

        /// <summary>Skala 0-100</summary>
        [Required]
        public int EstimasiManfaat { get; set; }

        /// <summary>Status program: 'Aktif' (dihitung) atau 'Ditunda' (tidak dihitung dalam optimasi)</summary>
        [Required]
        [MaxLength(20)]
        public string StatusProgram { get; set; } = "Aktif";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public AlokasiAnggaran? AlokasiAnggaran { get; set; }
        public HasilAlokasi? HasilAlokasi { get; set; }
    }
}
