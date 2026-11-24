
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.IO;
using System.Collections.Generic;
using MocBrewManagement.Models;

namespace MocBrewManagement.Controllers
{
    public class ProductsController : Controller
    {
        private readonly MOCBREWEntities db = new MOCBREWEntities();

        // ==========================================
        // KIỂM TRA QUYỀN XEM SẢN PHẨM
        // ==========================================
        private bool CanViewProducts()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            // Store Manager
            if (roleId == 2)
                return true;

            // Staff cần MENU_VIEW hoặc MENU_MANAGE
            if (roleId == 3)
            {
                int accountId = Convert.ToInt32(
                    Session["MOCBREW_AccountId"]);

                return db.UserPermissions.Any(up =>
                    up.AccountId == accountId &&
                    up.IsActive &&
                    (up.Permission.PermissionCode == "MENU_VIEW" ||
                     up.Permission.PermissionCode == "MENU_MANAGE"));
            }

            return false;
        }

        // ==========================================
        // KIỂM TRA QUYỀN QUẢN LÝ SẢN PHẨM
        // ==========================================
        private bool CanManageProducts()
        {
            if (Session["MOCBREW_AccountId"] == null ||
                Session["MOCBREW_RoleId"] == null)
            {
                return false;
            }

            int roleId = Convert.ToInt32(Session["MOCBREW_RoleId"]);

            // Store Manager
            if (roleId == 2)
                return true;

            // Staff cần MENU_MANAGE
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

        // ==========================================
        // DANH SÁCH SẢN PHẨM
        // ==========================================
        public ActionResult Index(
            string keyword,
            int? categoryId,
            int page = 1)
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!CanViewProducts())
            {
                return RedirectToAction("Index", "Home");
            }

            if (page < 1)
                page = 1;

            int pageSize = 10;

            // Lấy danh sách danh mục
            var categories = db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToList();

            // Lấy sản phẩm và các Size liên quan
            var query = db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes.Select(ps => ps.Size))
                .AsQueryable();

            // Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(p =>
                    p.ProductName.Contains(keyword));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            int totalRecords = query.Count();

            // Phân trang
            var products = query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new ProductViewModel();

            model.Categories = categories;
            model.Keyword = keyword;
            model.CategoryId = categoryId;
            model.CurrentPage = page;
            model.PageSize = pageSize;
            model.TotalRecords = totalRecords;

            model.TotalPages = (int)Math.Ceiling(
                (double)totalRecords / pageSize);

            // Thống kê sản phẩm
            model.TotalActive = db.Products.Count(p => p.IsActive);

            model.TotalInactive = db.Products.Count(p => !p.IsActive);

            model.TotalFeatured = db.Products.Count(p => p.IsFeatured);

            // Chuyển dữ liệu sang ViewModel
            model.Products = products.Select(p =>
            {
                var availableSizes = p.ProductSizes
                    .Where(ps => ps.IsAvailable)
                    .ToList();

                var sizeS = availableSizes.FirstOrDefault(
                    ps => ps.Size != null && ps.Size.SizeName == "S");

                var sizeM = availableSizes.FirstOrDefault(
                    ps => ps.Size != null && ps.Size.SizeName == "M");

                var sizeL = availableSizes.FirstOrDefault(
                    ps => ps.Size != null && ps.Size.SizeName == "L");

                return new ProductListItemViewModel
                {
                    ProductId = p.ProductId,

                    ProductName = p.ProductName,

                    CategoryName = p.Category != null
                        ? p.Category.CategoryName
                        : "Chưa phân loại",

                    ImageUrl = p.ImageUrl,

                    Description = p.Description,

                    IsFeatured = p.IsFeatured,

                    IsActive = p.IsActive,

                    CreatedAt = p.CreatedAt,

                    SizeSummary = string.Join(", ",
                        availableSizes
                            .Where(ps => ps.Size != null)
                            .Select(ps => ps.Size.SizeName)
                            .Distinct()),

                    // Giá từng Size
                    PriceS = sizeS != null
                        ? (decimal?)sizeS.Price
                        : null,

                    PriceM = sizeM != null
                        ? (decimal?)sizeM.Price
                        : null,

                    PriceL = sizeL != null
                        ? (decimal?)sizeL.Price
                        : null,

                    // Giữ lại giá thấp nhất để tương thích code cũ
                    MinPrice = availableSizes
                        .Select(ps => (decimal?)ps.Price)
                        .Min()
                };
            }).ToList();

            return View(model);
        }


        // ==========================================
        // GET: THÊM SẢN PHẨM
        // ==========================================
        public ActionResult Create()
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!CanManageProducts())
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ProductCreateViewModel
            {
                Categories = db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToList(),

                IsActive = true,
                IsFeatured = false
            };

            return View(model);
        }

        // ==========================================
        // POST: LƯU SẢN PHẨM
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductCreateViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            if (!CanManageProducts())
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra danh mục
            var category = db.Categories.FirstOrDefault(c =>
                c.CategoryId == model.CategoryId && c.IsActive);

            if (category == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Danh mục không hợp lệ hoặc đã ngừng hoạt động.");
            }

            // Lấy Size từ database
            var sizeS = db.Sizes.FirstOrDefault(s =>
                s.SizeName == "S" && s.IsActive);

            var sizeM = db.Sizes.FirstOrDefault(s =>
                s.SizeName == "M" && s.IsActive);

            var sizeL = db.Sizes.FirstOrDefault(s =>
                s.SizeName == "L" && s.IsActive);

            if (sizeS == null)
            {
                ModelState.AddModelError(
                    "PriceS",
                    "Không tìm thấy Size S đang hoạt động trong hệ thống.");
            }

            if (model.PriceM.HasValue && sizeM == null)
            {
                ModelState.AddModelError(
                    "PriceM",
                    "Không tìm thấy Size M đang hoạt động trong hệ thống.");
            }

            if (model.PriceL.HasValue && sizeL == null)
            {
                ModelState.AddModelError(
                    "PriceL",
                    "Không tìm thấy Size L đang hoạt động trong hệ thống.");
            }

            // Kiểm tra hình ảnh
            if (model.ImageFile != null &&
                model.ImageFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(
                    model.ImageFile.FileName).ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");
                }

                if (model.ImageFile.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Dung lượng ảnh không được vượt quá 5 MB.");
                }
            }

            // Nếu dữ liệu không hợp lệ
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e =>
                        x.Key + ": " +
                        (string.IsNullOrEmpty(e.ErrorMessage)
                            ? e.Exception?.Message
                            : e.ErrorMessage)))
                    .ToList();

                TempData["ProductError"] =
                    string.Join(" | ", errors);

                model.Categories = db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                return View(model);
            }

            string imageUrl = null;

            try
            {
                // Lưu ảnh nếu có
                if (model.ImageFile != null &&
                    model.ImageFile.ContentLength > 0)
                {
                    string folderPath = Server.MapPath(
                        "~/Content/Images/Products");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = Guid.NewGuid().ToString("N")
                        + Path.GetExtension(model.ImageFile.FileName)
                            .ToLowerInvariant();

                    string fullPath = Path.Combine(
                        folderPath, fileName);

                    model.ImageFile.SaveAs(fullPath);

                    imageUrl = "~/Content/Images/Products/" + fileName;
                }

                // Lưu sản phẩm và giá Size trong transaction
                using (var transaction = db.Database.BeginTransaction())
                {
                    var product = new Product
                    {
                        CategoryId = model.CategoryId,
                        ProductName = model.ProductName.Trim(),
                        Description = model.Description,
                        ImageUrl = imageUrl,
                        IsFeatured = model.IsFeatured,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.Now
                    };

                    db.Products.Add(product);
                    db.SaveChanges();

                    // Size S bắt buộc
                    db.ProductSizes.Add(new ProductSize
                    {
                        ProductId = product.ProductId,
                        SizeId = sizeS.SizeId,
                        Price = model.PriceS.Value,
                        IsAvailable = true
                    });

                    // Size M nếu có giá
                    if (model.PriceM.HasValue)
                    {
                        db.ProductSizes.Add(new ProductSize
                        {
                            ProductId = product.ProductId,
                            SizeId = sizeM.SizeId,
                            Price = model.PriceM.Value,
                            IsAvailable = true
                        });
                    }

                    // Size L nếu có giá
                    if (model.PriceL.HasValue)
                    {
                        db.ProductSizes.Add(new ProductSize
                        {
                            ProductId = product.ProductId,
                            SizeId = sizeL.SizeId,
                            Price = model.PriceL.Value,
                            IsAvailable = true
                        });
                    }

                    db.SaveChanges();
                    transaction.Commit();
                }

                TempData["ProductSuccess"] =
                    "Thêm sản phẩm và giá theo Size thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    string fullPath = Server.MapPath(imageUrl);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                ModelState.AddModelError(
                    "",
                    "Không thể lưu sản phẩm: " +
                    ex.GetBaseException().Message);

                model.Categories = db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                return View(model);
            }
        }
            

        // ==========================================
        // GET: SỬA SẢN PHẨM
        // ==========================================
        public ActionResult Edit(int id)
        {
            // Kiểm tra đăng nhập
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            // Kiểm tra quyền sửa sản phẩm
            if (!CanManageProducts())
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy sản phẩm cần sửa
            var product = db.Products
                .Include(p => p.ProductSizes.Select(ps => ps.Size))
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            // Lấy các Size đang có
            var sizeS = product.ProductSizes
                .FirstOrDefault(ps =>
                    ps.Size != null &&
                    ps.Size.SizeName == "S");

            var sizeM = product.ProductSizes
                .FirstOrDefault(ps =>
                    ps.Size != null &&
                    ps.Size.SizeName == "M");

            var sizeL = product.ProductSizes
                .FirstOrDefault(ps =>
                    ps.Size != null &&
                    ps.Size.SizeName == "L");

            // Đưa dữ liệu vào ViewModel
            var model = new ProductEditViewModel
            {
                ProductId = product.ProductId,

                ProductName = product.ProductName,

                CategoryId = product.CategoryId,

                Description = product.Description,

                IsFeatured = product.IsFeatured,

                IsActive = product.IsActive,

                CurrentImageUrl = product.ImageUrl,

                Categories = db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToList(),

                // Giá Size S
                PriceS = sizeS != null
                    ? (decimal?)sizeS.Price
                    : null,

                // Giá Size M
                PriceM = sizeM != null
                    ? (decimal?)sizeM.Price
                    : null,

                // Giá Size L
                PriceL = sizeL != null
                    ? (decimal?)sizeL.Price
                    : null,

                HasSizeS = sizeS != null && sizeS.IsAvailable,

                HasSizeM = sizeM != null && sizeM.IsAvailable,

                HasSizeL = sizeL != null && sizeL.IsAvailable
            };

            return View(model);
        }

        // ==========================================
        // POST: CẬP NHẬT SẢN PHẨM
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductEditViewModel model)
        {
            // Kiểm tra đăng nhập
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            // Kiểm tra quyền sửa sản phẩm
            if (!CanManageProducts())
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy sản phẩm cần cập nhật
            var product = db.Products
                .Include(p => p.ProductSizes.Select(ps => ps.Size))
                .FirstOrDefault(p => p.ProductId == model.ProductId);

            if (product == null)
            {
                return HttpNotFound();
            }


            // Kiểm tra danh mục được chọn
            var category = db.Categories.FirstOrDefault(c =>
                c.CategoryId == model.CategoryId && c.IsActive);

            if (category == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Danh mục không hợp lệ hoặc đã ngừng hoạt động.");
            }
            else
            {
                // Nếu là danh mục con, kiểm tra danh mục cha
                if (category.ParentCategoryId.HasValue)
                {
                    bool parentIsActive = db.Categories.Any(c =>
                        c.CategoryId == category.ParentCategoryId.Value &&
                        c.IsActive);

                    if (!parentIsActive)
                    {
                        ModelState.AddModelError(
                            "CategoryId",
                            "Không thể thêm sản phẩm vì danh mục cha đang ngừng hoạt động.");
                    }
                }
            }


            // Kiểm tra giá các Size đang được chọn
            if (model.HasSizeS &&
                (!model.PriceS.HasValue || model.PriceS.Value <= 0))
            {
                ModelState.AddModelError(
                    "PriceS",
                    "Vui lòng nhập giá Size S lớn hơn 0.");
            }

            if (model.HasSizeM &&
                (!model.PriceM.HasValue || model.PriceM.Value <= 0))
            {
                ModelState.AddModelError(
                    "PriceM",
                    "Vui lòng nhập giá Size M lớn hơn 0.");
            }

            if (model.HasSizeL &&
                (!model.PriceL.HasValue || model.PriceL.Value <= 0))
            {
                ModelState.AddModelError(
                    "PriceL",
                    "Vui lòng nhập giá Size L lớn hơn 0.");
            }

            // Kiểm tra hình ảnh
            if (model.ImageFile != null &&
                model.ImageFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(
                    model.ImageFile.FileName).ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");
                }

                if (model.ImageFile.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Dung lượng ảnh không được vượt quá 5 MB.");
                }
            }

            // Nếu dữ liệu không hợp lệ
            if (!ModelState.IsValid)
            {
                model.Categories = db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                model.CurrentImageUrl = product.ImageUrl;

                return View(model);
            }

            string newImageUrl = null;
            string oldImageUrl = product.ImageUrl;

            try
            {
                // Lưu ảnh mới nếu người dùng chọn ảnh
                if (model.ImageFile != null &&
                    model.ImageFile.ContentLength > 0)
                {
                    string folderPath = Server.MapPath(
                        "~/Content/Images/Products");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = Guid.NewGuid().ToString("N")
                        + Path.GetExtension(model.ImageFile.FileName)
                            .ToLowerInvariant();

                    string fullPath = Path.Combine(
                        folderPath, fileName);

                    model.ImageFile.SaveAs(fullPath);

                    newImageUrl =
                        "~/Content/Images/Products/" + fileName;
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    // Cập nhật thông tin sản phẩm
                    product.ProductName = model.ProductName.Trim();
                    product.CategoryId = model.CategoryId;
                    product.Description = model.Description;
                    product.IsFeatured = model.IsFeatured;
                    product.IsActive = model.IsActive;

                    if (!string.IsNullOrEmpty(newImageUrl))
                    {
                        product.ImageUrl = newImageUrl;
                    }

                    // Lấy danh sách Size trong database
                    var sizes = db.Sizes
                        .Where(s => s.IsActive)
                        .ToList();

                    // Cập nhật từng Size
                    UpdateProductSize(
                        product,
                        sizes,
                        "S",
                        model.HasSizeS,
                        model.PriceS);

                    UpdateProductSize(
                        product,
                        sizes,
                        "M",
                        model.HasSizeM,
                        model.PriceM);

                    UpdateProductSize(
                        product,
                        sizes,
                        "L",
                        model.HasSizeL,
                        model.PriceL);

                    db.SaveChanges();

                    transaction.Commit();
                }

                // Xóa ảnh cũ sau khi cập nhật thành công
                if (!string.IsNullOrEmpty(newImageUrl) &&
                    !string.IsNullOrEmpty(oldImageUrl))
                {
                    string oldFullPath = Server.MapPath(oldImageUrl);

                    if (System.IO.File.Exists(oldFullPath))
                    {
                        System.IO.File.Delete(oldFullPath);
                    }
                }

                TempData["ProductSuccess"] =
                    "Cập nhật sản phẩm thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Xóa ảnh mới nếu cập nhật thất bại
                if (!string.IsNullOrEmpty(newImageUrl))
                {
                    string newFullPath = Server.MapPath(newImageUrl);

                    if (System.IO.File.Exists(newFullPath))
                    {
                        System.IO.File.Delete(newFullPath);
                    }
                }

                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật sản phẩm: " +
                    ex.GetBaseException().Message);

                model.Categories = db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                model.CurrentImageUrl = product.ImageUrl;

                return View(model);
            }
        }

        // ==========================================
        // HÀM HỖ TRỢ CẬP NHẬT SIZE
        // ==========================================
        private void UpdateProductSize(
            Product product,
            List<Size> sizes,
            string sizeName,
            bool isAvailable,
            decimal? price)
        {
            var size = sizes.FirstOrDefault(s =>
                s.SizeName == sizeName);

            if (size == null)
            {
                if (isAvailable)
                {
                    throw new Exception(
                        "Không tìm thấy Size " + sizeName +
                        " đang hoạt động.");
                }

                return;
            }

            var productSize = product.ProductSizes
                .FirstOrDefault(ps => ps.SizeId == size.SizeId);

            if (productSize == null)
            {
                // Chỉ tạo mới khi Size được bật
                if (isAvailable)
                {
                    product.ProductSizes.Add(new ProductSize
                    {
                        ProductId = product.ProductId,
                        SizeId = size.SizeId,
                        Price = price.Value,
                        IsAvailable = true
                    });
                }
            }
            else
            {
                // Giữ bản ghi, chỉ bật/tắt bán
                productSize.IsAvailable = isAvailable;

                if (isAvailable)
                {
                    productSize.Price = price.Value;
                }
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            if (!CanManageProducts())
                return RedirectToAction("Index", "Home");

            var product = db.Products
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return HttpNotFound();

            try
            {
                // Nếu sản phẩm đang ngừng kinh doanh,
                // kiểm tra danh mục trước khi mở bán lại.
                if (!product.IsActive)
                {
                    var category = db.Categories
                        .FirstOrDefault(c =>
                            c.CategoryId == product.CategoryId);

                    if (category == null || !category.IsActive)
                    {
                        TempData["ProductError"] =
                            "Không thể mở bán sản phẩm vì danh mục đang ngừng hoạt động. " +
                            "Vui lòng kích hoạt danh mục trước.";

                        return RedirectToAction("Index");
                    }
                }

                // Đảo trạng thái kinh doanh
                product.IsActive = !product.IsActive;

                db.SaveChanges();

                TempData["ProductSuccess"] = product.IsActive
                    ? "Đã mở bán lại sản phẩm thành công!"
                    : "Đã ngừng kinh doanh sản phẩm thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ProductError"] =
                    "Không thể cập nhật trạng thái: " +
                    ex.GetBaseException().Message;

                return RedirectToAction("Index");
            }
        }

        // ==========================================
        // GIẢI PHÓNG DATABASE
        // ==========================================
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
