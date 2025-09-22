using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dal

{
 public class Order
    {
        [Required]
        public int OrderId { get; set; }  // مفتاح أساسي
        [Required]
        public string VehicleType { get; set; }
        [Required]
        [Display(Name = "Status ")]

        public string Status { get; set; }   // مثل (جديد، قيد التنفيذ، مكتمل)




        [Required]
        [Display(Name = "Pickup Location ")]

        public string PickupLocation { get; set; }
        [Required]
        [Display(Name = "Dropoff Location ")]

        public string DropoffLocation { get; set; }
   
        [Required]

        [Display(Name = "Distance ")]

        public double Distance { get; set; }

        [Required]

        [Display(Name = "DID ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DID { get; set; }

        [Required]
        [Display(Name = "Total Cost ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.Now;  // تعيين القيمة الافتراضية إلى الوقت الحالي

       
        public string TrackingNumber { get; set; }
        public string ETA { get; set; }  // حقل ETA
        public List<Customer_order> Customer_orders { get; set; } = new List<Customer_order>();

        public List<Order_Driver> Order_Drivers { get; set; } = new List<Order_Driver>();
    }
}
