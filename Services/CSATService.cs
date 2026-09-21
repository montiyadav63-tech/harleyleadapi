using Dapper;
using HarleyLeadApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HarleyLeadApi.Services
{
    public class CSATService : ICSATService
    {
        private readonly string _connectionString;
        private readonly string _logsFolder;

        public CSATService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DBCS");

            _logsFolder = Path.Combine(env.ContentRootPath, "Logs");
            if (!Directory.Exists(_logsFolder))
            {
                Directory.CreateDirectory(_logsFolder);
            }
        }

        private void WriteLog(string message)
        {
            string logPath = Path.Combine(_logsFolder, "CSATLeadPush_log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");
            try
            {
                File.AppendAllText(logPath, "\n[" + DateTime.Now + "] " + message);
            }
            catch
            {
                // Logging fail hone se poori API crash nahi honi chahiye
            }
        }

        public async Task<LeadResponse> InsertCSATLeadAsync(CSATLeadRequest request)
        {
            WriteLog("DATA RECEIVED | lead_uid=" + request.lead_uid + " | contactNumber=" + request.contactNumber + " | campaignSurvey=" + request.campaignSurvey);

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@lead_uid", request.lead_uid);
                    parameters.Add("@contactNumber", request.contactNumber);
                    parameters.Add("@campaignSurvey", request.campaignSurvey);
                    parameters.Add("@customerName", (object)request.customerName ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field1", (object)request.free_field1 ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field2", (object)request.free_field2 ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field3", (object)request.free_field3 ?? DBNull.Value, DbType.String);

                    var result = await con.QuerySingleAsync<(int success, int? record_id, string lead_uid, string message)>(
                        "sp_InsCSATSurveyLead",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var response = new LeadResponse
                    {
                        status = result.success,
                        message = result.message,
                        lead_uid = result.lead_uid,
                        record_id = result.record_id
                    };

                    WriteLog("RESPONSE | " + System.Text.Json.JsonSerializer.Serialize(response));
                    return response;
                }
            }
            catch (Exception ex)
            {
                WriteLog("ERROR | " + ex.ToString());
                throw;
            }
        }
    }
}