
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MocBrewManagement.Models;

namespace MocBrewManagement.Controllers
{
    public class SystemLogController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        // Kiểm tra quyền Super Admin
        private bool IsSuperAdmin()
        {
            return Session["MOCBREW_AccountId"] != null
                && Session["MOCBREW_RoleId"] != null
                && Convert.ToInt32(Session["MOCBREW_RoleId"]) == 1;
        }

        // Trang danh sách nhật ký hoạt động
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            // Chỉ Super Admin được truy cập
            if (!IsSuperAdmin())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền xem nhật ký hệ thống."
                );
            }

            // Lấy danh sách nhật ký
            var logs = db.SystemLogs
                .Include(s => s.Account)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            ViewBag.FullName = Session["MOCBREW_FullName"];
            ViewBag.RoleName = "Super Admin";

            ViewBag.TotalLogs = logs.Count;
            ViewBag.TechnicalLogs = logs.Count(
                s => s.LogType == "TECHNICAL");

            ViewBag.BusinessLogs = logs.Count(
                s => s.LogType == "BUSINESS");

            return View(logs);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
