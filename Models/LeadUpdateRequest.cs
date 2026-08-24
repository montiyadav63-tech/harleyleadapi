namespace HarleyLeadApi.Models
{
    public class LeadUpdateRequest
    {
        // lead_uid route se aayega, body me nahi
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string pincode { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string pre_selected_model { get; set; }
        public string lead_source { get; set; }
        public string utm_source { get; set; }
        public string source_campaign { get; set; }
        public string purchase_timelines { get; set; }
        public string exchange_required { get; set; }
        public string finance_required { get; set; }
        public string pre_selected_dealer_code { get; set; }
        public string pre_selected_dealer_name { get; set; }
        public string old_dealer_code { get; set; }
        public string old_dealer_name { get; set; }
        public string old_enquiry_date { get; set; }
        public string free_field1 { get; set; }
        public string free_field2 { get; set; }
        public string free_field3 { get; set; }
    }
}
