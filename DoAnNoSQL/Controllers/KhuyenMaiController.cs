using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class KhuyenMaiController : BaseController
    {
        public ActionResult Index(int? danhMucId, string sortBy = "discount_desc", int page = 1, int pageSize = 12)
        {
            var filterBuilder = Builders<tb_SanPham>.Filter;
            var filter = filterBuilder.Eq(sp => sp.CacCo.HienThi, true) &
                         filterBuilder.Where(sp => sp.GiaKhuyenMai.HasValue && sp.GiaKhuyenMai < sp.Gia);

            if (danhMucId.HasValue && danhMucId.Value > 0)
            {
                filter &= filterBuilder.Eq(sp => sp.DanhMuc.Id, danhMucId.Value);
            }

            var allPromotional = db.tb_SanPham.Collection.Find(filter).ToList();

            IEnumerable<tb_SanPham> query = allPromotional;

            switch (sortBy)
            {
                case "discount_desc":
                    query = query.OrderByDescending(sp => (sp.Gia - (sp.GiaKhuyenMai ?? 0)) / (sp.Gia > 0 ? sp.Gia : 1) * 100);
                    break;
                case "discount_asc":
                    query = query.OrderBy(sp => (sp.Gia - (sp.GiaKhuyenMai ?? 0)) / (sp.Gia > 0 ? sp.Gia : 1) * 100);
                    break;
                case "price_asc":
                    query = query.OrderBy(sp => sp.GiaKhuyenMai);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(sp => sp.GiaKhuyenMai);
                    break;
                case "newest":
                    query = query.OrderByDescending(sp => sp.NgayTao);
                    break;
                case "popular":
                    query = query.OrderByDescending(sp => sp.LuotXem);
                    break;
                default:
                    query = query.OrderByDescending(sp => (sp.Gia - (sp.GiaKhuyenMai ?? 0)) / (sp.Gia > 0 ? sp.Gia : 1) * 100);
                    break;
            }

            var totalProducts = query.Count();
            ViewBag.TotalProducts = totalProducts;

            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var products = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.DanhMucId = danhMucId ?? 0;
            ViewBag.SortBy = sortBy;
            ViewBag.DanhMucList = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(dm => dm.HienThi, true)).ToList();

            ViewBag.TotalSavings = products.Sum(sp => sp.Gia - (sp.GiaKhuyenMai ?? 0));

            return View(products);
        }

        public ActionResult ChiTiet(int id)
        {
            return RedirectToAction("Index");
        }
    }
}
