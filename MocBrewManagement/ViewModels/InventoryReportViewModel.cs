
using System;
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    // ViewModel chính của báo cáo tồn kho
    public class InventoryReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Thống kê tổng quan
        public int TotalIngredients { get; set; }
        public int LowStockIngredients { get; set; }
        public int OutOfStockIngredients { get; set; }

        public decimal TotalImportQuantity { get; set; }
        public decimal TotalExportQuantity { get; set; }
        public decimal TotalAdjustmentQuantity { get; set; }

        // Danh sách tồn kho hiện tại
        public List<IngredientStockReportViewModel> Ingredients
        { get; set; } = new List<IngredientStockReportViewModel>();

        // Lịch sử giao dịch kho
        public List<InventoryTransactionReportViewModel> Transactions
        { get; set; } = new List<InventoryTransactionReportViewModel>();

        // Lịch sử kiểm kê
        public List<InventoryStocktakeReportViewModel> Stocktakes
        { get; set; } = new List<InventoryStocktakeReportViewModel>();
    }

    // Thông tin tồn kho từng nguyên liệu
    public class IngredientStockReportViewModel
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }

        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }

        public string StockStatus { get; set; }
    }

    // Thông tin nhập, xuất, điều chỉnh kho
    public class InventoryTransactionReportViewModel
    {
        public long TransactionId { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }

        public string TransactionType { get; set; }
        public decimal Quantity { get; set; }

        public DateTime TransactionDate { get; set; }
        public string ReferenceCode { get; set; }
        public string Note { get; set; }
        public string EmployeeName { get; set; }
    }

    // Thông tin kiểm kê nguyên liệu
    public class InventoryStocktakeReportViewModel
    {
        public long StocktakeId { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }

        public decimal SystemStock { get; set; }
        public decimal ActualStock { get; set; }
        public decimal Difference { get; set; }

        public DateTime StocktakeDate { get; set; }
        public string EmployeeName { get; set; }
    }
}
