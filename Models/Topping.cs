using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class Topping
    {
        [Key]
        public int ToppingID { get; set; }

        [Required]
        [StringLength(100)]
        public string TenTopping { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaThem { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } // ConBan, HetHang

        // Navigation Properties
        public ICollection<MonAnTopping> MonAnToppings { get; set; }
        public ICollection<ChiTietTopping> ChiTietToppings { get; set; }
    }
}
