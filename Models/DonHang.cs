using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class DonHang
    {
        [Key]
        public int DonHangID { get; set; }

        [Required]
        public int PhienID { get; set; }

        public int? NguoiDungID { get; set; }

        [Required]
        [StringLength(20)]
        public string LoaiDon { get; set; } // TaiCho, MangDi

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } // ChoXacNhan, DaXacNhan, DangCheBien, SanSangPhucVu, DaHoanThanh, Huy

        [Required]
        [StringLength(20)]
        public string SoThuTu { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongGiamGia { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ThanhTien { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThaiTT { get; set; } // Chua ThanhToan, Da ThanhToan

        [StringLength(20)]
        public string? PhuongThucTT { get; set; } // TienMat, ChuyenKhoan, ViDienTu

        [Required]
        public DateTime NgayTao { get; set; }

        public DateTime? NgayHoanThanh { get; set; }

        // Navigation Properties
        [ForeignKey("PhienID")]
        public PhienOrder PhienOrder { get; set; }

        [ForeignKey("NguoiDungID")]
        public NguoiDung? NguoiDung { get; set; }

        public ICollection<ChiTietDon> ChiTietDons { get; set; }
        public ICollection<DonHangKhuyenMai> DonHangKhuyenMais { get; set; }
        public ICollection<NhatKyDon> NhatKyDons { get; set; }
    }
}
