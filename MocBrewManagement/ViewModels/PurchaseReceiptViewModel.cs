
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.ViewModels
{
    public class PurchaseReceiptViewModel
    {
        public int PurchaseReceiptId { get; set; }

        [Display(Name = "Mã phiếu nhập")]
        public string ReceiptCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
        [Display(Name = "Nhà cung cấp")]
        public int SupplierId { get; set; }

        [Display(Name = "Tên nhà cung cấp")]
        public string SupplierName { get; set; }

        [Display(Name = "Nhân viên lập phiếu")]
        public string EmployeeName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhập.")]
        [Display(Name = "Ngày nhập")]
        public DateTime ReceiptDate { get; set; }

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        public List<PurchaseReceiptDetailViewModel> Details { get; set; }
            = new List<PurchaseReceiptDetailViewModel>();

        public List<System.Web.Mvc.SelectListItem> Suppliers { get; set; }
            = new List<System.Web.Mvc.SelectListItem>();

        public List<System.Web.Mvc.SelectListItem> Ingredients { get; set; }
            = new List<System.Web.Mvc.SelectListItem>();
    }
}
