using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class SearchController : BaseController
    {
        public ActionResult Index(string q, int? danhMucId, int? thuongHieuId, decimal? minPrice, decimal? maxPrice, string sortBy = "newest")
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập từ khóa tìm kiếm";
                return RedirectToAction("Index", "Home");
            }

            var keyword = q.Trim();
            ViewBag.SearchKeyword = keyword;
            ViewBag.DanhMucId = danhMucId ?? 0;
            ViewBag.ThuongHieuId = thuongHieuId ?? 0;
            ViewBag.MinPrice = minPrice ?? 0;
            ViewBag.MaxPrice = maxPrice ?? 0;
            ViewBag.SortBy = sortBy ?? "newest";

            var keywordRegex = new MongoDB.Bson.BsonRegularExpression(keyword, "i");

            var filterDm = Builders<tb_DanhMuc>.Filter.Regex(dm => dm.TenDanhMuc, keywordRegex);
            var danhMucIds = db.tb_DanhMuc.Collection.Find(filterDm).ToList().Select(dm => dm.Id).ToList();

            var filterTh = Builders<tb_ThuongHieu>.Filter.Regex(th => th.TenThuongHieu, keywordRegex);
            var thuongHieuIds = db.tb_ThuongHieu.Collection.Find(filterTh).ToList().Select(th => (int?)th.Id).ToList();

            var filter = Builders<tb_SanPham>.Filter.Eq(sp => sp.CacCo.HienThi, true) &
                         (Builders<tb_SanPham>.Filter.Regex(sp => sp.TenSanPham, keywordRegex) |
                          Builders<tb_SanPham>.Filter.Regex(sp => sp.MaSanPham, keywordRegex) |
                          Builders<tb_SanPham>.Filter.Regex(sp => sp.MoTa, keywordRegex) |
                          Builders<tb_SanPham>.Filter.In(sp => sp.DanhMuc.Id, danhMucIds) |
                          Builders<tb_SanPham>.Filter.In(sp => sp.ThuongHieu.Id, thuongHieuIds));

            if (danhMucId.HasValue && danhMucId.Value > 0)
            {
                filter &= Builders<tb_SanPham>.Filter.Eq(sp => sp.DanhMuc.Id, danhMucId.Value);
            }

            if (thuongHieuId.HasValue && thuongHieuId.Value > 0)
            {
                filter &= Builders<tb_SanPham>.Filter.Eq(sp => sp.ThuongHieu.Id, thuongHieuId.Value);
            }

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                filter &= Builders<tb_SanPham>.Filter.Gte(sp => sp.Gia, minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                filter &= Builders<tb_SanPham>.Filter.Lte(sp => sp.Gia, maxPrice.Value);
            }

            var sortBuilder = Builders<tb_SanPham>.Sort;
            SortDefinition<tb_SanPham> sort = null;

            switch (sortBy)
            {
                case "price_asc": sort = sortBuilder.Ascending(sp => sp.Gia); break;
                case "price_desc": sort = sortBuilder.Descending(sp => sp.Gia); break;
                case "name_asc": sort = sortBuilder.Ascending(sp => sp.TenSanPham); break;
                case "name_desc": sort = sortBuilder.Descending(sp => sp.TenSanPham); break;
                case "popular": sort = sortBuilder.Descending(sp => sp.LuotXem); break;
                case "newest":
                default: sort = sortBuilder.Descending(sp => sp.NgayTao); break;
            }

            var findQuery = db.tb_SanPham.Collection.Find(filter);
            if (sort != null) findQuery = findQuery.Sort(sort);

            var results = findQuery.ToList();
            ViewBag.TotalResults = results.Count;

            ViewBag.DanhMucList = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(dm => dm.HienThi, true)).ToList();
            ViewBag.ThuongHieuList = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(th => th.HienThi, true)).ToList();

            return View(results);
        }

        [HttpGet]
        public JsonResult Autocomplete(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var keyword = term.Trim();
            var keywordRegex = new MongoDB.Bson.BsonRegularExpression(keyword, "i");
            
            var filter = Builders<tb_SanPham>.Filter.Eq(sp => sp.CacCo.HienThi, true) &
                         (Builders<tb_SanPham>.Filter.Regex(sp => sp.TenSanPham, keywordRegex) |
                          Builders<tb_SanPham>.Filter.Regex(sp => sp.MaSanPham, keywordRegex));

            var suggestions = db.tb_SanPham.Collection.Find(filter)
                .Limit(10)
                .ToList()
                .Select(sp => new
                {
                    id = sp.Id,
                    label = sp.TenSanPham,
                    value = sp.TenSanPham,
                    image = sp.HinhAnh,
                    price = sp.GiaKhuyenMai ?? sp.Gia,
                    url = Url.Action("ChiTietSP", "SanPham", new { id = sp.Id })
                })
                .ToList();

            return Json(suggestions, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new { success = false, message = "Từ khóa quá ngắn" }, JsonRequestBehavior.AllowGet);
            }

            var keyword = q.Trim();
            var keywordRegex = new MongoDB.Bson.BsonRegularExpression(keyword, "i");

            var filter = Builders<tb_SanPham>.Filter.Eq(sp => sp.CacCo.HienThi, true) &
                         (Builders<tb_SanPham>.Filter.Regex(sp => sp.TenSanPham, keywordRegex) |
                          Builders<tb_SanPham>.Filter.Regex(sp => sp.MaSanPham, keywordRegex));

            var results = db.tb_SanPham.Collection.Find(filter)
                .Limit(5)
                .ToList()
                .Select(sp => new
                {
                    id = sp.Id,
                    name = sp.TenSanPham,
                    image = sp.HinhAnh,
                    price = sp.GiaKhuyenMai ?? sp.Gia,
                    oldPrice = sp.Gia,
                    hasDiscount = sp.GiaKhuyenMai.HasValue && sp.GiaKhuyenMai < sp.Gia,
                    url = Url.Action("ChiTietSP", "SanPham", new { id = sp.Id })
                })
                .ToList();

            return Json(new
            {
                success = true,
                count = results.Count,
                data = results
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
