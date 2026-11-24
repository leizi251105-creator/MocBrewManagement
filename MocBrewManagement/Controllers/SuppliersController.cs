
using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;

namespace MocBrewManagement.Controllers
{
    public class SuppliersController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        private int GetAccountId()
        {
            return Session["MOCBREW_AccountId"] == null
                ? 0
                : Convert.ToInt32(Session["MOCBREW_AccountId"]);
        }

        private int GetRoleId()
        {
            return Session["MOCBREW_RoleId"] == null
                ? 0
                : Convert.ToInt32(Session["MOCBREW_RoleId"]);
        }

        private bool HasPermission(string permissionCode)
        {
            int accountId = GetAccountId();

            return db.UserPermissions.Any(up =>
                up.AccountId == accountId &&
                up.IsActive &&
                up.Permission.PermissionCode == permissionCode);
        }

        // Manager được quản lý nhà cung cấp.
        // Staff cần được cấp quyền SUPPLIER_MANAGE.
        // Super Admin không tham gia nghiệp vụ này.
        private bool CanManageSuppliers()
        {
            int roleId = GetRoleId();

            if (roleId == 2)
                return true;

            if (roleId == 3)
                return HasPermission("SUPPLIER_MANAGE");

            return false;
        }

        // DANH SÁCH NHÀ CUNG CẤP
        public ActionResult Index()
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanManageSuppliers())
                return new HttpStatusCodeResult(403);

            var suppliers = db.Suppliers
                .OrderByDescending(s => s.SupplierId)
                .Select(s => new SupplierViewModel
                {
                    SupplierId = s.SupplierId,
                    SupplierName = s.SupplierName,
                    Phone = s.Phone,
                    Email = s.Email,
                    Address = s.Address,
                    TaxCode = s.TaxCode,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                })
                .ToList();

            return View(suppliers);
        }

        // THÊM NHÀ CUNG CẤP - GET
        public ActionResult Create()
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanManageSuppliers())
                return new HttpStatusCodeResult(403);

            return View(new SupplierViewModel
            {
                IsActive = true
            });
        }

        // THÊM NHÀ CUNG CẤP - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SupplierViewModel model)
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanManageSuppliers())
                return new HttpStatusCodeResult(403);

            if (!ModelState.IsValid)
                return View(model);

            var supplier = new Supplier
            {
                SupplierName = model.SupplierName.Trim(),
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                TaxCode = string.IsNullOrWhiteSpace(model.TaxCode) ? null : model.TaxCode.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Suppliers.Add(supplier);
            db.SaveChanges();

            TempData["Success"] = "Thêm nhà cung cấp thành công.";

            return RedirectToAction("Index");
        }

        // SỬA NHÀ CUNG CẤP - GET
        public ActionResult Edit(int id)
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanManageSuppliers())
                return new HttpStatusCodeResult(403);

            var supplier = db.Suppliers.Find(id);

            if (supplier == null)
                return HttpNotFound();

            var model = new SupplierViewModel
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                TaxCode = supplier.TaxCode,
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt
            };

            return View(model);
        }

        // SỬA NHÀ CUNG CẤP - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SupplierViewModel model)
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanManageSuppliers())
                return new HttpStatusCodeResult(403);

            if (!ModelState.IsValid)
                return View(model);

            var supplier = db.Suppliers.Find(model.SupplierId);

            if (supplier == null)
                return HttpNotFound();

            supplier.SupplierName = model.SupplierName.Trim();
            supplier.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            supplier.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            supplier.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            supplier.TaxCode = string.IsNullOrWhiteSpace(model.TaxCode) ? null : model.TaxCode.Trim();

            db.SaveChanges();

            TempData["Success"] = "Cập nhật nhà cung cấp thành công.";

            return RedirectToAction("Index");
        }

        // NGỪNG HOẠT ĐỘNG / KÍCH HOẠT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanManageSuppliers())
                return new HttpStatusCodeResult(403);

            var supplier = db.Suppliers.Find(id);

            if (supplier == null)
                return HttpNotFound();

            // Không cho ngừng hoạt động nếu đang có phiếu nhập chờ duyệt
            if (supplier.IsActive)
            {
                bool hasPendingReceipt = db.PurchaseReceipts.Any(r =>
                    r.SupplierId == id && r.Status == "Pending");

                if (hasPendingReceipt)
                {
                    TempData["Error"] =
                        "Không thể ngừng hoạt động nhà cung cấp đang có phiếu nhập chờ duyệt.";

                    return RedirectToAction("Index");
                }
            }

            supplier.IsActive = !supplier.IsActive;

            db.SaveChanges();

            TempData["Success"] = supplier.IsActive
                ? "Đã kích hoạt nhà cung cấp."
                : "Đã ngừng hoạt động nhà cung cấp.";

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
