
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;

namespace MocBrewManagement.Controllers
{
    public class RecipesController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        private int? CurrentRoleId
        {
            get
            {
                return Session["MOCBREW_RoleId"] == null
                    ? (int?)null
                    : Convert.ToInt32(Session["MOCBREW_RoleId"]);
            }
        }

        private int? CurrentAccountId
        {
            get
            {
                return Session["MOCBREW_AccountId"] == null
                    ? (int?)null
                    : Convert.ToInt32(Session["MOCBREW_AccountId"]);
            }
        }

        // Kiểm tra quyền xem công thức
        private bool CanViewRecipes()
        {
            if (CurrentRoleId == 1 || CurrentRoleId == 2)
                return true;

            if (CurrentRoleId != 3 || CurrentAccountId == null)
                return false;

            return db.UserPermissions.Any(x =>
                x.AccountId == CurrentAccountId.Value &&
                x.IsActive &&
                x.Permission.PermissionCode == "RECIPE_MANAGE");
        }

        // Kiểm tra quyền quản lý công thức
        private bool CanManageRecipes()
        {
            if (CurrentRoleId == 2)
                return true;

            if (CurrentRoleId != 3 || CurrentAccountId == null)
                return false;

            return db.UserPermissions.Any(x =>
                x.AccountId == CurrentAccountId.Value &&
                x.IsActive &&
                x.Permission.PermissionCode == "RECIPE_MANAGE");
        }

        private ActionResult AccessDenied()
        {
            return RedirectToAction("Index", "Home");
        }

        // DANH SÁCH CÔNG THỨC
        public ActionResult Index()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            if (!CanViewRecipes())
                return AccessDenied();
            ViewBag.CanManageRecipes = CanManageRecipes();
            var recipes = db.Recipes
                .Include(x => x.ProductSize.Product)
                .Include(x => x.ProductSize.Size)
                .Include(x => x.RecipeDetails)
                .OrderBy(x => x.ProductSize.Product.ProductName)
                .ThenBy(x => x.ProductSize.SizeId)
                .ToList();

            var model = recipes.Select(x => new RecipeViewModel
            {
                RecipeId = x.RecipeId,
                ProductSizeId = x.ProductSizeId,
                ProductId = x.ProductSize.ProductId,
                ProductName = x.ProductSize.Product.ProductName,
                SizeName = x.ProductSize.Size.SizeName,
                Description = x.Description,
                IsActive = x.IsActive,
                TotalIngredients = x.RecipeDetails.Count
            }).ToList();

            return View(model);
        }

        // TẠO CÔNG THỨC
        public ActionResult Create()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            if (!CanManageRecipes())
                return AccessDenied();

            LoadProductSizes();
            LoadIngredients();

            var model = new RecipeCreateEditViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RecipeCreateEditViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            if (!CanManageRecipes())
                return AccessDenied();

            ValidateRecipe(model);

            if (!ModelState.IsValid)
            {
                LoadProductSizes(model.ProductSizeId);
                LoadIngredients();
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var productSize = db.ProductSizes
                        .FirstOrDefault(x => x.ProductSizeId == model.ProductSizeId
                            && x.IsAvailable);

                    if (productSize == null)
                    {
                        ModelState.AddModelError("", "Sản phẩm hoặc size không hợp lệ.");
                        transaction.Rollback();
                        LoadProductSizes(model.ProductSizeId);
                        LoadIngredients();
                        return View(model);
                    }

                    if (db.Recipes.Any(x => x.ProductSizeId == model.ProductSizeId))
                    {
                        ModelState.AddModelError("", "Sản phẩm và size này đã có công thức.");
                        transaction.Rollback();
                        LoadProductSizes(model.ProductSizeId);
                        LoadIngredients();
                        return View(model);
                    }

                    var recipe = new Recipe
                    {
                        ProductSizeId = model.ProductSizeId,
                        Description = model.Description,
                        IsActive = true
                    };

                    db.Recipes.Add(recipe);
                    db.SaveChanges();

                    foreach (var item in model.Ingredients)
                    {
                        var ingredient = db.Ingredients.FirstOrDefault(x =>
                            x.IngredientId == item.IngredientId && x.IsActive);

                        if (ingredient == null)
                            throw new Exception("Nguyên liệu không hợp lệ.");

                        db.RecipeDetails.Add(new RecipeDetail
                        {
                            RecipeId = recipe.RecipeId,
                            IngredientId = item.IngredientId,
                            QuantityRequired = item.QuantityRequired
                        });
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Tạo công thức thành công.";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Không thể tạo công thức. Vui lòng kiểm tra dữ liệu.");
                    LoadProductSizes(model.ProductSizeId);
                    LoadIngredients();
                    return View(model);
                }
            }
        }

        // SỬA CÔNG THỨC
        public ActionResult Edit(int? id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            if (!CanManageRecipes())
                return AccessDenied();

            if (id == null)
                return RedirectToAction("Index");

            var recipe = db.Recipes
                .Include(x => x.RecipeDetails)
                .FirstOrDefault(x => x.RecipeId == id.Value);

            if (recipe == null)
                return HttpNotFound();

            var model = new RecipeCreateEditViewModel
            {
                RecipeId = recipe.RecipeId,
                ProductSizeId = recipe.ProductSizeId,
                Description = recipe.Description,
                IsActive = recipe.IsActive,
                Ingredients = recipe.RecipeDetails.Select(x =>
                    new RecipeIngredientViewModel
                    {
                        IngredientId = x.IngredientId,
                        QuantityRequired = x.QuantityRequired
                    }).ToList()
            };

            LoadProductSizes(model.ProductSizeId);
            LoadIngredients();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RecipeCreateEditViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            if (!CanManageRecipes())
                return AccessDenied();

            ValidateRecipe(model);

            if (!ModelState.IsValid)
            {
                LoadProductSizes(model.ProductSizeId);
                LoadIngredients();
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var recipe = db.Recipes
                        .Include(x => x.RecipeDetails)
                        .FirstOrDefault(x => x.RecipeId == model.RecipeId);

                    if (recipe == null)
                        return HttpNotFound();

                    if (db.Recipes.Any(x =>
                        x.ProductSizeId == model.ProductSizeId &&
                        x.RecipeId != model.RecipeId))
                    {
                        ModelState.AddModelError("", "Sản phẩm và size đã có công thức khác.");
                        transaction.Rollback();
                        LoadProductSizes(model.ProductSizeId);
                        LoadIngredients();
                        return View(model);
                    }

                    recipe.ProductSizeId = model.ProductSizeId;
                    recipe.Description = model.Description;

                    db.RecipeDetails.RemoveRange(recipe.RecipeDetails.ToList());

                    foreach (var item in model.Ingredients)
                    {
                        var ingredient = db.Ingredients.FirstOrDefault(x =>
                            x.IngredientId == item.IngredientId && x.IsActive);

                        if (ingredient == null)
                            throw new Exception("Nguyên liệu không hợp lệ.");

                        db.RecipeDetails.Add(new RecipeDetail
                        {
                            RecipeId = recipe.RecipeId,
                            IngredientId = item.IngredientId,
                            QuantityRequired = item.QuantityRequired
                        });
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Cập nhật công thức thành công.";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Không thể cập nhật công thức.");
                    LoadProductSizes(model.ProductSizeId);
                    LoadIngredients();
                    return View(model);
                }
            }
        }

        // NGỪNG / MỞ LẠI CÔNG THỨC
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            if (!CanManageRecipes())
                return AccessDenied();

            var recipe = db.Recipes.Find(id);

            if (recipe == null)
                return HttpNotFound();

            recipe.IsActive = !recipe.IsActive;
            db.SaveChanges();

            TempData["SuccessMessage"] = recipe.IsActive
                ? "Đã mở lại công thức."
                : "Đã ngừng công thức.";

            return RedirectToAction("Index");
        }

        // KIỂM TRA DỮ LIỆU
        private void ValidateRecipe(RecipeCreateEditViewModel model)
        {
            if (model.Ingredients == null || model.Ingredients.Count == 0)
            {
                ModelState.AddModelError("Ingredients",
                    "Công thức phải có ít nhất một nguyên liệu.");
                return;
            }

            if (model.Ingredients.GroupBy(x => x.IngredientId)
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("Ingredients",
                    "Không được chọn trùng nguyên liệu trong một công thức.");
            }

            foreach (var item in model.Ingredients)
            {
                if (item.IngredientId <= 0)
                    ModelState.AddModelError("Ingredients",
                        "Vui lòng chọn nguyên liệu hợp lệ.");

                if (item.QuantityRequired <= 0)
                    ModelState.AddModelError("Ingredients",
                        "Định lượng nguyên liệu phải lớn hơn 0.");
            }
        }

        // DANH SÁCH SẢN PHẨM VÀ SIZE
        private void LoadProductSizes(int? selectedId = null)
        {
            var productSizes = db.ProductSizes
                .Include(x => x.Product)
                .Include(x => x.Size)
                .Where(x => x.IsAvailable)
                .OrderBy(x => x.Product.ProductName)
                .ThenBy(x => x.SizeId)
                .ToList();

            ViewBag.ProductSizes = new SelectList(
                productSizes.Select(x => new
                {
                    ProductSizeId = x.ProductSizeId,
                    DisplayName = x.Product.ProductName + " - " + x.Size.SizeName
                }),
                "ProductSizeId",
                "DisplayName",
                selectedId);
        }

        // DANH SÁCH NGUYÊN LIỆU
        private void LoadIngredients()
        {
            ViewBag.Ingredients = db.Ingredients
                .Where(x => x.IsActive)
                .OrderBy(x => x.IngredientName)
                .Select(x => new RecipeIngredientViewModel
                {
                    IngredientId = x.IngredientId,
                    IngredientName = x.IngredientName,
                    Unit = x.Unit,
                    QuantityRequired = 0
                })
                .ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
