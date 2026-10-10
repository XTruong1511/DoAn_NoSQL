using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var viewModel = new HomeViewModel();

            var filterSpMoi = Builders<tb_SanPham>.Filter.Where(sp => sp.CacCo.LaSanPhamMoi == true && sp.CacCo.HienThi == true && sp.SoLuong > 0);
            var sortSpMoi = Builders<tb_SanPham>.Sort.Descending(sp => sp.NgayTao);
            viewModel.SanPhamMoi = db.tb_SanPham.Collection.Find(filterSpMoi).Sort(sortSpMoi).Limit(10).ToList();

            var filterSpBanChay = Builders<tb_SanPham>.Filter.Where(sp => sp.CacCo.LaSanPhamBanChay == true && sp.CacCo.HienThi == true && sp.SoLuong > 0);
            var sortSpBanChay = Builders<tb_SanPham>.Sort.Descending(sp => sp.LuotXem);
            viewModel.SanPhamBanChay = db.tb_SanPham.Collection.Find(filterSpBanChay).Sort(sortSpBanChay).Limit(10).ToList();

            var filterDm = Builders<tb_DanhMuc>.Filter.Eq(dm => dm.HienThi, true);
            var sortDm = Builders<tb_DanhMuc>.Sort.Ascending(dm => dm.ThuTu);
            viewModel.DanhMuc = db.tb_DanhMuc.Collection.Find(filterDm).Sort(sortDm).ToList();

            var filterTt = Builders<tb_TinTuc>.Filter.Eq(tt => tt.HienThi, true);
            var sortTt = Builders<tb_TinTuc>.Sort.Descending(tt => tt.NgayTao);
            viewModel.TinTuc = db.tb_TinTuc.Collection.Find(filterTt).Sort(sortTt).Limit(3).ToList();

            var filterTh = Builders<tb_ThuongHieu>.Filter.Eq(th => th.HienThi, true);
            var sortTh = Builders<tb_ThuongHieu>.Sort.Ascending(th => th.TenThuongHieu);
            viewModel.ThuongHieu = db.tb_ThuongHieu.Collection.Find(filterTh).Sort(sortTh).ToList();

            var filterQc = Builders<tb_QuangCao>.Filter.Eq(qc => qc.HienThi, true);
            var sortQc = Builders<tb_QuangCao>.Sort.Ascending(qc => qc.ThuTu);
            viewModel.QuangCao = db.tb_QuangCao.Collection.Find(filterQc).Sort(sortQc).ToList();

            return View(viewModel);
        }

        // AJAX: Lấy thương hiệu + mức giá theo danh mục (mega menu flyout - giống sidebar _MenuDienThoai)
        [OutputCache(Duration = 3600, VaryByParam = "danhMucId")]
        public ActionResult GetSanPhamTheoDanhMuc(int danhMucId)
        {
            // ---- Thương hiệu: aggregate đếm SP theo hãng, sort by count DESC (giống MenuController) ----
            var productCounts = db.tb_SanPham.Collection.Aggregate()
                .Match(sp => sp.DanhMuc.Id == danhMucId
                          && sp.CacCo.HienThi == true
                          && sp.ThuongHieu != null
                          && sp.ThuongHieu.Id > 0
                          && sp.SoLuong > 0)
                .Group(sp => sp.ThuongHieu.Id,
                       g => new { ThuongHieuId = (int?)g.Key, Count = g.Count() })
                .SortByDescending(x => x.Count)
                .Limit(12)
                .ToList();

            var thuongHieuIds = productCounts
                .Where(x => x.ThuongHieuId.HasValue)
                .Select(x => x.ThuongHieuId.Value)
                .ToList();

            var thuongHieuList = new List<object>();
            if (thuongHieuIds.Any())
            {
                var filterTh = Builders<tb_ThuongHieu>.Filter
                    .Where(th => thuongHieuIds.Contains(th.Id) && th.HienThi == true);
                var thuongHieus = db.tb_ThuongHieu.Collection.Find(filterTh).ToList();

                // Giữ thứ tự theo count
                foreach (var pc in productCounts)
                {
                    if (!pc.ThuongHieuId.HasValue) continue;
                    var th = thuongHieus.FirstOrDefault(x => x.Id == pc.ThuongHieuId.Value);
                    if (th != null)
                    {
                        thuongHieuList.Add(new
                        {
                            id = th.Id,
                            ten = th.TenThuongHieu,
                            hinhAnh = th.HinhAnh,
                            soLuong = pc.Count
                        });
                    }
                }
            }

            // ---- Tổng số sản phẩm trong danh mục ----
            var totalFilter = Builders<tb_SanPham>.Filter.Where(sp =>
                sp.DanhMuc.Id == danhMucId && sp.CacCo.HienThi == true && sp.SoLuong > 0);
            var total = (int)db.tb_SanPham.Collection.CountDocuments(totalFilter);

            // ---- Mức giá (cố định theo chuẩn sidebar) ----
            var mucGia = new[]
            {
                new { nhan = "Dưới 5 triệu",   giaMin = (decimal?)null,       giaMax = (decimal?)5000000m  },
                new { nhan = "5 - 10 triệu",   giaMin = (decimal?)5000000m,   giaMax = (decimal?)10000000m },
                new { nhan = "10 - 20 triệu",  giaMin = (decimal?)10000000m,  giaMax = (decimal?)20000000m },
                new { nhan = "Trên 20 triệu",  giaMin = (decimal?)20000000m,  giaMax = (decimal?)null      }
            };

            return Json(new
            {
                soLuong     = total,
                thuongHieu  = thuongHieuList,
                mucGia      = mucGia
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}