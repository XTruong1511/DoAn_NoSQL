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
    public partial class AdminController
    {
        public class ChiTietDonHangLe
        {
            public int SanPhamId { get; set; }
            public int SoLuong { get; set; }
            public decimal Gia { get; set; }
        }

        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult BanHang()
        {
            var danhMucList = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty)
                .SortBy(x => x.TenDanhMuc)
                .ToList();
            ViewBag.DanhMucList = danhMucList;
            return View();
        }

        [HttpGet]
        public JsonResult LayDanhSachSanPhamBanLe(string search = "", int? danhMucId = null, int? thuongHieuId = null)
        {
            var filter = Builders<tb_SanPham>.Filter.Ne(x => x.CacCo.HienThi, false) & Builders<tb_SanPham>.Filter.Gt(x => x.SoLuong, 0);

            if (!string.IsNullOrEmpty(search))
            {
                var searchFilter = Builders<tb_SanPham>.Filter.Regex(x => x.TenSanPham, new BsonRegularExpression(search, "i")) |
                                   Builders<tb_SanPham>.Filter.Regex(x => x.MaSanPham, new BsonRegularExpression(search, "i"));
                filter &= searchFilter;
            }

            if (danhMucId.HasValue && danhMucId.Value > 0)
            {
                filter &= Builders<tb_SanPham>.Filter.Eq("DanhMuc.Id", danhMucId.Value);
            }

            if (thuongHieuId.HasValue && thuongHieuId.Value > 0)
            {
                filter &= Builders<tb_SanPham>.Filter.Eq("ThuongHieu.Id", thuongHieuId.Value);
            }

            var products = db.tb_SanPham.Collection.Find(filter)
                .Limit(100)
                .ToList()
                .Select(x => new
                {
                    x.Id,
                    x.TenSanPham,
                    x.MaSanPham,
                    x.Gia,
                    x.GiaKhuyenMai,
                    x.HinhAnh,
                    x.SoLuong
                })
                .ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LayThuongHieuTheoDanhMuc(int? danhMucId = null)
        {
            var filter = Builders<tb_SanPham>.Filter.Ne(x => x.CacCo.HienThi, false)
                       & Builders<tb_SanPham>.Filter.Gt(x => x.SoLuong, 0)
                       & Builders<tb_SanPham>.Filter.Ne(x => x.ThuongHieu, null);

            if (danhMucId.HasValue && danhMucId.Value > 0)
                filter &= Builders<tb_SanPham>.Filter.Eq("DanhMuc.Id", danhMucId.Value);

            var thuongHieuList = db.tb_SanPham.Collection.Find(filter)
                .ToList()
                .Where(x => x.ThuongHieu != null)
                .GroupBy(x => new { x.ThuongHieu.Id, x.ThuongHieu.TenThuongHieu })
                .Select(g => new { Id = g.Key.Id, Ten = g.Key.TenThuongHieu })
                .OrderBy(x => x.Ten)
                .ToList();

            return Json(thuongHieuList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult TaoDonHangLe(string tenKhachHang, string soDienThoai, List<ChiTietDonHangLe> chiTiet)
        {
            try
            {
                if (chiTiet == null || !chiTiet.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                decimal tongTien = chiTiet.Sum(x => x.Gia * x.SoLuong);
                int tongSoLuong = chiTiet.Sum(x => x.SoLuong);
                int accountId = Session["UserId"] != null ? Convert.ToInt32(Session["UserId"]) : 1;
                
                var order = new tb_DonHang
                {
                    Id = (int)db.GetNextSequenceValue("tb_DonHang"),
                    MaDonHang = "DH" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                    TenKhachHang = string.IsNullOrEmpty(tenKhachHang) ? "Khách vãng lai" : tenKhachHang,
                    SoDienThoai = string.IsNullOrEmpty(soDienThoai) ? "0000000000" : soDienThoai,
                    DiaChi = "Mua tại cửa hàng",
                    TongTien = tongTien,
                    SoLuongSanPham = tongSoLuong,
                    TrangThai = 5, 
                    LoaiThanhToan = "1", 
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now,
                    TaiKhoanId = accountId,
                    PhuongThucVanChuyenId = 1 
                };

                var spIds = chiTiet.Select(x => x.SanPhamId).Distinct().ToList();
                var sanPhamDict = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.In(s => s.Id, spIds))
                    .ToList().ToDictionary(s => s.Id);

                order.DanhSachSanPham = chiTiet.Select(item => new DonHangSanPham
                {
                    SanPhamId = item.SanPhamId,
                    TenSanPham = sanPhamDict.ContainsKey(item.SanPhamId) ? sanPhamDict[item.SanPhamId].TenSanPham : "Sản phẩm",
                    HinhAnh = sanPhamDict.ContainsKey(item.SanPhamId) ? sanPhamDict[item.SanPhamId].HinhAnh : "",
                    Gia = item.Gia,
                    SoLuong = item.SoLuong,
                    ThanhTien = item.Gia * item.SoLuong
                }).ToList();

                db.tb_DonHang.Collection.InsertOne(order);

                foreach (var item in chiTiet)
                {
                    var updateSp = Builders<tb_SanPham>.Update.Inc(x => x.SoLuong, -item.SoLuong);
                    db.tb_SanPham.Collection.UpdateOne(Builders<tb_SanPham>.Filter.Eq(x => x.Id, item.SanPhamId), updateSp);
                }

                return Json(new { success = true, orderId = order.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static string LayTenPhuongThucThanhToan(int? id)
        {
            switch (id)
            {
                case 1: return "Tiền mặt (COD)";
                case 2: return "Chuyển khoản ngân hàng";
                case 3: return "VNPay";
                case 4: return "Ví MoMo";
                case 5: return "Ví ZaloPay";
                default: return "Tiền mặt";
            }
        }

        private static string LayTenPhuongThucVanChuyen(int? id)
        {
            switch (id)
            {
                case 1: return "Giao hàng tiêu chuẩn";
                case 2: return "Giao hàng nhanh";
                case 3: return "Giao hàng hỏa tốc";
                default: return "Giao hàng tiêu chuẩn";
            }
        }

        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult DonHang(int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_DonHang>.Filter.Empty;

            var rawData = db.tb_DonHang.Collection.Find(filter)
                .SortByDescending(dh => dh.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            var mggIds = rawData.Where(d => d.MaGiamGiaId.HasValue).Select(d => d.MaGiamGiaId.Value).Distinct().ToList();
            var mggList = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.In(x => x.Id, mggIds)).ToList();

            var lstDH = rawData
                .Select(dh => new QLDonHangViewModel
                {
                    Id = dh.Id,
                    MaDonHang = dh.MaDonHang,
                    TenKhachHang = dh.TenKhachHang,
                    SoDienThoai = dh.SoDienThoai,
                    DiaChi = dh.DiaChi,
                    Email = dh.Email,
                    TongTien = dh.TongTien,
                    SoLuongSanPham = dh.SoLuongSanPham,
                    TrangThai = dh.TrangThai,
                    NgayTao = dh.NgayTao,
                    PhuongThucThanhToan = dh.PhuongThucThanhToan?.TenPhuongThuc ?? LayTenPhuongThucThanhToan(dh.PhuongThucThanhToanId),
                    PhuongThucVanChuyen = dh.PhuongThucVanChuyen?.TenPhuongThuc ?? LayTenPhuongThucVanChuyen(dh.PhuongThucVanChuyenId),
                    MaGiamGia = !string.IsNullOrEmpty(dh.MaGiamGia) ? dh.MaGiamGia : mggList.FirstOrDefault(x => x.Id == dh.MaGiamGiaId)?.MaCode
                })
                .ToList();

            int totalItems = (int)db.tb_DonHang.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = "";
            return View(lstDH);
        }

        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult TimDonHang(string timdh, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_DonHang>.Filter.Empty;

            if (!String.IsNullOrEmpty(timdh))
            {
                string keyword = System.Text.RegularExpressions.Regex.Escape(timdh.ToLower());
                filter = Builders<tb_DonHang>.Filter.Regex(dh => dh.KhachHang.TenKhachHang, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_DonHang>.Filter.Regex(dh => dh.MaDonHang, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_DonHang>.Filter.Regex(dh => dh.KhachHang.SoDienThoai, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_DonHang>.Filter.Regex("SoDienThoai", new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_DonHang>.Filter.Regex("TenKhachHang", new BsonRegularExpression(keyword, "i"));
            }

            int totalItems = (int)db.tb_DonHang.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var rawData = db.tb_DonHang.Collection.Find(filter)
                .SortByDescending(dh => dh.NgayTao)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            var mggIds = rawData.Where(d => d.MaGiamGiaId.HasValue).Select(d => d.MaGiamGiaId.Value).Distinct().ToList();
            var mggList = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.In(x => x.Id, mggIds)).ToList();

            var lstDH = rawData
                .Select(dh => new QLDonHangViewModel
                {
                    Id = dh.Id,
                    MaDonHang = dh.MaDonHang,
                    TenKhachHang = dh.TenKhachHang,
                    SoDienThoai = dh.SoDienThoai,
                    DiaChi = dh.DiaChi,
                    Email = dh.Email,
                    TongTien = dh.TongTien,
                    SoLuongSanPham = dh.SoLuongSanPham,
                    TrangThai = dh.TrangThai,
                    NgayTao = dh.NgayTao,
                    PhuongThucThanhToan = dh.PhuongThucThanhToan?.TenPhuongThuc ?? LayTenPhuongThucThanhToan(dh.PhuongThucThanhToanId),
                    PhuongThucVanChuyen = dh.PhuongThucVanChuyen?.TenPhuongThuc ?? LayTenPhuongThucVanChuyen(dh.PhuongThucVanChuyenId),
                    MaGiamGia = !string.IsNullOrEmpty(dh.MaGiamGia) ? dh.MaGiamGia : mggList.FirstOrDefault(x => x.Id == dh.MaGiamGiaId)?.MaCode
                })
                .ToList();


            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = timdh;

            return View("DonHang", lstDH);
        }

        [AdminAuthorize(Roles = "1,2,3")]
        public ActionResult LocDonHang(string trangthai, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_DonHang>.Filter.Empty;
            bool isFilterActive = false;

            if (!String.IsNullOrEmpty(trangthai))
            {
                if (trangthai == "store")
                {
                    filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.KhachHang.DiaChi, "Mua tại cửa hàng");
                    isFilterActive = true;
                }
                else
                {
                    int statusId;
                    if (int.TryParse(trangthai, out statusId))
                    {
                        filter = Builders<tb_DonHang>.Filter.Eq(dh => dh.TrangThai, statusId);
                        isFilterActive = true;
                    }
                }
            }

            int totalItems = (int)db.tb_DonHang.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var rawData = db.tb_DonHang.Collection.Find(filter)
                .SortByDescending(dh => dh.NgayTao)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            var mggIds = rawData.Where(d => d.MaGiamGiaId.HasValue).Select(d => d.MaGiamGiaId.Value).Distinct().ToList();
            var mggList = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.In(x => x.Id, mggIds)).ToList();

            var lstLocDH = rawData
                .Select(dh => new QLDonHangViewModel
                {
                    Id = dh.Id,
                    MaDonHang = dh.MaDonHang,
                    TenKhachHang = dh.TenKhachHang,
                    SoDienThoai = dh.SoDienThoai,
                    DiaChi = dh.DiaChi,
                    Email = dh.Email,
                    TongTien = dh.TongTien,
                    SoLuongSanPham = dh.SoLuongSanPham,
                    TrangThai = dh.TrangThai,
                    NgayTao = dh.NgayTao,
                    PhuongThucThanhToan = dh.PhuongThucThanhToan?.TenPhuongThuc ?? LayTenPhuongThucThanhToan(dh.PhuongThucThanhToanId),
                    PhuongThucVanChuyen = dh.PhuongThucVanChuyen?.TenPhuongThuc ?? LayTenPhuongThucVanChuyen(dh.PhuongThucVanChuyenId),
                    MaGiamGia = !string.IsNullOrEmpty(dh.MaGiamGia) ? dh.MaGiamGia : mggList.FirstOrDefault(x => x.Id == dh.MaGiamGiaId)?.MaCode
                })
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = isFilterActive ? trangthai : "";

            return View("DonHang", lstLocDH);
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3")]
        public JsonResult HoanThanhDonHang(int id)
        {
            try
            {
                var donHang = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id)).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai != 4)
                {
                    return Json(new { success = false, message = "Chỉ có thể hoàn thành đơn hàng ở trạng thái 'Đã giao'" });
                }

                var update = Builders<tb_DonHang>.Update
                    .Set(x => x.TrangThai, 5)
                    .Set(x => x.NgayCapNhat, DateTime.Now);

                db.tb_DonHang.Collection.UpdateOne(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id), update);

                return Json(new { success = true, message = "Đơn hàng đã được hoàn thành!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3")]
        public JsonResult CapNhatTrangThaiDonHang(int id, int trangThaiMoi)
        {
            try
            {
                var donHang = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id)).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (trangThaiMoi < 1 || trangThaiMoi > 5)
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ" });
                }

                if (trangThaiMoi <= donHang.TrangThai)
                {
                    return Json(new { success = false, message = "Không thể quay lại trạng thái trước đó" });
                }

                var update = Builders<tb_DonHang>.Update
                    .Set(x => x.TrangThai, trangThaiMoi)
                    .Set(x => x.NgayCapNhat, DateTime.Now);

                db.tb_DonHang.Collection.UpdateOne(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id), update);

                string message = "";
                switch (trangThaiMoi)
                {
                    case 2:
                        message = "Đơn hàng đã được xác nhận";
                        break;
                    case 3:
                        message = "Đơn hàng đang được giao";
                        break;
                    case 4:
                        message = "Đơn hàng đã giao thành công";
                        break;
                    case 5:
                        message = "Đơn hàng đã hoàn thành và kích hoạt bảo hành";
                        KichHoatBaoHanhChoDonHang(id);
                        break;
                    default:
                        message = "Cập nhật trạng thái thành công";
                        break;
                }

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3")]
        public JsonResult YeuCauTraHang(int id)
        {
            try
            {
                var donHang = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id)).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai != 4)
                {
                    return Json(new { success = false, message = "Chỉ có thể yêu cầu trả hàng khi đơn hàng đã giao" });
                }

                var update = Builders<tb_DonHang>.Update
                    .Set(x => x.TrangThai, 6)
                    .Set(x => x.NgayCapNhat, DateTime.Now);

                db.tb_DonHang.Collection.UpdateOne(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id), update);

                return Json(new { success = true, message = "Yêu cầu trả hàng đã được ghi nhận. Admin sẽ xem xét và xử lý." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3")]
        public JsonResult XacNhanTraHang(int id)
        {
            try
            {
                var donHang = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id)).FirstOrDefault();

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai != 6)
                {
                    return Json(new { success = false, message = "Chỉ có thể xác nhận khi đơn hàng ở trạng thái 'Yêu cầu trả hàng'" });
                }

                var update = Builders<tb_DonHang>.Update
                    .Set(x => x.TrangThai, 7)
                    .Set(x => x.NgayCapNhat, DateTime.Now);

                db.tb_DonHang.Collection.UpdateOne(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id), update);

                // Hoàn trả số lượng sản phẩm vào kho khi admin xác nhận nhận hàng trả lại
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

                return Json(new { success = true, message = "Đã xác nhận nhận hàng trả lại thành công! Kho hàng đã được cập nhật." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public JsonResult GetOrderDetails(int id)
        {
            try
            {
                var order = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" }, JsonRequestBehavior.AllowGet);
                }

                var chiTiet = (order.DanhSachSanPham ?? new List<DonHangSanPham>()).Select(ct => new {
                    TenSanPham = !string.IsNullOrEmpty(ct.TenSanPham) ? ct.TenSanPham : "Sản phẩm",
                    SoLuong = ct.SoLuong,
                    Gia = ct.Gia,
                    ThanhTien = ct.ThanhTien > 0 ? ct.ThanhTien : (ct.SoLuong * ct.Gia)
                }).ToList();

                var statusText = "";
                switch (order.TrangThai)
                {
                    case 1: statusText = "Chờ xác nhận"; break;
                    case 2: statusText = "Đã xác nhận"; break;
                    case 3: statusText = "Đang giao"; break;
                    case 4: statusText = "Đã giao"; break;
                    case 5: statusText = "Hoàn thành"; break;
                    case 6: statusText = "Yêu cầu trả hàng"; break;
                    case 7: statusText = "Đã trả hàng"; break;
                    case -1: statusText = "Đã hủy"; break;
                    default: statusText = "Không xác định"; break;
                }
                
                string tenPttt = order.PhuongThucThanhToan?.TenPhuongThuc;
                if (string.IsNullOrEmpty(tenPttt))
                {
                    tenPttt = LayTenPhuongThucThanhToan(order.PhuongThucThanhToanId);
                }

                var result = new {
                    success = true,
                    MaDonHang = order.MaDonHang,
                    TenKhachHang = order.TenKhachHang,
                    SoDienThoai = order.SoDienThoai,
                    Email = order.Email,
                    DiaChi = order.DiaChi,
                    NgayTao = order.NgayTao?.ToString("dd-MM-yyyy HH:mm"),
                    TrangThaiText = statusText,
                    PhuongThucThanhToan = !string.IsNullOrEmpty(tenPttt) ? tenPttt : "N/A",
                    TongTien = order.TongTien,
                    ChiTiet = chiTiet
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
