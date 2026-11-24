
using System;
using System.Collections.Generic;

namespace MocBrewManagement.Models
{
    // Thông tin một sản phẩm hiển thị trong danh sách
    public class ProductListItemViewModel
    {
       
            public int ProductId { get; set; }

            public string ProductName { get; set; }

            public string CategoryName { get; set; }

            public string ImageUrl { get; set; }

            public string Description { get; set; }

            public bool IsFeatured { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedAt { get; set; }

            public string SizeSummary { get; set; }

            public decimal? MinPrice { get; set; }

            // Giá riêng theo từng Size
            public decimal? PriceS { get; set; }

            public decimal? PriceM { get; set; }

            public decimal? PriceL { get; set; }
        
    }

    // ViewModel dùng cho trang danh sách sản phẩm
    public class ProductViewModel
    {
        public ProductViewModel()
        {
            Products = new List<ProductListItemViewModel>();
            Categories = new List<Category>();
        }

        public List<ProductListItemViewModel> Products { get; set; }

        public List<Category> Categories { get; set; }

        public string Keyword { get; set; }

        public int? CategoryId { get; set; }

        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }

        public int TotalActive { get; set; }

        public int TotalInactive { get; set; }

        public int TotalFeatured { get; set; }
    }
    // ViewModel thêm sản phẩm
    public class ProductCreateViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [System.ComponentModel.DataAnnotations.StringLength(
            200, ErrorMessage = "Tên sản phẩm không được quá 200 ký tự")]
        public string ProductName { get; set; }

        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(1000)]
        public string Description { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        public System.Web.HttpPostedFileBase ImageFile { get; set; }

        public List<Category> Categories { get; set; }

        // Giá bán theo từng Size
        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Vui lòng nhập giá Size S")]
        [System.ComponentModel.DataAnnotations.Range(
            typeof(decimal), "1", "999999999",
            ErrorMessage = "Giá Size S phải lớn hơn 0")]
        public decimal? PriceS { get; set; }

        [System.ComponentModel.DataAnnotations.Range(
            typeof(decimal), "1", "999999999",
            ErrorMessage = "Giá Size M phải lớn hơn 0")]
        public decimal? PriceM { get; set; }

        [System.ComponentModel.DataAnnotations.Range(
            typeof(decimal), "1", "999999999",
            ErrorMessage = "Giá Size L phải lớn hơn 0")]
        public decimal? PriceL { get; set; }
    }

    public class ProductEditViewModel
    {
        public int ProductId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [System.ComponentModel.DataAnnotations.StringLength(
            200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự")]
        public string ProductName { get; set; }

        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(1000)]
        public string Description { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        public string CurrentImageUrl { get; set; }

        public System.Web.HttpPostedFileBase ImageFile { get; set; }

        public List<Category> Categories { get; set; }

        public decimal? PriceS { get; set; }

        public decimal? PriceM { get; set; }

        public decimal? PriceL { get; set; }

        public bool HasSizeS { get; set; }

        public bool HasSizeM { get; set; }

        public bool HasSizeL { get; set; }
    }

}