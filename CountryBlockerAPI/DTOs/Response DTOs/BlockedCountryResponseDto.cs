namespace CountryBlockerAPI.DTOs.Response_DTOs
{
    public class BlockedCountryResponseDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
    }
}
