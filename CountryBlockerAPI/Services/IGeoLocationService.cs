using CountryBlockerAPI.Models;

namespace CountryBlockerAPI.Services
{
    public interface IGeoLocationService
    {
        Task<GeoLocationResult> LookupAsync(string ipAddress);
    }
}
