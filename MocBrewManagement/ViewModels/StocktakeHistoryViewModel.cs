
using System;
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    // ==========================================
    // DỮ LIỆU HIỂN THỊ DANH SÁCH PHIẾU
    // ==========================================
    public class StocktakeHistoryItemViewModel
    {
        public long SessionId { get; set; }

        public string StocktakeCode { get; set; }

        public DateTime StocktakeDate { get; set; }

        public string Username { get; set; }

        public string FullName { get; set; }

        public int TotalIngredients { get; set; }

        public int ChangedIngredients { get; set; }
    }

    // ==========================================
    // DỮ LIỆU CHI TIẾT TỪNG NGUYÊN LIỆU
    // ==========================================
    public class StocktakeDetailItemViewModel
    {
        public string IngredientName { get; set; }

        public string Unit { get; set; }

        public decimal SystemStock { get; set; }

        public decimal ActualStock { get; set; }

        public decimal Difference { get; set; }

        public string Note { get; set; }
    }

    // ==========================================
    // DỮ LIỆU CHI TIẾT PHIẾU KIỂM KÊ
    // ==========================================
    public class StocktakeDetailViewModel
    {
        public long SessionId { get; set; }

        public string StocktakeCode { get; set; }

        public DateTime StocktakeDate { get; set; }

        public string Username { get; set; }

        public string FullName { get; set; }

        public string SessionNote { get; set; }

        public int TotalIngredients { get; set; }

        public int ChangedIngredients { get; set; }

        public int UnchangedIngredients { get; set; }

        public List<StocktakeDetailItemViewModel> Items { get; set; }

        public StocktakeDetailViewModel()
        {
            Items = new List<StocktakeDetailItemViewModel>();
        }
    }
}
