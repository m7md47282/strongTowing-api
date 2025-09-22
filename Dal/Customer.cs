using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
   public class Customer
    {
        [Required]
        public int CustomerId { get; set; }  // مفتاح أساسي
        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name should contain only letters and spaces.")]

        public string Name { get; set; }
        [Required]
        [Phone(ErrorMessage = "Enter a valid phone number.")]

        public string Phone { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]

        public string Email { get; set; }
        [Required]
        public string Address { get; set; }

        public string VIN  { get; set; }

        public string Color_model_name { get; set; }


        public string Message_or_inquiry { get; set; }
        public List<Customer_driver> Customer_drivers { get; set; } = new List<Customer_driver>();
        public List<Customer_order> Customer_orders { get; set; } = new List<Customer_order>();

    }
}
