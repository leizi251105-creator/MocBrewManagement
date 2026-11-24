
using System;

namespace MocBrewManagement.ViewModels
{
    public class OrderRequestListViewModel
    {
        public long RequestId { get; set; }

        public int OrderId { get; set; }

        public string OrderCode { get; set; }

        public string RequestType { get; set; }

        public string RequestedByName { get; set; }

        public string Reason { get; set; }

        public string RequestStatus { get; set; }

        public DateTime RequestedAt { get; set; }

        public string ReviewedByName { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string ReviewNote { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentStatus { get; set; }

        public string OrderStatus { get; set; }
    }
}
