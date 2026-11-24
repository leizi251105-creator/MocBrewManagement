
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MocBrewManagement.Models;

namespace MocBrewManagement.Controllers
{
    public class AdminAccountController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        // Kiểm tra quyền Super Admin
        private bool IsSuperAdmin()
        {
            return Session["MOCBREW_AccountId"] != null
                && Session["MOCBREW_RoleId"] != null
                && Convert.ToInt32(Session["MOCBREW_RoleId"]) == 1;
        }

        // Hàm ghi nhật ký hoạt động
        private void AddSystemLog(
            int? accountId,
            string actionCode,
            string description)
        {
            SystemLog log = new SystemLog
            {
                AccountId = accountId,
                LogType = "TECHNICAL",
                ActionCode = actionCode,
                Description = description,
                CreatedAt = DateTime.Now
            };

            db.SystemLogs.Add(log);
        }

        

        // Trang danh sách tài khoản Super Admin
        public ActionResult Index()
        {
            // Chưa đăng nhập
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            // Không phải Super Admin
            if (!IsSuperAdmin())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền truy cập chức năng Quản lý Super Admin."
                );
            }

            // Chỉ lấy tài khoản có RoleId = 1
            var accounts = db.Accounts
                .Include(a => a.Role)
                .Where(a => a.RoleId == 1)
                .OrderBy(a => a.AccountId)
                .ToList();

            ViewBag.FullName = Session["MOCBREW_FullName"];
            ViewBag.RoleName = "Super Admin";

            ViewBag.TotalAccounts = accounts.Count;
            ViewBag.ActiveAccounts = accounts.Count(a => a.IsActive);
            ViewBag.LockedAccounts = accounts.Count(a => !a.IsActive);

            return View(accounts);
        }

        // GET: AdminAccount/Create
        public ActionResult Create()
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!IsSuperAdmin())
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền tạo tài khoản Super Admin.");
            }

            ViewBag.FullName = Session["MOCBREW_FullName"];

            return View();
        }

        // POST: AdminAccount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AdminAccountViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!IsSuperAdmin())
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền tạo tài khoản Super Admin.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FullName = Session["MOCBREW_FullName"];
                return View(model);
            }

            model.Username = model.Username.Trim();

            // Kiểm tra tên đăng nhập đã tồn tại
            bool usernameExists = db.Accounts.Any(a =>
                a.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError("Username",
                    "Tên đăng nhập này đã tồn tại.");

                ViewBag.FullName = Session["MOCBREW_FullName"];
                return View(model);
            }

            try
            {
                Account account = new Account();

                // Tự động gán vai trò Super Admin
                account.RoleId = 1;

                account.FullName = model.FullName.Trim();
                account.Username = model.Username;

                // Mã hóa mật khẩu
                account.PasswordHash =
                    PasswordHelper.HashPassword(model.Password);

                account.Email = string.IsNullOrWhiteSpace(model.Email)
                    ? null
                    : model.Email.Trim();

                account.Phone = string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim();

                account.IsActive = true;
                account.CreatedAt = DateTime.Now;


                db.Accounts.Add(account);

                // Lấy ID của Super Admin đang thực hiện thao tác
                int currentAccountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                // Ghi nhật ký tạo tài khoản
                AddSystemLog(
                    currentAccountId,
                    "ADMIN_CREATE",
                    "Tạo tài khoản Super Admin: " + account.Username);

                // Lưu tài khoản và nhật ký vào cơ sở dữ liệu
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Tạo tài khoản Super Admin thành công.";


                return RedirectToAction("Index");
            }

            catch (Exception ex)
            {
                string errorMessage = ex.GetBaseException().Message;

                ModelState.AddModelError("",
                    "Lỗi chi tiết: " + errorMessage);

                ViewBag.FullName = Session["MOCBREW_FullName"];
                return View(model);
            }

        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        // POST: AdminAccount/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!IsSuperAdmin())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền khóa hoặc mở khóa tài khoản.");

            var account = db.Accounts.FirstOrDefault(a =>
                a.AccountId == id && a.RoleId == 1);

            if (account == null)
                return HttpNotFound();

            int currentAccountId = Convert.ToInt32(
                Session["MOCBREW_AccountId"]);

            // Không cho phép tự khóa tài khoản đang đăng nhập
            if (account.AccountId == currentAccountId && account.IsActive)
            {
                TempData["ErrorMessage"] =
                    "Bạn không thể tự khóa tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            // Không cho phép khóa Super Admin hoạt động cuối cùng
            if (account.IsActive)
            {
                int activeAdminCount = db.Accounts.Count(a =>
                    a.RoleId == 1 && a.IsActive);

                if (activeAdminCount <= 1)
                {
                    TempData["ErrorMessage"] =
                        "Không thể khóa tài khoản Super Admin hoạt động cuối cùng.";

                    return RedirectToAction("Index");
                }
            }


            try
            {
                // Thay đổi trạng thái tài khoản
                account.IsActive = !account.IsActive;

                // Lấy ID Super Admin đang thực hiện thao tác
                int actorAccountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                // Xác định loại thao tác
                string actionCode = account.IsActive
                    ? "ADMIN_UNLOCK"
                    : "ADMIN_LOCK";

                string logDescription = account.IsActive
                    ? "Mở khóa tài khoản Super Admin: " + account.Username
                    : "Khóa tài khoản Super Admin: " + account.Username;

                // Ghi nhật ký hoạt động
                AddSystemLog(
                    actorAccountId,
                    actionCode,
                    logDescription);

                // Lưu trạng thái và nhật ký
                db.SaveChanges();

                TempData["SuccessMessage"] = account.IsActive
                    ? "Mở khóa tài khoản Super Admin thành công."
                    : "Khóa tài khoản Super Admin thành công.";
            }

            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Không thể cập nhật trạng thái tài khoản.";
            }

            return RedirectToAction("Index");
        }


        // GET: AdminAccount/Edit/5
        public ActionResult Edit(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!IsSuperAdmin())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền chỉnh sửa tài khoản Super Admin.");

            var account = db.Accounts.FirstOrDefault(a =>
                a.AccountId == id && a.RoleId == 1);

            if (account == null)
                return HttpNotFound();

            var model = new AdminAccountEditViewModel
            {
                AccountId = account.AccountId,
                FullName = account.FullName,
                Username = account.Username,
                Email = account.Email,
                Phone = account.Phone
            };

            ViewBag.FullName = Session["MOCBREW_FullName"];

            return View(model);
        }

    
    
        // POST: AdminAccount/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AdminAccountEditViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!IsSuperAdmin())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền chỉnh sửa tài khoản Super Admin.");

            if (!ModelState.IsValid)
            {
                ViewBag.FullName = Session["MOCBREW_FullName"];
                return View(model);
            }

            var account = db.Accounts.FirstOrDefault(a =>
                a.AccountId == model.AccountId && a.RoleId == 1);

            if (account == null)
                return HttpNotFound();

            model.Username = model.Username.Trim();

            bool usernameExists = db.Accounts.Any(a =>
                a.Username == model.Username &&
                a.AccountId != model.AccountId);

            if (usernameExists)
            {
                ModelState.AddModelError("Username",
                    "Tên đăng nhập này đã được sử dụng.");

                ViewBag.FullName = Session["MOCBREW_FullName"];
                return View(model);
            }

            try
            {
                account.FullName = model.FullName.Trim();
                account.Username = model.Username;

                account.Email = string.IsNullOrWhiteSpace(model.Email)
                    ? null : model.Email.Trim();

                account.Phone = string.IsNullOrWhiteSpace(model.Phone)
                    ? null : model.Phone.Trim();


                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    account.PasswordHash =
                        PasswordHelper.HashPassword(model.NewPassword);
                }

                // Lấy ID Super Admin đang thực hiện thao tác
                int actorAccountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                // Nội dung nhật ký
                string logDescription =
                    "Cập nhật tài khoản Super Admin: " + account.Username;

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    logDescription += " (có thay đổi mật khẩu)";
                }

                // Ghi nhật ký
                AddSystemLog(
                    actorAccountId,
                    "ADMIN_EDIT",
                    logDescription);

                // Lưu thông tin và nhật ký
                db.SaveChanges();

                // Cập nhật thông tin phiên đăng nhập nếu sửa chính mình
                int currentAccountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                if (currentAccountId == account.AccountId)
                {
                    Session["MOCBREW_Username"] = account.Username;
                    Session["MOCBREW_FullName"] = account.FullName;
                }

                TempData["SuccessMessage"] =
                    "Cập nhật tài khoản Super Admin thành công.";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("",
                    "Không thể cập nhật tài khoản. Vui lòng kiểm tra lại.");

                ViewBag.FullName = Session["MOCBREW_FullName"];
                return View(model);
            }
        }

    }
}