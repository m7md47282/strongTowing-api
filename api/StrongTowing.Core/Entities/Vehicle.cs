using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public string VIN { get; set; } = string.Empty; 

        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;

        // Optional owner link
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }
    }
}


