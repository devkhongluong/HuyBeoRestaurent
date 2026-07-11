using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class PhienOrder
    {
        [Key]
        public int PhienID { get; set; }

        [Required]
        [StringLength(100)]
        public string MaToken { get; set; }

        [Required]
        public DateTime ThoiGianBatDau { get; set; }

        public DateTime? ThoiGianKetThuc { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } // DangMo, DaDong

        // Navigation Properties
        public ICollection<DonHang> DonHangs { get; set; }
    }
}
