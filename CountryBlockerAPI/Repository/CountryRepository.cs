using System.Collections.Concurrent;
using CountryBlockerAPI.Models;

namespace CountryBlockerAPI.Repository
{
    public class CountryRepository
    {

        private readonly ConcurrentDictionary<string, BlockedCountry> _blockedCountries = new();
        private readonly ConcurrentDictionary<string, TemporalBlock> _temporalBlocks = new();


        private readonly List<BlockAttemptLog> _logs = new();
        private readonly object _logLock = new();


        public bool AddBlockedCountry(BlockedCountry country)
        {
            return _blockedCountries.TryAdd(country.CountryCode.ToUpperInvariant(), country);
        }

        public bool RemoveBlockedCountry(string countryCode)
        {
            return _blockedCountries.TryRemove(countryCode.ToUpperInvariant(), out _);
        }

        public bool ExistsBlockedCountry(string countryCode)
        {
            return _blockedCountries.ContainsKey(countryCode.ToUpperInvariant());
        }

        public IEnumerable<BlockedCountry> GetAllBlockedCountries()
        {
            return _blockedCountries.Values.ToList();
        }

        

        public bool AddTemporalBlock(TemporalBlock block)
        {
            return _temporalBlocks.TryAdd(block.CountryCode.ToUpperInvariant(), block);
        }

        public bool ExistsTemporalBlock(string countryCode)
        {
            return _temporalBlocks.ContainsKey(countryCode.ToUpperInvariant());
        }

        public TemporalBlock? GetTemporalBlock(string countryCode)
        {
            _temporalBlocks.TryGetValue(countryCode.ToUpperInvariant(), out var block);
            return block;
        }

        public IEnumerable<TemporalBlock> GetAllTemporalBlocks()
        {
            return _temporalBlocks.Values.ToList();
        }

        public void RemoveExpiredTemporalBlocks()
        {
            var expired = _temporalBlocks
                .Where(kvp => kvp.Value.ExpiresAt <= DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
                _temporalBlocks.TryRemove(key, out _);
        }

        

        public bool IsCountryBlocked(string countryCode)
        {
            var code = countryCode.ToUpperInvariant();

            // Permanently blocked?
            if (_blockedCountries.ContainsKey(code)) return true;

            // Temporarily blocked and not yet expired?
            if (_temporalBlocks.TryGetValue(code, out var temporal))
                return temporal.ExpiresAt > DateTime.UtcNow;

            return false;
        }

        

        public void AddLog(BlockAttemptLog log)
        {
            lock (_logLock)
            {
                _logs.Add(log);
            }
        }

        public IEnumerable<BlockAttemptLog> GetLogs()
        {
            lock (_logLock)
            {
                return _logs.ToList();
            }
        }
    }
}
