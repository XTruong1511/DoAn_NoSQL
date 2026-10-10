using System;
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
    public partial class AdminController
    {
        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult KhachHang(int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_TaiKhoan>.Filter.Eq(kh => kh.LaQuanTri, false);

            var lstKH = db.tb_TaiKhoan.Collection.Find(filter)
                .SortByDescending(kh => kh.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            int totalItems = (int)db.tb_TaiKhoan.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = "";
            return View(lstKH);
        }

        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult formThemKH()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult ThemKH(tb_TaiKhoan kh)
        {
            if (ModelState.IsValid)
            {
                if (db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Email, kh.Email)).Any())
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký!");
                    return View("formThemKH", kh);
                }

                kh.Id = (int)db.GetNextSequenceValue("tb_TaiKhoan");
                kh.LaQuanTri = false;
                db.tb_TaiKhoan.Collection.InsertOne(kh);
                TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                return RedirectToAction("KhachHang");
            }
            return View("formThemKH", kh);
        }

        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult formSuaKH(int id)
        {
            var kh = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (kh == null)
            {
                return HttpNotFound();
            }
            return View(kh);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult SuaKH(tb_TaiKhoan kh)
        {
            ModelState.Remove("MatKhau");
            
            if (ModelState.IsValid)
            {
                try
                {
                    if (db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Email, kh.Email) & Builders<tb_TaiKhoan>.Filter.Ne(x => x.Id, kh.Id)).Any())
                    {
                        ModelState.AddModelError("Email", "Email này đã được đăng ký cho tài khoản khác!");
                        return View("formSuaKH", kh);
                    }

                    var khGoc = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, kh.Id)).FirstOrDefault();
                    if (khGoc == null)
                    {
                        return HttpNotFound();
                    }
                    
                    var update = Builders<tb_TaiKhoan>.Update
                        .Set(x => x.HoTen, kh.HoTen)
                        .Set(x => x.Email, kh.Email)
                        .Set(x => x.SoDienThoai, kh.SoDienThoai)
                        .Set(x => x.HoatDong, kh.HoatDong);

                    db.tb_TaiKhoan.Collection.UpdateOne(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, kh.Id), update);
                    
                    TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                    return RedirectToAction("KhachHang");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                    return View("formSuaKH", kh);
                }
            }
            
            return View("formSuaKH", kh);
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3")]
        public JsonResult ToggleKhoaTaiKhoan(int id)
        {
            try
            {
                var taiKhoan = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                
                if (taiKhoan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                if (taiKhoan.LaQuanTri == true)
                {
                    return Json(new { success = false, message = "Không thể khóa tài khoản quản trị viên" });
                }

                var newStatus = !(taiKhoan.HoatDong ?? false);
                db.tb_TaiKhoan.Collection.UpdateOne(
                    Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, id),
                    Builders<tb_TaiKhoan>.Update.Set(x => x.HoatDong, newStatus)
                );

                string message = newStatus ? "Đã mở khóa tài khoản thành công" : "Đã khóa tài khoản thành công";

                return Json(new { success = true, message = message, newStatus = newStatus });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult TimKhachHang(string timkh, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_TaiKhoan>.Filter.Eq(kh => kh.LaQuanTri, false);

            if (!String.IsNullOrEmpty(timkh))
            {
                var keyword = timkh.ToLower();
                var searchFilter = Builders<tb_TaiKhoan>.Filter.Regex(kh => kh.HoTen, new BsonRegularExpression(keyword, "i")) |
                                   Builders<tb_TaiKhoan>.Filter.Regex(kh => kh.TenDangNhap, new BsonRegularExpression(keyword, "i")) |
                                   Builders<tb_TaiKhoan>.Filter.Regex(kh => kh.SoDienThoai, new BsonRegularExpression(keyword, "i"));
                filter = filter & searchFilter;
            }

            int totalItems = (int)db.tb_TaiKhoan.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var lstKH = db.tb_TaiKhoan.Collection.Find(filter)
                .SortByDescending(kh => kh.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = timkh;

            return View("KhachHang", lstKH);
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult QuanTriVien(int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_TaiKhoan>.Filter.Eq(kh => kh.LaQuanTri, true);

            var lstKH = db.tb_TaiKhoan.Collection.Find(filter)
                .SortByDescending(kh => kh.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            int totalItems = (int)db.tb_TaiKhoan.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = "";
            return View(lstKH);
        }

        private static readonly System.Collections.Generic.List<tb_Quyen> DefaultRoles = new System.Collections.Generic.List<tb_Quyen>
        {
            new tb_Quyen { Id = 1, TenQuyen = "Quản trị viên" },
            new tb_Quyen { Id = 2, TenQuyen = "Thu ngân" },
            new tb_Quyen { Id = 3, TenQuyen = "Nhân viên bán hàng" },
            new tb_Quyen { Id = 4, TenQuyen = "Nhân viên kho" },
            new tb_Quyen { Id = 5, TenQuyen = "Nhân viên kỹ thuật & sửa chữa" }
        };

        private System.Collections.Generic.List<tb_Quyen> LayDanhSachQuyen()
        {
            return DefaultRoles;
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult formThemQTV()
        {
            var quyenList = LayDanhSachQuyen();
            ViewBag.Quyen = new SelectList(quyenList, "Id", "TenQuyen");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1")]
        public ActionResult ThemQTV(tb_TaiKhoan qtv)
        {
            if (ModelState.IsValid)
            {
                if (db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Email, qtv.Email)).Any())
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng cho tài khoản khác!");
                }
                if (db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.TenDangNhap, qtv.TenDangNhap)).Any())
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại!");
                }

                if (ModelState.IsValid)
                {
                    qtv.Id = (int)db.GetNextSequenceValue("tb_TaiKhoan");
                    qtv.LaQuanTri = true;
                    if (!qtv.QuyenId.HasValue)
                    {
                        qtv.QuyenId = 2; // Default: Staff role
                    }
                    var roleInfo = LayDanhSachQuyen().FirstOrDefault(r => r.Id == qtv.QuyenId.Value);
                    if (roleInfo != null)
                    {
                        qtv.Quyen = new TaiKhoanQuyen { Id = roleInfo.Id, TenQuyen = roleInfo.TenQuyen };
                    }
                    
                    if (!string.IsNullOrEmpty(qtv.MatKhau))
                    {
                        qtv.MatKhau = HashPassword(qtv.MatKhau);
                    }

                    try
                    {
                        db.tb_TaiKhoan.Collection.InsertOne(qtv);
                        TempData["SuccessMessage"] = "Thêm quản trị viên thành công!";
                        return RedirectToAction("QuanTriVien");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                    }
                }
            }
            
            var quyenList = LayDanhSachQuyen();
            ViewBag.Quyen = new SelectList(quyenList, "Id", "TenQuyen", qtv.QuyenId);
            return View("formThemQTV", qtv);
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult formSuaQTV(int id)
        {
            var qtv = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (qtv == null)
            {
                return HttpNotFound();
            }
            
            var quyenList = LayDanhSachQuyen();
            ViewBag.Quyen = new SelectList(quyenList, "Id", "TenQuyen", qtv.QuyenId);
            return View(qtv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1")]
        public ActionResult SuaQTV(tb_TaiKhoan qtv)
        {
            if (ModelState.IsValid)
            {
                if (db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Email, qtv.Email) & Builders<tb_TaiKhoan>.Filter.Ne(x => x.Id, qtv.Id)).Any())
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng cho tài khoản khác!");
                }
                if (db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.TenDangNhap, qtv.TenDangNhap) & Builders<tb_TaiKhoan>.Filter.Ne(x => x.Id, qtv.Id)).Any())
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại!");
                }

                if (ModelState.IsValid)
                {
                    var qtvGoc = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, qtv.Id)).FirstOrDefault();
                    if (qtvGoc == null)
                    {
                        return HttpNotFound();
                    }
                    
                    int roleId = qtv.QuyenId ?? 2;
                    var roleInfo = LayDanhSachQuyen().FirstOrDefault(r => r.Id == roleId);
                    var quyenObj = roleInfo != null ? new TaiKhoanQuyen { Id = roleInfo.Id, TenQuyen = roleInfo.TenQuyen } : null;

                    var updateBuilder = Builders<tb_TaiKhoan>.Update
                        .Set(x => x.HoTen, qtv.HoTen)
                        .Set(x => x.TenDangNhap, qtv.TenDangNhap)
                        .Set(x => x.Email, qtv.Email)
                        .Set(x => x.HoatDong, qtv.HoatDong)
                        .Set(x => x.Quyen, quyenObj);

                    if (!string.IsNullOrEmpty(qtv.MatKhau))
                    {
                        updateBuilder = updateBuilder.Set(x => x.MatKhau, HashPassword(qtv.MatKhau));
                    }
                    
                    try
                    {
                        db.tb_TaiKhoan.Collection.UpdateOne(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, qtv.Id), updateBuilder);
                        TempData["SuccessMessage"] = "Cập nhật quản trị viên thành công!";
                        return RedirectToAction("QuanTriVien");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                    }
                }
            }
            
            var quyenList = LayDanhSachQuyen();
            ViewBag.Quyen = new SelectList(quyenList, "Id", "TenQuyen", qtv.QuyenId);
            return View("formSuaQTV", qtv);
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult XoaQTV(int id)
        {
            var qtv = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (qtv != null)
            {
                db.tb_TaiKhoan.Collection.DeleteOne(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, id));
            }
            return RedirectToAction("QuanTriVien");
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult TimQuanTriVien(string timqtv, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_TaiKhoan>.Filter.Eq(qtv => qtv.LaQuanTri, true);

            if (!String.IsNullOrEmpty(timqtv))
            {
                var keyword = timqtv.ToLower();
                var searchFilter = Builders<tb_TaiKhoan>.Filter.Regex(qtv => qtv.HoTen, new BsonRegularExpression(keyword, "i")) |
                                   Builders<tb_TaiKhoan>.Filter.Regex(qtv => qtv.TenDangNhap, new BsonRegularExpression(keyword, "i"));
                filter = filter & searchFilter;
            }

            int totalItems = (int)db.tb_TaiKhoan.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var lstQTV = db.tb_TaiKhoan.Collection.Find(filter)
                .SortByDescending(qtv => qtv.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = timqtv;

            return View("QuanTriVien", lstQTV);
        }
    }
}
