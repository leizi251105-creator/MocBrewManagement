
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Mvc;
using MocBrewManagement.Models;

namespace MocBrewManagement.Controllers
{
    public class MocBrewAuthController : Controller
    {
        private MOCBREWEntities db = new MOCBREWEntities();

        // GET: MocBrewAuth/Login
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["MOCBREW_AccountId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: MocBrewAuth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return View();
            }

            var account = db.Accounts
                .Include("Role")
                .FirstOrDefault(a =>
                    a.Username == username &&
                    a.IsActive == true);

            if (account == null ||
                account.Role == null ||
                !account.Role.IsActive)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return View();
            }

            if (!VerifyPassword(password, account.PasswordHash))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return View();
            }

            // Lưu thông tin đăng nhập
            Session["MOCBREW_AccountId"] = account.AccountId;
            Session["MOCBREW_Username"] = account.Username;
            Session["MOCBREW_FullName"] = account.FullName;
            Session["MOCBREW_RoleId"] = account.RoleId;
            Session["MOCBREW_RoleName"] = account.Role.RoleName;

            return RedirectToAction("Index", "Home");
        }

        // Kiểm tra mật khẩu PBKDF2
        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(storedHash))
                    return false;

                string[] parts = storedHash.Split('$');

                if (parts.Length != 4 || parts[0] != "PBKDF2")
                    return false;

                int iterations;

                if (!int.TryParse(parts[1], out iterations) ||
                    iterations < 10000 ||
                    iterations > 1000000)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                if (salt.Length != 16 || expectedHash.Length != 32)
                    return false;

                byte[] actualHash;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password, salt, iterations))
                {
                    actualHash = pbkdf2.GetBytes(32);
                }

                if (actualHash.Length != expectedHash.Length)
                    return false;

                int difference = 0;

                for (int i = 0; i < actualHash.Length; i++)
                {
                    difference |= actualHash[i] ^ expectedHash[i];
                }

                return difference == 0;
            }
            catch
            {
                return false;
            }
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "MocBrewAuth");
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

