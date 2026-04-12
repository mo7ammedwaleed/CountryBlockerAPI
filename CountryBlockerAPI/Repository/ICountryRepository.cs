using CountryBlockerAPI.Models;

namespace CountryBlockerAPI.Repository
{
    public interface ICountryRepository
    {
        bool AddBlockedCountry(BlockedCountry country);
        bool RemoveBlockedCountry(string countryCode);
        bool ExistsBlockedCountry(string countryCode);
        IEnumerable<BlockedCountry> GetAllBlockedCountries();

        bool AddTemporalBlock(TemporalBlock block);
        bool ExistsTemporalBlock(string countryCode);
        TemporalBlock? GetTemporalBlock(string countryCode);
        IEnumerable<TemporalBlock> GetAllTemporalBlocks();
        void RemoveExpiredTemporalBlocks();

        bool IsCountryBlocked(string countryCode);

        void AddLog(BlockAttemptLog log);
        IEnumerable<BlockAttemptLog> GetLogs();
    }
}
