
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;

namespace MocBrewManagement.Controllers
{
    public class SupplierIngredientsController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        private int GetAccountId()
        {
            return Convert.ToInt32(Session["MOCBREW_AccountId"]);
        }

        private int GetRoleId()
        {
            return Convert.ToInt32(Session["MOCBREW_RoleId"]);
        }

        private bool HasPermission(string permissionCode)
        {
            int accountId = GetAccountId();

            return db.UserPermissions.Any(up =>
                up.AccountId == accountId &&
                up.IsActive &&
                up.Permission.PermissionCode == permissionCode);
        }

        private bool CanManage()
        {
            int roleId = GetRoleId();

            if (roleId == 2)
                return true;

            if (roleId == 3)
                return HasPermission("SUPPLIER_MANAGE");

            return false;
        }

        // Kiểm tra đăng nhập và quyền
        private bool CheckAccess()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            return CanManage();
        }

        // GET: SupplierIngredients/Index/1
        public ActionResult Index(int id)
        {
            if (!CheckAccess())
                return RedirectToAction("Index", "Home");

            var supplier = db.Suppliers.Find(id);

            if (supplier == null)
                return HttpNotFound();

            var assignedIds = db.SupplierIngredients
                .Where(x => x.SupplierId == id && x.IsActive)
                .Select(x => x.IngredientId)
                .ToList();

            var ingredients = db.Ingredients
                .Where(x => x.IsActive)
                .OrderBy(x => x.IngredientName)
                .ToList();

            var model = new SupplierIngredientViewModel
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName,
                Ingredients = ingredients.Select(x =>
                    new SupplierIngredientItemViewModel
                    {
                        IngredientId = x.IngredientId,
                        IngredientName = x.IngredientName,
                        Unit = x.Unit,
                        IsSelected = assignedIds.Contains(x.IngredientId),
                        IsActive = assignedIds.Contains(x.IngredientId)
                    }).ToList()
            };

            return View(model);
        }

        // POST: SupplierIngredients/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(SupplierIngredientViewModel model)
        {
            if (!CheckAccess())
                return RedirectToAction("Index", "Home");

            var supplier = db.Suppliers.Find(model.SupplierId);

            if (supplier == null)
                return HttpNotFound();

            if (!supplier.IsActive)
            {
                TempData["Error"] =
                    "Không thể thiết lập nguyên liệu cho nhà cung cấp đã ngừng hoạt động.";

                return RedirectToAction("Index",
                    new { id = model.SupplierId });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var selectedIds = (model.Ingredients ??
                        new System.Collections.Generic.List<SupplierIngredientItemViewModel>())
                        .Where(x => x.IsSelected)
                        .Select(x => x.IngredientId)
                        .Distinct()
                        .ToList();

                    var activeIngredients = db.Ingredients
                        .Where(x => x.IsActive)
                        .Select(x => x.IngredientId)
                        .ToList();

                    if (selectedIds.Any(x => !activeIngredients.Contains(x)))
                    {
                        TempData["Error"] =
                            "Danh sách có nguyên liệu không hợp lệ.";

                        transaction.Rollback();

                        return RedirectToAction("Index",
                            new { id = model.SupplierId });
                    }

                    var existing = db.SupplierIngredients
                        .Where(x => x.SupplierId == model.SupplierId)
                        .ToList();

                    // Kích hoạt lại hoặc ngừng các liên kết cũ
                    foreach (var item in existing)
                    {
                        item.IsActive = selectedIds.Contains(item.IngredientId);
                    }

                    // Thêm liên kết mới
                    foreach (var ingredientId in selectedIds)
                    {
                        bool exists = existing.Any(x =>
                            x.IngredientId == ingredientId);

                        if (!exists)
                        {
                            db.SupplierIngredients.Add(
                                new SupplierIngredient
                                {
                                    SupplierId = model.SupplierId,
                                    IngredientId = ingredientId,
                                    IsActive = true,
                                    CreatedAt = DateTime.Now
                                });
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] =
                        "Đã cập nhật danh sách nguyên liệu cung cấp thành công.";

                    return RedirectToAction("Index",
                        new { id = model.SupplierId });
                }
                catch
                {
                    transaction.Rollback();

                    TempData["Error"] =
                        "Có lỗi xảy ra khi lưu danh sách nguyên liệu.";
                }
            }

            return RedirectToAction("Index",
                new { id = model.SupplierId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
