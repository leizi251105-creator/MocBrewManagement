
using System.ComponentModel.DataAnnotations;

namespace MocBrewManagement.Models
{
    public class StaffEditViewModel
    {
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
        [StringLength(20)]
        public string EmployeeCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(150)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50)]
        public string Username { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; }

        [StringLength(15)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public int? RoleId { get; set; }

        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        public string NewPassword { get; set; }
    }
}
