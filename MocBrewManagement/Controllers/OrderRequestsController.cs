
using System;
using System.Linq;
using System.Web.Mvc;
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;

namespace MocBrewManagement.Controllers
{
    public class OrderRequestsController : Controller
    {
        private bool IsSuperAdmin()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(
                Session["MOCBREW_RoleId"]
            );

            return roleId == 1;
        }

        // GET: OrderRequests
        public ActionResult Index()
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!IsSuperAdmin())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Chỉ Super Admin mới được quản lý yêu cầu hủy đơn và hoàn tiền."
                );
            }

            using (var db = new MOCBREWEntities())
            {
                var requests = db.OrderRequests
                    .OrderByDescending(r => r.RequestedAt)
                    .Select(r => new OrderRequestListViewModel
                    {
                        RequestId = r.RequestId,
                        OrderId = r.OrderId,
                        OrderCode = r.Order.OrderCode,
                        RequestType = r.RequestType,
                        RequestedByName = r.Account.FullName,
                        Reason = r.Reason,
                        RequestStatus = r.RequestStatus,
                        RequestedAt = r.RequestedAt,
                        ReviewedByName = r.Account1 != null
                            ? r.Account1.FullName
                            : null,
                        ReviewedAt = r.ReviewedAt,
                        ReviewNote = r.ReviewNote,
                        TotalAmount = r.Order.TotalAmount,
                        PaymentStatus = r.Order.PaymentStatus,
                        OrderStatus = r.Order.OrderStatus
                    })
                    .ToList();

                return View(requests);
            }
        }


        // GET: OrderRequests/Details/1
        [HttpGet]
        public ActionResult Details(long id)
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!IsSuperAdmin())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Chỉ Super Admin mới được xem và xử lý yêu cầu."
                );
            }

            using (var db = new MOCBREWEntities())
            {
                var request = db.OrderRequests
                    .Where(r => r.RequestId == id)
                    .Select(r => new OrderRequestDetailViewModel
                    {
                        RequestId = r.RequestId,
                        OrderId = r.OrderId,
                        OrderCode = r.Order.OrderCode,

                        RequestType = r.RequestType,
                        RequestedByName = r.Account.FullName,
                        RequestedAt = r.RequestedAt,
                        Reason = r.Reason,
                        RequestStatus = r.RequestStatus,

                        TotalAmount = r.Order.TotalAmount,
                        SubTotal = r.Order.SubTotal,
                        DiscountAmount = r.Order.DiscountAmount,
                        ShippingFee = r.Order.ShippingFee,

                        PaymentMethod = r.Order.PaymentMethod,
                        PaymentStatus = r.Order.PaymentStatus,
                        OrderStatus = r.Order.OrderStatus,

                        FulfillmentMethod = r.Order.FulfillmentMethod,
                        ReceiverName = r.Order.ReceiverName,
                        ReceiverPhone = r.Order.ReceiverPhone,
                        DeliveryAddress = r.Order.DeliveryAddress,
                        OrderNote = r.Order.Note,

                        ReviewedByName = r.Account1 != null
                            ? r.Account1.FullName
                            : null,

                        ReviewedAt = r.ReviewedAt,
                        ReviewNote = r.ReviewNote
                    })
                    .FirstOrDefault();

                if (request == null)
                {
                    return HttpNotFound("Không tìm thấy yêu cầu.");
                }

                return View(request);
            }
        }



        // POST: OrderRequests/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(OrderRequestReviewViewModel model)
        {
            return ProcessRequest(model, true);
        }

        // POST: OrderRequests/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(OrderRequestReviewViewModel model)
        {
            return ProcessRequest(model, false);
        }

        // Xử lý phê duyệt hoặc từ chối yêu cầu
        private ActionResult ProcessRequest(
            OrderRequestReviewViewModel model,
            bool isApproved)
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!IsSuperAdmin())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Chỉ Super Admin mới được xử lý yêu cầu."
                );
            }

            if (model == null || model.RequestId <= 0)
            {
                return new HttpStatusCodeResult(400, "Yêu cầu không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Ghi chú xử lý không được vượt quá 500 ký tự.";
                return RedirectToAction("Details", new { id = model.RequestId });
            }

            int accountId = Convert.ToInt32(
                Session["MOCBREW_AccountId"]
            );

            using (var db = new MOCBREWEntities())
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var request = db.OrderRequests
                        .FirstOrDefault(r => r.RequestId == model.RequestId);

                    if (request == null)
                    {
                        return HttpNotFound("Không tìm thấy yêu cầu.");
                    }

                    // Chỉ xử lý yêu cầu đang chờ duyệt
                    if (request.RequestStatus != "Pending")
                    {
                        TempData["Error"] = "Yêu cầu này đã được xử lý trước đó.";
                        return RedirectToAction("Details",
                            new { id = request.RequestId });
                    }

                    var order = db.Orders
                        .FirstOrDefault(o => o.OrderId == request.OrderId);

                    if (order == null)
                    {
                        return HttpNotFound("Không tìm thấy đơn hàng.");
                    }

                    string oldOrderStatus = order.OrderStatus;

                    if (isApproved)
                    {
                        if (request.RequestType == "Refund")
                        {
                            // Chỉ hoàn tiền cho đơn đã thanh toán
                            if (order.PaymentStatus != "Paid")
                            {
                                TempData["Error"] =
                                    "Đơn hàng không còn ở trạng thái đã thanh toán. Không thể phê duyệt hoàn tiền.";

                                return RedirectToAction("Details",
                                    new { id = request.RequestId });
                            }

                            // Ghi nhận hoàn tiền trong hệ thống
                            order.PaymentStatus = "Refunded";
                        }
                        else if (request.RequestType == "Cancel")
                        {
                            // Không cho hủy đơn đã hoàn thành hoặc đã hủy
                            if (order.OrderStatus == "Completed" ||
                                order.OrderStatus == "Cancelled")
                            {
                                TempData["Error"] =
                                    "Không thể phê duyệt hủy đơn ở trạng thái hiện tại.";

                                return RedirectToAction("Details",
                                    new { id = request.RequestId });
                            }

                            order.OrderStatus = "Cancelled";

                            // Ghi lịch sử thay đổi trạng thái đơn hàng
                            var history = new OrderStatusHistory
                            {
                                OrderId = order.OrderId,
                                AccountId = accountId,
                                OldStatus = oldOrderStatus,
                                NewStatus = "Cancelled",
                                Note = "Super Admin phê duyệt yêu cầu hủy đơn.",
                                ChangedAt = DateTime.Now
                            };

                            db.OrderStatusHistories.Add(history);
                        }
                        else
                        {
                            return new HttpStatusCodeResult(
                                400,
                                "Loại yêu cầu không hợp lệ."
                            );
                        }

                        request.RequestStatus = "Approved";
                    }
                    else
                    {
                        // Từ chối: không thay đổi đơn hàng
                        request.RequestStatus = "Rejected";
                    }

                    request.ReviewedByAccountId = accountId;
                    request.ReviewedAt = DateTime.Now;
                    request.ReviewNote = string.IsNullOrWhiteSpace(model.ReviewNote)
                        ? null
                        : model.ReviewNote.Trim();

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = isApproved
                        ? "Đã phê duyệt yêu cầu thành công."
                        : "Đã từ chối yêu cầu thành công.";

                    return RedirectToAction(
                        "Details",
                        new { id = request.RequestId }
                    );
                }
                catch
                {
                    transaction.Rollback();

                    TempData["Error"] =
                        "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.";

                    return RedirectToAction(
                        "Details",
                        new { id = model.RequestId }
                    );
                }
            }
        }

    }
}
