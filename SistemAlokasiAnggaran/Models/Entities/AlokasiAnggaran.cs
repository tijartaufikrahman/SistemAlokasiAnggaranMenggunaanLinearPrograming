using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemAlokasiAnggaran.Models.Entities
{
    [Table("alokasi_anggaran")]
    public class AlokasiAnggaran
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(20)]
        public string Periode { get; set; } = DateTime.UtcNow.Year.ToString();

        [MaxLength(255)]
        public string? Catatan { get; set; }

        [Required]
        [Column(TypeName = "REAL")]
        public decimal TotalDana { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Optimal";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public ICollection<ProgramAnggaran> Programs { get; set; } = new List<ProgramAnggaran>();

        [NotMapped]
        public User? CreatedByUser { get; set; }
    }
}
