using HarleyLeadApi.Models;

namespace HarleyLeadApi.Services
{
    public interface IDispositionService
    {
        Task<string> CallDispositionAsync(DispositionRequest request);
    }
}