using System.ComponentModel.DataAnnotations;

namespace CountryBlockerAPI.DTOs.Request_DTOs
{
    public class TemporalBlockRequestDto
    {
        [Required]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters.")]
        public string CountryCode { get; set; } = string.Empty;

        [Required]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        public int DurationMinutes { get; set; }
    }
}
