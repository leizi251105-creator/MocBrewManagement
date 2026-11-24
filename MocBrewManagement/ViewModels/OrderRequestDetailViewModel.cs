
using System;

namespace MocBrewManagement.ViewModels
{
    public class OrderRequestDetailViewModel
    {
        public long RequestId { get; set; }

        public int OrderId { get; set; }

        public string OrderCode { get; set; }

        public string RequestType { get; set; }

        public string RequestedByName { get; set; }

        public DateTime RequestedAt { get; set; }

        public string Reason { get; set; }

        public string RequestStatus { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal SubTotal { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal ShippingFee { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string OrderStatus { get; set; }

        public string FulfillmentMethod { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string DeliveryAddress { get; set; }

        public string OrderNote { get; set; }

        public string ReviewedByName { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string ReviewNote { get; set; }
    }
}
