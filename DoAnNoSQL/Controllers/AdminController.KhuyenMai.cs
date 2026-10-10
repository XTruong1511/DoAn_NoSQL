using System;
using System.Linq;
using System.Web.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using DoAnNoSQL.Models;
using DoAnNoSQL.Filters;

namespace DoAnNoSQL.Controllers
{
    public partial class AdminController
    {
        [HttpPost]
        public JsonResult KiemTraKhuyenMai(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Mã không hợp lệ." });
            }
            
            code = code.Trim().ToUpper();
            var km = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.Eq(x => x.MaCode, code) & Builders<tb_MaGiamGia>.Filter.Eq(x => x.HienThi, true)).FirstOrDefault();
            
            if (km == null)
            {
                return Json(new { success = false, message = "Mã khuyến mãi không hợp lệ hoặc không tồn tại." });
            }
            
            if (km.NgayKetThuc.HasValue && km.NgayKetThuc.Value < DateTime.Now)
            {
                return Json(new { success = false, message = "Mã khuyến mãi đã hết hạn." });
            }
            
            if (km.SoLuong.HasValue && km.SoLuong.Value <= 0)
            {
                return Json(new { success = false, message = "Mã khuyến mãi đã hết lượt sử dụng." });
            }
            
            return Json(new { 
                success = true, 
                soTienGiam = km.SoTienGiam ?? 0, 
                phanTramGiam = km.PhanTramGiam ?? 0 
            });
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult KhuyenMai(int page = 1)
        {
            int pageSize = 10;
            var lstKM = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.Empty)
                .SortByDescending(km => km.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            int totalItems = (int)db.tb_MaGiamGia.Collection.CountDocuments(Builders<tb_MaGiamGia>.Filter.Empty);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = "";
            return View(lstKM);
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult formThemKM()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,3")]
        public ActionResult ThemKM(tb_MaGiamGia km)
        {
            if (ModelState.IsValid)
            {
                km.Id = (int)db.GetNextSequenceValue("tb_MaGiamGia");
                km.NgayTao = DateTime.Now;
                db.tb_MaGiamGia.Collection.InsertOne(km);
                return RedirectToAction("KhuyenMai");
            }
            return View("formThemKM", km);
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult formSuaKM(int id)
        {
            var km = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (km == null)
            {
                return HttpNotFound();
            }
            return View(km);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,3")]
        public ActionResult SuaKM(tb_MaGiamGia km)
        {
            if (ModelState.IsValid)
            {
                var kmGoc = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.Eq(x => x.Id, km.Id)).FirstOrDefault();
                if (kmGoc == null)
                {
                    return HttpNotFound();
                }

                var updateBuilder = Builders<tb_MaGiamGia>.Update
                    .Set(x => x.MaCode, km.MaCode)
                    .Set(x => x.SoTienGiam, km.SoTienGiam)
                    .Set(x => x.PhanTramGiam, km.PhanTramGiam)
                    .Set(x => x.SoLuong, km.SoLuong)
                    .Set(x => x.HienThi, km.HienThi);

                if (km.NgayKetThuc.HasValue)
                {
                    updateBuilder = updateBuilder.Set(x => x.NgayKetThuc, km.NgayKetThuc);
                }

                db.tb_MaGiamGia.Collection.UpdateOne(Builders<tb_MaGiamGia>.Filter.Eq(x => x.Id, km.Id), updateBuilder);
                return RedirectToAction("KhuyenMai");
            }
            return View("formSuaKM", km);
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,3")]
        public JsonResult XoaKM(int id)
        {
            try
            {
                var km = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (km == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá" });
                }

                var isUsed = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(d => d.MaGiamGia, km.MaCode)).Any();
                if (isUsed)
                {
                    return Json(new { success = false, message = "Mã này đã được sử dụng trong đơn hàng, không thể xóa." });
                }

                db.tb_MaGiamGia.Collection.DeleteOne(Builders<tb_MaGiamGia>.Filter.Eq(x => x.Id, id));
                return Json(new { success = true, message = "Xóa mã giảm giá thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult TimKhuyenMai(string timkm, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_MaGiamGia>.Filter.Empty;

            if (!String.IsNullOrEmpty(timkm))
            {
                var keyword = timkm.ToLower();
                filter = Builders<tb_MaGiamGia>.Filter.Regex(tt => tt.MaCode, new BsonRegularExpression(keyword, "i"));
            }

            int totalItems = (int)db.tb_MaGiamGia.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var lstKM = db.tb_MaGiamGia.Collection.Find(filter)
                .SortByDescending(tt => tt.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = timkm;
            return View("KhuyenMai", lstKM);
        }
    }
}
