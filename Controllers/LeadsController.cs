using HarleyLeadApi.Models;
using HarleyLeadApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HarleyLeadApi.Controllers
{
    [ApiController]
    [Route("/api/leads")]
    public class LeadsController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly ILogger<LeadsController> _logger;

        public LeadsController(ILeadService leadService, ILogger<LeadsController> logger)
        {
            _leadService = leadService;
            _logger = logger;
        }

        // POST api/leads
        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] LeadInsertRequest request)
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
                _logger.LogInformation("Lead insert request: {@request}", request);
                var response = await _leadService.InsertLeadAsync(request);
                _logger.LogInformation("Lead insert response: {@response}", response);

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
               // Console.WriteLine(ex);

                _logger.LogError(ex, "Error inserting lead");
                return StatusCode(500, new { status = 0, message = "Internal server error" });
                
            }
        }

        //// PUT api/leads/{lead_uid}
        //[HttpPut("{lead_uid}")]
        //public async Task<IActionResult> UpdateLead(string lead_uid, [FromBody] LeadUpdateRequest request)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Lead update request for {lead_uid}: {@request}", lead_uid, request);
        //        var response = await _leadService.UpdateLeadAsync(lead_uid, request);
        //        _logger.LogInformation("Lead update response: {@response}", response);

        //        if (response.status == 0)
        //            return NotFound(response);

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating lead");
        //        return StatusCode(500, new { status = 0, message = "Internal server error" });
        //    }
        //}


        // PUT api/leads/callback


        [HttpPost("callback")]
        public async Task<IActionResult> UpdateCallBack([FromBody] CallBackRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.lead_uid))
            {
                return BadRequest(new { status = 0, message = "lead_uid is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CampaignName))
            {
                return BadRequest(new { status = 0, message = "CampaignName is required" });
            }

            if (!DateTime.TryParseExact(request.callbackTime, "yyyy-MM-dd HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsedDateTime))
            {
                return BadRequest(new { status = 0, message = "callbackTime must be in format yyyy-MM-dd HH:mm:ss" });
            }

            try
            {
                _logger.LogInformation("CallBack update request for {lead_uid}, CampaignName: {CampaignName}, callbackTime: {callbackTime}",
                    request.lead_uid, request.CampaignName, parsedDateTime);

                var response = await _leadService.UpdateCallBackAsync(request.lead_uid, parsedDateTime, request.CampaignName);

                _logger.LogInformation("CallBack update response: {@response}", response);

                if (response.status == 0)
                    return NotFound(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating callback time");
                return StatusCode(500, new { status = 0, message = "Internal server error" });
            }
        }
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelLead([FromBody] CancelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.lead_uid))
            {
                return BadRequest(new { status = 0, message = "lead_uid is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CampaignName))
            {
                return BadRequest(new { status = 0, message = "CampaignName is required" });
            }

            try
            {
                _logger.LogInformation("Cancel request for lead_uid: {lead_uid}, CampaignName: {CampaignName}",
                    request.lead_uid, request.CampaignName);

                var response = await _leadService.CancelLeadAsync(
                    request.lead_uid,
                    request.CampaignName);

                _logger.LogInformation("Cancel response: {@response}", response);

                if (response.status == 0)
                    return NotFound(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling lead");
                return StatusCode(500, new { status = 0, message = "Internal server error" });
            }
        }
    }
}
