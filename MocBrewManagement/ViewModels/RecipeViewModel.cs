
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    // Hiển thị danh sách công thức
    public class RecipeViewModel
    {
        public int RecipeId { get; set; }

        public int ProductSizeId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string SizeName { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public int TotalIngredients { get; set; }
    }

    // Một nguyên liệu trong công thức
    public class RecipeIngredientViewModel
    {
        public int IngredientId { get; set; }

        public string IngredientName { get; set; }

        public string Unit { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập định lượng.")]
        [Range(typeof(decimal), "0.001", "999999999999999.999",
            ErrorMessage = "Định lượng phải lớn hơn 0.")]
        public decimal QuantityRequired { get; set; }
    }

    // Dữ liệu tạo và sửa công thức
    public class RecipeCreateEditViewModel
    {
        public RecipeCreateEditViewModel()
        {
            Ingredients = new List<RecipeIngredientViewModel>();
        }

        public int RecipeId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm và size.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn sản phẩm và size.")]
        [Display(Name = "Sản phẩm - Size")]
        public int ProductSizeId { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        [Display(Name = "Mô tả công thức")]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        public List<RecipeIngredientViewModel> Ingredients { get; set; }
    }
}
