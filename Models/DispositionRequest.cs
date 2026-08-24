using System.ComponentModel.DataAnnotations;

namespace HarleyLeadApi.Models
{
    public class DispositionRequest
    {
        [Required] public string AgentId { get; set; }
        [Required] public string DispCode { get; set; }
        [Required] public string SubDispCode { get; set; }
        [Required] public string CRMId { get; set; }
        [Required] public string PhoneNumber { get; set; }
    }
}
