using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
   public  class Admin : IdentityUser<int>
    {
        public bool  IsAdmin { get; set; }

    }
}
