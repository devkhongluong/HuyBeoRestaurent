using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class ChiTietTopping
    {
        [Key, Column(Order = 0)]
        public int ChiTietID { get; set; }

        [Key, Column(Order = 1)]
        public int ToppingID { get; set; }

        [Required]
        public int SoLuong { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        // Navigation Properties
        [ForeignKey("ChiTietID")]
        public ChiTietDon ChiTietDon { get; set; }

        [ForeignKey("ToppingID")]
        public Topping Topping { get; set; }
    }
}
