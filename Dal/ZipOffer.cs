using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
    public class ZipOffer

    {
        public int Id { get; set; }
        public string ZipCode { get; set; }
        public string OfferDescription { get; set; }
        public string AreaName { get; set; }
        public int VisitCount { get; set; } = 0;
    }
}
