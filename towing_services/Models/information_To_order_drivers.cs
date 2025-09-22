using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class information_To_order_drivers
    {
        public int OrderId { get; set; }  // مفتاح أساسي
        public string VehicleType { get; set; }
        public string Status { get; set; }  // مثل (جديد، قيد التنفيذ، مكتمل)

        public string PickupLocation { get; set; }
        public string DropoffLocation { get; set; }

        public double Distance { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;  // تعيين القيمة الافتراضية إلى الوقت الحالي

        public string Name { get; set; }




    }
}
