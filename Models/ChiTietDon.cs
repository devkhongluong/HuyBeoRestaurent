using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class ChiTietDon
    {
        [Key]
        public int ChiTietID { get; set; }

        [Required]
        public int DonHangID { get; set; }

        [Required]
        public int MonAnID { get; set; }

        [Required]
        public int SoLuong { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ThanhTien { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThaiBep { get; set; } // Cho, DangLam, HoanThanh

        public DateTime? ThoiGianBatDauCB { get; set; }
        public DateTime? ThoiGianHoanThanh { get; set; }

        // Navigation Properties
        [ForeignKey("DonHangID")]
        public DonHang DonHang { get; set; }

        [ForeignKey("MonAnID")]
        public MonAn MonAn { get; set; }

        public ICollection<ChiTietTopping> ChiTietToppings { get; set; }
    }
}
