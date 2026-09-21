using HarleyLeadApi.Models;
using HarleyLeadApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HarleyLeadApi.Controllers
{
    [ApiController]
    [Route("api/")]
    public class CSATController : ControllerBase
    {
        private readonly ICSATService _csatService;
        private readonly ILogger<CSATController> _logger;

        public CSATController(ICSATService csatService, ILogger<CSATController> logger)
        {
            _csatService = csatService;
            _logger = logger;
        }

        // POST api/csat/csatleadpush
        [HttpPost("csatleadpush")]
        public async Task<IActionResult> csatleadpush([FromBody] CSATLeadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(new
                {
                    status = 0,
                    message = "Validation failed",
                    errors = ModelState
                });
            }

            try
            {
                var response = await _csatService.InsertCSATLeadAsync(request);

                if (response.status == 0)
                {
                    return Conflict(response);
                }

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting CSAT lead");
                return StatusCode(500, new { status = 0, message = "Internal server error" });
            }
        }
    }
}