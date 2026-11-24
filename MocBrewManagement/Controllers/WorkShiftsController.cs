
using System;
using System.Linq;
using System.Web.Mvc;
using MocBrewManagement.Models;
using MocBrewManagement.ViewModels;

namespace MocBrewManagement.Controllers
{
    public class WorkShiftsController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        private int? CurrentAccountId
        {
            get
            {
                if (Session["MOCBREW_AccountId"] == null)
                    return null;

                return Convert.ToInt32(
                    Session["MOCBREW_AccountId"]
                );
            }
        }

        private bool IsAuthorized()
        {
            if (CurrentAccountId == null)
                return false;

            int roleId = Convert.ToInt32(
                Session["MOCBREW_RoleId"]
            );

            // SystemAdmin không tham gia nghiệp vụ cửa hàng
            return roleId == 2 || roleId == 3;
        }

        // DANH SÁCH CA LÀM CỦA NHÂN VIÊN
        public ActionResult Index()
        {
            if (!IsAuthorized())
                return RedirectToAction("Index", "Home");

            int accountId = CurrentAccountId.Value;

            var shifts = db.WorkShifts
                .Where(s => s.AccountId == accountId)
                .OrderByDescending(s => s.StartTime)
                .ToList()
                .Select(s => new WorkShiftViewModel
                {
                    ShiftId = s.ShiftId,
                    ShiftCode = s.ShiftCode,
                    AccountId = s.AccountId,
                    EmployeeName = s.Account.FullName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    OpeningCash = s.OpeningCash,
                    ExpectedCash = s.ExpectedCash,
                    ActualCash = s.ActualCash,
                    CashDifference = s.CashDifference,
                    Status = s.Status,
                    Note = s.Note
                })
                .ToList();

            return View(shifts);
        }

        // MỞ CA - GET
        public ActionResult Open()
        {
            if (!IsAuthorized())
                return RedirectToAction("Index", "Home");

            int accountId = CurrentAccountId.Value;

            bool hasOpenShift = db.WorkShifts.Any(s =>
                s.AccountId == accountId &&
                s.Status == "Open"
            );

            if (hasOpenShift)
            {
                TempData["Error"] =
                    "Bạn đang có ca làm chưa đóng.";

                return RedirectToAction("Index");
            }

            return View();
        }

        // MỞ CA - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Open(decimal openingCash, string note)
        {
            if (!IsAuthorized())
                return RedirectToAction("Index", "Home");

            if (openingCash < 0)
            {
                ModelState.AddModelError(
                    "openingCash",
                    "Tiền đầu ca không được nhỏ hơn 0."
                );

                return View();
            }

            int accountId = CurrentAccountId.Value;

            bool hasOpenShift = db.WorkShifts.Any(s =>
                s.AccountId == accountId &&
                s.Status == "Open"
            );

            if (hasOpenShift)
            {
                TempData["Error"] =
                    "Bạn đang có ca làm chưa đóng.";

                return RedirectToAction("Index");
            }

            var shift = new WorkShift
            {
                ShiftCode = "CA-" +
                    DateTime.Now.ToString("yyyyMMddHHmmss") +
                    "-" + accountId,

                AccountId = accountId,
                StartTime = DateTime.Now,
                OpeningCash = openingCash,
                ExpectedCash = openingCash,
                Status = "Open",
                Note = note
            };

            db.WorkShifts.Add(shift);
            db.SaveChanges();

            TempData["Success"] =
                "Mở ca làm việc thành công.";

            return RedirectToAction("Index");
        }

        // ĐÓNG CA - GET
        public ActionResult Close(long id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Index", "Home");

            int accountId = CurrentAccountId.Value;

            var shift = db.WorkShifts.FirstOrDefault(s =>
                s.ShiftId == id &&
                s.AccountId == accountId &&
                s.Status == "Open"
            );

            if (shift == null)
                return HttpNotFound();

            var model = new WorkShiftCloseViewModel
            {
                ShiftId = shift.ShiftId,
                ActualCash = 0
            };

            return View(model);
        }

        // ĐÓNG CA - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Close(WorkShiftCloseViewModel model)
        {
            if (!IsAuthorized())
                return RedirectToAction("Index", "Home");

            if (model.ActualCash < 0)
            {
                ModelState.AddModelError(
                    "ActualCash",
                    "Tiền mặt thực tế không được nhỏ hơn 0."
                );

                return View(model);
            }

            int accountId = CurrentAccountId.Value;

            var shift = db.WorkShifts.FirstOrDefault(s =>
                s.ShiftId == model.ShiftId &&
                s.AccountId == accountId &&
                s.Status == "Open"
            );

            if (shift == null)
                return HttpNotFound();

            // Tính tiền mặt dự kiến:
            // Tiền đầu ca + doanh thu tiền mặt từ các đơn
            // POS đã thanh toán được gắn với ca.
            decimal cashRevenue =
                (from link in db.WorkShiftOrders
                 join order in db.Orders
                    on link.OrderId equals order.OrderId
                 where link.ShiftId == shift.ShiftId
                    && order.PaymentMethod == "Cash"
                    && order.PaymentStatus == "Paid"
                    && order.OrderStatus != "Cancelled"
                 select (decimal?)order.TotalAmount
                ).Sum() ?? 0;

            shift.ExpectedCash =
                shift.OpeningCash + cashRevenue;

            shift.ActualCash = model.ActualCash;

            shift.CashDifference =
                model.ActualCash - shift.ExpectedCash;

            shift.EndTime = DateTime.Now;
            shift.Status = "Closed";

            if (!string.IsNullOrWhiteSpace(model.Note))
                shift.Note = model.Note;

            db.SaveChanges();

            TempData["Success"] =
                "Đóng ca thành công.";

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
