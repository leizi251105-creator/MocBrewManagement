
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using MocBrewManagement.Models;

namespace MocBrewManagement.Controllers
{
    public class StaffController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        // Kiểm tra đăng nhập và quyền quản lý nhân viên
        private int? GetCurrentRoleId()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return null;

            return Convert.ToInt32(Session["MOCBREW_RoleId"]);
        }

        // Nạp danh sách vai trò được phép tạo
        private void LoadRoleList(int currentRoleId, int? selectedRoleId = null)
        {
            var roles = db.Roles.Where(r =>
                r.IsActive &&
                (
                    (currentRoleId == 1 && (r.RoleId == 2 || r.RoleId == 3))
                    ||
                    (currentRoleId == 2 && r.RoleId == 3)
                ))
                .OrderBy(r => r.RoleId)
                .ToList();

            ViewBag.RoleList = new SelectList(
                roles,
                "RoleId",
                "RoleName",
                selectedRoleId
            );
        }

        // GET: Staff
        public ActionResult Index(string keyword, int? roleId, int page = 1)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            int currentRoleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            if (currentRoleId != 1 && currentRoleId != 2)
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền truy cập chức năng này."
                );
            }

            // Giới hạn tài khoản được xem theo quyền
            var baseQuery = db.Accounts
                .Include("Role")
                .Where(a => a.RoleId == 2 || a.RoleId == 3);

            // Store Manager chỉ được xem Staff
            if (currentRoleId == 2)
            {
                baseQuery = baseQuery.Where(a => a.RoleId == 3);
            }

            // Thống kê tổng số tài khoản trước khi tìm kiếm và lọc
            ViewBag.TotalAccounts = baseQuery.Count();
            ViewBag.ActiveAccounts = baseQuery.Count(a => a.IsActive);
            ViewBag.LockedAccounts = baseQuery.Count(a => !a.IsActive);

            // Chuẩn hóa từ khóa tìm kiếm
            keyword = string.IsNullOrWhiteSpace(keyword)
                ? null
                : keyword.Trim();

            var query = baseQuery;

            // Tìm theo mã nhân viên, họ tên, tên đăng nhập hoặc email
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(a =>
                    a.EmployeeCode.Contains(keyword) ||
                    a.FullName.Contains(keyword) ||
                    a.Username.Contains(keyword) ||
                    a.Email.Contains(keyword));
            }

            // Lọc theo vai trò
            if (roleId.HasValue)
            {
                query = query.Where(a => a.RoleId == roleId.Value);
            }

            // Tổng số kết quả sau khi tìm kiếm và lọc
            int totalResults = query.Count();

            // Phân trang: 10 tài khoản mỗi trang
            int pageSize = 10;
            int totalPages = (int)Math.Ceiling(
                totalResults / (double)pageSize
            );

            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var accounts = query
                .OrderBy(a => a.AccountId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Danh sách vai trò được phép lọc
            var roles = db.Roles.Where(r =>
                r.IsActive &&
                (
                    (currentRoleId == 1 &&
                        (r.RoleId == 2 || r.RoleId == 3))
                    ||
                    (currentRoleId == 2 && r.RoleId == 3)
                ))
                .OrderBy(r => r.RoleId)
                .ToList();

            ViewBag.RoleFilterList = new SelectList(
                roles,
                "RoleId",
                "RoleName",
                roleId
            );

            // Dữ liệu phục vụ giao diện
            ViewBag.CurrentRoleId = currentRoleId;
            ViewBag.CurrentRoleName = currentRoleId == 1
                ? "Super Admin"
                : "Store Manager";

            ViewBag.Keyword = keyword;
            ViewBag.SelectedRoleId = roleId;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalResults = totalResults;

            return View(accounts);
        }


        // GET: Staff/Create
        public ActionResult Create()
        {
            int? roleId = GetCurrentRoleId();

            if (roleId == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (roleId != 1 && roleId != 2)
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền tạo tài khoản nhân viên."
                );
            }

            LoadRoleList(roleId.Value);

            return View();
        }

        // POST: Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(StaffCreateViewModel model)
        {
            int? currentRoleId = GetCurrentRoleId();

            if (currentRoleId == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (currentRoleId != 1 && currentRoleId != 2)
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền tạo tài khoản nhân viên."
                );
            }

            if (model == null)
                return new HttpStatusCodeResult(400);

            // Chuẩn hóa dữ liệu
            if (model.EmployeeCode != null)
                model.EmployeeCode = model.EmployeeCode.Trim();

            if (model.FullName != null)
                model.FullName = model.FullName.Trim();

            if (model.Username != null)
                model.Username = model.Username.Trim();

            if (model.Email != null)
                model.Email = model.Email.Trim();

            if (model.Phone != null)
                model.Phone = model.Phone.Trim();

            // Kiểm tra quyền tạo vai trò
            bool allowedRole =
                model.RoleId.HasValue &&
                (
                    (currentRoleId == 1 &&
                        (model.RoleId == 2 || model.RoleId == 3))
                    ||
                    (currentRoleId == 2 && model.RoleId == 3)
                );

            if (!allowedRole)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Bạn không được phép tạo tài khoản với vai trò này."
                );
            }
            else if (!db.Roles.Any(r =>
                r.RoleId == model.RoleId.Value && r.IsActive))
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Vai trò không tồn tại hoặc đã bị khóa."
                );
            }

            // Kiểm tra trùng tên đăng nhập
            if (!string.IsNullOrWhiteSpace(model.Username) &&
                db.Accounts.Any(a => a.Username == model.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã tồn tại."
                );
            }

            // Kiểm tra trùng mã nhân viên
            if (!string.IsNullOrWhiteSpace(model.EmployeeCode) &&
                db.Accounts.Any(a => a.EmployeeCode == model.EmployeeCode))
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Mã nhân viên đã tồn tại."
                );
            }

            if (!ModelState.IsValid)
            {
                LoadRoleList(currentRoleId.Value, model.RoleId);
                return View(model);
            }

            // Tạo tài khoản mới
            Account account = new Account();

            account.EmployeeCode = model.EmployeeCode;
            account.FullName = model.FullName;
            account.Username = model.Username;
            account.PasswordHash = PasswordHelper.HashPassword(model.Password);
            account.Email = string.IsNullOrWhiteSpace(model.Email)
                ? null : model.Email;
            account.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null : model.Phone;
            account.RoleId = model.RoleId.Value;
            account.IsActive = true;
            account.CreatedAt = DateTime.Now;

            db.Accounts.Add(account);
            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Tạo tài khoản nhân viên thành công!";

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }

        // POST: Staff/SavePermissions
        // Lưu và thu hồi quyền của nhân viên Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SavePermissions(
            int id,
            List<int> selectedPermissionIds)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            int currentRoleId =
                Convert.ToInt32(Session["MOCBREW_RoleId"]);

            int currentAccountId =
                Convert.ToInt32(Session["MOCBREW_AccountId"]);

            // 2. Chỉ Super Admin và Store Manager được thao tác
            if (currentRoleId != 1 && currentRoleId != 2)
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền phân quyền nhân viên."
                );
            }

            // 3. Kiểm tra tài khoản nhân viên
            var account = db.Accounts.FirstOrDefault(a =>
                a.AccountId == id && a.RoleId == 3);

            if (account == null)
            {
                return HttpNotFound("Không tìm thấy nhân viên Staff.");
            }

            // 4. Chuẩn hóa danh sách quyền được chọn
            selectedPermissionIds = selectedPermissionIds
                ?? new List<int>();

            selectedPermissionIds = selectedPermissionIds
                .Distinct()
                .ToList();

            // 5. Kiểm tra các PermissionId có hợp lệ không
            var validPermissionIds = db.Permissions
                .Select(p => p.PermissionId)
                .ToList();

            bool hasInvalidPermission = selectedPermissionIds
                .Any(permissionId =>
                    !validPermissionIds.Contains(permissionId));

            if (hasInvalidPermission)
            {
                TempData["ErrorMessage"] =
                    "Danh sách quyền không hợp lệ.";

                return RedirectToAction("Permissions",
                    new { id = id });
            }

            // 6. Lấy danh sách quyền đang được cấp
            var activePermissions = db.UserPermissions
                .Where(up =>
                    up.AccountId == id &&
                    up.IsActive)
                .ToList();

            var currentPermissionIds = activePermissions
                .Select(up => up.PermissionId)
                .Distinct()
                .ToList();

            // 7. Thu hồi những quyền đã bỏ chọn
            var permissionsToRevoke = activePermissions
                .Where(up =>
                    !selectedPermissionIds.Contains(up.PermissionId))
                .ToList();

            foreach (var permission in permissionsToRevoke)
            {
                permission.IsActive = false;
                permission.RevokedByAccountId = currentAccountId;
                permission.RevokedAt = DateTime.Now;
            }

            // 8. Cấp mới những quyền chưa được cấp
            var permissionsToGrant = selectedPermissionIds
                .Where(permissionId =>
                    !currentPermissionIds.Contains(permissionId))
                .ToList();

            foreach (int permissionId in permissionsToGrant)
            {
                var newPermission = new UserPermission
                {
                    AccountId = id,
                    PermissionId = permissionId,
                    GrantedByAccountId = currentAccountId,
                    GrantedAt = DateTime.Now,
                    IsActive = true
                };

                db.UserPermissions.Add(newPermission);
            }

            // 9. Ghi nhật ký hoạt động
            if (permissionsToGrant.Any() ||
                permissionsToRevoke.Any())
            {
                SystemLog log = new SystemLog
                {
                    AccountId = currentAccountId,
                    LogType = "BUSINESS",
                    ActionCode = "STAFF_PERMISSION_UPDATE",
                    Description =
                        "Cập nhật phân quyền cho nhân viên " +
                        account.FullName +
                        ". Cấp mới: " +
                        permissionsToGrant.Count +
                        " quyền; thu hồi: " +
                        permissionsToRevoke.Count +
                        " quyền.",
                    CreatedAt = DateTime.Now
                };

                db.SystemLogs.Add(log);
            }

            // 10. Lưu dữ liệu
            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Lưu phân quyền nhân viên thành công.";

            return RedirectToAction("Permissions",
                new { id = id });
        }


        // GET: Staff/Permissions/5
        // Hiển thị danh sách quyền của một nhân viên
        public ActionResult Permissions(int? id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            int currentRoleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            if (currentRoleId != 1 && currentRoleId != 2)
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền phân quyền nhân viên."
                );
            }

            if (id == null)
                return new HttpStatusCodeResult(400);

            // Chỉ phân quyền cho tài khoản Staff
            var account = db.Accounts.FirstOrDefault(a =>
                a.AccountId == id.Value && a.RoleId == 3);

            if (account == null)
                return HttpNotFound("Không tìm thấy nhân viên Staff.");

            // Lấy toàn bộ quyền đã được khai báo trong hệ thống
            var allPermissions = db.Permissions
                .OrderBy(p => p.PermissionId)
                .ToList();

            // Lấy những quyền đang được cấp cho nhân viên
            var grantedPermissionIds = db.UserPermissions
                .Where(up =>
                    up.AccountId == account.AccountId &&
                    up.IsActive)
                .Select(up => up.PermissionId)
                .ToList();

            // Tạo ViewModel
            var model = new StaffPermissionViewModel
            {
                AccountId = account.AccountId,
                EmployeeCode = account.EmployeeCode,
                FullName = account.FullName,
                Username = account.Username
            };

            foreach (var permission in allPermissions)
            {
                model.Permissions.Add(
                    new StaffPermissionItemViewModel
                    {
                        PermissionId = permission.PermissionId,
                        PermissionCode = permission.PermissionCode,
                        PermissionName = permission.PermissionName,
                        Description = permission.Description,
                        IsGranted = grantedPermissionIds.Contains(
                            permission.PermissionId)
                    }
                );
            }

            ViewBag.CurrentRoleId = currentRoleId;
            ViewBag.CurrentRoleName = currentRoleId == 1
                ? "Super Admin"
                : "Store Manager";

            return View(model);
        }



        // GET: Staff/Edit/5
        public ActionResult Edit(int? id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            int currentRoleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            if (currentRoleId != 1 && currentRoleId != 2)
                return new HttpStatusCodeResult(403);

            if (id == null)
                return new HttpStatusCodeResult(400);

            var account = db.Accounts.Find(id);

            if (account == null)
                return HttpNotFound();

            // Super Admin chỉ sửa Manager và Staff.
            // Store Manager chỉ sửa Staff.
            bool allowed =
                (currentRoleId == 1 &&
                    (account.RoleId == 2 || account.RoleId == 3))
                ||
                (currentRoleId == 2 && account.RoleId == 3);

            if (!allowed)
                return new HttpStatusCodeResult(403,
                    "Bạn không được phép chỉnh sửa tài khoản này.");

            var model = new StaffEditViewModel
            {
                AccountId = account.AccountId,
                EmployeeCode = account.EmployeeCode,
                FullName = account.FullName,
                Username = account.Username,
                Email = account.Email,
                Phone = account.Phone,
                RoleId = account.RoleId
            };

            LoadRoleList(currentRoleId, account.RoleId);

            return View(model);
        }

        // POST: Staff/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(StaffEditViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            int currentRoleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            if (currentRoleId != 1 && currentRoleId != 2)
                return new HttpStatusCodeResult(403);

            if (model == null)
                return new HttpStatusCodeResult(400);

            var account = db.Accounts.Find(model.AccountId);

            if (account == null)
                return HttpNotFound();

            bool allowed =
                (currentRoleId == 1 &&
                    (account.RoleId == 2 || account.RoleId == 3))
                ||
                (currentRoleId == 2 && account.RoleId == 3);

            if (!allowed)
                return new HttpStatusCodeResult(403,
                    "Bạn không được phép chỉnh sửa tài khoản này.");

            // Chuẩn hóa dữ liệu
            if (model.EmployeeCode != null)
                model.EmployeeCode = model.EmployeeCode.Trim();

            if (model.FullName != null)
                model.FullName = model.FullName.Trim();

            if (model.Username != null)
                model.Username = model.Username.Trim();

            if (model.Email != null)
                model.Email = model.Email.Trim();

            if (model.Phone != null)
                model.Phone = model.Phone.Trim();

            // Kiểm tra quyền thay đổi vai trò
            bool allowedRole =
                model.RoleId.HasValue &&
                (
                    (currentRoleId == 1 &&
                        (model.RoleId == 2 || model.RoleId == 3))
                    ||
                    (currentRoleId == 2 && model.RoleId == 3)
                );

            if (!allowedRole)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Bạn không được phép chọn vai trò này.");
            }
            else if (!db.Roles.Any(r =>
                r.RoleId == model.RoleId.Value && r.IsActive))
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Vai trò không tồn tại hoặc đã bị khóa.");
            }

            // Kiểm tra tên đăng nhập trùng
            if (!string.IsNullOrWhiteSpace(model.Username) &&
                db.Accounts.Any(a =>
                    a.Username == model.Username &&
                    a.AccountId != model.AccountId))
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã được sử dụng.");
            }

            // Kiểm tra mã nhân viên trùng
            if (!string.IsNullOrWhiteSpace(model.EmployeeCode) &&
                db.Accounts.Any(a =>
                    a.EmployeeCode == model.EmployeeCode &&
                    a.AccountId != model.AccountId))
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Mã nhân viên đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                LoadRoleList(currentRoleId, model.RoleId);
                return View(model);
            }

            // Cập nhật thông tin
            account.EmployeeCode = model.EmployeeCode;
            account.FullName = model.FullName;
            account.Username = model.Username;
            account.Email = string.IsNullOrWhiteSpace(model.Email)
                ? null : model.Email;
            account.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null : model.Phone;
            account.RoleId = model.RoleId.Value;

            // Chỉ đổi mật khẩu khi người dùng nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                account.PasswordHash =
                    PasswordHelper.HashPassword(model.NewPassword);
            }

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Cập nhật thông tin nhân viên thành công!";

            return RedirectToAction("Index");
        }

    }
}
