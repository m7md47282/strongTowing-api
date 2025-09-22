using Dal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class AdminDashboardViewModel
    {
        public List<Driver> Drivers { get; set; } // جميع السائقين
        public List<Driver> PendingDrivers { get; set; } // السائقون المعلقون
        public List<Order> Orders { get; set; } // جميع الطلبات
        public int NewRequestCount { get; set; } // عداد الطلبات الجديدة
        public List<DriverViewModel> AvailableDrivers { get; set; } // السائقون المتاحون
        public Order SelectedOrder { get; set; } // الطلب المختار للتفاصيل
    }
}
