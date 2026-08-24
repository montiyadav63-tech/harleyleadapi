namespace HarleyLeadApi.Models
{
    public class LeadResponse
    {
        public int status { get; set; }
        public string message { get; set; }
        public string lead_uid { get; set; }
        public int? record_id { get; set; }
    }
}
