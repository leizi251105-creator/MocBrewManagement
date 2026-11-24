
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    public class OrderRequestReviewViewModel
    {
        public long RequestId { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string ReviewNote { get; set; }
    }
}
