using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class TinTucController : BaseController
    {
        public ActionResult Index(int? danhMucId, string sortBy = "newest", string q = "", int page = 1, int pageSize = 9)
        {
            var builder = Builders<tb_TinTuc>.Filter;
            var filter = builder.Eq(tt => tt.HienThi, true);

            bool isSearching = !string.IsNullOrWhiteSpace(q);
            if (isSearching)
            {
                var keyword = q.Trim();
                var searchFilter = builder.Regex(tt => tt.TieuDe, new MongoDB.Bson.BsonRegularExpression(keyword, "i")) |
                                   builder.Regex(tt => tt.MoTa, new MongoDB.Bson.BsonRegularExpression(keyword, "i")) |
                                   builder.Regex(tt => tt.ChiTiet, new MongoDB.Bson.BsonRegularExpression(keyword, "i"));
                filter &= searchFilter;
                ViewBag.SearchKeyword = keyword;
            }
            ViewBag.IsSearching = isSearching;
            ViewBag.SortBy = sortBy;

            if (danhMucId.HasValue && danhMucId.Value > 0)
            {
                filter &= builder.Eq(tt => tt.DanhMucTin.Id, danhMucId.Value);
            }

            var sortBuilder = Builders<tb_TinTuc>.Sort;
            SortDefinition<tb_TinTuc> sort = null;

            switch (sortBy)
            {
                case "newest": sort = sortBuilder.Descending(tt => tt.NgayTao); break;
                case "oldest": sort = sortBuilder.Ascending(tt => tt.NgayTao); break;
                case "popular": sort = sortBuilder.Descending(tt => tt.LuotXem); break;
                case "title_asc": sort = sortBuilder.Ascending(tt => tt.TieuDe); break;
                case "title_desc": sort = sortBuilder.Descending(tt => tt.TieuDe); break;
                default: sort = sortBuilder.Descending(tt => tt.NgayTao); break;
            }

            var totalNews = (int)db.tb_TinTuc.Collection.CountDocuments(filter);
            ViewBag.TotalNews = totalNews;

            var totalPages = (int)Math.Ceiling((double)totalNews / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var findQuery = db.tb_TinTuc.Collection.Find(filter);
            if (sort != null) findQuery = findQuery.Sort(sort);
            
            var news = findQuery.Skip((page - 1) * pageSize).Limit(pageSize).ToList();

            ViewBag.DanhMucId = danhMucId ?? 0;
            ViewBag.DanhMucTinList = db.tb_TinTuc.Collection.Find(Builders<tb_TinTuc>.Filter.Ne(x => x.DanhMucTin, null))
                .ToList()
                .Where(x => x.DanhMucTin != null && x.DanhMucTin.Id > 0)
                .Select(x => x.DanhMucTin)
                .GroupBy(x => x.Id)
                .Select(g => new tb_DanhMucTin { Id = g.Key, TenDanhMuc = g.First().TenDanhMuc, HienThi = true })
                .ToList();

            ViewBag.TinNoiBat = db.tb_TinTuc.Collection.Find(Builders<tb_TinTuc>.Filter.Eq(tt => tt.HienThi, true))
                .SortByDescending(tt => tt.NgayTao)
                .Limit(5)
                .ToList();

            return View(news);
        }

        public ActionResult ChiTiet(int id)
        {
            var tinTuc = db.tb_TinTuc.Collection.Find(Builders<tb_TinTuc>.Filter.Eq(x => x.Id, id)).FirstOrDefault();

            if (tinTuc == null || tinTuc.HienThi == false)
            {
                TempData["ErrorMessage"] = "Tin tức không tồn tại";
                return RedirectToAction("Index");
            }

            // Tăng lượt xem
            try
            {
                db.tb_TinTuc.Collection.UpdateOne(
                    Builders<tb_TinTuc>.Filter.Eq(x => x.Id, id),
                    Builders<tb_TinTuc>.Update.Inc(x => x.LuotXem, 1)
                );
                tinTuc.LuotXem = (tinTuc.LuotXem ?? 0) + 1;
            }
            catch { }

            var filterLq = Builders<tb_TinTuc>.Filter.Eq(tt => tt.HienThi, true) &
                           Builders<tb_TinTuc>.Filter.Ne(tt => tt.Id, id);

            if (tinTuc.DanhMucTinId.HasValue)
            {
                filterLq &= Builders<tb_TinTuc>.Filter.Eq(tt => tt.DanhMucTin.Id, tinTuc.DanhMucTinId.Value);
            }
            ViewBag.TinLienQuan = db.tb_TinTuc.Collection.Find(filterLq)
                .SortByDescending(tt => tt.NgayTao)
                .Limit(4)
                .ToList();

            var filterMn = Builders<tb_TinTuc>.Filter.Eq(tt => tt.HienThi, true) & Builders<tb_TinTuc>.Filter.Ne(tt => tt.Id, id);
            ViewBag.TinMoiNhat = db.tb_TinTuc.Collection.Find(filterMn)
                .SortByDescending(tt => tt.NgayTao)
                .Limit(5)
                .ToList();

            return View(tinTuc);
        }

        [HttpGet]
        public JsonResult LiveSearch(string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                {
                    return Json(new { success = false, message = "Từ khóa quá ngắn" }, JsonRequestBehavior.AllowGet);
                }

                var keyword = q.Trim();
                var filter = Builders<tb_TinTuc>.Filter.Eq(tt => tt.HienThi, true) &
                             (Builders<tb_TinTuc>.Filter.Regex(tt => tt.TieuDe, new MongoDB.Bson.BsonRegularExpression(keyword, "i")) |
                              Builders<tb_TinTuc>.Filter.Regex(tt => tt.MoTa, new MongoDB.Bson.BsonRegularExpression(keyword, "i")));

                var rawResults = db.tb_TinTuc.Collection.Find(filter)
                    .SortByDescending(tt => tt.NgayTao)
                    .Limit(5)
                    .ToList();
                
                var categoryIds = rawResults.Where(x => x.DanhMucTinId.HasValue).Select(x => x.DanhMucTinId.Value).Distinct().ToList();
                var categories = db.tb_DanhMucTin.Collection.Find(Builders<tb_DanhMucTin>.Filter.In(dm => dm.Id, categoryIds)).ToList();

                var results = rawResults.Select(tt => new
                    {
                        id = tt.Id,
                        tieuDe = tt.TieuDe,
                        moTa = tt.MoTa,
                        hinhAnh = tt.HinhAnh,
                        ngayTao = tt.NgayTao.HasValue ? tt.NgayTao.Value.ToString("dd/MM/yyyy") : "",
                        danhMuc = tt.DanhMucTinId.HasValue ? categories.FirstOrDefault(c => c.Id == tt.DanhMucTinId.Value)?.TenDanhMuc : ""
                    }).ToList();

                return Json(new
                {
                    success = true,
                    count = results.Count,
                    results = results
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
