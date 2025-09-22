using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
 public   class Order_Driver
    {

        [Required]
        public int Order_DriverId { get; set; }


        [ForeignKey(nameof(Drivers))]
        public int DriverId { get; set; }  // مفتاح أجنبي
        public virtual Driver Drivers { get; set; }  // علاقة مع جدول العملاء

        [ForeignKey(nameof(Orders))]
        public int OrderId { get; set; }  // مفتاح أجنبي
        public virtual Order Orders { get; set; }  // علاقة مع جدول العملاء

    }
}
