namespace CountryBlockerAPI.DTOs.Response_DTOs
{
    public class CheckBlockResponseDto
    {
        public string IpAddress { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
