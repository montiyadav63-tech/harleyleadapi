using Dapper;
using HarleyLeadApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace HarleyLeadApi.Services
{
    public class LeadService : ILeadService
    {
        private readonly string _connectionString;
        private readonly string _logsFolder;

        public LeadService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DBCS");

            _logsFolder = Path.Combine(env.ContentRootPath, "Logs");
            if (!Directory.Exists(_logsFolder))
            {
                Directory.CreateDirectory(_logsFolder);
            }
        }

        // Helper method — har API apna naam bhejegi (jaise "CreateLead"),
        // usi naam ki alag log file me likha jayega
        private void WriteLog(string apiName, string message)
        {
            string logPath = Path.Combine(_logsFolder, apiName + "_log.txt");
            File.AppendAllText(logPath, "\n[" + DateTime.Now + "] " + message);
        }

        public async Task<LeadResponse> InsertLeadAsync(LeadInsertRequest request)
        {
            const string apiName = "CreateLead";
            WriteLog(apiName, "DATA RECEIVED | " + JsonSerializer.Serialize(request));

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@lead_uid", request.lead_uid);
                    parameters.Add("@first_name", request.first_name);
                    parameters.Add("@last_name", request.last_name);
                    parameters.Add("@name", (object)request.name ?? DBNull.Value, DbType.String);
                    parameters.Add("@phone", request.phone);
                    parameters.Add("@email", (object)request.email ?? DBNull.Value, DbType.String);
                    parameters.Add("@pincode", request.pincode);
                    parameters.Add("@city", request.city);
                    parameters.Add("@state", request.state);
                    parameters.Add("@pre_selected_model", request.pre_selected_model);
                    parameters.Add("@lead_source", request.lead_source);
                    parameters.Add("@utm_source", request.utm_source);
                    parameters.Add("@source_campaign", request.source_campaign);
                    parameters.Add("@CampaignName", (object)request.CampaignName ?? DBNull.Value, DbType.String);
                    parameters.Add("@purchase_timelines", (object)request.purchase_timelines ?? DBNull.Value, DbType.String);
                    parameters.Add("@exchange_required", (object)request.exchange_required ?? DBNull.Value, DbType.String);
                    parameters.Add("@finance_required", (object)request.finance_required ?? DBNull.Value, DbType.String);
                    parameters.Add("@pre_selected_dealer_code", (object)request.pre_selected_dealer_code ?? DBNull.Value, DbType.String);
                    parameters.Add("@pre_selected_dealer_name", (object)request.pre_selected_dealer_name ?? DBNull.Value, DbType.String);
                    parameters.Add("@old_dealer_code", (object)request.old_dealer_code ?? DBNull.Value, DbType.String);
                    parameters.Add("@old_dealer_name", (object)request.old_dealer_name ?? DBNull.Value, DbType.String);
                    parameters.Add("@old_enquiry_date", (object)request.old_enquiry_date ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field2", (object)request.free_field2 ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field3", (object)request.free_field3 ?? DBNull.Value, DbType.String);

                    var result = await con.QuerySingleAsync<(int success, int? record_id, string lead_uid, string message)>(
                        "sp_InsHarleyLead",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var response = new LeadResponse
                    {
                        status = result.success,
                        message = result.message,
                        lead_uid = result.lead_uid,
                        record_id = result.record_id
                    };

                    WriteLog(apiName, "RESPONSE | " + JsonSerializer.Serialize(response));
                    return response;
                }
            }
            catch (Exception ex)
            {
                WriteLog(apiName, "ERROR | " + ex.ToString());
                throw;
            }
        }

        public async Task<LeadResponse> UpdateLeadAsync(string leadUid, LeadUpdateRequest request)
        {
            const string apiName = "UpdateLead";
            WriteLog(apiName, "DATA RECEIVED | lead_uid=" + leadUid + " | " + JsonSerializer.Serialize(request));

            if (string.IsNullOrWhiteSpace(leadUid))
            {
                var errResponse = new LeadResponse { status = 0, message = "lead_uid is required" };
                WriteLog(apiName, "RESPONSE | " + JsonSerializer.Serialize(errResponse));
                return errResponse;
            }

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@lead_uid", leadUid);
                    parameters.Add("@first_name", (object)request.first_name ?? DBNull.Value, DbType.String);
                    parameters.Add("@last_name", (object)request.last_name ?? DBNull.Value, DbType.String);
                    parameters.Add("@name", (object)request.name ?? DBNull.Value, DbType.String);
                    parameters.Add("@phone", (object)request.phone ?? DBNull.Value, DbType.String);
                    parameters.Add("@email", (object)request.email ?? DBNull.Value, DbType.String);
                    parameters.Add("@pincode", (object)request.pincode ?? DBNull.Value, DbType.String);
                    parameters.Add("@city", (object)request.city ?? DBNull.Value, DbType.String);
                    parameters.Add("@state", (object)request.state ?? DBNull.Value, DbType.String);
                    parameters.Add("@pre_selected_model", (object)request.pre_selected_model ?? DBNull.Value, DbType.String);
                    parameters.Add("@lead_source", (object)request.lead_source ?? DBNull.Value, DbType.String);
                    parameters.Add("@utm_source", (object)request.utm_source ?? DBNull.Value, DbType.String);
                    parameters.Add("@source_campaign", (object)request.source_campaign ?? DBNull.Value, DbType.String);
                    parameters.Add("@purchase_timelines", (object)request.purchase_timelines ?? DBNull.Value, DbType.String);
                    parameters.Add("@exchange_required", (object)request.exchange_required ?? DBNull.Value, DbType.String);
                    parameters.Add("@finance_required", (object)request.finance_required ?? DBNull.Value, DbType.String);
                    parameters.Add("@pre_selected_dealer_code", (object)request.pre_selected_dealer_code ?? DBNull.Value, DbType.String);
                    parameters.Add("@pre_selected_dealer_name", (object)request.pre_selected_dealer_name ?? DBNull.Value, DbType.String);
                    parameters.Add("@old_dealer_code", (object)request.old_dealer_code ?? DBNull.Value, DbType.String);
                    parameters.Add("@old_dealer_name", (object)request.old_dealer_name ?? DBNull.Value, DbType.String);
                    parameters.Add("@old_enquiry_date", (object)request.old_enquiry_date ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field1", (object)request.free_field1 ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field2", (object)request.free_field2 ?? DBNull.Value, DbType.String);
                    parameters.Add("@free_field3", (object)request.free_field3 ?? DBNull.Value, DbType.String);

                    var result = await con.QuerySingleAsync<(int success, string message)>(
                        "sp_UpdHarleyLead",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var response = new LeadResponse
                    {
                        status = result.success,
                        message = result.message,
                        lead_uid = leadUid
                    };

                    WriteLog(apiName, "RESPONSE | " + JsonSerializer.Serialize(response));
                    return response;
                }
            }
            catch (Exception ex)
            {
                WriteLog(apiName, "ERROR | " + ex.ToString());
                throw;
            }
        }

        public async Task<LeadResponse> UpdateCallBackAsync(string leadUid, DateTime callBackDateTime, string campaignName = null)
        {
            const string apiName = "UpdateCallBack";
            WriteLog(apiName, "DATA RECEIVED | lead_uid=" + leadUid + " | callBackDateTime=" + callBackDateTime + " | CampaignName=" + campaignName);

            if (string.IsNullOrWhiteSpace(leadUid))
            {
                var errResponse = new LeadResponse { status = 0, message = "lead_uid is required" };
                WriteLog(apiName, "RESPONSE | " + JsonSerializer.Serialize(errResponse));
                return errResponse;
            }

            int unixTime = (int)new DateTimeOffset(callBackDateTime).ToUnixTimeSeconds();

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@lead_uid", leadUid);
                    parameters.Add("@dial_sched_time", unixTime);
                    parameters.Add("@CampaignName", (object)campaignName ?? DBNull.Value, DbType.String);

                    var result = await con.QuerySingleAsync<(int success, string message)>(
                        "sp_UpdHarleyCallBack",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var response = new LeadResponse
                    {
                        status = result.success,
                        message = result.message,
                        lead_uid = leadUid
                    };

                    WriteLog(apiName, "RESPONSE | " + JsonSerializer.Serialize(response));
                    return response;
                }
            }
            catch (Exception ex)
            {
                WriteLog(apiName, "ERROR | " + ex.ToString());
                throw;
            }
        }

        public async Task<LeadResponse> CancelLeadAsync(string leadUid, string campaignName = null)
        {
            const string apiName = "CancelLead";
            WriteLog(apiName, "DATA RECEIVED | lead_uid=" + leadUid + " | CampaignName=" + campaignName);

            if (string.IsNullOrWhiteSpace(leadUid))
            {
                var errResponse = new LeadResponse { status = 0, message = "lead_uid is required" };
                WriteLog(apiName, "RESPONSE | " + JsonSerializer.Serialize(errResponse));
                return errResponse;
            }

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@lead_uid", leadUid);
                    parameters.Add("@CampaignName", (object)campaignName ?? DBNull.Value, DbType.String);

                    var result = await con.QuerySingleAsync<(int success, string message)>(
                        "sp_UpdHarleyCancel",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var response = new LeadResponse
                    {
                        status = result.success,
                        message = result.message,
                        lead_uid = leadUid
                    };

                    WriteLog(apiName, "RESPONSE | " + JsonSerializer.Serialize(response));
                    return response;
                }
            }
            catch (Exception ex)
            {
                WriteLog(apiName, "ERROR | " + ex.ToString());
                throw;
            }
        }
    }
}