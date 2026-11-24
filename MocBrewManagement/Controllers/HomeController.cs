
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MocBrewManagement.Models;

namespace MocBrewManagement.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            int accountId = Convert.ToInt32(
                Session["MOCBREW_AccountId"]);

            int roleId = Convert.ToInt32(
                Session["MOCBREW_RoleId"]);

            // 2. Lấy thông tin tài khoản
            ViewBag.FullName = Session["MOCBREW_FullName"];
            ViewBag.Username = Session["MOCBREW_Username"];
            ViewBag.RoleId = roleId;

            // 3. Xác định tên vai trò hiển thị
            switch (roleId)
            {
                case 1:
                    ViewBag.RoleName = "Super Admin";
                    break;

                case 2:
                    ViewBag.RoleName = "Store Manager";
                    break;

                case 3:
                    ViewBag.RoleName = "Staff";
                    break;

                default:
                    Session.Clear();
                    Session.Abandon();

                    return RedirectToAction("Login", "MocBrewAuth");
            }

            // 4. Lấy danh sách quyền được cấp trực tiếp
            var permissionCodes = new List<string>();

            // Chỉ Staff sử dụng quyền cấp trực tiếp
            if (roleId == 3)
            {
                using (var db = new MOCBREWEntities())
                {
                    permissionCodes = db.UserPermissions
                        .Where(up =>
                            up.AccountId == accountId &&
                            up.IsActive)
                        .Select(up => up.Permission.PermissionCode)
                        .Distinct()
                        .ToList();
                }
            }

            // 5. Chuyển danh sách quyền sang View
            ViewBag.PermissionCodes =
                new HashSet<string>(permissionCodes);

            // 6. Các thông tin hỗ trợ giao diện
            ViewBag.IsSuperAdmin = roleId == 1;
            ViewBag.IsStoreManager = roleId == 2;
            ViewBag.IsStaff = roleId == 3;

            return View();
        }
    }
}
