
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MocBrewManagement.Controllers
{
    public class ReportsController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        public ActionResult Index()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);
            int accountId = Convert.ToInt32(Session["MOCBREW_AccountId"]);

            bool canViewReports = false;

            // Manager được xem báo cáo
            if (roleId == 2)
            {
                canViewReports = true;
            }
            // Staff cần có ít nhất một quyền báo cáo
            else if (roleId == 3)
            {
                canViewReports = db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    (
                        up.Permission.PermissionCode == "REPORT_SHIFT_VIEW" ||
                        up.Permission.PermissionCode == "REPORT_REVENUE_VIEW" ||
                        up.Permission.PermissionCode == "REPORT_PROFIT_VIEW" ||
                        up.Permission.PermissionCode == "REPORT_INVENTORY_VIEW"
                    ));
            }

            if (!canViewReports)
                return new HttpStatusCodeResult(403, "Bạn không có quyền xem báo cáo.");

            return View();
        }

        // Kiểm tra quyền xem báo cáo doanh thu
        private bool CanViewRevenueReport()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            // Store Manager được xem báo cáo
            if (roleId == 2)
                return true;

            // Staff cần được cấp quyền cụ thể
            if (roleId == 3)
            {
                int accountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                return db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    up.Permission.PermissionCode == "REPORT_REVENUE_VIEW");
            }

            // System Admin không xem báo cáo kinh doanh
            return false;
        }

        // GET: Reports/Revenue
        public ActionResult Revenue(DateTime? fromDate, DateTime? toDate)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Index", "Login");

            if (!CanViewRevenueReport())
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền xem báo cáo doanh thu.");

            DateTime startDate = fromDate ?? DateTime.Today.AddDays(-6);
            DateTime endDate = toDate ?? DateTime.Today;

            if (startDate.Date > endDate.Date)
            {
                ModelState.AddModelError("",
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.");

                startDate = endDate.Date;
            }

            DateTime endExclusive = endDate.Date.AddDays(1);

            // Lấy đơn hàng trong khoảng thời gian
            var orders = db.Orders.Where(o =>
                o.OrderDate >= startDate.Date &&
                o.OrderDate < endExclusive);

            var model = new RevenueReportViewModel
            {
                FromDate = startDate.Date,
                ToDate = endDate.Date,

                TotalOrders = orders.Count(),

                CompletedOrders = orders.Count(o =>
                    o.OrderStatus == "Completed"),

                CancelledOrders = orders.Count(o =>
                    o.OrderStatus == "Cancelled"),

                RefundedOrders = orders.Count(o =>
                    o.PaymentStatus == "Refunded"),

                // Chỉ tính đơn hoàn thành, chưa hoàn tiền
                TotalRevenue = orders
                    .Where(o =>
                        o.OrderStatus == "Completed" &&
                        o.PaymentStatus != "Refunded")
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0,

                POSRevenue = orders
                    .Where(o =>
                        o.OrderStatus == "Completed" &&
                        o.PaymentStatus != "Refunded" &&
                        o.OrderSource == "POS")
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0,

                OnlineRevenue = orders
                    .Where(o =>
                        o.OrderStatus == "Completed" &&
                        o.PaymentStatus != "Refunded" &&
                        o.OrderSource == "ONLINE")
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0
            };

            // Thống kê doanh thu theo ngày
            model.DailyRevenues = orders
                .Where(o =>
                    o.OrderStatus == "Completed" &&
                    o.PaymentStatus != "Refunded")
                .GroupBy(o => System.Data.Entity.DbFunctions.TruncateTime(
                    o.OrderDate))
                .Select(g => new DailyRevenueViewModel
                {
                    Date = g.Key.Value,
                    TotalOrders = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Top 10 sản phẩm bán chạy
            model.TopProducts = (
                from d in db.OrderDetails
                join o in db.Orders
                    on d.OrderId equals o.OrderId
                where o.OrderDate >= startDate.Date
                    && o.OrderDate < endExclusive
                    && o.OrderStatus == "Completed"
                    && o.PaymentStatus != "Refunded"
                group d by new
                {
                    d.ProductName,
                    d.SizeName
                } into g
                orderby g.Sum(x => x.Quantity) descending
                select new TopSellingProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    SizeName = g.Key.SizeName,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x =>
                        x.UnitPrice * x.Quantity)
                })
                .Take(10)
                .ToList();

            return View(model);
        }

        public ActionResult Inventory(DateTime? fromDate, DateTime? toDate)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);
            int accountId = Convert.ToInt32(Session["MOCBREW_AccountId"]);

            // 2. Kiểm tra quyền xem báo cáo tồn kho
            bool canView = false;

            if (roleId == 2)
            {
                canView = true;
            }
            else if (roleId == 3)
            {
                canView = db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    up.Permission.PermissionCode == "REPORT_INVENTORY_VIEW");
            }

            if (!canView)
            {
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền xem báo cáo tồn kho.");
            }

            // 3. Xác định khoảng thời gian báo cáo
            DateTime startDate = (fromDate ?? DateTime.Today.AddDays(-6)).Date;
            DateTime endDate = (toDate ?? DateTime.Today).Date;

            if (startDate > endDate)
            {
                ModelState.AddModelError("",
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.");

                startDate = endDate;
            }

            DateTime endExclusive = endDate.AddDays(1);

            // 4. Khởi tạo ViewModel
            var model = new InventoryReportViewModel
            {
                FromDate = startDate,
                ToDate = endDate
            };

            // 5. Thống kê tồn kho hiện tại
            var ingredients = db.Ingredients
                .Where(i => i.IsActive);

            model.TotalIngredients = ingredients.Count();

            model.LowStockIngredients = ingredients.Count(i =>
                i.CurrentStock < i.MinimumStock);

            model.OutOfStockIngredients = ingredients.Count(i =>
                i.CurrentStock <= 0);

            model.Ingredients = ingredients
                .OrderBy(i => i.IngredientName)
                .Select(i => new IngredientStockReportViewModel
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CurrentStock = i.CurrentStock,
                    MinimumStock = i.MinimumStock,
                    StockStatus = i.CurrentStock <= 0
                        ? "Hết hàng"
                        : (i.CurrentStock < i.MinimumStock
                            ? "Dưới mức tối thiểu"
                            : "Đủ hàng")
                })
                .ToList();

            // 6. Lấy giao dịch kho trong khoảng thời gian
            var transactions = db.InventoryTransactions
                .Where(t => t.TransactionDate >= startDate &&
                            t.TransactionDate < endExclusive);

            model.TotalImportQuantity = transactions
                .Where(t => t.TransactionType == "Import")
                .Select(t => (decimal?)t.Quantity)
                .Sum() ?? 0;

            model.TotalExportQuantity = transactions
                .Where(t => t.TransactionType == "Export")
                .Select(t => (decimal?)t.Quantity)
                .Sum() ?? 0;

            model.TotalAdjustmentQuantity = transactions
                .Where(t => t.TransactionType == "Adjustment")
                .Select(t => (decimal?)t.Quantity)
                .Sum() ?? 0;

            // 7. Danh sách giao dịch kho gần đây
            model.Transactions = transactions
                .OrderByDescending(t => t.TransactionDate)
                .Take(100)
                .Select(t => new InventoryTransactionReportViewModel
                {
                    TransactionId = t.TransactionId,
                    IngredientName = t.Ingredient.IngredientName,
                    Unit = t.Ingredient.Unit,
                    TransactionType = t.TransactionType,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    ReferenceCode = t.ReferenceCode,
                    Note = t.Note,
                    EmployeeName = t.Account != null
                        ? t.Account.FullName
                        : "Không xác định"
                })
                .ToList();

            // 8. Lịch sử kiểm kê trong khoảng thời gian
            model.Stocktakes = db.InventoryStocktakes
                .Where(s => s.StocktakeDate >= startDate &&
                            s.StocktakeDate < endExclusive)
                .OrderByDescending(s => s.StocktakeDate)
                .Take(100)
                .Select(s => new InventoryStocktakeReportViewModel
                {
                    StocktakeId = s.StocktakeId,
                    IngredientName = s.Ingredient.IngredientName,
                    Unit = s.Ingredient.Unit,
                    SystemStock = s.SystemStock,
                    ActualStock = s.ActualStock,
                    Difference = s.Difference,
                    StocktakeDate = s.StocktakeDate,
                    EmployeeName = s.Account.FullName
                })
                .ToList();

            return View(model);
        }

        public ActionResult Profit(DateTime? fromDate, DateTime? toDate)
        {
            int accountId = Convert.ToInt32(Session["MOCBREW_AccountId"] ?? 0);
            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"] ?? 0);

            if (accountId <= 0)
                return RedirectToAction("Login", "Account");

            bool canView = roleId == 2 ||
                (roleId == 3 && db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    up.Permission.PermissionCode == "REPORT_PROFIT_VIEW"));

            if (!canView)
                return new HttpStatusCodeResult(403, "Bạn không có quyền xem báo cáo lợi nhuận.");

            DateTime endDate = (toDate ?? DateTime.Today).Date;
            DateTime startDate = (fromDate ?? endDate.AddDays(-6)).Date;

            if (startDate > endDate)
            {
                TempData["Error"] = "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
                startDate = endDate.AddDays(-6);
            }

            DateTime endExclusive = endDate.AddDays(1);

            var model = new ProfitReportViewModel
            {
                FromDate = startDate,
                ToDate = endDate
            };

            // Lấy đơn hàng trong kỳ
            var orders = db.Orders
                .Where(o =>
                    o.OrderDate >= startDate &&
                    o.OrderDate < endExclusive)
                .ToList();

            model.TotalOrders = orders.Count;

            model.RefundedOrders = orders.Count(o =>
                o.PaymentStatus == "Refunded");

            var completedOrders = orders
                .Where(o =>
                    o.OrderStatus == "Completed" &&
                    o.PaymentStatus != "Refunded")
                .ToList();

            model.CompletedOrders = completedOrders.Count;

            var completedOrderIds = completedOrders
                .Select(o => o.OrderId)
                .ToList();

            // Chỉ lấy chi tiết của đơn hoàn thành
            var orderDetails = db.OrderDetails
                .Where(d => completedOrderIds.Contains(d.OrderId))
                .ToList();

            // Lấy giá nhập bình quân từ phiếu nhập đã duyệt
            var ingredientCosts = db.PurchaseReceiptDetails
                .Where(d => d.PurchaseReceipt.Status == "Approved")
                .GroupBy(d => d.IngredientId)
                .Select(g => new
                {
                    IngredientId = g.Key,
                    AveragePrice = g.Sum(x => x.Quantity * x.UnitPrice)
                        / g.Sum(x => x.Quantity)
                })
                .ToList()
                .ToDictionary(x => x.IngredientId, x => x.AveragePrice);

            // Lấy định lượng nguyên liệu theo công thức
            var recipeDetails = (
                from r in db.Recipes
                join rd in db.RecipeDetails
                    on r.RecipeId equals rd.RecipeId
                where r.IsActive
                select new
                {
                    r.ProductSizeId,
                    rd.IngredientId,
                    rd.QuantityRequired
                }
            ).ToList();

            var recipeCostBySize = recipeDetails
                .GroupBy(x => x.ProductSizeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x =>
                        x.QuantityRequired *
                        (ingredientCosts.ContainsKey(x.IngredientId)
                            ? ingredientCosts[x.IngredientId]
                            : 0))
                );

            // Tính doanh thu, giá vốn và lợi nhuận theo từng dòng sản phẩm
            var productRows = new List<ProductProfitViewModel>();

            foreach (var detail in orderDetails)
            {
                var order = completedOrders
                    .FirstOrDefault(o => o.OrderId == detail.OrderId);

                if (order == null)
                    continue;

                decimal lineRevenue = detail.UnitPrice * detail.Quantity;

                // Phân bổ giảm giá theo tỷ trọng doanh thu dòng hàng
                decimal orderSubTotal = order.SubTotal;
                decimal allocatedDiscount = orderSubTotal > 0
                    ? order.DiscountAmount * lineRevenue / orderSubTotal
                    : 0;

                decimal netRevenue = lineRevenue - allocatedDiscount;

                decimal unitCost = 0;

                if (detail.ProductSizeId.HasValue &&
                    recipeCostBySize.ContainsKey(detail.ProductSizeId.Value))
                {
                    unitCost = recipeCostBySize[detail.ProductSizeId.Value];
                }

                decimal totalCost = unitCost * detail.Quantity;
                decimal profit = netRevenue - totalCost;

                string sizeName = detail.SizeName ?? "Không có size";

                var row = productRows.FirstOrDefault(x =>
                    x.ProductName == detail.ProductName &&
                    x.SizeName == sizeName);

                if (row == null)
                {
                    row = new ProductProfitViewModel
                    {
                        ProductName = detail.ProductName,
                        SizeName = sizeName
                    };

                    productRows.Add(row);
                }

                row.TotalQuantity += detail.Quantity;
                row.Revenue += netRevenue;
                row.Cost += totalCost;
                row.Profit += profit;
            }

            // Hoàn thiện tỷ suất lợi nhuận
            foreach (var item in productRows)
            {
                item.ProfitMargin = item.Revenue > 0
                    ? item.Profit / item.Revenue * 100
                    : 0;
            }

            model.ProductProfits = productRows
                .OrderByDescending(x => x.Profit)
                .ToList();

            model.TotalRevenue = model.ProductProfits.Sum(x => x.Revenue);
            model.TotalCost = model.ProductProfits.Sum(x => x.Cost);
            model.GrossProfit = model.TotalRevenue - model.TotalCost;

            model.GrossMargin = model.TotalRevenue > 0
                ? model.GrossProfit / model.TotalRevenue * 100
                : 0;

            // Báo cáo lợi nhuận theo ngày
            model.DailyProfits = completedOrders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g =>
                {
                    var dayDetails = orderDetails
                        .Where(d => g.Any(o => o.OrderId == d.OrderId))
                        .ToList();

                    decimal revenue = 0;
                    decimal cost = 0;

                    foreach (var detail in dayDetails)
                    {
                        var order = g.FirstOrDefault(o => o.OrderId == detail.OrderId);

                        decimal lineRevenue = detail.UnitPrice * detail.Quantity;

                        decimal discount = order.SubTotal > 0
                            ? order.DiscountAmount * lineRevenue / order.SubTotal
                            : 0;

                        revenue += lineRevenue - discount;

                        if (detail.ProductSizeId.HasValue &&
                            recipeCostBySize.ContainsKey(detail.ProductSizeId.Value))
                        {
                            cost += recipeCostBySize[detail.ProductSizeId.Value]
                                * detail.Quantity;
                        }
                    }

                    return new DailyProfitViewModel
                    {
                        Date = g.Key,
                        Revenue = revenue,
                        Cost = cost,
                        Profit = revenue - cost
                    };
                })
                .OrderBy(x => x.Date)
                .ToList();

            return View(model);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
