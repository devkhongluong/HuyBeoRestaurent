using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class NguoiDung
    {
        [Key]
        public int NguoiDungID { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; } // UNIQUE

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; }

        [Required]
        [StringLength(100)]
        public string Hoten { get; set; }

        [Required]
        [StringLength(20)]
        public string VaiTro { get; set; } // Admin, ThuNgan, Bep, QuanLy

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } // DangLamViec, DaNghi

        // Navigation Properties
        public ICollection<DonHang> DonHangs { get; set; }
        public ICollection<NhatKyDon> NhatKyDons { get; set; }
    }
}
