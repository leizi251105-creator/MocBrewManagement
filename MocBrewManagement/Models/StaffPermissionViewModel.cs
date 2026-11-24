
using System.Collections.Generic;

namespace MocBrewManagement.Models
{
    // ViewModel thông tin nhân viên và danh sách quyền
    public class StaffPermissionViewModel
    {
        public int AccountId { get; set; }

        public string EmployeeCode { get; set; }

        public string FullName { get; set; }

        public string Username { get; set; }

        public List<StaffPermissionItemViewModel> Permissions
        {
            get;
            set;
        }

        public StaffPermissionViewModel()
        {
            Permissions = new List<StaffPermissionItemViewModel>();
        }
    }

    // ViewModel từng quyền riêng lẻ
    public class StaffPermissionItemViewModel
    {
        public int PermissionId { get; set; }

        public string PermissionCode { get; set; }

        public string PermissionName { get; set; }

        public string Description { get; set; }

        public bool IsGranted { get; set; }
    }
}
