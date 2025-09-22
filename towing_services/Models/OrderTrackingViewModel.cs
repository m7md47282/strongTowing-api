using Dal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class OrderTrackingViewModel
    {
        public Order Order { get; set; } // يحتوي على تفاصيل الطلب.
        public string CustomerName { get; set; } // اسم الزبون.
        public string VIN { get; set; }
        public string Color_model_name { get; set; } // اسم الزبون.



    }
}
