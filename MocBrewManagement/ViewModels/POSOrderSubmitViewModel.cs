
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    public class POSOrderSubmitViewModel
    {
        public string CartItemsJson { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string Note { get; set; }

        public string PaymentMethod { get; set; }

        public bool ConfirmPayment { get; set; }
    }

    public class POSCartSubmitItemViewModel
    {
        public int ProductId { get; set; }

        public int ProductSizeId { get; set; }

        public int Quantity { get; set; }

        public string Note { get; set; }

        public List<int> ToppingIds { get; set; }
    }
}
