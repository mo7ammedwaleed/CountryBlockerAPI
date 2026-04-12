namespace CountryBlockerAPI.DTOs.Response_DTOs
{
    public class TemporalBlockResponseDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int DurationMinutes { get; set; }
        public int MinutesRemaining { get; set; }
    }
}
