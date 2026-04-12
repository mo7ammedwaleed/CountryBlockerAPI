using CountryBlockerAPI.Models;
using Newtonsoft.Json;

namespace CountryBlockerAPI.Services
{
    public class GeoLocationService : IGeoLocationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeoLocationService> _logger;

        public GeoLocationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeoLocationService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeoLocation:ApiKey"] ?? string.Empty;
            _logger = logger;
        }

        public async Task<GeoLocationResult> LookupAsync(string ipAddress)
        {
            var url = string.IsNullOrEmpty(_apiKey)
                ? $"https://ipapi.co/{ipAddress}/json/"
                : $"https://ipapi.co/{ipAddress}/json/?key={_apiKey}";

            _logger.LogInformation("GeoLocation lookup for IP: {Ip}", ipAddress);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling GeoLocation API for IP {Ip}", ipAddress);
                throw new GeoLocationException("Network error reaching GeoLocation API.", ex);
            }

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "GeoLocation API returned {Status} for IP {Ip}: {Body}",
                    response.StatusCode, ipAddress, json);
                throw new GeoLocationException(
                    $"GeoLocation API error ({(int)response.StatusCode}): {response.ReasonPhrase}");
            }

            GeoLocationResult? result;
            try
            {
                result = JsonConvert.DeserializeObject<GeoLocationResult>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse GeoLocation response for IP {Ip}", ipAddress);
                throw new GeoLocationException("Failed to parse GeoLocation API response.", ex);
            }

            if (result is null)
                throw new GeoLocationException("GeoLocation API returned an empty response.");

            if (result.Error)
            {
                _logger.LogWarning(
                    "GeoLocation API returned error for IP {Ip}: {Reason}", ipAddress, result.Reason);
                throw new GeoLocationException(
                    $"GeoLocation API error: {result.Reason ?? "Unknown error"}");
            }

            return result;
        }
    }
}

public class GeoLocationException : Exception
{
    public GeoLocationException(string message) : base(message) { }
    public GeoLocationException(string message, Exception inner) : base(message, inner) { }
}
