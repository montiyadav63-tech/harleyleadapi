namespace HarleyLeadApi.Services
{
    public interface INotConnectedService
    {
        Task<(int totalFound, int totalPushed, int totalFailed)> ProcessNotConnectedLeadsAsync(string type, string campaignName);
    }
}