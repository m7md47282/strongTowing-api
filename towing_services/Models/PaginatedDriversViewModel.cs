using System.Collections.Generic;

namespace towing_services.Models
{
    public class PaginatedDriversViewModel
    {
        public List<DriverViewModel> Drivers { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
