using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class SanPhamController : BaseController
    {
        private const int PAGE_SIZE = 15;
        private const string CART_SESSION = "Cart";

        private ShoppingCart GetCart()
        {
            var cart = Session[CART_SESSION] as ShoppingCart;
            if (cart == null)
            {
                cart = new ShoppingCart();
                Session[CART_SESSION] = cart;
            }
            return cart;
        }

        public ActionResult Index(int? danhMucId, int? thuongHieuId, decimal? giaMin, decimal? giaMax, string loc, string search, int? page)
        {
            var filterBuilder = Builders<tb_SanPham>.Filter;
            var filter = filterBuilder.Eq(sp => sp.CacCo.HienThi, true) & filterBuilder.Gt(sp => sp.SoLuong, 0);
            string categoryName = "Tất cả sản phẩm";

            if (danhMucId.HasValue)
            {
                filter &= filterBuilder.Eq(sp => sp.DanhMuc.Id, danhMucId.Value);
                var danhMuc = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(x => x.Id, danhMucId.Value)).FirstOrDefault();
                if (danhMuc != null) categoryName = danhMuc.TenDanhMuc;
            }

            if (thuongHieuId.HasValue)
            {
                filter &= filterBuilder.Eq(sp => sp.ThuongHieu.Id, thuongHieuId.Value);
                var thuongHieu = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(x => x.Id, thuongHieuId.Value)).FirstOrDefault();
                if (thuongHieu != null)
                {
                    categoryName = danhMucId.HasValue ? $"{categoryName} - {thuongHieu.TenThuongHieu}" : thuongHieu.TenThuongHieu;
                }
            }

            if (giaMin.HasValue)
            {
                var priceFilter = filterBuilder.Gte(sp => sp.Gia, giaMin.Value) | 
                                  (filterBuilder.Ne(sp => sp.GiaKhuyenMai, null) & filterBuilder.Gte(sp => sp.GiaKhuyenMai, giaMin.Value));
                filter &= priceFilter;
            }

            if (giaMax.HasValue)
            {
                var priceFilter = filterBuilder.Lte(sp => sp.Gia, giaMax.Value) | 
                                  (filterBuilder.Ne(sp => sp.GiaKhuyenMai, null) & filterBuilder.Lte(sp => sp.GiaKhuyenMai, giaMax.Value));
                filter &= priceFilter;
            }

            if (!string.IsNullOrEmpty(loc))
            {
                switch (loc.ToLower())
                {
                    case "moi":
                        filter &= filterBuilder.Eq(sp => sp.CacCo.LaSanPhamMoi, true);
                        categoryName = "Sản phẩm mới";
                        break;
                    case "banchay":
                        filter &= filterBuilder.Eq(sp => sp.CacCo.LaSanPhamBanChay, true);
                        categoryName = "Sản phẩm bán chạy";
                        break;
                    case "khuyenmai":
                        filter &= filterBuilder.Where(sp => sp.GiaKhuyenMai.HasValue && sp.GiaKhuyenMai < sp.Gia);
                        categoryName = "Sản phẩm khuyến mãi";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchFilter = filterBuilder.Regex(sp => sp.TenSanPham, new MongoDB.Bson.BsonRegularExpression(search, "i")) |
                                   filterBuilder.Regex(sp => sp.MoTa, new MongoDB.Bson.BsonRegularExpression(search, "i"));
                filter &= searchFilter;
                categoryName = $"Kết quả tìm kiếm: \"{search}\"";
            }

            var sortBuilder = Builders<tb_SanPham>.Sort;
            SortDefinition<tb_SanPham> sort = null;

            if (!string.IsNullOrEmpty(loc))
            {
                switch (loc.ToLower())
                {
                    case "moi": sort = sortBuilder.Descending(sp => sp.NgayTao); break;
                    case "banchay": sort = sortBuilder.Descending(sp => sp.LuotXem); break;
                    case "gia-tang": sort = sortBuilder.Ascending(sp => sp.Gia); break;
                    case "gia-giam": sort = sortBuilder.Descending(sp => sp.Gia); break;
                    case "khuyenmai": /* Will be handled in memory */ break;
                    default: sort = sortBuilder.Descending(sp => sp.NgayTao); break;
                }
            }
            else
            {
                sort = sortBuilder.Descending(sp => sp.NgayTao);
            }

            int totalProducts = (int)db.tb_SanPham.Collection.CountDocuments(filter);
            int currentPage = page ?? 1;
            int totalPages = (int)Math.Ceiling((double)totalProducts / PAGE_SIZE);

            if (currentPage < 1) currentPage = 1;
            if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

            List<tb_SanPham> products;
            
            if (!string.IsNullOrEmpty(loc) && loc.ToLower() == "khuyenmai")
            {
                // Optimize: only load required fields to memory for sorting instead of entire product documents
                var allMatchedIds = db.tb_SanPham.Collection.Find(filter)
                    .Project(sp => new { sp.Id, sp.Gia, sp.GiaKhuyenMai })
                    .ToList();

                var pagedIds = allMatchedIds
                    .OrderByDescending(sp => 
                    {
                        var giaGoc = sp.Gia ?? 0;
                        var giaKm = sp.GiaKhuyenMai ?? 0;
                        if (giaGoc > 0 && giaKm > 0 && giaKm < giaGoc)
                        {
                            return (giaGoc - giaKm) / giaGoc;
                        }
                        return 0m;
                    })
                    .Skip((currentPage - 1) * PAGE_SIZE)
                    .Take(PAGE_SIZE)
                    .Select(x => x.Id)
                    .ToList();

                // Fetch full products for the page, maintaining order
                var fetchedProducts = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.In(x => x.Id, pagedIds)).ToList();
                products = pagedIds.Select(id => fetchedProducts.FirstOrDefault(p => p.Id == id)).Where(p => p != null).ToList();
            }
            else
            {
                var findQuery = db.tb_SanPham.Collection.Find(filter);
                if (sort != null) findQuery = findQuery.Sort(sort);
                products = findQuery.Skip((currentPage - 1) * PAGE_SIZE).Limit(PAGE_SIZE).ToList();
            }

            var viewModel = new ProductListViewModel
            {
                Products = products,
                CurrentCategoryName = categoryName,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalProducts = totalProducts,
                PageSize = PAGE_SIZE,
                DanhMucId = danhMucId,
                ThuongHieuId = thuongHieuId,
                SearchKeyword = search,
                FilterType = loc
            };

            ViewBag.DanhMucId = danhMucId;
            ViewBag.ThuongHieuId = thuongHieuId;
            ViewBag.DanhMucList = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(x => x.HienThi, true)).ToList();

            if (danhMucId.HasValue)
            {
                var danhMuc = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(x => x.Id, danhMucId.Value)).FirstOrDefault();
                ViewBag.TenDanhMuc = danhMuc?.TenDanhMuc;
            }

            if (thuongHieuId.HasValue)
            {
                var thuongHieu = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(x => x.Id, thuongHieuId.Value)).FirstOrDefault();
                ViewBag.TenThuongHieu = thuongHieu?.TenThuongHieu;
            }

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult ChiTietSP(int? id)
        {
            if (id == null) return RedirectToAction("Index", "Home");

            var sanPham = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, id.Value)).FirstOrDefault();
            if (sanPham == null || sanPham.HienThi == false)
            {
                return HttpNotFound("Sản phẩm không tồn tại hoặc đã bị ẩn");
            }

            sanPham.LuotXem = (sanPham.LuotXem ?? 0) + 1;
            var update = Builders<tb_SanPham>.Update.Set(x => x.LuotXem, sanPham.LuotXem);
            db.tb_SanPham.Collection.UpdateOne(Builders<tb_SanPham>.Filter.Eq(x => x.Id, sanPham.Id), update);

            var filterLq = Builders<tb_SanPham>.Filter.Eq(sp => sp.DanhMuc.Id, sanPham.DanhMucId)
                         & Builders<tb_SanPham>.Filter.Ne(sp => sp.Id, sanPham.Id)
                         & Builders<tb_SanPham>.Filter.Eq(sp => sp.CacCo.HienThi, true)
                         & Builders<tb_SanPham>.Filter.Gt(sp => sp.SoLuong, 0);
            
            var sanPhamLienQuan = db.tb_SanPham.Collection.Find(filterLq).SortByDescending(sp => sp.LuotXem).Limit(5).ToList();

            var viewModel = new ProductDetailViewModel
            {
                SanPham = sanPham,
                SanPhamLienQuan = sanPhamLienQuan
            };

            viewModel.DanhSachAnh = sanPham.tb_AnhSanPham.OrderByDescending(a => a.LaAnhMacDinh).ToList();

            var filterDg = Builders<tb_DanhGia>.Filter.Eq(dg => dg.SanPham.Id, id.Value) & Builders<tb_DanhGia>.Filter.Eq(dg => dg.HienThi, true);
            var listDanhGia = db.tb_DanhGia.Collection.Find(filterDg).SortByDescending(dg => dg.NgayTao).ToList();

            var soLuotDanhGia = listDanhGia.Count;
            double saoTrungBinh = 0;
            if (listDanhGia.Any() && listDanhGia.Any(dg => dg.Sao.HasValue))
            {
                saoTrungBinh = listDanhGia.Where(dg => dg.Sao.HasValue).Average(dg => dg.Sao.Value);
            }

            ViewBag.ListDanhGia = listDanhGia;
            ViewBag.SoLuotDanhGia = soLuotDanhGia;
            ViewBag.SaoTrungBinh = Math.Round(saoTrungBinh, 1);

            viewModel.DanhSachDanhGia = listDanhGia;
            viewModel.TongSoDanhGia = soLuotDanhGia;
            viewModel.DiemTrungBinh = saoTrungBinh;

            ViewBag.TenDanhMuc = sanPham.tb_DanhMuc?.TenDanhMuc;
            ViewBag.DanhMucId = sanPham.DanhMucId;

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult GetQuickView(int id)
        {
            var sanPham = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (sanPham == null || sanPham.HienThi == false)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" }, JsonRequestBehavior.AllowGet);
            }

            var filterDg = Builders<tb_DanhGia>.Filter.Eq(dg => dg.SanPham.Id, id) & Builders<tb_DanhGia>.Filter.Eq(dg => dg.HienThi, true);
            var listDanhGia = db.tb_DanhGia.Collection.Find(filterDg).ToList();
            double saoTrungBinh = listDanhGia.Any(dg => dg.Sao.HasValue) ? Math.Round(listDanhGia.Where(dg => dg.Sao.HasValue).Average(dg => dg.Sao.Value), 1) : 0;

            var images = new List<string>();
            if (sanPham.tb_AnhSanPham != null && sanPham.tb_AnhSanPham.Any())
            {
                images = sanPham.tb_AnhSanPham.OrderByDescending(a => a.LaAnhMacDinh).Select(a => a.HinhAnh).ToList();
            }
            if (!images.Any() && !string.IsNullOrEmpty(sanPham.HinhAnh))
            {
                images.Add(sanPham.HinhAnh);
            }

            decimal giaGoc = sanPham.Gia ?? 0;
            decimal giaBan = (sanPham.GiaKhuyenMai.HasValue && sanPham.GiaKhuyenMai.Value > 0) ? sanPham.GiaKhuyenMai.Value : giaGoc;
            int percentDiscount = (giaGoc > 0 && giaBan < giaGoc) ? (int)Math.Round((giaGoc - giaBan) / giaGoc * 100) : 0;

            var result = new
            {
                success = true,
                id = sanPham.Id,
                tenSanPham = sanPham.TenSanPham,
                hinhAnh = sanPham.HinhAnh,
                images = images,
                gia = string.Format("{0:N0}₫", giaGoc),
                giaKhuyenMai = (sanPham.GiaKhuyenMai.HasValue && sanPham.GiaKhuyenMai.Value > 0) ? string.Format("{0:N0}₫", sanPham.GiaKhuyenMai.Value) : null,
                hasDiscount = percentDiscount > 0,
                giamGiaPercent = percentDiscount,
                moTa = sanPham.MoTa,
                thongSoKyThuat = sanPham.ThongSoKyThuat,
                danhMuc = sanPham.tb_DanhMuc?.TenDanhMuc ?? "Sản phẩm",
                soLuong = sanPham.SoLuong ?? 0,
                conHang = (sanPham.SoLuong ?? 0) > 0,
                saoTrungBinh = saoTrungBinh,
                soLuotDanhGia = listDanhGia.Count,
                urlDetail = Url.Action("ChiTietSP", "SanPham", new { id = sanPham.Id })
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemVaoGioHang(int sanPhamId, int soLuong = 1, string color = "")
        {
            try
            {
                var product = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, sanPhamId)).FirstOrDefault();

                if (product == null || product.HienThi == false)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "Home");
                }

                if (product.SoLuong.HasValue && product.SoLuong.Value < soLuong)
                {
                    TempData["ErrorMessage"] = $"Chỉ còn {product.SoLuong.Value} sản phẩm trong kho";
                    return RedirectToAction("ChiTietSP", new { id = sanPhamId });
                }

                var cart = GetCart();
                var cartItem = new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.TenSanPham,
                    ProductImage = product.HinhAnh,
                    Price = product.GiaKhuyenMai.HasValue && product.GiaKhuyenMai.Value > 0
                        ? product.GiaKhuyenMai.Value
                        : product.Gia ?? 0,
                    Quantity = soLuong,
                    Color = color
                };

                cart.AddItem(cartItem);
                Session[CART_SESSION] = cart;

                TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";
                return RedirectToAction("ChiTietSP", new { id = sanPhamId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("ChiTietSP", new { id = sanPhamId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ThemDanhGia(int sanPhamId, int sao, string noiDung, string tenNguoiGui, string email)
        {
            try
            {
                if (sao < 1 || sao > 5) return Json(new { success = false, message = "Số sao không hợp lệ" });
                if (string.IsNullOrWhiteSpace(tenNguoiGui) || string.IsNullOrWhiteSpace(noiDung))
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });

                var sp = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, sanPhamId)).FirstOrDefault();

                var danhGia = new tb_DanhGia
                {
                    Id = db.GetNextSequenceValue("DanhGia"),
                    SanPham = new DanhGiaSanPham { Id = sanPhamId, TenSanPham = sp?.TenSanPham ?? "" },
                    TenNguoiGui = tenNguoiGui.Trim(),
                    Email = email?.Trim(),
                    NoiDung = noiDung.Trim(),
                    Sao = sao,
                    NgayTao = DateTime.Now,
                    HienThi = true
                };

                db.tb_DanhGia.Collection.InsertOne(danhGia);

                return Json(new
                {
                    success = true,
                    message = "Cảm ơn bạn đã đánh giá sản phẩm!",
                    review = new
                    {
                        tenNguoiGui = danhGia.TenNguoiGui,
                        sao = danhGia.Sao,
                        noiDung = danhGia.NoiDung,
                        ngayTao = danhGia.NgayTao.Value.ToString("dd/MM/yyyy HH:mm")
                    }
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        [HttpGet]
        public JsonResult LayDanhGia(int sanPhamId, int page = 1, int pageSize = 5)
        {
            try
            {
                var filter = Builders<tb_DanhGia>.Filter.Eq(dg => dg.SanPham.Id, sanPhamId) & Builders<tb_DanhGia>.Filter.Eq(dg => dg.HienThi, true);
                var totalReviews = (int)db.tb_DanhGia.Collection.CountDocuments(filter);
                var totalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

                var reviews = db.tb_DanhGia.Collection.Find(filter)
                    .SortByDescending(dg => dg.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToList()
                    .Select(dg => new {
                        id = dg.Id,
                        tenNguoiGui = dg.TenNguoiGui,
                        sao = dg.Sao,
                        noiDung = dg.NoiDung,
                        ngayTao = dg.NgayTao.HasValue ? dg.NgayTao.Value.ToString("dd/MM/yyyy HH:mm") : ""
                    })
                    .ToList();

                var allReviews = db.tb_DanhGia.Collection.Find(filter & Builders<tb_DanhGia>.Filter.Ne(dg => dg.Sao, null)).ToList();
                var avgRating = allReviews.Any() ? allReviews.Average(dg => (double?)dg.Sao) ?? 0 : 0;

                return Json(new
                {
                    success = true,
                    reviews = reviews,
                    totalReviews = totalReviews,
                    currentPage = page,
                    totalPages = totalPages,
                    avgRating = Math.Round(avgRating, 1)
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
