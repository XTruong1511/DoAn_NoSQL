using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class TaiKhoanController : BaseController
    {
        public ActionResult Index()
        {
            if (!IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var taiKhoan = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, CurrentUser.Id)).FirstOrDefault();
            if (taiKhoan == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            return View(taiKhoan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhat(string hoTen, string email, string soDienThoai)
        {
            if (!IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc";
                return RedirectToAction("Index");
            }

            try
            {
                var taiKhoan = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, CurrentUser.Id)).FirstOrDefault();
                if (taiKhoan == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản";
                    return RedirectToAction("Index");
                }

                var filter = Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, CurrentUser.Id);
                var update = Builders<tb_TaiKhoan>.Update
                    .Set(x => x.HoTen, hoTen.Trim())
                    .Set(x => x.Email, email.Trim())
                    .Set(x => x.SoDienThoai, soDienThoai?.Trim());
                
                db.tb_TaiKhoan.Collection.UpdateOne(filter, update);

                var userSession = new UserSessionViewModel
                {
                    Id = taiKhoan.Id,
                    HoTen = hoTen.Trim(),
                    Email = email.Trim(),
                    LaQuanTri = taiKhoan.LaQuanTri ?? false
                };
                Session["User"] = userSession;
                Session["UserName"] = hoTen.Trim();

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult DoiMatKhau()
        {
            if (!IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            if (!IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(matKhauCu) ||
                string.IsNullOrWhiteSpace(matKhauMoi) ||
                string.IsNullOrWhiteSpace(xacNhanMatKhau))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin";
                return View();
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp";
                return View();
            }

            if (matKhauMoi.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự";
                return View();
            }

            try
            {
                var taiKhoan = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, CurrentUser.Id)).FirstOrDefault();
                if (taiKhoan == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản";
                    return View();
                }

                string inputHash = HashPassword(matKhauCu);
                if (taiKhoan.MatKhau != inputHash && taiKhoan.MatKhau != matKhauCu)
                {
                    TempData["ErrorMessage"] = "Mật khẩu cũ không đúng";
                    return View();
                }

                string newHash = HashPassword(matKhauMoi);
                var filter = Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, CurrentUser.Id);
                var update = Builders<tb_TaiKhoan>.Update.Set(x => x.MatKhau, newHash);
                db.tb_TaiKhoan.Collection.UpdateOne(filter, update);

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return View();
            }
        }

        public ActionResult DonHang(int page = 1, int pageSize = 10)
        {
            if (!IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.KhachHang.TaiKhoanId, CurrentUser.Id);
            var query = db.tb_DonHang.Collection.Find(filter).SortByDescending(dh => dh.NgayTao);

            var totalOrders = (int)db.tb_DonHang.Collection.CountDocuments(filter);
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalOrders = totalOrders;

            var orders = query
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            return View(orders);
        }

        public ActionResult ChiTietDonHang(int id)
        {
            if (!IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.Id, id) & Builders<tb_DonHang>.Filter.Eq(dh => dh.KhachHang.TaiKhoanId, CurrentUser.Id);
            var donHang = db.tb_DonHang.Collection.Find(filter).FirstOrDefault();

            if (donHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("DonHang");
            }

            try
            {
                var chiTietViewModel = donHang.DanhSachSanPham?.Select(sp => new ChiTietDonHangViewModel
                {
                    SanPhamId = sp.SanPhamId,
                    TenSanPham = !string.IsNullOrEmpty(sp.TenSanPham) ? sp.TenSanPham : "Không có tên",
                    HinhAnh = sp.HinhAnh ?? "",
                    SoLuong = sp.SoLuong,
                    Gia = sp.Gia,
                    ThanhTien = sp.ThanhTien > 0 ? sp.ThanhTien : (sp.SoLuong * sp.Gia)
                }).ToList() ?? new List<ChiTietDonHangViewModel>();

                ViewBag.ChiTiet = chiTietViewModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading order details: {ex.Message}");
                ViewBag.ChiTiet = new List<ChiTietDonHangViewModel>();
                TempData["ErrorMessage"] = "Có lỗi khi tải chi tiết đơn hàng: " + ex.Message;
            }

            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult HuyDonHang(int id)
        {
            if (!IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.Id, id) & Builders<tb_DonHang>.Filter.Eq(dh => dh.KhachHang.TaiKhoanId, CurrentUser.Id);
                var donHang = db.tb_DonHang.Collection.Find(filter).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai > 2)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng này" });
                }

                var update = Builders<tb_DonHang>.Update
                    .Set(dh => dh.TrangThai, -1)
                    .Set(dh => dh.NgayCapNhat, DateTime.Now);
                db.tb_DonHang.Collection.UpdateOne(filter, update);

                if (donHang.DanhSachSanPham != null)
                {
                    foreach (var item in donHang.DanhSachSanPham)
                    {
                        if (item.SanPhamId > 0 && item.SoLuong > 0)
                        {
                            var spUpdate = Builders<tb_SanPham>.Update.Inc(sp => sp.SoLuong, item.SoLuong);
                            db.tb_SanPham.Collection.UpdateOne(Builders<tb_SanPham>.Filter.Eq(sp => sp.Id, item.SanPhamId), spUpdate);
                        }
                    }
                }

                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult XacNhanDaNhan(int id)
        {
            if (!IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.Id, id) & Builders<tb_DonHang>.Filter.Eq(dh => dh.KhachHang.TaiKhoanId, CurrentUser.Id);
                var donHang = db.tb_DonHang.Collection.Find(filter).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai != 3)
                {
                    return Json(new { success = false, message = "Không thể xác nhận đơn hàng này" });
                }

                var update = Builders<tb_DonHang>.Update
                    .Set(dh => dh.TrangThai, 4)
                    .Set(dh => dh.NgayCapNhat, DateTime.Now);
                db.tb_DonHang.Collection.UpdateOne(filter, update);

                return Json(new { success = true, message = "Cảm ơn bạn đã xác nhận! Đơn hàng đã hoàn thành." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult YeuCauTraHang(int id, string lyDo)
        {
            if (!IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.Id, id) & Builders<tb_DonHang>.Filter.Eq(dh => dh.KhachHang.TaiKhoanId, CurrentUser.Id);
                var donHang = db.tb_DonHang.Collection.Find(filter).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai != 4)
                {
                    return Json(new { success = false, message = "Chỉ có thể yêu cầu trả hàng sau khi đã nhận được hàng" });
                }

                if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Length < 10)
                {
                    return Json(new { success = false, message = "Vui lòng nhập lý do trả hàng (tối thiểu 10 ký tự)" });
                }

                var update = Builders<tb_DonHang>.Update
                    .Set(dh => dh.TrangThai, 6)
                    .Set(dh => dh.NgayCapNhat, DateTime.Now);
                db.tb_DonHang.Collection.UpdateOne(filter, update);

                return Json(new { success = true, message = "Đã gửi yêu cầu trả hàng. Admin sẽ xem xét và liên hệ với bạn sớm nhất." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult XoaDonHang(int id)
        {
            if (!IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                var filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.Id, id) & Builders<tb_DonHang>.Filter.Eq(dh => dh.KhachHang.TaiKhoanId, CurrentUser.Id);
                var donHang = db.tb_DonHang.Collection.Find(filter).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai != -1)
                {
                    return Json(new { success = false, message = "Chỉ có thể xóa đơn hàng đã hủy" });
                }

                db.tb_DonHang.Collection.DeleteOne(filter);

                return Json(new { success = true, message = "Đã xóa đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
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
