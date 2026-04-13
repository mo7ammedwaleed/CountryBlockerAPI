using CountryBlockerAPI.Models;
using CountryBlockerAPI.Repository;
using CountryBlockerAPI.DTOs.Response_DTOs;

namespace CountryBlockerAPI.Services
{
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _repo;
        private readonly IGeoLocationService _geoService;
        private readonly ILogger<CountryService> _logger;

        
        private static readonly HashSet<string> ValidCountryCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AF","AX","AL","DZ","AS","AD","AO","AI","AQ","AG","AR","AM","AW","AU","AT",
        "AZ","BS","BH","BD","BB","BY","BE","BZ","BJ","BM","BT","BO","BQ","BA","BW",
        "BV","BR","IO","BN","BG","BF","BI","CV","KH","CM","CA","KY","CF","TD","CL",
        "CN","CX","CC","CO","KM","CG","CD","CK","CR","CI","HR","CU","CW","CY","CZ",
        "DK","DJ","DM","DO","EC","EG","SV","GQ","ER","EE","SZ","ET","FK","FO","FJ",
        "FI","FR","GF","PF","TF","GA","GM","GE","DE","GH","GI","GR","GL","GD","GP",
        "GU","GT","GG","GN","GW","GY","HT","HM","VA","HN","HK","HU","IS","IN","ID",
        "IR","IQ","IE","IM","IL","IT","JM","JP","JE","JO","KZ","KE","KI","KP","KR",
        "KW","KG","LA","LV","LB","LS","LR","LY","LI","LT","LU","MO","MG","MW","MY",
        "MV","ML","MT","MH","MQ","MR","MU","YT","MX","FM","MD","MC","MN","ME","MS",
        "MA","MZ","MM","NA","NR","NP","NL","NC","NZ","NI","NE","NG","NU","NF","MK",
        "MP","NO","OM","PK","PW","PS","PA","PG","PY","PE","PH","PN","PL","PT","PR",
        "QA","RE","RO","RU","RW","BL","SH","KN","LC","MF","PM","VC","WS","SM","ST",
        "SA","SN","RS","SC","SL","SG","SX","SK","SI","SB","SO","ZA","GS","SS","ES",
        "LK","SD","SR","SJ","SE","CH","SY","TW","TJ","TZ","TH","TL","TG","TK","TO",
        "TT","TN","TR","TM","TC","TV","UG","UA","AE","GB","US","UM","UY","UZ","VU",
        "VE","VN","VG","VI","WF","EH","YE","ZM","ZW"
    };

        public CountryService(
            ICountryRepository repo,
            IGeoLocationService geoService,
            ILogger<CountryService> logger)
        {
            _repo = repo;
            _geoService = geoService;
            _logger = logger;
        }


        public async Task<(bool Success, string Error)> BlockCountryAsync(string countryCode)
        {
            var code = countryCode.ToUpperInvariant();

            if (!ValidCountryCodes.Contains(code))
                return (false, $"'{code}' is not a valid ISO 3166-1 alpha-2 country code.");

            if (_repo.ExistsBlockedCountry(code))
                return (false, $"Country '{code}' is already blocked.");

            
            string countryName = code; 
            try
            {
                
                var result = await _geoService.LookupAsync("8.8.8.8"); 
                                                                      
                countryName = GetCountryName(code);
            }
            catch (GeoLocationException ex)
            {
                _logger.LogWarning(ex, "Could not resolve country name for {Code}, using code as name.", code);
                countryName = code;
            }

            var country = new BlockedCountry
            {
                CountryCode = code,
                CountryName = countryName,
                BlockedAt = DateTime.UtcNow
            };

            var added = _repo.AddBlockedCountry(country);
            return added
                ? (true, string.Empty)
                : (false, $"Country '{code}' is already blocked.");
        }

        public (bool Success, string Error) UnblockCountry(string countryCode)
        {
            var code = countryCode.ToUpperInvariant();

            if (!_repo.ExistsBlockedCountry(code))
                return (false, $"Country '{code}' is not in the blocked list.");

            _repo.RemoveBlockedCountry(code);
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


        public async Task<(bool Success, string Error, int StatusCode)> AddTemporalBlockAsync(
            string countryCode, int durationMinutes)
        {
            var code = countryCode.ToUpperInvariant();

            if (!ValidCountryCodes.Contains(code))
                return (false, $"'{code}' is not a valid ISO 3166-1 alpha-2 country code.", 400);

            if (_repo.ExistsTemporalBlock(code))
                return (false, $"Country '{code}' is already temporarily blocked.", 409);

            var block = new TemporalBlock
            {
                CountryCode = code,
                CountryName = GetCountryName(code),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
                DurationMinutes = durationMinutes
            };

            var added = _repo.AddTemporalBlock(block);
            if (!added)
                return (false, $"Country '{code}' is already temporarily blocked.", 409);

            _logger.LogInformation(
                "Temporal block added for {Code} – expires at {Expiry}", code, block.ExpiresAt);

            return (true, string.Empty, 200);
        }


        public static bool IsValidCountryCode(string code) =>
            ValidCountryCodes.Contains(code?.ToUpperInvariant() ?? "");

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
            _ => code
        };
    }
}
