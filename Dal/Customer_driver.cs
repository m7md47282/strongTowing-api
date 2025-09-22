using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
  public  class Customer_driver
    {
       [ Required]
        public int Customer_driverId{ get; set; }

        [ForeignKey(nameof(Customers))]
        public int CustomerId { get; set; }  // مفتاح أجنبي
        public virtual Customer Customers { get; set; }  // علاقة مع جدول العملاء
     
        [ForeignKey(nameof(Drivers))]
        public int DriverId { get; set; }  // مفتاح أجنبي
        public virtual Driver Drivers { get; set; }  // علاقة مع جدول العملاء
    }
}
