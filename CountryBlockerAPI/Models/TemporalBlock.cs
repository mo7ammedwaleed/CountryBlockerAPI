namespace CountryBlockerAPI.Models
{
    public class TemporalBlock
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public int DurationMinutes { get; set; }
    }
}
