using System.ComponentModel.DataAnnotations;

namespace HarleyLeadApi.Models
{
    public class CSATLeadRequest
    {
        [Required] public string lead_uid { get; set; }
        [Required] public string contactNumber { get; set; }
        [Required] public string campaignSurvey { get; set; }
        public string customerName { get; set; }
        public string free_field1 { get; set; }
        public string free_field2 { get; set; }
        public string free_field3 { get; set; }
    }
}