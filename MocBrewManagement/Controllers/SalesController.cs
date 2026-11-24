
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.Mvc;

namespace MocBrewManagement.Controllers
{
    public class SalesController : Controller
    {
        // Trang tổng quan Bán hàng
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

            // 2. Kiểm tra vai trò hợp lệ
            if (roleId != 1 && roleId != 2 && roleId != 3)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            // 3. Lấy quyền của tài khoản
            var permissionCodes = new HashSet<string>();

            if (roleId == 3)
            {
                using (var db = new MOCBREWEntities())
                {
                    permissionCodes = new HashSet<string>(
                        db.UserPermissions
                            .Where(up =>
                                up.AccountId == accountId &&
                                up.IsActive)
                            .Select(up => up.Permission.PermissionCode)
                            .Distinct()
                            .ToList()
                    );
                }
            }

            // 4. Kiểm tra quyền truy cập Bán hàng
            bool canAccessPOS =
                roleId == 2 ||
                (roleId == 3 &&
                (permissionCodes.Contains("POS_CREATE") ||
                 permissionCodes.Contains("POS_PAYMENT") ||
                 permissionCodes.Contains("POS_DISCOUNT")));

            if (!canAccessPOS)
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền truy cập chức năng Bán hàng."
                );
            }

            // 5. Thông tin hiển thị
            ViewBag.FullName = Session["MOCBREW_FullName"];
            ViewBag.RoleName = Session["MOCBREW_RoleName"];
            ViewBag.RoleId = roleId;

            return View();
        }


        // GET: Sales/Create
        public ActionResult Create()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            int accountId = Convert.ToInt32(Session["MOCBREW_AccountId"]);
            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            // Chỉ Manager hoặc Staff có quyền POS_CREATE
            bool canCreatePOS = false;

            if (roleId == 2)
            {
                canCreatePOS = true;
            }
            else if (roleId == 3)
            {
                using (var db = new MOCBREWEntities())
                {
                    canCreatePOS = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == "POS_CREATE");
                }
            }

            if (!canCreatePOS)
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền tạo đơn hàng POS."
                );
            }

            using (var db = new MOCBREWEntities())
            {
                var products = db.Products
                    .Where(p => p.IsActive)
                    .Select(p => new POSProductViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ImageUrl = p.ImageUrl,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.CategoryName,

                        Sizes = p.ProductSizes
                            .Where(ps => ps.IsAvailable && ps.Size.IsActive)
                            .Select(ps => new POSProductSizeViewModel
                            {
                                ProductSizeId = ps.ProductSizeId,
                                SizeId = ps.SizeId,
                                SizeName = ps.Size.SizeName,
                                Price = ps.Price,
                                IsAvailable = ps.IsAvailable
                            })
                            .ToList(),

                        Toppings = p.ProductToppings
                            .Where(pt =>
                                pt.IsAvailable &&
                                pt.Topping.IsActive)
                            .Select(pt => new POSToppingViewModel
                            {
                                ToppingId = pt.Topping.ToppingId,
                                ToppingName = pt.Topping.ToppingName,
                                Price = pt.Topping.Price
                            })
                            .ToList()
                    })
                    .ToList();

                // Chỉ hiển thị sản phẩm có ít nhất một kích cỡ đang bán
                products = products
                    .Where(p => p.Sizes.Any())
                    .ToList();

                var model = new POSCreateViewModel
                {
                    Products = products
                };

                return View(model);
            }
        }



        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(POSOrderSubmitViewModel model)
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

            // 2. Kiểm tra quyền tạo đơn và thanh toán
            bool canCreatePOS = roleId == 2;
            bool canPayment = roleId == 2;

            using (var db = new MOCBREWEntities())
            {
                if (roleId == 3)
                {
                    var permissions = db.UserPermissions
                        .Where(up =>
                            up.AccountId == accountId &&
                            up.IsActive)
                        .Select(up => up.Permission.PermissionCode)
                        .ToList();

                    canCreatePOS = permissions.Contains("POS_CREATE");
                    canPayment = permissions.Contains("POS_PAYMENT");
                }

                if (!canCreatePOS || !canPayment)
                {
                    return new HttpStatusCodeResult(
                        403,
                        "Bạn cần có cả quyền POS_CREATE và POS_PAYMENT để tạo và thanh toán đơn hàng."
                    );
                }
                // 2.1. Kiểm tra nhân viên đã mở ca làm việc chưa
                var openShift = db.WorkShifts
                    .FirstOrDefault(s =>
                        s.AccountId == accountId &&
                        s.Status == "Open"
                    );

                if (openShift == null)
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Bạn chưa mở ca làm việc. Vui lòng mở ca trước khi tạo đơn POS."
                    );
                }
                // 3. Kiểm tra dữ liệu thanh toán
                if (model == null ||
                    string.IsNullOrWhiteSpace(model.ReceiverName) ||
                    string.IsNullOrWhiteSpace(model.ReceiverPhone))
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Vui lòng nhập đầy đủ tên và số điện thoại khách hàng."
                    );
                }

                if (!model.ConfirmPayment)
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Bạn phải xác nhận đã nhận đủ tiền."
                    );
                }

                if (model.PaymentMethod != "Cash" &&
                    model.PaymentMethod != "BankTransfer")
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Phương thức thanh toán không hợp lệ."
                    );
                }

                if (string.IsNullOrWhiteSpace(model.CartItemsJson))
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Giỏ hàng không có sản phẩm."
                    );
                }

                // 4. Đọc dữ liệu giỏ hàng JSON
                List<POSCartSubmitItemViewModel> cartItems;

                try
                {
                    var serializer = new JavaScriptSerializer
                    {
                        MaxJsonLength = int.MaxValue
                    };

                    cartItems = serializer.Deserialize<
                        List<POSCartSubmitItemViewModel>
                    >(model.CartItemsJson);
                }
                catch
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Dữ liệu giỏ hàng không hợp lệ."
                    );
                }

                if (cartItems == null || cartItems.Count == 0)
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Giỏ hàng không có sản phẩm."
                    );
                }

                // 5. Kiểm tra số lượng và dữ liệu sản phẩm
                if (cartItems.Any(x =>
                    x == null ||
                    x.ProductId <= 0 ||
                    x.ProductSizeId <= 0 ||
                    x.Quantity <= 0 ||
                    x.Quantity > 1000))
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Thông tin sản phẩm hoặc số lượng không hợp lệ."
                    );
                }

                decimal subTotal = 0;

                var orderDetails = new List<OrderDetail>();

                // 6. Kiểm tra sản phẩm, size, topping và lấy giá từ SQL
                foreach (var item in cartItems)
                {
                    var product = db.Products.SingleOrDefault(p =>
                        p.ProductId == item.ProductId &&
                        p.IsActive);

                    if (product == null)
                    {
                        return new HttpStatusCodeResult(
                            400,
                            "Một sản phẩm không còn kinh doanh."
                        );
                    }

                    var productSize = db.ProductSizes
                        .SingleOrDefault(ps =>
                            ps.ProductSizeId == item.ProductSizeId &&
                            ps.ProductId == item.ProductId &&
                            ps.IsAvailable &&
                            ps.Size.IsActive);

                    if (productSize == null)
                    {
                        return new HttpStatusCodeResult(
                            400,
                            "Kích cỡ sản phẩm không còn được bán."
                        );
                    }

                    var toppingIds = item.ToppingIds ??
                        new List<int>();

                    if (toppingIds.Any(id => id <= 0) ||
                        toppingIds.Distinct().Count() != toppingIds.Count)
                    {
                        return new HttpStatusCodeResult(
                            400,
                            "Danh sách topping không hợp lệ."
                        );
                    }

                    var toppings = new List<Topping>();

                    if (toppingIds.Count > 0)
                    {
                        toppings = db.ProductToppings
                            .Where(pt =>
                                pt.ProductId == item.ProductId &&
                                pt.IsAvailable &&
                                pt.Topping.IsActive &&
                                toppingIds.Contains(pt.ToppingId))
                            .Select(pt => pt.Topping)
                            .ToList();

                        if (toppings.Count != toppingIds.Count)
                        {
                            return new HttpStatusCodeResult(
                                400,
                                "Một topping không hợp lệ hoặc không còn được bán cho sản phẩm này."
                            );
                        }
                    }

                    decimal toppingTotal = toppings.Sum(t => t.Price);

                    decimal lineTotal =
                        (productSize.Price + toppingTotal) *
                        item.Quantity;

                    subTotal += lineTotal;

                    var detail = new OrderDetail
                    {
                        ProductId = product.ProductId,
                        ProductSizeId = productSize.ProductSizeId,
                        ProductName = product.ProductName,
                        SizeName = productSize.Size.SizeName,
                        UnitPrice = productSize.Price,
                        Quantity = item.Quantity,
                        Note = item.Note
                    };

                    // Lưu topping đã kiểm tra từ cơ sở dữ liệu
                    foreach (var topping in toppings)
                    {
                        detail.OrderDetailToppings.Add(
                            new OrderDetailTopping
                            {
                                ToppingId = topping.ToppingId,
                                ToppingName = topping.ToppingName,
                                UnitPrice = topping.Price,
                                Quantity = 1
                            }
                        );
                    }

                    orderDetails.Add(detail);
                }

                // 7. Tạo mã đơn hàng duy nhất
                string orderCode =
                    "POS-" + Guid.NewGuid().ToString("N")
                        .Substring(0, 20).ToUpper();

                // 8. Lưu đơn và chi tiết trong cùng transaction
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var order = new Order
                        {
                            OrderCode = orderCode,
                            CustomerId = null,
                            AccountId = accountId,

                            ReceiverName = model.ReceiverName.Trim(),
                            ReceiverPhone = model.ReceiverPhone.Trim(),
                            DeliveryAddress = null,

                            OrderDate = DateTime.Now,

                            SubTotal = subTotal,
                            DiscountAmount = 0,
                            ShippingFee = 0,
                            TotalAmount = subTotal,

                            PaymentMethod = model.PaymentMethod,
                            PaymentStatus = "Paid",
                            OrderStatus = "Pending",

                            OrderSource = "POS",
                            FulfillmentMethod = "PICKUP",

                            Note = model.Note
                        };

                        db.Orders.Add(order);
                        db.SaveChanges();

                        foreach (var detail in orderDetails)
                        {
                            detail.OrderId = order.OrderId;
                            db.OrderDetails.Add(detail);
                        }

                        db.SaveChanges();

                        transaction.Commit();

                        TempData["SuccessMessage"] =
                            "Tạo đơn hàng và xác nhận thanh toán thành công. Mã đơn: "
                            + orderCode;

                        return RedirectToAction("Index", "Sales");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }



        // GET: Sales/OrderList
        public ActionResult OrderList(string keyword, string status)
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

            using (var db = new MOCBREWEntities())
            {
                // 2. Kiểm tra quyền xem đơn hàng
                bool canViewAllOrders = roleId == 2;

                if (roleId == 3)
                {
                    canViewAllOrders = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == "ORDER_VIEW_ALL");
                }

                if (!canViewAllOrders)
                {
                    return new HttpStatusCodeResult(
                        403,
                        "Bạn không có quyền xem danh sách đơn hàng."
                    );
                }

                // 3. Truy vấn đơn hàng POS
                var query = db.Orders
                    .Where(o => o.OrderSource == "POS");

                // 4. Tìm kiếm theo mã đơn, tên hoặc số điện thoại
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim();

                    query = query.Where(o =>
                        o.OrderCode.Contains(keyword) ||
                        o.ReceiverName.Contains(keyword) ||
                        o.ReceiverPhone.Contains(keyword));
                }

                // 5. Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(o =>
                        o.OrderStatus == status);
                }

                // 6. Lấy dữ liệu hiển thị
                var orders = query
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new POSOrderListViewModel
                    {
                        OrderId = o.OrderId,
                        OrderCode = o.OrderCode,
                        ReceiverName = o.ReceiverName,
                        ReceiverPhone = o.ReceiverPhone,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        PaymentMethod = o.PaymentMethod,
                        PaymentStatus = o.PaymentStatus,
                        OrderStatus = o.OrderStatus,
                        FulfillmentMethod = o.FulfillmentMethod,
                        OrderSource = o.OrderSource
                    })
                    .ToList();

                // 7. Truyền dữ liệu sang View
                ViewBag.Keyword = keyword;
                ViewBag.Status = status;

                return View(orders);
            }
        }


        // GET: Sales/Details/1
        public ActionResult Details(int id)
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

            using (var db = new MOCBREWEntities())
            {
                // 2. Kiểm tra quyền xem đơn hàng
                bool canViewAllOrders = roleId == 2;

                if (roleId == 3)
                {
                    canViewAllOrders = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == "ORDER_VIEW_ALL");
                }

                if (!canViewAllOrders)
                {
                    return new HttpStatusCodeResult(
                        403,
                        "Bạn không có quyền xem chi tiết đơn hàng."
                    );
                }

                // 3. Lấy đơn hàng POS
                var order = db.Orders.FirstOrDefault(o =>
                    o.OrderId == id &&
                    o.OrderSource == "POS");

                if (order == null)
                {
                    return HttpNotFound("Không tìm thấy đơn hàng.");
                }

                // 4. Lấy danh sách sản phẩm
                var orderDetails = db.OrderDetails
                    .Where(od => od.OrderId == id)
                    .ToList();

                // 5. Lấy topping của các sản phẩm
                var detailIds = orderDetails
                    .Select(od => od.OrderDetailId)
                    .ToList();

                var toppingRows = db.OrderDetailToppings
                    .Where(t => detailIds.Contains(t.OrderDetailId))
                    .ToList();

                // 6. Tạo ViewModel đơn hàng
                var model = new POSOrderDetailViewModel
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    OrderDate = order.OrderDate,

                    ReceiverName = order.ReceiverName,
                    ReceiverPhone = order.ReceiverPhone,
                    DeliveryAddress = order.DeliveryAddress,

                    SubTotal = order.SubTotal,
                    DiscountAmount = order.DiscountAmount,
                    ShippingFee = order.ShippingFee,
                    TotalAmount = order.TotalAmount,

                    PaymentMethod = order.PaymentMethod,
                    PaymentStatus = order.PaymentStatus,

                    OrderStatus = order.OrderStatus,
                    OrderSource = order.OrderSource,
                    FulfillmentMethod = order.FulfillmentMethod,

                    Note = order.Note
                };

                // 7. Đưa sản phẩm và topping vào ViewModel
                foreach (var detail in orderDetails)
                {
                    var itemToppings = toppingRows
                        .Where(t => t.OrderDetailId == detail.OrderDetailId)
                        .Select(t => new POSOrderToppingViewModel
                        {
                            ToppingName = t.ToppingName,
                            UnitPrice = t.UnitPrice,
                            Quantity = t.Quantity
                        })
                        .ToList();

                    decimal toppingTotal = itemToppings.Sum(t =>
                        t.UnitPrice * t.Quantity);

                    var item = new POSOrderDetailItemViewModel
                    {
                        ProductId = detail.ProductId,
                        ProductName = detail.ProductName,
                        SizeName = detail.SizeName,

                        UnitPrice = detail.UnitPrice,
                        Quantity = detail.Quantity,

                        TotalPrice = (detail.UnitPrice * detail.Quantity)
                                     + toppingTotal,

                        Note = detail.Note,
                        Toppings = itemToppings
                    };

                    model.Items.Add(item);
                }

                // 8. Kiểm tra quyền cập nhật trạng thái
                bool canUpdateStatus = roleId == 2;

                if (roleId == 3)
                {
                    canUpdateStatus = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == "ORDER_STATUS_UPDATE");
                }
                ViewBag.CanUpdateStatus = canUpdateStatus;

                // Kiểm tra quyền gửi yêu cầu hủy đơn và hoàn tiền
                bool canRequestCancel = roleId == 2;
                bool canRequestRefund = roleId == 2;

                if (roleId == 3)
                {
                    canRequestCancel = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == "ORDER_CANCEL");

                    canRequestRefund = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == "ORDER_REFUND");
                }

                // Kiểm tra đơn hàng đã có yêu cầu chờ duyệt chưa
                bool hasPendingRequest = db.OrderRequests.Any(r =>
                    r.OrderId == id &&
                    r.RequestStatus == "Pending");

                // Chỉ hiển thị nút nếu đơn hàng đủ điều kiện
                ViewBag.CanRequestCancel =
                    canRequestCancel &&
                    order.OrderStatus != "Completed" &&
                    !hasPendingRequest;

                ViewBag.CanRequestRefund =
                    canRequestRefund &&
                    order.PaymentStatus == "Paid" &&
                    !hasPendingRequest;


                // Lấy lịch sử cập nhật trạng thái đơn hàng
                var orderHistory = db.OrderStatusHistories
                    .Where(h => h.OrderId == id)
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new POSOrderHistoryViewModel
                    {
                        HistoryId = h.HistoryId,
                        OrderId = h.OrderId,
                        OldStatus = h.OldStatus,
                        NewStatus = h.NewStatus,
                        Note = h.Note,
                        ChangedAt = h.ChangedAt,
                        EmployeeName = h.Account != null
                            ? h.Account.FullName
                            : "Không xác định"
                    })
                    .ToList();

                ViewBag.OrderHistory = orderHistory;

                return View(model);
            }
        }



        // POST: Sales/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int id, string newStatus)
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

            using (var db = new MOCBREWEntities())
            {
                // 2. Kiểm tra quyền cập nhật trạng thái
                bool canUpdateStatus = roleId == 2;

                if (roleId == 3)
                {
                    canUpdateStatus = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == "ORDER_STATUS_UPDATE");
                }

                if (!canUpdateStatus)
                {
                    return new HttpStatusCodeResult(
                        403,
                        "Bạn không có quyền cập nhật trạng thái đơn hàng."
                    );
                }

                // 3. Chỉ cho phép các trạng thái hợp lệ
                var validStatuses = new[]
                {
                    "Pending",
                    "Confirmed",
                    "Preparing",
                    "Completed"
                };

                if (!validStatuses.Contains(newStatus))
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Trạng thái đơn hàng không hợp lệ."
                    );
                }

                // 4. Tìm đơn hàng POS
                var order = db.Orders.FirstOrDefault(o =>
                    o.OrderId == id &&
                    o.OrderSource == "POS");

                if (order == null)
                {
                    return HttpNotFound("Không tìm thấy đơn hàng POS.");
                }

                // 5. Kiểm tra quy trình chuyển trạng thái
                string nextStatus = null;

                switch (order.OrderStatus)
                {
                    case "Pending":
                        nextStatus = "Confirmed";
                        break;

                    case "Confirmed":
                        nextStatus = "Preparing";
                        break;

                    case "Preparing":
                        nextStatus = "Completed";
                        break;
                }

                // 6. Không cho phép bỏ qua hoặc lặp trạng thái
                if (nextStatus == null || newStatus != nextStatus)
                {
                    TempData["Error"] =
                        "Không thể chuyển từ trạng thái " +
                        order.OrderStatus + " sang " + newStatus + ".";

                    return RedirectToAction("Details", new { id = id });
                }

                // 7. Lưu lịch sử trạng thái trước khi cập nhật
                string oldStatus = order.OrderStatus;

                var history = new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    AccountId = accountId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    Note = "Cập nhật trạng thái đơn hàng POS",
                    ChangedAt = DateTime.Now
                };

                // 8. Cập nhật trạng thái đơn hàng
                order.OrderStatus = newStatus;

                // 9. Thêm lịch sử vào cơ sở dữ liệu
                db.OrderStatusHistories.Add(history);

                // 10. Lưu đồng thời trạng thái và lịch sử
                db.SaveChanges();

                TempData["Success"] =
                    "Cập nhật trạng thái đơn hàng thành công.";

                return RedirectToAction("Details", new { id = id });
            }
        }



        [HttpGet]
        public ActionResult CreateOrderRequest(int id, string requestType)
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            if (roleId != 2 && roleId != 3)
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền gửi yêu cầu.");
            }

            if (requestType != "Cancel" && requestType != "Refund")
            {
                return new HttpStatusCodeResult(400,
                    "Loại yêu cầu không hợp lệ.");
            }

            using (var db = new MOCBREWEntities())
            {
                var order = db.Orders.FirstOrDefault(o =>
                    o.OrderId == id && o.OrderSource == "POS");

                if (order == null)
                    return HttpNotFound("Không tìm thấy đơn hàng POS.");

                if (requestType == "Cancel" &&
                    order.OrderStatus == "Completed")
                {
                    TempData["Error"] =
                        "Không thể yêu cầu hủy đơn đã hoàn thành.";

                    return RedirectToAction("Details", new { id = id });
                }

                if (requestType == "Refund" &&
                    order.PaymentStatus != "Paid")
                {
                    TempData["Error"] =
                        "Chỉ có thể yêu cầu hoàn tiền cho đơn đã thanh toán.";

                    return RedirectToAction("Details", new { id = id });
                }

                bool hasPendingRequest = db.OrderRequests.Any(r =>
                    r.OrderId == id &&
                    r.RequestStatus == "Pending");

                if (hasPendingRequest)
                {
                    TempData["Error"] =
                        "Đơn hàng đã có yêu cầu đang chờ xử lý.";

                    return RedirectToAction("Details", new { id = id });
                }

                var model = new OrderRequestViewModel
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    RequestType = requestType,
                    TotalAmount = order.TotalAmount,
                    PaymentStatus = order.PaymentStatus,
                    OrderStatus = order.OrderStatus
                };

                return View(model);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrderRequest(OrderRequestViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            int accountId = Convert.ToInt32(Session["MOCBREW_AccountId"]);
            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            if (roleId != 2 && roleId != 3)
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền gửi yêu cầu.");
            }

            using (var db = new MOCBREWEntities())
            {
                // Kiểm tra quyền gửi yêu cầu
                string requiredPermission = model.RequestType == "Cancel"
                    ? "ORDER_CANCEL"
                    : model.RequestType == "Refund"
                        ? "ORDER_REFUND"
                        : null;

                if (requiredPermission == null)
                {
                    return new HttpStatusCodeResult(400,
                        "Loại yêu cầu không hợp lệ.");
                }

                bool hasPermission = roleId == 2;

                if (roleId == 3)
                {
                    hasPermission = db.UserPermissions.Any(up =>
                        up.AccountId == accountId &&
                        up.IsActive &&
                        up.Permission.PermissionCode == requiredPermission);
                }

                if (!hasPermission)
                {
                    return new HttpStatusCodeResult(403,
                        "Bạn không có quyền thực hiện yêu cầu này.");
                }

                var order = db.Orders.FirstOrDefault(o =>
                    o.OrderId == model.OrderId &&
                    o.OrderSource == "POS");

                if (order == null)
                    return HttpNotFound("Không tìm thấy đơn hàng.");

                if (string.IsNullOrWhiteSpace(model.Reason))
                {
                    ModelState.AddModelError("Reason",
                        "Vui lòng nhập lý do yêu cầu.");
                }

                if (model.RequestType == "Cancel" &&
                    order.OrderStatus == "Completed")
                {
                    ModelState.AddModelError("RequestType",
                        "Không thể hủy đơn hàng đã hoàn thành.");
                }

                if (model.RequestType == "Refund" &&
                    order.PaymentStatus != "Paid")
                {
                    ModelState.AddModelError("RequestType",
                        "Chỉ có thể hoàn tiền cho đơn đã thanh toán.");
                }

                bool hasPendingRequest = db.OrderRequests.Any(r =>
                    r.OrderId == order.OrderId &&
                    r.RequestStatus == "Pending");

                if (hasPendingRequest)
                {
                    ModelState.AddModelError("RequestType",
                        "Đơn hàng đã có yêu cầu đang chờ xử lý.");
                }

                if (!ModelState.IsValid)
                {
                    model.OrderCode = order.OrderCode;
                    model.TotalAmount = order.TotalAmount;
                    model.PaymentStatus = order.PaymentStatus;
                    model.OrderStatus = order.OrderStatus;

                    return View(model);
                }

                var request = new OrderRequest
                {
                    OrderId = order.OrderId,
                    RequestType = model.RequestType,
                    RequestedByAccountId = accountId,
                    Reason = model.Reason.Trim(),
                    RequestStatus = "Pending",
                    RequestedAt = DateTime.Now
                };

                db.OrderRequests.Add(request);
                db.SaveChanges();

                TempData["Success"] =
                    "Đã gửi yêu cầu thành công. Vui lòng chờ Super Admin phê duyệt.";

                return RedirectToAction("Details",
                    new { id = order.OrderId });
            }
        }

    }
}
