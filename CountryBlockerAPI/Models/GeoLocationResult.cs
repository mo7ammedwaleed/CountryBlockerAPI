using Newtonsoft.Json;

namespace CountryBlockerAPI.Models
{
    public class GeoLocationResult
    {
        [JsonProperty("ip")]
        public string Ip { get; set; } = string.Empty;

        [JsonProperty("country_code")]
        public string CountryCode { get; set; } = string.Empty;

        [JsonProperty("country_name")]
        public string CountryName { get; set; } = string.Empty;

        [JsonProperty("org")]
        public string Isp { get; set; } = string.Empty;

        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;

        [JsonProperty("region")]
        public string Region { get; set; } = string.Empty;

        [JsonProperty("error")]
        public bool Error { get; set; }

        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }
}
