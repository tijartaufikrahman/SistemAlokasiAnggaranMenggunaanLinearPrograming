using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemAlokasiAnggaran.Models.Entities
{
    [Table("hasil_alokasi")]
    public class HasilAlokasi
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("ProgramAnggaran")]
        public int ProgramAnggaranId { get; set; }

        [Required]
        [Column(TypeName = "REAL")]
        public decimal AlokasiOptimal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public ProgramAnggaran? ProgramAnggaran { get; set; }
    }
}
