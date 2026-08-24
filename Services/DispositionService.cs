using HarleyLeadApi.Models;
using System.Text;
using System.Text.Json;

namespace HarleyLeadApi.Services
{
    public class DispositionService : IDispositionService
    {
        private readonly HttpClient _httpClient;
        private const string DispositionUrl = "http://192.168.160.90/SignalR/api/disposition";

        private readonly string _logPath;

        public DispositionService(HttpClient httpClient, IWebHostEnvironment env)
        {
            _httpClient = httpClient;

            string logsFolder = Path.Combine(env.ContentRootPath, "Logs");
            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
            }
            _logPath = Path.Combine(logsFolder, "disposition_log.txt");
        }

        public async Task<string> CallDispositionAsync(DispositionRequest request)
        {
            File.AppendAllText(_logPath,
                "\n\n[" + DateTime.Now + "] DATA RECEIVED | AgentId=" + request.AgentId +
                " | DispCode=" + request.DispCode +
                " | SubDispCode=" + request.SubDispCode +
                " | CRMId=" + request.CRMId +
                " | PhoneNumber=" + request.PhoneNumber);

            try
            {
                var payload = new
                {
                    AgentId = request.AgentId,
                    DispCode = request.DispCode,
                    SubDispCode = request.SubDispCode,
                    CRMId = request.CRMId,
                    PhoneNumber = request.PhoneNumber
                };

                string jsonBody = JsonSerializer.Serialize(payload);

                File.AppendAllText(_logPath,
                    "\n[" + DateTime.Now + "] REQUEST:\n" + jsonBody);

                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    var response = await _httpClient.PostAsync(DispositionUrl, content);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    File.AppendAllText(_logPath,"\n[" + DateTime.Now + "] RESPONSE:\n" + responseBody);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException( $"Disposition API call failed with status {(int)response.StatusCode}: {responseBody}");
                    }

                    return responseBody;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logPath, "\n[" + DateTime.Now + "] ERROR:\n" + ex.ToString());
                throw;
            }
        }
    }
}