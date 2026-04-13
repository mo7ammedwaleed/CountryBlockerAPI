using System.Net;
using CountryBlockerAPI.Models;
using CountryBlockerAPI.Repository;
using CountryBlockerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using CountryBlockerAPI.DTOs.Response_DTOs;

namespace CountryBlockerAPI.Controllers
{
    [ApiController]
    [Route("api/ip")]
    [Produces("application/json")]
    public class IpController : ControllerBase
    {
        private readonly IGeoLocationService _geoService;
        private readonly ICountryRepository _repo;
        private readonly ILogger<IpController> _logger;

        public IpController(
            IGeoLocationService geoService,
            ICountryRepository repo,
            ILogger<IpController> logger)
        {
            _geoService = geoService;
            _repo = repo;
            _logger = logger;
        }


        [HttpGet("lookup")]
        [ProducesResponseType(typeof(IpLookupResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Lookup([FromQuery] string? ipAddress = null)
        {

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = GetCallerIp();
                if (string.IsNullOrWhiteSpace(ipAddress))
                    return BadRequest(new { message = "Could not determine caller IP address." });
            }

            if (!IPAddress.TryParse(ipAddress, out _))
                return BadRequest(new { message = $"'{ipAddress}' is not a valid IP address." });

            GeoLocationResult result;
            try
            {
                result = await _geoService.LookupAsync(ipAddress);
            }
            catch (GeoLocationException ex)
            {
                _logger.LogWarning(ex, "GeoLocation lookup failed for {Ip}", ipAddress);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "GeoLocation service error.", detail = ex.Message });
            }

            return Ok(new IpLookupResponseDto
            {
                Ip = result.Ip,
                CountryCode = result.CountryCode,
                CountryName = result.CountryName,
                Isp = result.Isp,
                City = result.City,
                Region = result.Region
            });
        }

        [HttpGet("check-block")]
        [ProducesResponseType(typeof(CheckBlockResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> CheckBlock()
        {
            var ip = GetCallerIp() ?? "unknown";
            var userAgent = Request.Headers.UserAgent.ToString();

            GeoLocationResult geoResult;
            try
            {
                geoResult = await _geoService.LookupAsync(ip);
            }
            catch (GeoLocationException ex)
            {
                _logger.LogWarning(ex, "Could not resolve country for IP {Ip}", ip);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "GeoLocation service error.", detail = ex.Message });
            }

            var isBlocked = _repo.IsCountryBlocked(geoResult.CountryCode);


            _repo.AddLog(new BlockAttemptLog
            {
                IpAddress = ip,
                Timestamp = DateTime.UtcNow,
                CountryCode = geoResult.CountryCode,
                CountryName = geoResult.CountryName,
                IsBlocked = isBlocked,
                UserAgent = userAgent
            });

            _logger.LogInformation(
                "CheckBlock: IP={Ip}, Country={Country}, Blocked={Blocked}",
                ip, geoResult.CountryCode, isBlocked);

            return Ok(new CheckBlockResponseDto
            {
                IpAddress = ip,
                CountryCode = geoResult.CountryCode,
                CountryName = geoResult.CountryName,
                IsBlocked = isBlocked,
                Message = isBlocked
                    ? $"Access denied – country '{geoResult.CountryCode}' is blocked."
                    : $"Access allowed – country '{geoResult.CountryCode}' is not blocked."
            });
        }

        private string? GetCallerIp()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
