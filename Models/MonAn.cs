using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class MonAn
    {
        [Key]
        public int MonAnID { get; set; }

        [Required]
        public int DanhMucID { get; set; }

        [Required]
        [StringLength(200)]
        public string TenMon { get; set; }

        public string? MoTa { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaBan { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiaVon { get; set; }

        [StringLength(255)]
        public string? HinhAnh { get; set; }

        [Required]
        public bool ConBan { get; set; }

        // Navigation Properties
        [ForeignKey("DanhMucID")]
        public DanhMuc DanhMuc { get; set; }

        public ICollection<MonAnTopping> MonAnToppings { get; set; }
        public ICollection<ChiTietDon> ChiTietDons { get; set; }
    }
}
