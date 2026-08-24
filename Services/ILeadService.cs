using HarleyLeadApi.Models;

namespace HarleyLeadApi.Services
{
    public interface ILeadService
    {
        Task<LeadResponse> InsertLeadAsync(LeadInsertRequest request);
        Task<LeadResponse> UpdateLeadAsync(string leadUid, LeadUpdateRequest request);
        Task<LeadResponse> UpdateCallBackAsync(string leadUid, DateTime callBackDateTime, string campaignName = null);
        Task<LeadResponse> CancelLeadAsync(string leadUid, string campaignName = null);
    }
}
