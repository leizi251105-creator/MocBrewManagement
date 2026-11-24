
using System;
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    public class PurchaseReceiptDetailViewModel
    {
        public int PurchaseReceiptDetailId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nguyên liệu.")]
        [Display(Name = "Nguyên liệu")]
        public int IngredientId { get; set; }

        [Display(Name = "Tên nguyên liệu")]
        public string IngredientName { get; set; }

        [Display(Name = "Đơn vị")]
        public string Unit { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Range(typeof(decimal), "0.001", "999999999",
            ErrorMessage = "Số lượng phải lớn hơn 0.")]
        [Display(Name = "Số lượng")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn giá.")]
        [Range(typeof(decimal), "0.01", "999999999",
            ErrorMessage = "Đơn giá phải lớn hơn 0.")]
        [Display(Name = "Đơn giá nhập")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Hạn sử dụng")]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Thành tiền")]
        public decimal Amount
        {
            get { return Quantity * UnitPrice; }
        }
    }
}
