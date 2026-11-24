
using System;
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    public class ProfitReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Tổng quan
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossMargin { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int RefundedOrders { get; set; }

        // Chi tiết lợi nhuận theo sản phẩm
        public List<ProductProfitViewModel> ProductProfits { get; set; }
            = new List<ProductProfitViewModel>();

        // Lợi nhuận theo ngày
        public List<DailyProfitViewModel> DailyProfits { get; set; }
            = new List<DailyProfitViewModel>();
    }

    public class ProductProfitViewModel
    {
        public string ProductName { get; set; }
        public string SizeName { get; set; }

        public int TotalQuantity { get; set; }

        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class DailyProfitViewModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }
}
