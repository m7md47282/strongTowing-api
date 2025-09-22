using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
    public class HR
    {
        public int Id { get; set; }
        public string FullName { get; set; }

        public string Email { get; set; }   
        public string PhoneNumber { get; set; }

        public string PasswordHash { get; set; }  // تخزن الباسورد مشفر



        public bool IsHR { get; set; } 

    }
}
