
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    // ==========================================
    // VIEW MODEL DANH SÁCH NGUYÊN LIỆU
    // ==========================================
    public class IngredientViewModel
    {
        public int IngredientId { get; set; }

        public string IngredientName { get; set; }

        public string Unit { get; set; }

        public decimal CurrentStock { get; set; }

        public decimal MinimumStock { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        // Kiểm tra nguyên liệu sắp hết
        public bool IsLowStock
        {
            get
            {
                return CurrentStock <= MinimumStock;
            }
        }
    }

    // ==========================================
    // VIEW MODEL THÊM NGUYÊN LIỆU
    // ==========================================
    public class IngredientCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên nguyên liệu.")]
        [StringLength(200, ErrorMessage = "Tên nguyên liệu tối đa 200 ký tự.")]
        [Display(Name = "Tên nguyên liệu")]
        public string IngredientName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
        [StringLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự.")]
        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; }

        [Range(typeof(decimal), "0", "999999999999999.999",
            ErrorMessage = "Số lượng tồn không hợp lệ.")]
        [Display(Name = "Số lượng tồn")]
        public decimal CurrentStock { get; set; }

        [Range(typeof(decimal), "0", "999999999999999.999",
            ErrorMessage = "Mức tồn tối thiểu không hợp lệ.")]
        [Display(Name = "Mức tồn tối thiểu")]
        public decimal MinimumStock { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }
    }

    // ==========================================
    // VIEW MODEL CHỈNH SỬA NGUYÊN LIỆU
    // ==========================================
    public class IngredientEditViewModel
    {
        public int IngredientId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nguyên liệu.")]
        [StringLength(200, ErrorMessage = "Tên nguyên liệu tối đa 200 ký tự.")]
        [Display(Name = "Tên nguyên liệu")]
        public string IngredientName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
        [StringLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự.")]
        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; }

        [Range(typeof(decimal), "0", "999999999999999.999",
            ErrorMessage = "Mức tồn tối thiểu không hợp lệ.")]
        [Display(Name = "Mức tồn tối thiểu")]
        public decimal MinimumStock { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }
    }
}
