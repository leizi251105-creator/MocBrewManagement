
using System;
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    public class OrderRequestViewModel
    {
        public int OrderId { get; set; }

        public string OrderCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại yêu cầu.")]
        public string RequestType { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do.")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        public string Reason { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentStatus { get; set; }

        public string OrderStatus { get; set; }
    }
}
