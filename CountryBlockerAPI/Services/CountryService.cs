using CountryBlockerAPI.Models;
using CountryBlockerAPI.Repository;
using CountryBlockerAPI.DTOs.Response_DTOs;
using CountryBlockerAPI.Utilities;

namespace CountryBlockerAPI.Services
{
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _repo;
        private readonly ILogger<CountryService> _logger;

        public CountryService(ICountryRepository repo, ILogger<CountryService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<(bool Success, string Error)> BlockCountryAsync(string countryCode)
        {
            var code = countryCode.ToUpperInvariant();

            if (!Constants.ValidCountryCodes.Contains(code))
                return Task.FromResult((false, $"'{code}' is not a valid ISO 3166-1 alpha-2 country code."));

            if (_repo.ExistsBlockedCountry(code))
                return Task.FromResult((false, $"Country '{code}' is already blocked."));

            var country = new BlockedCountry
            {
                CountryCode = code,
                CountryName = GetCountryName(code),
                BlockedAt = DateTime.UtcNow
            };

            var added = _repo.AddBlockedCountry(country);
            if (!added)
                return Task.FromResult((false, $"Country '{code}' is already blocked."));

            _logger.LogInformation("Country {Code} permanently blocked.", code);
            return Task.FromResult((true, string.Empty));
        }

        public (bool Success, string Error) UnblockCountry(string countryCode)
        {
            var code = countryCode.ToUpperInvariant();

            if (!_repo.ExistsBlockedCountry(code))
                return (false, $"Country '{code}' is not in the blocked list.");

            _repo.RemoveBlockedCountry(code);
            _logger.LogInformation("Country {Code} unblocked.", code);
            return (true, string.Empty);
        }

        public PagedResponseDto<BlockedCountryResponseDto> GetAllBlocked(int page, int pageSize, string? search)
        {
            var all = _repo.GetAllBlockedCountries();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                all = all.Where(c =>
                    c.CountryCode.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    c.CountryName.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var list = all.ToList();
            var paged = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new BlockedCountryResponseDto
                {
                    CountryCode = c.CountryCode,
                    CountryName = c.CountryName,
                    BlockedAt = c.BlockedAt
                });

            return new PagedResponseDto<BlockedCountryResponseDto>
            {
                Data = paged,
                Page = page,
                PageSize = pageSize,
                TotalCount = list.Count
            };
        }

        public Task<(bool Success, string Error, int StatusCode)> AddTemporalBlockAsync(
            string countryCode, int durationMinutes)
        {
            var code = countryCode.ToUpperInvariant();

            if (!Constants.ValidCountryCodes.Contains(code))
                return Task.FromResult((false, $"'{code}' is not a valid ISO 3166-1 alpha-2 country code.", 400));

            if (_repo.ExistsTemporalBlock(code))
                return Task.FromResult((false, $"Country '{code}' is already temporarily blocked.", 409));

            var block = new TemporalBlock
            {
                CountryCode = code,
                CountryName = GetCountryName(code),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
                DurationMinutes = durationMinutes
            };

            var blockedAtemps = new BlockAttemptLog
            {
                CountryCode = code,
                CountryName = GetCountryName(code),
                IsBlocked = true
            };

            var added = _repo.AddTemporalBlock(block);
            if (!added)
                return Task.FromResult((false, $"Country '{code}' is already temporarily blocked.", 409));

            _logger.LogInformation(
                "Temporal block added for {Code} – expires at {Expiry}", code, block.ExpiresAt);

            _repo.AddLog(blockedAtemps);

            return Task.FromResult((true, string.Empty, 200));
        }

        public TemporalBlockResponseDto? GetTemporalBlockInfo(string countryCode)
        {
            var block = _repo.GetTemporalBlock(countryCode.ToUpperInvariant());
            if (block is null || block.ExpiresAt <= DateTime.UtcNow) return null;

            return new TemporalBlockResponseDto
            {
                CountryCode = block.CountryCode,
                CountryName = block.CountryName,
                ExpiresAt = block.ExpiresAt,
                DurationMinutes = block.DurationMinutes,
                MinutesRemaining = (int)(block.ExpiresAt - DateTime.UtcNow).TotalMinutes
            };
        }

        public IEnumerable<TemporalBlockResponseDto> GetAllActiveTemporalBlocks()
        {
            return _repo.GetAllTemporalBlocks()
                .Where(b => b.ExpiresAt > DateTime.UtcNow)
                .Select(b => new TemporalBlockResponseDto
                {
                    CountryCode = b.CountryCode,
                    CountryName = b.CountryName,
                    ExpiresAt = b.ExpiresAt,
                    DurationMinutes = b.DurationMinutes,
                    MinutesRemaining = (int)(b.ExpiresAt - DateTime.UtcNow).TotalMinutes
                });
        }

        public static bool IsValidCountryCode(string code) =>
            Constants.ValidCountryCodes.Contains(code?.ToUpperInvariant() ?? "");

        private static string GetCountryName(string code) => code.ToUpperInvariant() switch
        {
            "US" => "United States",
            "GB" => "United Kingdom",
            "EG" => "Egypt",
            "DE" => "Germany",
            "FR" => "France",
            "CN" => "China",
            "RU" => "Russia",
            "JP" => "Japan",
            "IN" => "India",
            "BR" => "Brazil",
            "CA" => "Canada",
            "AU" => "Australia",
            "SA" => "Saudi Arabia",
            "AE" => "United Arab Emirates",
            "IL" => "Israel",
            "IR" => "Iran",
            "PK" => "Pakistan",
            "TR" => "Turkey",
            "NG" => "Nigeria",
            "ZA" => "South Africa",
            "IT" => "Italy",
            "ES" => "Spain",
            "KR" => "South Korea",
            "MX" => "Mexico",
            "ID" => "Indonesia",
            "PH" => "Philippines",
            "VN" => "Vietnam",
            "TH" => "Thailand",
            "SE" => "Sweden",
            "NO" => "Norway",
            "DK" => "Denmark",
            "FI" => "Finland",
            "PL" => "Poland",
            "NL" => "Netherlands",
            "BE" => "Belgium",
            "CH" => "Switzerland",
            "AT" => "Austria",
            "PT" => "Portugal",
            "GR" => "Greece",
            "SY" => "Syria",
            "IQ" => "Iraq",
            "LY" => "Libya",
            "SD" => "Sudan",
            "YE" => "Yemen",
            "KP" => "North Korea",
            "CU" => "Cuba",
            "VE" => "Venezuela",
            _ => code
        };
    }
}
