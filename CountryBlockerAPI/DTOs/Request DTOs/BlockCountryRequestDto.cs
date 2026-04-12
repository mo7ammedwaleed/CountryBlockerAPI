using System.ComponentModel.DataAnnotations;

namespace CountryBlockerAPI.DTOs.Request_DTOs
{
    public class BlockCountryRequestDto
    {
        [Required]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters.")]
        public string CountryCode { get; set; } = string.Empty;
    }
}
