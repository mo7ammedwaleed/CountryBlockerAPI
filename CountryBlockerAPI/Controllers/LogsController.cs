using CountryBlockerAPI.Repository;
using Microsoft.AspNetCore.Mvc;
using CountryBlockerAPI.DTOs.Response_DTOs;

namespace CountryBlockerAPI.Controllers
{
    [ApiController]
    [Route("api/logs")]
    [Produces("application/json")]
    public class LogsController : ControllerBase
    {
        private readonly ICountryRepository _repo;

        public LogsController(ICountryRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("blocked-attempts")]
        [ProducesResponseType(typeof(PagedResponseDto<BlockAttemptLogResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetBlockedAttempts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? blockedOnly = null,
            [FromQuery] string? countryCode = null)
        {
            if (page < 1) return BadRequest(new { message = "Page must be >= 1." });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "PageSize must be between 1 and 100." });

            var logs = _repo.GetLogs().AsEnumerable();

            // Optional filters
            if (blockedOnly.HasValue)
                logs = logs.Where(l => l.IsBlocked == blockedOnly.Value);

            if (!string.IsNullOrWhiteSpace(countryCode))
                logs = logs.Where(l =>
                    l.CountryCode.Equals(countryCode.Trim(), StringComparison.OrdinalIgnoreCase));

            // Most recent first
            var ordered = logs.OrderByDescending(l => l.Timestamp).ToList();

            var paged = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new BlockAttemptLogResponseDto
                {
                    Id = l.Id,
                    IpAddress = l.IpAddress,
                    Timestamp = l.Timestamp,
                    CountryCode = l.CountryCode,
                    CountryName = l.CountryName,
                    IsBlocked = l.IsBlocked,
                    UserAgent = l.UserAgent
                });

            return Ok(new PagedResponseDto<BlockAttemptLogResponseDto>
            {
                Data = paged,
                Page = page,
                PageSize = pageSize,
                TotalCount = ordered.Count
            });
        }
    }
}
