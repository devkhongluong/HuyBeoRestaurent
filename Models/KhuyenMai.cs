using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class KhuyenMai
    {
        [Key]
        public int KhuyenMaiID { get; set; }

        [Required]
        [StringLength(20)]
        public string MaCode { get; set; }

        [StringLength(150)]
        public string? TenKM { get; set; }

        [Required]
        [StringLength(20)]
        public string LoaiGiam { get; set; } // PhanTram, TienMat

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaTri { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiamToiDa { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DonToiThieu { get; set; }

        public int? SoLuongToiDa { get; set; }

        [Required]
        public int SoLuongDaDung { get; set; }

        [Required]
        public DateTime NgayBatDau { get; set; }

        [Required]
        public DateTime NgayKetThuc { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } // HoatDong, TamDung

        // Navigation Properties
        public ICollection<DonHangKhuyenMai> DonHangKhuyenMais { get; set; }
    }
}
