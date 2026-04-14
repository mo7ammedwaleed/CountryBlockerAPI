using System.Net;
using CountryBlockerAPI.DTOs.Request_DTOs;
using CountryBlockerAPI.DTOs.Response_DTOs;
using CountryBlockerAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CountryBlockerAPI.Controllers
{
    [ApiController]
    [Route("api/countries")]
    [Produces("application/json")]
    public class CountriesController : ControllerBase
    {
        private readonly ICountryService _countryService;
        private readonly ILogger<CountriesController> _logger;

        public CountriesController(ICountryService countryService, ILogger<CountriesController> logger)
        {
            _countryService = countryService;
            _logger = logger;

        }

        [HttpPost("block")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> BlockCountry([FromBody] BlockCountryRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);


            var (success, error) = await _countryService.BlockCountryAsync(request.CountryCode);

            if (!success)
            {
                if (error.Contains("already blocked"))
                    return Conflict(new { message = error });
                return BadRequest(new { message = error });
            }

            return Ok(new { message = $"Country '{request.CountryCode.ToUpper()}' has been blocked." });
        }

        [HttpDelete("block/{countryCode}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UnblockCountry(string countryCode)
        {
            var (success, error) = _countryService.UnblockCountry(countryCode);

            if (!success)
                return NotFound(new { message = error });

            return Ok(new { message = $"Country '{countryCode.ToUpper()}' has been unblocked." });
        }

        [HttpGet("blocked")]
        [ProducesResponseType(typeof(PagedResponseDto<BlockedCountryResponseDto>), StatusCodes.Status200OK)]
        public IActionResult GetBlockedCountries(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            if (page < 1) return BadRequest(new { message = "Page must be >= 1." });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "PageSize must be between 1 and 100." });

            var result = _countryService.GetAllBlocked(page, pageSize, search);
            return Ok(result);
        }

        [HttpPost("temporal-block")]
        [ProducesResponseType(typeof(TemporalBlockResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> TemporalBlock([FromBody] TemporalBlockRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, error, statusCode) =
                await _countryService.AddTemporalBlockAsync(request.CountryCode, request.DurationMinutes);

            if (!success)
            {
                return statusCode switch
                {
                    409 => Conflict(new { message = error }),
                    _ => BadRequest(new { message = error })
                };
            }

            var block = _countryService.GetTemporalBlockInfo(request.CountryCode);
            return Ok(block);
        }


        [HttpGet("temporal-blocks")]
        [ProducesResponseType(typeof(IEnumerable<TemporalBlockResponseDto>), StatusCodes.Status200OK)]
        public IActionResult GetTemporalBlocks()
        {
            return Ok(_countryService.GetAllActiveTemporalBlocks());
        }
    }
}
