namespace HarleyLeadApi.Models
{
    public class DialerWebhookRequest
    {
        public string lead_uid { get; set; }
        public string sub_disposition_code { get; set; }
        public bool attempts_exhausted { get; set; }
        public int attempt { get; set; }
        public string remarks { get; set; }
        public string disposed_at { get; set; }
    }
}