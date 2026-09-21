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
        // Ab "type" (UAT/PROD) ke hisaab se ALAG file me likha jayega, taaki
        // dono environments ek doosre ki file lock na kare (jo pehle error
        // ka reason tha), aur logs bhi clearly separate ho jaayein.
        // ===================================================================

        private void WriteLog(string message, string type)
        {
            string logPath = Path.Combine(_logsFolder, "NotConnectedAPI_" + type + "log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");

            File.AppendAllText(logPath, "\n[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
        }


        // ===================================================================
        // nc_attempts - Duplicate Check
        // lead_uid + attempt + callTime ka combination check karega.
        // Agar already maujood hai -> true return karega (skip karna hai).
        // ===================================================================

        //private async Task<bool> CheckIfAttemptExistsAsync(NotConnectedLead lead, string callTime, string type)
        //{
        //    try
        //    {
        //        using (var con = new SqlConnection(_connectionString))
        //        {
        //            var parameters = new DynamicParameters();

        //            parameters.Add("@record_id", lead.record_id.ToString());
        //            parameters.Add("@lead_uid", lead.lead_uid);
        //            parameters.Add("@mobile", lead.mobile);
        //            parameters.Add("@attempt", lead.attempt.ToString());
        //            parameters.Add("@callTime", callTime);


        //            const string query = @"SELECT COUNT(1)
        //               FROM nc_attempts
						  //WHERE record_id = @record_id
						  //AND lead_uid  = @lead_uid
						  //AND mobile    = @mobile
						  //AND attempt   = @attempt
						  //AND callTime  = @callTime";

        //            int count = await con.ExecuteScalarAsync<int>(query, parameters);

        //            return count > 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog($"ERROR CHECKING nc_attempts | " + $"Type={type} | " + $"lead_uid={lead.lead_uid} | " + $"Error={ex}", type);

        //        // Fail-safe: check fail ho jaye to false return, taaki lead galti se skip na ho.
        //        return false;
        //    }
        //}


        // ===================================================================
        // nc_attempts - Insert
        // ===================================================================


        private async Task InsertNcAttemptAsync(NotConnectedLead lead, long callTime, string type)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();

                    parameters.Add("@record_id", lead.record_id.ToString());
                    parameters.Add("@lead_uid", lead.lead_uid);
                    parameters.Add("@callTime", callTime);

                    const string query = @"
                INSERT INTO nc_attempts (record_id, lead_uid,call_time  )
                VALUES (@record_id, @lead_uid,@callTime)";

                    await con.ExecuteAsync(query, parameters);

                    WriteLog($"INSERTED INTO nc_attempts | " + $"Type={type} | " + $"lead_uid={lead.lead_uid} | " + $"attempt={lead.attempt}", type);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR INSERTING INTO nc_attempts | " + $"Type={type} | " + $"lead_uid={lead.lead_uid} | " + $"Error={ex}", type);
            }
        }


        // ===================================================================
        // Process Not Connected Leads
        // ===================================================================

        public async Task<(int totalFound, int totalPushed, int totalFailed)> ProcessNotConnectedLeadsAsync(string type, string campaignName)
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

            WriteLog($"ENVIRONMENT SELECTED | " + $"Type={type} | " + $"TargetURL={webhookUrl}" + $"ApiKey={apiKey}", type);


            // ===============================================================
            // Calculate Previous 5-Minute Window
            // ===============================================================

            DateTime now = DateTime.Now;

            DateTime windowEnd = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddSeconds(-1);

            DateTime windowStart = windowEnd.AddMinutes(-29).AddSeconds(-59);


            WriteLog($"SCHEDULER TRIGGERED | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"Window (IST): " + $"{windowStart:yyyy-MM-dd HH:mm:ss} to " + $"{windowEnd:yyyy-MM-dd HH:mm:ss}", type);


            // ===============================================================
            // Get Leads From Database
            // ===============================================================

            List<NotConnectedLead> leads;

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();

                    parameters.Add("@start_time_ist", windowStart);

                    parameters.Add("@end_time_ist", windowEnd);

                    parameters.Add("@CampaignName", campaignName);


                    var result = await con.QueryAsync<NotConnectedLead>("usp_GetNCdata", parameters, commandType: CommandType.StoredProcedure);


                    leads = result.ToList();
                }

                // -----------------------------------------------------------
                //  leads DB se mili hain, unko JSON format me
                // -----------------------------------------------------------

                string leadsJson = JsonSerializer.Serialize(leads);

                WriteLog($"LEADS FETCHED (JSON) | " + $"Type={type} | " + $"Data={leadsJson}", type);
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR FETCHING LEADS FROM DB | " + $"Type={type} | " + $"Error={ex}", type);

                throw;
            }


            // ===============================================================
            // Total Leads
            // ===============================================================

            WriteLog($"TOTAL LEADS FOUND | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"Count={leads.Count}", type);


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

                    DateTimeOffset disposedAtOffset = DateTimeOffset.FromUnixTimeSeconds(lead.call_time);
                    string disposedAt = disposedAtOffset.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");





                    bool attempts_exhausted = false;

                    // -------------------------------------------------------
                    // Create Webhook Payload
                    // -------------------------------------------------------

                    var payload = new DialerWebhookRequest
                    {
                        lead_uid = lead.lead_uid,
                        sub_disposition_code = "switched_off",   // Hard Code
                        attempts_exhausted = attempts_exhausted,   // Manager ke naye logic se (upar wala if-else)
                        attempt = 0,                              // DB se aayi asli attempt-count
                        remarks = "Not Connected",                 // Hard Code
                        disposed_at = disposedAt                  // Call time se
                    };


                    // -------------------------------------------------------
                    // Serialize JSON
                    // -------------------------------------------------------

                    string jsonBody = JsonSerializer.Serialize(payload);


                    // -------------------------------------------------------
                    // Request Log
                    // -------------------------------------------------------

                    WriteLog($"REQUEST | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"ApiKey={apiKey} | " + $"lead_uid={lead.lead_uid} | " + $"Payload={jsonBody}", type);


                    // -------------------------------------------------------
                    // Create HTTP Request
                    // -------------------------------------------------------

                    using (var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl))
                    {
                        // API Key
                        request.Headers.Add("X-API-Key", apiKey);


                        // JSON Body
                        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");


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

                        WriteLog($"RESPONSE | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"lead_uid={lead.lead_uid} | " + $"Status={(int)response.StatusCode} " + $"({response.StatusCode}) | " + $"Response={responseBody}", type);


                        // ---------------------------------------------------
                        // Success / Failed
                        // ---------------------------------------------------

                        if (response.IsSuccessStatusCode)
                        {
                            pushed++;

                            WriteLog($"PUSH SUCCESS | " + $"Type={type} | " + $"lead_uid={lead.lead_uid}", type);

                            // -------------------------------------------
                            // nc_attempts me insert - sirf successful
                            // push hone par.
                            // -------------------------------------------

                            await InsertNcAttemptAsync(lead, lead.call_time, type);
                        }
                        else
                        {
                            failed++;

                            WriteLog($"PUSH FAILED | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"lead_uid={lead.lead_uid} | " + $"Status={(int)response.StatusCode}", type);
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    failed++;

                    WriteLog($"HTTP CONNECTION ERROR | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"lead_uid={lead.lead_uid} | " + $"Error={ex}", type);
                }
                catch (TaskCanceledException ex)
                {
                    failed++;

                    WriteLog($"HTTP TIMEOUT ERROR | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"lead_uid={lead.lead_uid} | " + $"Error={ex}", type);
                }
                catch (Exception ex)
                {
                    failed++;

                    WriteLog($"ERROR PUSHING LEAD | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"lead_uid={lead.lead_uid} | " + $"Error={ex}", type);
                }
            }


            // ===============================================================
            // Final Summary
            // ===============================================================

            WriteLog($"SUMMARY | " + $"Type={type} | " + $"TargetURL={webhookUrl} | " + $"Found={leads.Count} | " + $"Pushed={pushed} | " + $"Failed={failed}", type);


            return (
                leads.Count,
                pushed,
                failed
            );
        }
    }
}