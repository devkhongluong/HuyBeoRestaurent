using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class DonHangKhuyenMai
    {
        [Key, Column(Order = 0)]
        public int DonHangID { get; set; }

        [Key, Column(Order = 1)]
        public int KhuyenMaiID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienGiam { get; set; }

        // Navigation Properties
        [ForeignKey("DonHangID")]
        public DonHang DonHang { get; set; }

        [ForeignKey("KhuyenMaiID")]
        public KhuyenMai KhuyenMai { get; set; }
    }
}
