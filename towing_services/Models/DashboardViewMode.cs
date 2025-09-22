using Dal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Models
{
    public class DashboardViewMode
    {
        public Driver Driver { get; set; }
        public List<OrderViewModel> Orders { get; set; }
        public int TotalOrders { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}
