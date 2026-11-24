
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MocBrewManagement.Controllers
{
    public class IngredientsController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        // ==========================================
        // QUYỀN XEM KHO NGUYÊN LIỆU
        // ==========================================
        private bool CanViewInventory()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(
                Session["MOCBREW_RoleId"]);

            // Super Admin
            if (roleId == 1)
                return true;

            // Store Manager
            if (roleId == 2)
                return true;

            // Staff cần quyền INVENTORY_VIEW
            if (roleId == 3)
            {
                int accountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                return db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    up.Permission.PermissionCode == "INVENTORY_VIEW");
            }

            return false;
        }

        // ==========================================
        // QUYỀN KIỂM KÊ KHO NGUYÊN LIỆU
        // SUPER ADMIN + STORE MANAGER
        // STAFF CẦN QUYỀN INVENTORY_STOCKTAKE
        // ==========================================
        private bool CanStocktakeInventory()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(
                Session["MOCBREW_RoleId"]);

            // Super Admin
            if (roleId == 1)
                return true;

            // Store Manager
            if (roleId == 2)
                return true;

            // Staff cần quyền kiểm kê
            if (roleId == 3)
            {
                int accountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                return db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    up.Permission.PermissionCode == "INVENTORY_STOCKTAKE");
            }

            return false;
        }

        // ==========================================
        // QUYỀN THÊM, SỬA, NGỪNG NGUYÊN LIỆU
        // CHỈ SUPER ADMIN
        // ==========================================
        private bool CanManageIngredients()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(
                Session["MOCBREW_RoleId"]);

            return roleId == 1;
        }

        // ==========================================
        // TRANG QUẢN LÝ NGUYÊN LIỆU
        // ==========================================

        public ActionResult Index(string search, string status = "all")
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!CanViewInventory())
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách nguyên liệu
            var query = db.Ingredients.AsQueryable();

            // Tìm kiếm theo tên nguyên liệu
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.IngredientName.Contains(search));
            }

            // Lọc theo trạng thái
            if (status == "active")
            {
                query = query.Where(i => i.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(i => !i.IsActive);
            }

            // Chuyển dữ liệu sang ViewModel
            var ingredients = query
                .OrderBy(i => i.IngredientName)
                .Select(i => new IngredientViewModel
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CurrentStock = i.CurrentStock,
                    MinimumStock = i.MinimumStock,
                    Description = i.Description,
                    IsActive = i.IsActive
                })
                .ToList();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(ingredients);
        }



        // ==========================================
        // GET: THÊM NGUYÊN LIỆU
        // ==========================================
        public ActionResult Create()
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!CanManageIngredients())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }



        // ==========================================
        // POST: LƯU NGUYÊN LIỆU MỚI
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IngredientCreateViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!CanManageIngredients())
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int accountId = Convert.ToInt32(
                        Session["MOCBREW_AccountId"]);

                    // Tạo nguyên liệu mới
                    var ingredient = new Ingredient
                    {
                        IngredientName = model.IngredientName.Trim(),
                        Unit = model.Unit.Trim(),
                        CurrentStock = model.CurrentStock,
                        MinimumStock = model.MinimumStock,
                        Description = string.IsNullOrWhiteSpace(model.Description)
                            ? null
                            : model.Description.Trim(),
                        IsActive = true
                    };

                    db.Ingredients.Add(ingredient);

                    // Lưu để lấy IngredientId
                    db.SaveChanges();

                    // Nếu có tồn kho ban đầu, ghi nhận giao dịch nhập kho
                    if (model.CurrentStock > 0)
                    {
                        var inventoryTransaction = new InventoryTransaction
                        {
                            IngredientId = ingredient.IngredientId,
                            AccountId = accountId,
                            TransactionType = "Import",
                            Quantity = model.CurrentStock,
                            TransactionDate = DateTime.Now,
                            ReferenceCode = "INIT-" + ingredient.IngredientId,
                            Note = "Ghi nhận số lượng tồn kho ban đầu khi tạo nguyên liệu."
                        };

                        db.InventoryTransactions.Add(inventoryTransaction);
                        db.SaveChanges();
                    }

                    transaction.Commit();

                    TempData["SuccessMessage"] =
                        "Thêm nguyên liệu thành công!";

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    ModelState.AddModelError("",
                        "Không thể lưu nguyên liệu. Vui lòng kiểm tra dữ liệu và thử lại.");

                    return View(model);
                }
            }
        }


        // ==========================================
        // GET: SỬA THÔNG TIN NGUYÊN LIỆU
        // CHỈ SUPER ADMIN
        // ==========================================
        public ActionResult Edit(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageIngredients())
                return RedirectToAction("Index", "Home");

            var ingredient = db.Ingredients
                .FirstOrDefault(i => i.IngredientId == id);

            if (ingredient == null)
                return HttpNotFound();

            var model = new IngredientEditViewModel
            {
                IngredientId = ingredient.IngredientId,
                IngredientName = ingredient.IngredientName,
                Unit = ingredient.Unit,
                MinimumStock = ingredient.MinimumStock,
                Description = ingredient.Description
            };

            return View(model);
        }

        // ==========================================
        // POST: LƯU THÔNG TIN NGUYÊN LIỆU
        // CHỈ SUPER ADMIN
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(IngredientEditViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageIngredients())
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return View(model);

            var ingredient = db.Ingredients
                .FirstOrDefault(i => i.IngredientId == model.IngredientId);

            if (ingredient == null)
                return HttpNotFound();

            ingredient.IngredientName = model.IngredientName.Trim();
            ingredient.Unit = model.Unit.Trim();
            ingredient.MinimumStock = model.MinimumStock;
            ingredient.Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật nguyên liệu thành công!";

            return RedirectToAction("Index");
        }

        // ==========================================
        // POST: NGỪNG HOẠT ĐỘNG / MỞ LẠI NGUYÊN LIỆU
        // CHỈ SUPER ADMIN
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageIngredients())
                return RedirectToAction("Index", "Home");

            var ingredient = db.Ingredients
                .FirstOrDefault(i => i.IngredientId == id);

            if (ingredient == null)
                return HttpNotFound();

            ingredient.IsActive = !ingredient.IsActive;

            db.SaveChanges();

            TempData["SuccessMessage"] = ingredient.IsActive
                ? "Đã mở lại nguyên liệu thành công!"
                : "Đã ngừng hoạt động nguyên liệu thành công!";

            return RedirectToAction("Index");
        }



        // ==========================================
        // GET: MÀN HÌNH KIỂM KÊ KHO
        // ==========================================
        public ActionResult Stocktake()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanStocktakeInventory())
                return RedirectToAction("Index", "Ingredients");

            var ingredients = db.Ingredients
                .Where(i => i.IsActive)
                .OrderBy(i => i.IngredientName)
                .ToList();

            var model = new StocktakeViewModel
            {
                StocktakeDate = DateTime.Now,
                Items = ingredients.Select(i => new StocktakeItemViewModel
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    SystemStock = i.CurrentStock,
                    ActualStock = i.CurrentStock
                }).ToList()
            };

            return View(model);
        }

        // ==========================================
        // POST: LƯU KẾT QUẢ KIỂM KÊ KHO
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Stocktake(StocktakeViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanStocktakeInventory())
                return RedirectToAction("Index", "Ingredients");

            if (model == null || model.Items == null || !model.Items.Any())
            {
                TempData["ErrorMessage"] = "Không có dữ liệu kiểm kê.";
                return RedirectToAction("Stocktake");
            }

            foreach (var item in model.Items)
            {
                if (item.ActualStock < 0)
                    ModelState.AddModelError("", "Số lượng thực tế không được âm.");

                if (item.ActualStock > 999999999999999.999m)
                    ModelState.AddModelError("", "Số lượng thực tế vượt giới hạn cho phép.");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Dữ liệu kiểm kê không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Stocktake");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int accountId =
                        Convert.ToInt32(Session["MOCBREW_AccountId"]);

                    var ingredientIds =
                        model.Items.Select(x => x.IngredientId).ToList();

                    if (ingredientIds.Distinct().Count() != ingredientIds.Count)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] =
                            "Dữ liệu kiểm kê bị trùng nguyên liệu.";
                        return RedirectToAction("Stocktake");
                    }

                    // ==========================================
                    // 1. TẠO PHIẾU KIỂM KÊ CHUNG
                    // ==========================================

                    DateTime stocktakeDate = DateTime.Now;

                    var stocktakeSession = new InventoryStocktakeSession
                    {
                        StocktakeCode = "KK-" +
                            stocktakeDate.ToString("yyyyMMdd-HHmmssfff"),

                        AccountId = accountId,
                        StocktakeDate = stocktakeDate,

                        Note = string.IsNullOrWhiteSpace(model.Note)
                            ? null
                            : model.Note.Trim()
                    };

                    db.InventoryStocktakeSessions.Add(stocktakeSession);
                    db.SaveChanges();

                    // Sau SaveChanges, hệ thống đã có SessionId
                    // để liên kết các dòng chi tiết với phiếu.

                    // ==========================================
                    // 2. LƯU CHI TIẾT TỪNG NGUYÊN LIỆU
                    // ==========================================

                    foreach (var item in model.Items)
                    {
                        var ingredient = db.Ingredients
                            .FirstOrDefault(i =>
                                i.IngredientId == item.IngredientId &&
                                i.IsActive);

                        if (ingredient == null)
                            throw new Exception(
                                "Có nguyên liệu không tồn tại hoặc đã ngừng hoạt động.");

                        decimal systemStock = ingredient.CurrentStock;
                        decimal actualStock = item.ActualStock;
                        decimal difference = actualStock - systemStock;

                        long? inventoryTransactionId = null;

                        // ==========================================
                        // 3. TẠO GIAO DỊCH ĐIỀU CHỈNH NẾU CÓ CHÊNH LỆCH
                        // ==========================================

                        if (difference != 0)
                        {
                            var inventoryTransaction =
                                new InventoryTransaction
                                {
                                    IngredientId = ingredient.IngredientId,
                                    AccountId = accountId,
                                    TransactionType = "Adjustment",
                                    Quantity = Math.Abs(difference),
                                    TransactionDate = stocktakeDate,

                                    ReferenceCode =
                                        stocktakeSession.StocktakeCode,

                                    Note = difference > 0
                                        ? "Kiểm kê thừa " +
                                          difference.ToString("N3") +
                                          " " + ingredient.Unit
                                        : "Kiểm kê thiếu " +
                                          Math.Abs(difference).ToString("N3") +
                                          " " + ingredient.Unit
                                };

                            db.InventoryTransactions.Add(inventoryTransaction);
                            db.SaveChanges();

                            inventoryTransactionId =
                                inventoryTransaction.TransactionId;
                        }

                        // ==========================================
                        // 4. CẬP NHẬT TỒN KHO
                        // ==========================================

                        ingredient.CurrentStock = actualStock;

                        // ==========================================
                        // 5. LƯU DÒNG CHI TIẾT PHIẾU KIỂM KÊ
                        // ==========================================

                        var stocktake = new InventoryStocktake
                        {
                            SessionId = stocktakeSession.SessionId,

                            IngredientId = ingredient.IngredientId,
                            AccountId = accountId,

                            SystemStock = systemStock,
                            ActualStock = actualStock,
                            Difference = difference,

                            StocktakeDate = stocktakeDate,
                            TransactionId = inventoryTransactionId,

                            Note = string.IsNullOrWhiteSpace(item.Note)
                                ? null
                                : item.Note.Trim()
                        };

                        db.InventoryStocktakes.Add(stocktake);
                        db.SaveChanges();
                    }

                    // ==========================================
                    // 6. HOÀN TẤT PHIẾU KIỂM KÊ
                    // ==========================================

                    transaction.Commit();

                    TempData["SuccessMessage"] =
                        "Kiểm kê kho thành công! Mã phiếu: " +
                        stocktakeSession.StocktakeCode;

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    TempData["ErrorMessage"] =
                        "Không thể lưu kết quả kiểm kê. Vui lòng kiểm tra dữ liệu và thử lại.";

                    return RedirectToAction("Stocktake");
                }
            }
        }



        // ==========================================
        // GET: DANH SÁCH PHIẾU KIỂM KÊ
        // ==========================================
        public ActionResult StocktakeHistory()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            // Chỉ Super Admin và Store Manager được xem
            if (roleId != 1 && roleId != 2)
                return RedirectToAction("Index", "Ingredients");

            var sessions = db.InventoryStocktakeSessions
                .OrderByDescending(s => s.StocktakeDate)
                .ToList();

            var model = new List<StocktakeHistoryItemViewModel>();

            foreach (var session in sessions)
            {
                var account = db.Accounts
                    .FirstOrDefault(a => a.AccountId == session.AccountId);

                var details = db.InventoryStocktakes
                    .Where(x => x.SessionId == session.SessionId)
                    .ToList();

                model.Add(new StocktakeHistoryItemViewModel
                {
                    SessionId = session.SessionId,
                    StocktakeCode = session.StocktakeCode,
                    StocktakeDate = session.StocktakeDate,

                    Username = account != null ? account.Username : "Không xác định",
                    FullName = account != null ? account.FullName : "",

                    TotalIngredients = details.Count,

                    ChangedIngredients = details.Count(
                        x => x.Difference != 0)
                });
            }

            return View(model);
        }


        // ==========================================
        // GET: CHI TIẾT PHIẾU KIỂM KÊ
        // ==========================================
        public ActionResult StocktakeDetails(long id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            // Chỉ Super Admin và Store Manager được xem
            if (roleId != 1 && roleId != 2)
                return RedirectToAction("Index", "Ingredients");

            var session = db.InventoryStocktakeSessions
                .FirstOrDefault(s => s.SessionId == id);

            if (session == null)
            {
                TempData["ErrorMessage"] =
                    "Không tìm thấy phiếu kiểm kê.";

                return RedirectToAction("StocktakeHistory");
            }

            var account = db.Accounts
                .FirstOrDefault(a => a.AccountId == session.AccountId);

            var details = db.InventoryStocktakes
                .Where(x => x.SessionId == session.SessionId)
                .OrderBy(x => x.IngredientId)
                .ToList();

            var model = new StocktakeDetailViewModel
            {
                SessionId = session.SessionId,
                StocktakeCode = session.StocktakeCode,
                StocktakeDate = session.StocktakeDate,

                Username = account != null ? account.Username : "Không xác định",
                FullName = account != null ? account.FullName : "",

                SessionNote = session.Note,

                TotalIngredients = details.Count,

                ChangedIngredients = details.Count(
                    x => x.Difference != 0),

                UnchangedIngredients = details.Count(
                    x => x.Difference == 0)
            };

            foreach (var item in details)
            {
                var ingredient = db.Ingredients
                    .FirstOrDefault(i =>
                        i.IngredientId == item.IngredientId);

                model.Items.Add(new StocktakeDetailItemViewModel
                {
                    IngredientName = ingredient != null
                        ? ingredient.IngredientName
                        : "Nguyên liệu không tồn tại",

                    Unit = ingredient != null
                        ? ingredient.Unit
                        : "",

                    SystemStock = item.SystemStock,
                    ActualStock = item.ActualStock,
                    Difference = item.Difference,
                    Note = item.Note
                });
            }

            return View(model);
        }

    }
}




