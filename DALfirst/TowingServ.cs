using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALfirst
{
    class TowingServ
    {
        [Key]
        public int id { get; set; }
        [Required]

        public string Location_one { get; set; }
        [Required]
        public string Location_tow { get; set; }

        [Required]
        public string Kind_of_car { get; set; }

        [Required]
        public string towing_needs { get; set; }
        [Required]
        public string Name_of_user { get; set; }
        [Required]
        public string Number_Of_user { get; set; }

        [Required]
        public string Gmail { get; set; }
    }
}
