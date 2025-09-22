using Dal;
using System.Collections.Generic;

namespace towing_services.Models
{
    public class OrdersModel
    {
        public List<Order> NewRequests { get; set; }
        public List<Order> InProgressRequests { get; set; }
    }
}
