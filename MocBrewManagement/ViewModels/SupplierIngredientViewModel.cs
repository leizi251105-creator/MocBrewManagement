
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    public class SupplierIngredientViewModel
    {
        public int SupplierId { get; set; }

        public string SupplierName { get; set; }

        public List<SupplierIngredientItemViewModel> Ingredients
        {
            get;
            set;
        } = new List<SupplierIngredientItemViewModel>();
    }

    public class SupplierIngredientItemViewModel
    {
        public int IngredientId { get; set; }

        public string IngredientName { get; set; }

        public string Unit { get; set; }

        // Đánh dấu nguyên liệu được nhà cung cấp này cung cấp
        public bool IsSelected { get; set; }

        // Trạng thái liên kết cung cấp
        public bool IsActive { get; set; }
    }
}
