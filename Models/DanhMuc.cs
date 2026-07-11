using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class DanhMuc
    {
        [Key]
        public int DanhMucID { get; set; }

        [Required]
        [StringLength(100)]
        public string TenDanhMuc { get; set; }

        public string? MoTa { get; set; }

        public int? ThuTu { get; set; }

        // Navigation Properties
        public ICollection<MonAn> MonAns { get; set; }
    }
}
