using Dal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class DriverDashboardViewModel
    {
        // معلومات السائق
        public Driver Driver { get; set; }
        public Order Order { get; set; }

        // قائمة الطلبات المخصصة للسائق والتي لم تكتمل بعد
        public List<Order> Orders { get; set; }

        // الحقل الذي يخزن الموقع الحالي (اختياري حسب الحاجة)
        public string CurrentLocation { get; set; }
        public bool IsAvailable { get; set; }
    }
}
