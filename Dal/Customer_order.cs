using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
 public class Customer_order
    {
        [Required]
        public int Customer_orderId { get; set; }




        [ForeignKey(nameof(Customers))]
        public int CustomerId { get; set; }  // مفتاح أجنبي
        public virtual Customer Customers { get; set; }  // علاقة مع جدول العملاء

        [ForeignKey(nameof(Orders))]
        public int OrderId { get; set; }  // مفتاح أجنبي
        public virtual Order Orders { get; set; }  // علاقة مع جدول العملاء
    }
}
