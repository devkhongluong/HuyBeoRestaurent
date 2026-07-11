using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webHuyBeo.Models
{
    public class MonAnTopping
    {
        [Key, Column(Order = 0)]
        public int MonAnID { get; set; }

        [Key, Column(Order = 1)]
        public int ToppingID { get; set; }

        // Navigation Properties
        [ForeignKey("MonAnID")]
        public MonAn MonAn { get; set; }

        [ForeignKey("ToppingID")]
        public Topping Topping { get; set; }
    }
}
