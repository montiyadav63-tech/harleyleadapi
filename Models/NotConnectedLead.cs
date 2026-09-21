namespace HarleyLeadApi.Models
{
    public class NotConnectedLead
    {
        public int call_result { get; set; }
        public string lead_uid { get; set; }
        public long call_time { get; set; }
        public int attempt { get; set; }
        public int record_id { get; set; }
        public string mobile { get; set; }
    }
}