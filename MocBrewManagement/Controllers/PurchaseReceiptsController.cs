
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;

namespace MocBrewManagement.Controllers
{
    public class PurchaseReceiptsController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        // =========================================
        // KIỂM TRA TÀI KHOẢN VÀ PHÂN QUYỀN
        // =========================================

        private int GetAccountId()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return 0;

            return Convert.ToInt32(Session["MOCBREW_AccountId"]);
        }

        private int GetRoleId()
        {
            if (Session["MOCBREW_RoleId"] == null)
                return 0;

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

        private bool CanCreate()
        {
            int roleId = GetRoleId();

            // Store Manager được thực hiện nghiệp vụ nhập hàng
            if (roleId == 2)
                return true;

            // Staff phải được cấp quyền
            if (roleId == 3)
                return HasPermission("PURCHASE_CREATE");

            return false;
        }

        private bool CanApprove()
        {
            int roleId = GetRoleId();

            // Manager được quyền duyệt phiếu nhập
            if (roleId == 2)
                return true;

            // Staff kiểm tra quyền theo tài khoản đang đăng nhập
            if (roleId == 3)
                return HasPermission("PURCHASE_APPROVE");

            // Super Admin không tham gia nghiệp vụ nhập hàng
            return false;
        }

        private bool CanView()
        {
            int roleId = GetRoleId();

            if (roleId == 2)
                return true;

            if (roleId == 3)
            {
                return HasPermission("PURCHASE_CREATE") ||
                       HasPermission("PURCHASE_APPROVE");
            }

            return false;
        }

        // =========================================
        // NẠP DANH SÁCH NHÀ CUNG CẤP VÀ NGUYÊN LIỆU
        // =========================================

        private void LoadDropdowns(PurchaseReceiptViewModel model)
        {
            model.Suppliers = db.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.SupplierName)
                .ToList()
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                }).ToList();

            model.Ingredients = db.Ingredients
                .Where(i => i.IsActive)
                .OrderBy(i => i.IngredientName)
                .ToList()
                .Select(i => new SelectListItem
                {
                    Value = i.IngredientId.ToString(),
                    Text = i.IngredientName + " (" + i.Unit + ")"
                }).ToList();
        }

        // =========================================
        // DANH SÁCH PHIẾU NHẬP
        // =========================================

        public ActionResult Index()
        {
            if (GetAccountId() == 0)
                return RedirectToAction("Login", "Account");

            if (!CanView())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền xem phiếu nhập hàng.");

            var receipts = db.PurchaseReceipts
                .OrderByDescending(r => r.ReceiptDate)
                .ToList();

            var result = new List<PurchaseReceiptViewModel>();

            foreach (var receipt in receipts)
            {
                var supplier = db.Suppliers.Find(receipt.SupplierId);
                var account = db.Accounts.Find(receipt.AccountId);

                result.Add(new PurchaseReceiptViewModel
                {
                    PurchaseReceiptId = receipt.PurchaseReceiptId,
                    ReceiptCode = receipt.ReceiptCode,
                    SupplierId = receipt.SupplierId,
                    SupplierName = supplier != null
                        ? supplier.SupplierName : "",
                    EmployeeName = account != null
                        ? account.FullName : "",
                    ReceiptDate = receipt.ReceiptDate,
                    TotalAmount = receipt.TotalAmount,
                    Note = receipt.Note,
                    Status = receipt.Status
                });
            }

            return View(result);
        }

        // =========================================
        // HIỂN THỊ FORM TẠO PHIẾU NHẬP
        // =========================================

        [HttpGet]
        public ActionResult Create()
        {
            if (GetAccountId() == 0)
                return RedirectToAction("Login", "Account");

            if (!CanCreate())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền tạo phiếu nhập.");

            var model = new PurchaseReceiptViewModel
            {
                ReceiptDate = DateTime.Today,
                Details = new List<PurchaseReceiptDetailViewModel>
                {
                    new PurchaseReceiptDetailViewModel()
                }
            };

            LoadDropdowns(model);

            return View(model);
        }

        // =========================================
        // LƯU PHIẾU NHẬP
        // =========================================


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PurchaseReceiptViewModel model)
        {
            if (GetAccountId() == 0)
                return RedirectToAction("Login", "Account");

            if (!CanCreate())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền tạo phiếu nhập.");

            // 1. Kiểm tra nhà cung cấp
            var supplier = db.Suppliers.FirstOrDefault(s =>
                s.SupplierId == model.SupplierId &&
                s.IsActive);

            if (supplier == null)
            {
                ModelState.AddModelError("SupplierId",
                    "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động.");
            }

            // 2. Kiểm tra danh sách nguyên liệu
            if (model.Details == null || !model.Details.Any())
            {
                ModelState.AddModelError("Details",
                    "Phiếu nhập phải có ít nhất một nguyên liệu.");
            }
            else
            {
                // Kiểm tra số lượng, đơn giá và mã nguyên liệu
                foreach (var detail in model.Details)
                {
                    if (detail == null ||
                        detail.IngredientId <= 0 ||
                        detail.Quantity <= 0 ||
                        detail.UnitPrice <= 0)
                    {
                        ModelState.AddModelError("Details",
                            "Vui lòng nhập nguyên liệu, số lượng và đơn giá hợp lệ.");
                        break;
                    }
                }

                // Không cho nhập trùng nguyên liệu
                if (model.Details.All(d => d != null))
                {
                    bool duplicated = model.Details
                        .GroupBy(d => d.IngredientId)
                        .Any(g => g.Count() > 1);

                    if (duplicated)
                    {
                        ModelState.AddModelError("Details",
                            "Một nguyên liệu không được xuất hiện nhiều lần trong cùng phiếu nhập.");
                    }
                }

                // 3. Lấy danh sách nguyên liệu được phép nhập từ nhà cung cấp
                if (supplier != null)
                {
                    var allowedIngredientIds = (
                        from si in db.SupplierIngredients
                        join i in db.Ingredients
                            on si.IngredientId equals i.IngredientId
                        where si.SupplierId == model.SupplierId
                              && si.IsActive
                              && i.IsActive
                        select si.IngredientId
                    ).ToList();

                    foreach (var detail in model.Details)
                    {
                        if (detail == null)
                            continue;

                        if (!allowedIngredientIds.Contains(detail.IngredientId))
                        {
                            ModelState.AddModelError("Details",
                                "Có nguyên liệu không thuộc danh sách cung cấp của nhà cung cấp đã chọn.");
                            break;
                        }
                    }
                }
            }

            // 4. Nếu dữ liệu không hợp lệ, hiển thị lại form
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }

            // 5. Lưu phiếu nhập
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    decimal totalAmount = model.Details.Sum(d =>
                        d.Quantity * d.UnitPrice);

                    var receipt = new PurchaseReceipt
                    {
                        ReceiptCode = "PN" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        SupplierId = model.SupplierId,
                        AccountId = GetAccountId(),
                        ReceiptDate = DateTime.Now,
                        TotalAmount = totalAmount,
                        Note = model.Note,
                        Status = "Pending"
                    };

                    db.PurchaseReceipts.Add(receipt);
                    db.SaveChanges();

                    foreach (var detail in model.Details)
                    {
                        var receiptDetail = new PurchaseReceiptDetail
                        {
                            PurchaseReceiptId = receipt.PurchaseReceiptId,
                            IngredientId = detail.IngredientId,
                            Quantity = detail.Quantity,
                            UnitPrice = detail.UnitPrice,
                            ExpiryDate = detail.ExpiryDate
                        };

                        db.PurchaseReceiptDetails.Add(receiptDetail);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] =
                        "Tạo phiếu nhập hàng thành công. Phiếu đang chờ duyệt.";

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    ModelState.AddModelError("",
                        "Không thể lưu phiếu nhập. Vui lòng kiểm tra lại dữ liệu.");

                    LoadDropdowns(model);
                    return View(model);
                }
            }
        }

        // =========================================
        // XEM CHI TIẾT PHIẾU NHẬP
        // =========================================

        public ActionResult Details(int id)
        {
            if (GetAccountId() == 0)
                return RedirectToAction("Login", "Account");

            if (!CanView())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền xem phiếu nhập.");

            var receipt = db.PurchaseReceipts.Find(id);

            if (receipt == null)
                return HttpNotFound();

            var supplier = db.Suppliers.Find(receipt.SupplierId);
            var account = db.Accounts.Find(receipt.AccountId);

            var model = new PurchaseReceiptViewModel
            {
                PurchaseReceiptId = receipt.PurchaseReceiptId,
                ReceiptCode = receipt.ReceiptCode,
                SupplierId = receipt.SupplierId,
                SupplierName = supplier != null
                    ? supplier.SupplierName : "",
                EmployeeName = account != null
                    ? account.FullName : "",
                ReceiptDate = receipt.ReceiptDate,
                TotalAmount = receipt.TotalAmount,
                Note = receipt.Note,
                Status = receipt.Status
            };

            var details = db.PurchaseReceiptDetails
                .Where(d => d.PurchaseReceiptId == id)
                .ToList();

            foreach (var detail in details)
            {
                var ingredient = db.Ingredients.Find(detail.IngredientId);

                model.Details.Add(new PurchaseReceiptDetailViewModel
                {
                    PurchaseReceiptDetailId =
                        detail.PurchaseReceiptDetailId,
                    IngredientId = detail.IngredientId,
                    IngredientName = ingredient != null
                        ? ingredient.IngredientName : "",
                    Unit = ingredient != null
                        ? ingredient.Unit : "",
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    ExpiryDate = detail.ExpiryDate
                });
            }
            ViewBag.CanApprove = CanApprove();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanApprove())
                return new HttpStatusCodeResult(403, "Bạn không có quyền duyệt phiếu nhập.");

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var receipt = db.PurchaseReceipts
                        .FirstOrDefault(x => x.PurchaseReceiptId == id);

                    if (receipt == null)
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Không tìm thấy phiếu nhập.";
                        return RedirectToAction("Index");
                    }

                    if (receipt.Status != "Pending")
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Phiếu nhập này đã được xử lý trước đó.";
                        return RedirectToAction("Details", new { id = id });
                    }

                    var details = db.PurchaseReceiptDetails
                        .Where(x => x.PurchaseReceiptId == id)
                        .ToList();

                    if (!details.Any())
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Phiếu nhập chưa có nguyên liệu.";
                        return RedirectToAction("Details", new { id = id });
                    }

                    int accountId = GetAccountId();

                    foreach (var item in details)
                    {
                        var ingredient = db.Ingredients
                            .FirstOrDefault(x => x.IngredientId == item.IngredientId);

                        if (ingredient == null || item.Quantity <= 0)
                        {
                            throw new Exception("Dữ liệu nguyên liệu không hợp lệ.");
                        }

                        // Cộng tồn kho
                        ingredient.CurrentStock += item.Quantity;

                        // Ghi nhận lịch sử nhập kho
                        db.InventoryTransactions.Add(new InventoryTransaction
                        {
                            IngredientId = item.IngredientId,
                            AccountId = accountId,
                            TransactionType = "Import",
                            Quantity = item.Quantity,
                            TransactionDate = DateTime.Now,
                            ReferenceCode = receipt.ReceiptCode,
                            Note = "Nhập kho từ phiếu nhập hàng " + receipt.ReceiptCode
                        });
                    }

                    // Cập nhật trạng thái phiếu
                    receipt.Status = "Approved";

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Duyệt phiếu nhập thành công. Tồn kho đã được cập nhật.";

                    return RedirectToAction("Details", new { id = id });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    TempData["Error"] = "Không thể duyệt phiếu nhập: " + ex.Message;

                    return RedirectToAction("Details", new { id = id });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            if (GetAccountId() <= 0)
                return RedirectToAction("Login", "Account");

            if (!CanApprove())
                return new HttpStatusCodeResult(403, "Bạn không có quyền từ chối phiếu nhập.");

            var receipt = db.PurchaseReceipts
                .FirstOrDefault(x => x.PurchaseReceiptId == id);

            if (receipt == null)
                return HttpNotFound();

            if (receipt.Status != "Pending")
            {
                TempData["Error"] = "Chỉ được từ chối phiếu đang chờ duyệt.";
                return RedirectToAction("Details", new { id = id });
            }

            receipt.Status = "Rejected";

            db.SaveChanges();

            TempData["Success"] = "Đã từ chối phiếu nhập. Tồn kho không thay đổi.";

            return RedirectToAction("Details", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }


        [HttpGet]
        public JsonResult GetIngredientsBySupplier(int supplierId)
        {
            if (GetAccountId() <= 0 || !CanCreate())
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn không có quyền thực hiện thao tác này."
                }, JsonRequestBehavior.AllowGet);
            }

            var supplier = db.Suppliers
                .FirstOrDefault(s => s.SupplierId == supplierId && s.IsActive);

            if (supplier == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động."
                }, JsonRequestBehavior.AllowGet);
            }

            var ingredients = (
                from si in db.SupplierIngredients
                join i in db.Ingredients
                    on si.IngredientId equals i.IngredientId
                where si.SupplierId == supplierId
                      && si.IsActive
                      && i.IsActive
                orderby i.IngredientName
                select new
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit
                }
            ).ToList();

            return Json(new
            {
                success = true,
                data = ingredients
            }, JsonRequestBehavior.AllowGet);
        }

    }
}

