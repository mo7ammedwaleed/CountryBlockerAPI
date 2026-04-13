using CountryBlockerAPI.DTOs.Response_DTOs;

namespace CountryBlockerAPI.Services
{
    public interface ICountryService
    {
        Task<(bool Success, string Error)> BlockCountryAsync(string countryCode);
        (bool Success, string Error) UnblockCountry(string countryCode);
        PagedResponseDto<BlockedCountryResponseDto> GetAllBlocked(int page, int pageSize, string? search);

        Task<(bool Success, string Error, int StatusCode)> AddTemporalBlockAsync(string countryCode, int durationMinutes);
    }
}
