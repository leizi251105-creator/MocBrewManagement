
using System;
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    public class RevenueReportViewModel
    {
        // Khoảng thời gian báo cáo
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Tổng quan doanh thu
        public decimal TotalRevenue { get; set; }

        // Thống kê đơn hàng
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int RefundedOrders { get; set; }

        // Thống kê doanh thu theo nguồn đơn
        public decimal POSRevenue { get; set; }
        public decimal OnlineRevenue { get; set; }

        // Thống kê sản phẩm
        public List<TopSellingProductViewModel> TopProducts
        {
            get;
            set;
        } = new List<TopSellingProductViewModel>();

        // Doanh thu theo ngày
        public List<DailyRevenueViewModel> DailyRevenues
        {
            get;
            set;
        } = new List<DailyRevenueViewModel>();
    }

    public class TopSellingProductViewModel
    {
        public string ProductName { get; set; }
        public string SizeName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public decimal Revenue { get; set; }
    }
}
