using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using DoAnNoSQL.Models;
using DoAnNoSQL.Filters;

namespace DoAnNoSQL.Controllers
{
    public partial class AdminController : Controller
    {
        private MongoDbContext db = new MongoDbContext();
        
        // GET: Admin - Tổng quan
        [AdminAuthorize(Roles = "1")]
        public ActionResult TongQuan()
        {
            var completedFilter = Builders<tb_DonHang>.Filter.Eq(d => d.TrangThai, 5);
            var totalRevenueResult = db.tb_DonHang.Collection.Aggregate()
                .Match(completedFilter)
                .Group(d => 1, g => new { Total = g.Sum(d => (decimal)d.TongTien) })
                .FirstOrDefault();
            var totalRevenue = totalRevenueResult != null ? totalRevenueResult.Total : 0;

            var totalOrders = db.tb_DonHang.Collection.CountDocuments(Builders<tb_DonHang>.Filter.Empty);

            var totalCustomers = db.tb_TaiKhoan.Collection.CountDocuments(Builders<tb_TaiKhoan>.Filter.Eq(t => t.LaQuanTri, false));

            var totalProductsResult = db.tb_SanPham.Collection.Aggregate()
                .Group(s => 1, g => new { Total = g.Sum(s => s.SoLuong) })
                .FirstOrDefault();
            var totalProducts = totalProductsResult != null ? (int?)totalProductsResult.Total ?? 0 : 0;

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var revenueFilter = Builders<tb_DonHang>.Filter.Gte(d => d.NgayTao, sixMonthsAgo) & completedFilter;
            
            var revenueDocs = db.tb_DonHang.Collection.Aggregate()
                .Match(revenueFilter)
                .ToList();

            var revenueByMonth = revenueDocs
                .Where(d => d.NgayTao.HasValue)
                .GroupBy(d => new { d.NgayTao.Value.Year, d.NgayTao.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(d => d.TongTien)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var ordersWithItems = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Ne(d => d.DanhSachSanPham, null))
                .Project(d => d.DanhSachSanPham)
                .ToList();

            var topProductsRaw = ordersWithItems
                .SelectMany(list => list ?? Enumerable.Empty<DonHangSanPham>())
                .GroupBy(p => p.SanPhamId)
                .Select(g => new
                {
                    ProductId = (int?)g.Key,
                    TotalQuantity = (int?)g.Sum(x => x.SoLuong),
                    ProductName = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.TenSanPham))?.TenSanPham
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();

            var topProducts = topProductsRaw.Select(g => new
            {
                ProductId = g.ProductId,
                TotalQuantity = g.TotalQuantity,
                ProductName = g.ProductName
            }).ToList();

            var recentOrdersRaw = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Empty)
                .SortByDescending(d => d.NgayTao)
                .Limit(10)
                .ToList();

            var recentOrders = recentOrdersRaw.Select(d => new RecentOrderViewModel
            {
                Id = d.Id,
                MaDonHang = d.MaDonHang,
                TenKhachHang = d.TenKhachHang,
                TongTien = d.TongTien,
                TrangThai = d.TrangThai,
                NgayTao = d.NgayTao
            }).ToList();

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.RevenueByMonth = revenueByMonth;
            ViewBag.TopProducts = topProducts;
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // db.Dispose(); // MongoDbContext không cần dispose
            }
            base.Dispose(disposing);
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            if (Session["User"] != null && (bool?)Session["IsAdmin"] == true)
            {
                int roleId = Convert.ToInt32(Session["Role"]);
                if (roleId == 1) return RedirectToAction("TongQuan");
                if (roleId == 2) return RedirectToAction("BanHang");
                if (roleId == 3) return RedirectToAction("BanHang");
                if (roleId == 4) return RedirectToAction("QLSP");
                if (roleId == 5) return RedirectToAction("BaoHanh");
                return RedirectToAction("TongQuan");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var builder = Builders<tb_TaiKhoan>.Filter;
                var filter = builder.And(
                    builder.Eq(u => u.HoatDong, true),
                    builder.Eq(u => u.LaQuanTri, true), // Only admin
                    builder.Or(
                        builder.Eq(u => u.Email, model.EmailOrPhone),
                        builder.Eq(u => u.SoDienThoai, model.EmailOrPhone)
                    )
                );
                
                var user = db.tb_TaiKhoan.Collection.Find(filter).FirstOrDefault();

                if (user != null)
                {
                    bool isPasswordValid = false;
                    string inputHash = HashPassword(model.Password);
                    
                    if (user.MatKhau == inputHash || user.MatKhau == model.Password)
                    {
                        isPasswordValid = true;
                        if (user.MatKhau == model.Password)
                        {
                            var updateFilter = Builders<tb_TaiKhoan>.Filter.Eq(u => u.Id, user.Id);
                            var update = Builders<tb_TaiKhoan>.Update.Set(u => u.MatKhau, inputHash);
                            db.tb_TaiKhoan.Collection.UpdateOne(updateFilter, update);
                        }
                    }

                    if (isPasswordValid)
                    {
                        var userSession = new UserSessionViewModel
                        {
                            Id = user.Id,
                            HoTen = user.HoTen,
                            Email = user.Email,
                            LaQuanTri = user.LaQuanTri ?? false
                        };
                        Session["User"] = userSession;
                        Session["UserId"] = user.Id;
                        Session["UserName"] = user.HoTen;
                        Session["UserEmail"] = user.Email;
                        Session["IsAdmin"] = true;
                        
                        int roleId = user.QuyenId ?? 1;
                        Session["Role"] = roleId;

                        if (roleId == 1) return RedirectToAction("TongQuan");
                        if (roleId == 2) return RedirectToAction("BanHang");
                        if (roleId == 3) return RedirectToAction("BanHang");
                        if (roleId == 4) return RedirectToAction("QLSP");
                        if (roleId == 5) return RedirectToAction("BaoHanh");
                        return RedirectToAction("TongQuan");
                    }
                }
                
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
            
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}
