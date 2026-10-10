using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class MenuController : Controller
    {
        private MongoDbContext db = new MongoDbContext();

        [ChildActionOnly]
        public ActionResult DanhMuc()
        {
            var filterDm = Builders<tb_DanhMuc>.Filter.Eq(dm => dm.HienThi, true);
            var sortDm = Builders<tb_DanhMuc>.Sort.Ascending(dm => dm.ThuTu);
            var danhSachDanhMuc = db.tb_DanhMuc.Collection.Find(filterDm).Sort(sortDm).ToList();

            var viewModelList = new List<MenuDanhMucViewModel>();

            foreach (var danhMuc in danhSachDanhMuc)
            {
                var filterSp = Builders<tb_SanPham>.Filter.Where(sp => sp.DanhMuc.Id == danhMuc.Id && sp.CacCo.HienThi == true);
                var count = (int)db.tb_SanPham.Collection.CountDocuments(filterSp);

                var viewModel = new MenuDanhMucViewModel
                {
                    DanhMuc = danhMuc,
                    SoLuongSanPham = count,
                    DanhSachThuongHieu = GetThuongHieuByDanhMuc(danhMuc.Id)
                };

                viewModelList.Add(viewModel);
            }

            return PartialView("_DanhMuc", viewModelList);
        }

        private List<ThuongHieuItem> GetThuongHieuByDanhMuc(int danhMucId)
        {
            var productCounts = db.tb_SanPham.Collection.Aggregate()
                .Match(sp => sp.DanhMuc.Id == danhMucId && sp.CacCo.HienThi == true && sp.ThuongHieu != null && sp.ThuongHieu.Id > 0)
                .Group(sp => sp.ThuongHieu.Id, g => new { ThuongHieuId = (int?)g.Key, Count = g.Count() })
                .SortByDescending(x => x.Count)
                .Limit(10)
                .ToList();

            var thuongHieuIds = productCounts.Where(x => x.ThuongHieuId.HasValue).Select(x => x.ThuongHieuId.Value).ToList();

            var filterTh = Builders<tb_ThuongHieu>.Filter.Where(th => thuongHieuIds.Contains(th.Id) && th.HienThi == true);
            var thuongHieus = db.tb_ThuongHieu.Collection.Find(filterTh).ToList();

            var thuongHieuList = new List<ThuongHieuItem>();
            foreach (var pc in productCounts)
            {
                if (!pc.ThuongHieuId.HasValue) continue;
                var th = thuongHieus.FirstOrDefault(x => x.Id == pc.ThuongHieuId.Value);
                if (th != null)
                {
                    thuongHieuList.Add(new ThuongHieuItem
                    {
                        Id = th.Id,
                        TenThuongHieu = th.TenThuongHieu,
                        HinhAnh = th.HinhAnh,
                        SoLuongSanPham = pc.Count
                    });
                }
            }

            return thuongHieuList;
        }

        private string GetPartialViewName(string tenDanhMuc)
        {
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Điện thoại", "_MenuDienThoai" },
                { "Laptop", "_MenuLaptop" },
                { "Tablet", "_MenuTablet" },
                { "Phụ kiện", "_MenuPhuKien" }
            };

            return mapping.ContainsKey(tenDanhMuc) 
                ? mapping[tenDanhMuc] 
                : "_CategoryMenuItem";
        }

        [ChildActionOnly]
        public ActionResult TopMenu()
        {
            var filterDm = Builders<tb_DanhMuc>.Filter.Eq(dm => dm.HienThi, true);
            var sortDm = Builders<tb_DanhMuc>.Sort.Ascending(dm => dm.ThuTu);
            
            var danhSachDanhMuc = db.tb_DanhMuc.Collection.Find(filterDm)
                .Sort(sortDm)
                .Limit(5)
                .Project(dm => new TopMenuItemViewModel
                {
                    Id = dm.Id,
                    TenDanhMuc = dm.TenDanhMuc,
                    Icon = dm.Icon,
                    BiDanh = dm.BiDanh
                })
                .ToList();

            return PartialView("_TopMenu", danhSachDanhMuc);
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