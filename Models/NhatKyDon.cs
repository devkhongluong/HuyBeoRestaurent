using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class NhatKyDon
    {
        [Key]
        public int LogID { get; set; }

        [Required]
        public int DonHangID { get; set; }

        public int? NguoiDungID { get; set; }

        [Required]
        [StringLength(50)]
        public string HanhDong { get; set; } // Tao moi, Xac nhan, Huy don, Sua mon...

        [Required]
        public DateTime NgayTao { get; set; }

        // Navigation Properties
        [ForeignKey("DonHangID")]
        public DonHang DonHang { get; set; }

        [ForeignKey("NguoiDungID")]
        public NguoiDung? NguoiDung { get; set; }
    }
}
