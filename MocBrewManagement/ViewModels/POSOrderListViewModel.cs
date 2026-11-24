
using System;

namespace MocBrewManagement.ViewModels
{
    public class POSOrderListViewModel
    {
        public int OrderId { get; set; }

        public string OrderCode { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string OrderStatus { get; set; }

        public string FulfillmentMethod { get; set; }

        public string OrderSource { get; set; }
    }
}
