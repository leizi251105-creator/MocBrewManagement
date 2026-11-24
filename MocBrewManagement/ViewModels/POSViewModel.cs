
using System;
using System.Collections.Generic;
using System.Linq;

namespace MocBrewManagement.ViewModels
{
    // Thông tin sản phẩm hiển thị trên màn hình POS
    public class POSProductViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string ImageUrl { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        public List<POSProductSizeViewModel> Sizes { get; set; }
            = new List<POSProductSizeViewModel>();

        public List<POSToppingViewModel> Toppings { get; set; }
            = new List<POSToppingViewModel>();
    }

    // Kích cỡ và giá bán của sản phẩm
    public class POSProductSizeViewModel
    {
        public int ProductSizeId { get; set; }

        public int SizeId { get; set; }

        public string SizeName { get; set; }

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }
    }

    // Topping có thể chọn cho sản phẩm
    public class POSToppingViewModel
    {
        public int ToppingId { get; set; }

        public string ToppingName { get; set; }

        public decimal Price { get; set; }
    }

    // Một dòng sản phẩm trong giỏ hàng
    public class POSCartItemViewModel
    {
        public int ProductId { get; set; }

        public int ProductSizeId { get; set; }

        public string ProductName { get; set; }

        public string SizeName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public string Note { get; set; }

        public List<POSToppingViewModel> Toppings { get; set; }
            = new List<POSToppingViewModel>();

        public decimal ToppingTotal
        {
            get
            {
                return Toppings.Sum(t => t.Price);
            }
        }

        public decimal LineTotal
        {
            get
            {
                return (UnitPrice + ToppingTotal) * Quantity;
            }
        }
    }

    // ViewModel chính của màn hình tạo đơn POS
    public class POSCreateViewModel
    {
        public POSCreateViewModel()
        {
            Products = new List<POSProductViewModel>();
            CartItems = new List<POSCartItemViewModel>();
        }

        public List<POSProductViewModel> Products { get; set; }

        public List<POSCartItemViewModel> CartItems { get; set; }

        public string SearchKeyword { get; set; }

        public int? SelectedCategoryId { get; set; }

        public int? CustomerId { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string Note { get; set; }
        // Thông tin thanh toán
        public string PaymentMethod { get; set; }

        // Nhân viên xác nhận đã nhận đủ tiền
        public bool ConfirmPayment { get; set; }

        public decimal SubTotal
        {
            get
            {
                return CartItems.Sum(x => x.LineTotal);
            }
        }

        public decimal DiscountAmount { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal TotalAmount
        {
            get
            {
                return SubTotal - DiscountAmount + ShippingFee;
            }
        }
    }
}
