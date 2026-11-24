
using System;
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    public class POSOrderDetailViewModel
    {
        // Thông tin đơn hàng
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }

        // Thông tin khách hàng
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string DeliveryAddress { get; set; }

        // Thông tin thanh toán
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }

        // Trạng thái và hình thức nhận hàng
        public string OrderStatus { get; set; }
        public string OrderSource { get; set; }
        public string FulfillmentMethod { get; set; }

        // Ghi chú
        public string Note { get; set; }

        // Danh sách sản phẩm
        public List<POSOrderDetailItemViewModel> Items { get; set; }
            = new List<POSOrderDetailItemViewModel>();
    }

    public class POSOrderDetailItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SizeName { get; set; }

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }

        public string Note { get; set; }

        // Danh sách topping
        public List<POSOrderToppingViewModel> Toppings { get; set; }
            = new List<POSOrderToppingViewModel>();
    }

    public class POSOrderToppingViewModel
    {
        public string ToppingName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
