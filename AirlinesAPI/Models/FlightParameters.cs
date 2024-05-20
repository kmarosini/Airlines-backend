using System.ComponentModel.DataAnnotations;

namespace AirlinesAPI.Models
{
    public class FlightParameters
    {
        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Departure airport code must be exactly 3 characters.")]
        public string DepartureAirport { get; set; }
        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Departure airport code must be exactly 3 characters.")]
        public string DestinationAirport { get; set; }
        [Required]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Departure Date must be in the format YYYY-MM-DD.")]
        public string DepartureDate { get; set; }
        [Required]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Return Date must be in the format YYYY-MM-DD.")]
        public string ReturnDate { get; set; }
        [Required]
        public int NumberOfPassengers { get; set; }
        [Required]
        public int Max { get; set; }
        [Required]
        public string Currency { get; set; }

    }
}
