using HarleyLeadApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HarleyLeadApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class NotConnectedController : ControllerBase
    {
        private readonly INotConnectedService _notConnectedService;
        private readonly ILogger<NotConnectedController> _logger;

        public NotConnectedController(INotConnectedService notConnectedService, ILogger<NotConnectedController> logger)
        {
            _notConnectedService = notConnectedService;
            _logger = logger;
        }

        // POST api/notconnected/run
        // Windows Task Scheduler ise har 5 minute pe hit karega
        [HttpGet("notconnected")]
        public async Task<IActionResult> NotConnectedAPI([FromQuery] string type, [FromQuery] string campaignName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(type))
                {
                    return BadRequest(new
                    {
                        status = 0,
                        message = "type is required. Please use UAT or PROD."
                    });
                }

                if (!type.Equals("UAT", StringComparison.OrdinalIgnoreCase) &&
                    !type.Equals("PROD", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        status = 0,
                        message = "Invalid type. Please use UAT or PROD."
                    });
                }

                var (totalFound, totalPushed, totalFailed) =
                    await _notConnectedService.ProcessNotConnectedLeadsAsync(type, campaignName);

                return Ok(new
                {
                    status = 1,
                    message = "Not-connected leads processed",
                    type = type.ToUpper(),
                    notAnswered = totalFound,
                    totalPushed = totalPushed,
                    totalFailed = totalFailed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotConnectedAPI");

                return StatusCode(500, new
                {
                    status = 0,
                    message = "Internal server error"
                });
            }
        }
    }
    }