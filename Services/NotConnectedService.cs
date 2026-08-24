using Dapper;
using HarleyLeadApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HarleyLeadApi.Services
{
    public class NotConnectedService : INotConnectedService
    {
        private readonly string _connectionString;
        private readonly HttpClient _httpClient;
        private readonly string _logsFolder;

        // ===================================================================
        // HD LMS Webhook Settings
        // ===================================================================

        private const string WebhookUATUrl = "https://hdlms-uat.cogentlab.com/api/v1/webhooks/dialer/system";
        private const string WebhookPRODUrl = "https://hdlms.cogentlab.com/api/v1/webhooks/dialer/system";
        private const string ApiUATKey = "hdlms_nJXFFAcaR9EFHP8NWZ4nIv7MJicyTLWQD9Hm9hBl";
        private const string ApiPRODKey = "hdlms_2U4Qi0y2fbf1zlGQ4iVbR4hieP5sHVFNY3LtpgvC";



        // ===================================================================
        // call_result = 33
        // 33 ko exclude kiya jayega.
        // ===================================================================

        private const string AnsweredCallResultCode = "33";


        // ===================================================================
        // Constructor
        // ===================================================================

        public NotConnectedService(IConfiguration configuration, HttpClient httpClient, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DBCS");
            _httpClient = httpClient;
            _logsFolder = Path.Combine(env.ContentRootPath, "Logs");
            if (!Directory.Exists(_logsFolder))
            {
                Directory.CreateDirectory(_logsFolder);
            }
        }


        // ===================================================================
        // Logging
        // ===================================================================

        private void WriteLog(string message)
        {
            string logPath = Path.Combine( _logsFolder,"NotConnectedAPI_log_" +DateTime.Now.ToString("yyyyMMdd") +".txt");

            File.AppendAllText(logPath, "\n[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
        }


        // ===================================================================
        // Process Not Connected Leads
        // ===================================================================

        public async Task<(int totalFound, int totalPushed, int totalFailed)>ProcessNotConnectedLeadsAsync(string type)
        {
            string webhookUrl;
            string apiKey;

            if (type == "UAT")
            {
                webhookUrl = WebhookUATUrl;
                apiKey = ApiUATKey;
            }
            else
            {
                webhookUrl = WebhookPRODUrl;
                apiKey = ApiPRODKey;
            }


            // ===============================================================
            // Environment / URL Log
            // ===============================================================

            WriteLog($"ENVIRONMENT SELECTED | " +$"Type={type} | " +$"TargetURL={webhookUrl}" + $"ApiKey={apiKey}");


            // ===============================================================
            // Calculate Previous 5-Minute Window
            // ===============================================================

            DateTime now = DateTime.Now;

            DateTime windowEnd = new DateTime( now.Year, now.Month, now.Day, now.Hour,now.Minute,0).AddSeconds(-1);

            DateTime windowStart = windowEnd.AddMinutes(-4).AddSeconds(-59);


            WriteLog( $"SCHEDULER TRIGGERED | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"Window (IST): " +   $"{windowStart:yyyy-MM-dd HH:mm:ss} to " +$"{windowEnd:yyyy-MM-dd HH:mm:ss}");


            // ===============================================================
            // Get Leads From Database
            // ===============================================================

            List<NotConnectedLead> leads;

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();

                    parameters.Add( "@start_time_ist",windowStart);

                    parameters.Add("@end_time_ist",windowEnd);

                    parameters.Add( "@exclude_call_result", AnsweredCallResultCode);


                    var result = await con.QueryAsync<NotConnectedLead>("usp_GetHarleyOB_ByISTRange",parameters,commandType: CommandType.StoredProcedure);


                    leads = result.ToList();
                }
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR FETCHING LEADS FROM DB | " +$"Type={type} | " +$"Error={ex}");

                throw;
            }


            // ===============================================================
            // Total Leads
            // ===============================================================

            WriteLog($"TOTAL LEADS FOUND | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"Count={leads.Count}");


            int pushed = 0;
            int failed = 0;


            // ===============================================================
            // Process Each Lead
            // ===============================================================

            foreach (var lead in leads)
            {
                try
                {
                    // -------------------------------------------------------
                    // Convert Unix call_time to DateTime
                    // -------------------------------------------------------

                    DateTimeOffset disposedAtOffset =DateTimeOffset.FromUnixTimeSeconds(lead.call_time);


                    string disposedAt = disposedAtOffset .LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");


                    // -------------------------------------------------------
                    // Create Webhook Payload
                    // -------------------------------------------------------

                    var payload = new DialerWebhookRequest
                        {
                            lead_uid =
                                lead.lead_uid,

                            sub_disposition_code =
                                "switched_off",

                            attempts_exhausted =
                                true,

                            attempt =
                                6,

                            remarks =
                                "Not Connected",

                            disposed_at =
                                disposedAt
                        };


                    // -------------------------------------------------------
                    // Serialize JSON
                    // -------------------------------------------------------

                    string jsonBody = JsonSerializer.Serialize(payload);


                    // -------------------------------------------------------
                    // Request Log
                    // -------------------------------------------------------

                    WriteLog($"REQUEST | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"ApiKey={apiKey} | "  + $"lead_uid={lead.lead_uid} | " + $"Payload={jsonBody}");


                    // -------------------------------------------------------
                    // Create HTTP Request
                    // -------------------------------------------------------

                    using (var request = new HttpRequestMessage( HttpMethod.Post,webhookUrl))
                    {
                        // API Key
                        request.Headers.Add( "X-API-Key",apiKey);


                        // JSON Body
                        request.Content = new StringContent(jsonBody,Encoding.UTF8,"application/json");


                        // ---------------------------------------------------
                        // Send Request
                        // ---------------------------------------------------

                        var response = await _httpClient.SendAsync(request);


                        // ---------------------------------------------------
                        // Read Response
                        // ---------------------------------------------------

                        string responseBody = await response.Content.ReadAsStringAsync();


                        // ---------------------------------------------------
                        // Response Log
                        // ---------------------------------------------------

                        WriteLog($"RESPONSE | " + $"Type={type} | " +$"TargetURL={webhookUrl} | " +$"lead_uid={lead.lead_uid} | " +$"Status={(int)response.StatusCode} " + $"({response.StatusCode}) | " +$"Response={responseBody}");


                        // ---------------------------------------------------
                        // Success / Failed
                        // ---------------------------------------------------

                        if (response.IsSuccessStatusCode)
                        {
                            pushed++;

                            WriteLog($"PUSH SUCCESS | " + $"Type={type} | " + $"lead_uid={lead.lead_uid}");
                        }
                        else
                        {
                            failed++;

                            WriteLog($"PUSH FAILED | " +$"Type={type} | " +$"TargetURL={webhookUrl} | " +$"lead_uid={lead.lead_uid} | " +$"Status={(int)response.StatusCode}");
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    failed++;

                    WriteLog( $"HTTP CONNECTION ERROR | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"lead_uid={lead.lead_uid} | " + $"Error={ex}");
                }
                catch (TaskCanceledException ex)
                {
                    failed++;

                    WriteLog( $"HTTP TIMEOUT ERROR | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " +$"lead_uid={lead.lead_uid} | " +$"Error={ex}");
                }
                catch (Exception ex)
                {
                    failed++;

                    WriteLog($"ERROR PUSHING LEAD | " +$"Type={type} | " + $"TargetURL={webhookUrl} | " + $"lead_uid={lead.lead_uid} | " +$"Error={ex}");
                }
            }


            // ===============================================================
            // Final Summary
            // ===============================================================

            WriteLog( $"SUMMARY | " +  $"Type={type} | " +$"TargetURL={webhookUrl} | " +$"Found={leads.Count} | " +$"Pushed={pushed} | " +$"Failed={failed}");


            return (
                leads.Count,
                pushed,
                failed
            );
        }
    }
}