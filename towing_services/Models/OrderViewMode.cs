using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class OrderViewMode
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public string DropoffLocation { get; set; }
        public string PickupLocation { get; set; }
        public string VehicleType { get; set; }
        public string ETA { get; set; }
        public string TrackingNumber { get; set; }
    }
}
