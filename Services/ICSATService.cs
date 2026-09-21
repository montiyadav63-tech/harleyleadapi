using HarleyLeadApi.Models;

namespace HarleyLeadApi.Services
{
    public interface ICSATService
    {
        Task<LeadResponse> InsertCSATLeadAsync(CSATLeadRequest request);
    }
}