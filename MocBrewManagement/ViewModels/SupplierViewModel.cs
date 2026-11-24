
using System;
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    public class SupplierViewModel
    {
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
        [StringLength(200, ErrorMessage = "Tên nhà cung cấp không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên nhà cung cấp")]
        public string SupplierName { get; set; }

        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(500)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã số thuế")]
        public string TaxCode { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
