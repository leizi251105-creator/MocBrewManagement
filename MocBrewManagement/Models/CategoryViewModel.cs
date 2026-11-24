
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MocBrewManagement.Models
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(150, ErrorMessage = "Tên danh mục tối đa 150 ký tự")]
        public string CategoryName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<SelectListItem> ParentCategories { get; set; }
    }

    public class CategoryEditViewModel
    {
        public int CategoryId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Vui lòng nhập tên danh mục")]
        [System.ComponentModel.DataAnnotations.StringLength(
            150, ErrorMessage = "Tên danh mục tối đa 150 ký tự")]
        public string CategoryName { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(
            500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<System.Web.Mvc.SelectListItem> ParentCategories
        {
            get;
            set;
        }
    }

}
