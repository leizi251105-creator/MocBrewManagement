
using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using MocBrewManagement.Models;

namespace MocBrewManagement.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly MOCBREWEntities db = new MOCBREWEntities();

        // Kiểm tra quyền quản lý danh mục
        private bool CanManageCategories()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            // Store Manager được quản lý danh mục
            if (roleId == 2)
                return true;

            // Staff cần được cấp quyền MENU_MANAGE
            if (roleId == 3)
            {
                int accountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                return db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    up.Permission.PermissionCode == "MENU_MANAGE");
            }

            return false;
        }

        // GET: Categories
        public ActionResult Index()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageCategories())
                return RedirectToAction("Index", "Home");

            var categories = db.Categories
                .Include(c => c.Category1)
                .Include(c => c.Categories1)
                .OrderBy(c => c.CategoryId)
                .ToList();

            return View(categories);
        }

        // GET: Categories/Create
        public ActionResult Create()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageCategories())
                return RedirectToAction("Index", "Home");

            var model = new CategoryCreateViewModel
            {
                IsActive = true,
                ParentCategories = db.Categories
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToList()
            };

            return View(model);
        }


        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryCreateViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageCategories())
                return RedirectToAction("Index", "Home");

            if (model.ParentCategoryId.HasValue)
            {
                bool parentExists = db.Categories.Any(c =>
                    c.CategoryId == model.ParentCategoryId.Value &&
                    c.ParentCategoryId == null &&
                    c.IsActive);

                if (!parentExists)
                {
                    ModelState.AddModelError(
                        "ParentCategoryId",
                        "Danh mục cha không hợp lệ hoặc đã ngừng hoạt động.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.ParentCategories = db.Categories
                    .Where(c => c.ParentCategoryId == null && c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToList();

                return View(model);
            }

            var category = new Category
            {
                CategoryName = model.CategoryName.Trim(),
                Description = model.Description,
                ParentCategoryId = model.ParentCategoryId,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);
            db.SaveChanges();

            TempData["CategorySuccess"] = "Thêm danh mục thành công!";

            return RedirectToAction("Index");
        }

        // ==========================================
        // EDIT - HIỂN THỊ FORM CHỈNH SỬA DANH MỤC
        // ==========================================

        public ActionResult Edit(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageCategories())
                return RedirectToAction("Index", "Home");

            var category = db.Categories
                .FirstOrDefault(c => c.CategoryId == id);

            if (category == null)
                return HttpNotFound();

            var model = new CategoryEditViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                IsActive = category.IsActive,

                ParentCategories = db.Categories
                    .Where(c =>
                        c.IsActive &&
                        c.ParentCategoryId == null &&
                        c.CategoryId != id)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToList()
            };

            return View(model);
        }


        // ==========================================
        // EDIT - LƯU THÔNG TIN DANH MỤC
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CategoryEditViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageCategories())
                return RedirectToAction("Index", "Home");

            var category = db.Categories
                .FirstOrDefault(c => c.CategoryId == model.CategoryId);

            if (category == null)
                return HttpNotFound();

            // Kiểm tra danh mục cha được chọn
            if (model.ParentCategoryId.HasValue)
            {
                if (model.ParentCategoryId.Value == model.CategoryId)
                {
                    ModelState.AddModelError(
                        "ParentCategoryId",
                        "Danh mục không thể là danh mục cha của chính nó.");
                }
                else
                {
                    var parentCategory = db.Categories.FirstOrDefault(c =>
                        c.CategoryId == model.ParentCategoryId.Value &&
                        c.IsActive &&
                        c.ParentCategoryId == null);

                    if (parentCategory == null)
                    {
                        ModelState.AddModelError(
                            "ParentCategoryId",
                            "Danh mục cha không hợp lệ hoặc đã ngừng hoạt động.");
                    }
                }
            }

            // Không cho danh mục cha có danh mục con chuyển thành danh mục con
            bool hasChildren = db.Categories.Any(c =>
                c.ParentCategoryId == category.CategoryId);

            if (hasChildren && model.ParentCategoryId.HasValue)
            {
                ModelState.AddModelError(
                    "ParentCategoryId",
                    "Danh mục đang có danh mục con nên không thể chuyển thành danh mục con.");
            }

            // Không cho ngừng hoạt động danh mục cha khi còn danh mục con hoạt động
            if (!model.IsActive && hasChildren)
            {
                bool hasActiveChildren = db.Categories.Any(c =>
                    c.ParentCategoryId == category.CategoryId &&
                    c.IsActive);

                if (hasActiveChildren)
                {
                    ModelState.AddModelError(
                        "IsActive",
                        "Vui lòng ngừng hoạt động các danh mục con trước.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.ParentCategories = db.Categories
                    .Where(c =>
                        c.IsActive &&
                        c.ParentCategoryId == null &&
                        c.CategoryId != model.CategoryId)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToList();

                return View(model);
            }

            try
            {
                category.CategoryName = model.CategoryName.Trim();
                category.Description = model.Description;
                category.ParentCategoryId = model.ParentCategoryId;
                category.IsActive = model.IsActive;

                db.SaveChanges();

                TempData["CategorySuccess"] =
                    "Cập nhật danh mục thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật danh mục: " +
                    ex.GetBaseException().Message);

                model.ParentCategories = db.Categories
                    .Where(c =>
                        c.IsActive &&
                        c.ParentCategoryId == null &&
                        c.CategoryId != model.CategoryId)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToList();

                return View(model);
            }
        }


        // ==========================================
        // TOGGLE STATUS - BẬT / NGỪNG HOẠT ĐỘNG DANH MỤC
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageCategories())
                return RedirectToAction("Index", "Home");

            var category = db.Categories
                .FirstOrDefault(c => c.CategoryId == id);

            if (category == null)
                return HttpNotFound();

            // Trường hợp đang hoạt động: chuyển sang ngừng hoạt động
            if (category.IsActive)
            {
                // Không cho ngừng danh mục cha khi còn danh mục con hoạt động
                bool hasActiveChildren = db.Categories.Any(c =>
                    c.ParentCategoryId == category.CategoryId &&
                    c.IsActive);

                if (hasActiveChildren)
                {
                    TempData["CategoryError"] =
                        "Không thể ngừng hoạt động danh mục cha khi vẫn còn danh mục con đang hoạt động.";

                    return RedirectToAction("Index");
                }

                // Không cho ngừng danh mục khi còn sản phẩm đang hoạt động
                bool hasActiveProducts = db.Products.Any(p =>
                    p.CategoryId == category.CategoryId &&
                    p.IsActive);

                if (hasActiveProducts)
                {
                    TempData["CategoryError"] =
                        "Không thể ngừng hoạt động danh mục vì vẫn còn sản phẩm đang hoạt động thuộc danh mục này.";

                    return RedirectToAction("Index");
                }

                category.IsActive = false;
                TempData["CategorySuccess"] =
                    "Đã ngừng hoạt động danh mục thành công!";
            }
            else
            {
                // Không cho kích hoạt danh mục con nếu danh mục cha đang ngừng hoạt động
                if (category.ParentCategoryId.HasValue)
                {
                    bool parentIsActive = db.Categories.Any(c =>
                        c.CategoryId == category.ParentCategoryId.Value &&
                        c.IsActive);

                    if (!parentIsActive)
                    {
                        TempData["CategoryError"] =
                            "Không thể kích hoạt danh mục con khi danh mục cha đang ngừng hoạt động.";

                        return RedirectToAction("Index");
                    }
                }

                category.IsActive = true;

                TempData["CategorySuccess"] =
                    "Đã kích hoạt danh mục thành công!";
            }

            db.SaveChanges();

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
