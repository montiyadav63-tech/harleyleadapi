using HarleyLeadApi.Models;
using HarleyLeadApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HarleyLeadApi.Controllers
{
    [ApiController]
    [Route("api/calldisposition")]
    public class DispositionController : ControllerBase
    {
        private readonly IDispositionService _dispositionService;
        private readonly ILogger<DispositionController> _logger;

        public DispositionController(IDispositionService dispositionService, ILogger<DispositionController> logger)
        {
            _dispositionService = dispositionService;
            _logger = logger;
        }

        // POST api/disposition
        [HttpPost]
        public async Task<IActionResult> CallDisposition([FromBody] DispositionRequest request)
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
                _logger.LogInformation("Disposition push request: {@request}", request);
                string externalResponse = await _dispositionService.CallDispositionAsync(request);
                _logger.LogInformation("Disposition push succeeded: {externalResponse}", externalResponse);

                return Ok(new
                {
                    status = 1,
                    message = "Disposition pushed successfully",
                    externalResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing disposition");
                return StatusCode(500, new { status = 0, message = "Internal server error" });
            }
        }
    }
}