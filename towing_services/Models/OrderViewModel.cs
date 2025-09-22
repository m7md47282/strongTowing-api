using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class OrderViewModel
    {

        public int orderId { get; set; }  // مفتاح أساسي
        public string vehicleType { get; set; }
      

        public string status { get; set; }   // مثل (جديد، قيد التنفيذ، مكتمل)

        public double Distance { get; set; }




        public string pickupLocation { get; set; }
    
        public string dropoffLocation { get; set; }
        public string customerName { get; set; }




        public string TrackingNumber { get; set; }
        public string ETA { get; set; }  // حقل ETA
    }
}
