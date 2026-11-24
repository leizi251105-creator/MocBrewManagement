
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    // ==========================================
    // THÔNG TIN TỪNG NGUYÊN LIỆU TRONG PHIẾU KIỂM KÊ
    // ==========================================
    public class StocktakeItemViewModel
    {
        public int IngredientId { get; set; }

        public string IngredientName { get; set; }

        public string Unit { get; set; }

        public decimal SystemStock { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng thực tế.")]
        [Range(typeof(decimal), "0", "999999999999999.999",
            ErrorMessage = "Số lượng thực tế phải lớn hơn hoặc bằng 0.")]
        [Display(Name = "Số lượng thực tế")]
        public decimal ActualStock { get; set; }

        public decimal Difference
        {
            get { return ActualStock - SystemStock; }
        }

        public string Note { get; set; }
    }

    // ==========================================
    // VIEWMODEL DANH SÁCH KIỂM KÊ
    // ==========================================
    public class StocktakeViewModel
    {
        public StocktakeViewModel()
        {
            Items = new List<StocktakeItemViewModel>();
        }

        public DateTime StocktakeDate { get; set; }

        public List<StocktakeItemViewModel> Items { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
        [Display(Name = "Ghi chú kiểm kê")]
        public string Note { get; set; }
    }
}
