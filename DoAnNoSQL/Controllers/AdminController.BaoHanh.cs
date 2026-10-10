using System;
using System.Collections.Generic;
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
        private void KichHoatBaoHanhChoDonHang(int donHangId)
        {
            try
            {
                var donHang = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, donHangId)).FirstOrDefault();
                if (donHang == null) return;

                var chiTietList = donHang.DanhSachSanPham;
                if (chiTietList == null || !chiTietList.Any()) return;

                var baoHanhsToInsert = new List<tb_BaoHanh>();

                foreach (var item in chiTietList)
                {
                    var sp = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, item.SanPhamId)).FirstOrDefault();
                    int thoiHan = (sp != null && sp.ThoiGianBaoHanh.HasValue && sp.ThoiGianBaoHanh.Value > 0) ? sp.ThoiGianBaoHanh.Value : 12;

                    for (int i = 0; i < item.SoLuong; i++)
                    {
                        int countExist = (int)db.tb_BaoHanh.Collection.CountDocuments(
                            Builders<tb_BaoHanh>.Filter.Eq(b => b.DonHangId, donHangId) & 
                            Builders<tb_BaoHanh>.Filter.Eq(b => b.SanPhamId, item.SanPhamId)
                        );
                        if (countExist >= item.SoLuong) break;

                        string maBH = "BH" + DateTime.Now.ToString("yyyyMMddHHmmss") + (i + 1).ToString("D2");
                        var bh = new tb_BaoHanh
                        {
                            Id = (int)db.GetNextSequenceValue("tb_BaoHanh"),
                            MaBaoHanh = maBH,
                            SanPham = new BaoHanhSanPham
                            {
                                Id = item.SanPhamId,
                                TenSanPham = item.TenSanPham,
                                HinhAnh = item.HinhAnh,
                                MaSanPham = sp?.MaSanPham
                            },
                            DonHangId = donHang.Id,
                            TaiKhoanId = donHang.TaiKhoanId,
                            KhachHang = new BaoHanhKhachHang
                            {
                                TenKhachHang = string.IsNullOrEmpty(donHang.TenKhachHang) ? "Khách mua hàng" : donHang.TenKhachHang,
                                SoDienThoai = string.IsNullOrEmpty(donHang.SoDienThoai) ? "0000000000" : donHang.SoDienThoai,
                                Email = donHang.Email,
                                DiaChi = donHang.DiaChi
                            },
                            MaSeri = (sp != null ? sp.MaSanPham : "SP") + "-" + DateTime.Now.ToString("yyMMdd") + "-" + (i + 1).ToString("D2"),
                            NgayKichHoat = DateTime.Now,
                            ThoiHanBaoHanh = thoiHan,
                            NgayHetHan = DateTime.Now.AddMonths(thoiHan),
                            TrangThai = 1,
                            GhiChu = "Tự động kích hoạt từ đơn hàng " + donHang.MaDonHang,
                            NgayTao = DateTime.Now,
                            NguoiTao = "Hệ thống"
                        };
                        baoHanhsToInsert.Add(bh);
                    }
                }
                
                if (baoHanhsToInsert.Any())
                {
                    db.tb_BaoHanh.Collection.InsertMany(baoHanhsToInsert);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi kích hoạt bảo hành: " + ex.Message);
            }
        }

        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public ActionResult BaoHanh(int page = 1, string search = "", string status = "", string tab = "baohanh", int repairPage = 1, int? repairStatus = null)
        {
            try
            {
                int pageSize = 10;
                var now = DateTime.Now;
                var thirtyDaysLater = now.AddDays(30);

                var expiredFilter = Builders<tb_BaoHanh>.Filter.Lt(b => b.NgayHetHan, now) & Builders<tb_BaoHanh>.Filter.Eq(b => b.TrangThai, 1);
                var expiredUpdate = Builders<tb_BaoHanh>.Update.Set(b => b.TrangThai, 2);
                db.tb_BaoHanh.Collection.UpdateMany(expiredFilter, expiredUpdate);

                var stats = new BaoHanhStatsViewModel
                {
                    TongSoPhieu = (int)db.tb_BaoHanh.Collection.CountDocuments(Builders<tb_BaoHanh>.Filter.Ne(b => b.TrangThai, 0)),
                    ConHan = (int)db.tb_BaoHanh.Collection.CountDocuments(Builders<tb_BaoHanh>.Filter.Gte(b => b.NgayHetHan, now) & Builders<tb_BaoHanh>.Filter.Ne(b => b.TrangThai, 0)),
                    SapHetHan = (int)db.tb_BaoHanh.Collection.CountDocuments(Builders<tb_BaoHanh>.Filter.Gte(b => b.NgayHetHan, now) & Builders<tb_BaoHanh>.Filter.Lte(b => b.NgayHetHan, thirtyDaysLater) & Builders<tb_BaoHanh>.Filter.Ne(b => b.TrangThai, 0)),
                    HetHan = (int)db.tb_BaoHanh.Collection.CountDocuments(Builders<tb_BaoHanh>.Filter.Lt(b => b.NgayHetHan, now) & Builders<tb_BaoHanh>.Filter.Ne(b => b.TrangThai, 0)),
                    DangSuaChua = (int)db.tb_BaoHanh.Collection.CountDocuments(Builders<tb_BaoHanh>.Filter.Eq(b => b.TrangThai, 3)),
                    TongPhieuSuaChua = (int)db.tb_PhieuSuaChua.Collection.CountDocuments(Builders<tb_PhieuSuaChua>.Filter.Empty),
                    DangXuLySuaChua = (int)db.tb_PhieuSuaChua.Collection.CountDocuments(Builders<tb_PhieuSuaChua>.Filter.Gte(s => s.TrangThai, 1) & Builders<tb_PhieuSuaChua>.Filter.Lte(s => s.TrangThai, 3))
                };

                var bhFilter = Builders<tb_BaoHanh>.Filter.Ne(b => b.TrangThai, 0);

                if (!string.IsNullOrWhiteSpace(search) && tab == "baohanh")
                {
                    string sSearch = System.Text.RegularExpressions.Regex.Escape(search.Trim().ToLower());
                    // Basic search in Mongo without joins to simplify
                    bhFilter &= (
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.MaBaoHanh, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.MaSeri, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.KhachHang.TenKhachHang, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.KhachHang.SoDienThoai, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex("TenKhachHang", new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex("SoDienThoai", new BsonRegularExpression(sSearch, "i"))
                    );
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    switch (status.ToLower())
                    {
                        case "conhan":
                            bhFilter &= Builders<tb_BaoHanh>.Filter.Gte(b => b.NgayHetHan, now) & Builders<tb_BaoHanh>.Filter.Eq(b => b.TrangThai, 1);
                            break;
                        case "saphethan":
                            bhFilter &= Builders<tb_BaoHanh>.Filter.Gte(b => b.NgayHetHan, now) & Builders<tb_BaoHanh>.Filter.Lte(b => b.NgayHetHan, thirtyDaysLater) & Builders<tb_BaoHanh>.Filter.Ne(b => b.TrangThai, 0);
                            break;
                        case "hethan":
                            bhFilter &= Builders<tb_BaoHanh>.Filter.Lt(b => b.NgayHetHan, now);
                            break;
                        case "dangsua":
                            bhFilter &= Builders<tb_BaoHanh>.Filter.Eq(b => b.TrangThai, 3);
                            break;
                    }
                }

                int totalBhItems = (int)db.tb_BaoHanh.Collection.CountDocuments(bhFilter);
                int totalBhPages = (int)Math.Ceiling((double)totalBhItems / pageSize);
                if (page < 1) page = 1;
                if (totalBhPages > 0 && page > totalBhPages) page = totalBhPages;

                var rawBh = db.tb_BaoHanh.Collection.Find(bhFilter)
                    .SortByDescending(b => b.Id)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToList();
                
                var sanPhamIds = rawBh.Where(x => x.SanPhamId.HasValue).Select(x => x.SanPhamId.Value).Distinct().ToList();
                var donHangIds = rawBh.Where(x => x.DonHangId.HasValue).Select(x => x.DonHangId.Value).Distinct().ToList();
                var bhIds = rawBh.Select(x => x.Id).ToList();

                var sanPhams = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.In(x => x.Id, sanPhamIds)).ToList();
                var donHangs = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.In(x => x.Id, donHangIds)).ToList();
                var phieuSuaChuas = db.tb_PhieuSuaChua.Collection.Find(Builders<tb_PhieuSuaChua>.Filter.In(x => x.BaoHanhId, bhIds.Cast<int?>())).ToList();

                var lstBaoHanh = rawBh.Select(b => {
                    var sp = sanPhams.FirstOrDefault(s => s.Id == b.SanPhamId);
                    var dh = donHangs.FirstOrDefault(d => d.Id == b.DonHangId);
                    var pscs = phieuSuaChuas.Where(p => p.BaoHanhId == b.Id).ToList();
                    
                    var choGiao = pscs.FirstOrDefault(p => p.TrangThai == 4);
                    bool isChoGiao = choGiao != null;

                    string ttText = b.TrangThai == 3 ? (isChoGiao ? "Đã sửa xong" : "Đang sửa chữa") : (b.NgayHetHan >= now ? "Còn hạn" : "Hết hạn");
                    string ttBadge = b.TrangThai == 3 ? (isChoGiao ? "bg-primary" : "bg-info text-dark") : (b.NgayHetHan >= now ? ((b.NgayHetHan <= thirtyDaysLater) ? "bg-warning text-dark" : "bg-success") : "bg-secondary");

                    return new BaoHanhViewModel
                    {
                        Id = b.Id,
                        MaBaoHanh = b.MaBaoHanh,
                        SanPhamId = b.SanPhamId ?? 0,
                        TenSanPham = sp != null ? sp.TenSanPham : "N/A",
                        HinhAnh = sp != null ? sp.HinhAnh : "",
                        MaSanPham = sp != null ? sp.MaSanPham : "",
                        DonHangId = b.DonHangId,
                        MaDonHang = dh != null ? dh.MaDonHang : "",
                        TenKhachHang = b.TenKhachHang,
                        SoDienThoai = b.SoDienThoai,
                        Email = b.Email,
                        DiaChi = b.DiaChi,
                        MaSeri = b.MaSeri,
                        NgayKichHoat = b.NgayKichHoat ?? DateTime.MinValue,
                        ThoiHanBaoHanh = b.ThoiHanBaoHanh ?? 0,
                        NgayHetHan = b.NgayHetHan ?? DateTime.MinValue,
                        TrangThai = b.TrangThai ?? 0,
                        TrangThaiText = ttText,
                        TrangThaiBadgeClass = ttBadge,
                        SoNgayConLai = b.NgayHetHan.HasValue ? (int)(b.NgayHetHan.Value - now).TotalDays : 0,
                        SoLanSuaChua = pscs.Count,
                        GhiChu = b.GhiChu,
                        NgayTao = b.NgayTao,
                        ThongTinBoSung = b.ThongTinBoSung ?? new List<BaoHanhThongTinBoSung>(),
                        IsChoGiaoKhach = isChoGiao,
                        PhieuSuaChuaChoGiaoId = choGiao?.Id ?? 0
                    };
                }).ToList();

                var scFilter = Builders<tb_PhieuSuaChua>.Filter.Empty;

                if (!string.IsNullOrWhiteSpace(search) && tab == "suachua")
                {
                    string sSearch = System.Text.RegularExpressions.Regex.Escape(search.Trim().ToLower());
                    
                    // Tìm các thẻ bảo hành khớp với từ khóa (Tên KH, SĐT, Mã BH)
                    var matchingBhs = db.tb_BaoHanh.Collection.Find(
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.MaBaoHanh, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.MaSeri, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.KhachHang.TenKhachHang, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex(b => b.KhachHang.SoDienThoai, new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex("TenKhachHang", new BsonRegularExpression(sSearch, "i")) |
                        Builders<tb_BaoHanh>.Filter.Regex("SoDienThoai", new BsonRegularExpression(sSearch, "i"))
                    ).Project(b => b.Id).ToList();

                    var searchFilter = Builders<tb_PhieuSuaChua>.Filter.Regex(s => s.MaPhieu, new BsonRegularExpression(sSearch, "i"));
                    
                    if (matchingBhs.Any()) {
                        searchFilter |= Builders<tb_PhieuSuaChua>.Filter.In(s => s.BaoHanhId, matchingBhs.Cast<int?>());
                    }
                    
                    scFilter &= searchFilter;
                }

                if (repairStatus.HasValue && repairStatus.Value > 0)
                {
                    scFilter &= Builders<tb_PhieuSuaChua>.Filter.Eq(s => s.TrangThai, repairStatus.Value);
                }

                int totalScItems = (int)db.tb_PhieuSuaChua.Collection.CountDocuments(scFilter);
                int totalScPages = (int)Math.Ceiling((double)totalScItems / pageSize);
                if (repairPage < 1) repairPage = 1;
                if (totalScPages > 0 && repairPage > totalScPages) repairPage = totalScPages;

                var rawSc = db.tb_PhieuSuaChua.Collection.Find(scFilter)
                    .SortByDescending(s => s.Id)
                    .Skip((repairPage - 1) * pageSize)
                    .Limit(pageSize)
                    .ToList();
                
                var scBhIds = rawSc.Where(s => s.BaoHanhId.HasValue).Select(s => s.BaoHanhId.Value).Distinct().ToList();
                var scBhs = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.In(b => b.Id, scBhIds)).ToList();
                var scSpIds = scBhs.Where(b => b.SanPhamId.HasValue).Select(b => b.SanPhamId.Value).Distinct().ToList();
                var scSps = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.In(s => s.Id, scSpIds)).ToList();

                var lstPhieuSuaChua = rawSc.Select(s => {
                    var bh = scBhs.FirstOrDefault(b => b.Id == s.BaoHanhId);
                    var sp = bh != null ? scSps.FirstOrDefault(x => x.Id == bh.SanPhamId) : null;
                    
                    string ttText = "";
                    string ttBadge = "";
                    switch (s.TrangThai)
                    {
                        case 1: ttText = "Tiếp nhận"; ttBadge = "bg-primary"; break;
                        case 2: ttText = "Đang sửa chữa"; ttBadge = "bg-warning text-dark"; break;
                        case 3: ttText = "Chờ linh kiện/Hãng"; ttBadge = "bg-info text-dark"; break;
                        case 4: ttText = "Đã sửa xong"; ttBadge = "bg-success"; break;
                        case 5: ttText = "Đã trả khách"; ttBadge = "bg-secondary"; break;
                        case 6: ttText = "Đổi mới"; ttBadge = "bg-dark"; break;
                        case 7: ttText = "Từ chối bảo hành"; ttBadge = "bg-danger"; break;
                        default: ttText = "Không xác định"; ttBadge = "bg-secondary"; break;
                    }
                    return new PhieuSuaChuaViewModel
                    {
                        Id = s.Id,
                        MaPhieu = s.MaPhieu,
                        BaoHanhId = s.BaoHanhId ?? 0,
                        MaBaoHanh = bh != null ? bh.MaBaoHanh : "",
                        TenSanPham = sp?.TenSanPham ?? "N/A",
                        HinhAnh = sp?.HinhAnh ?? "",
                        MaSeri = bh?.MaSeri ?? "",
                        TenKhachHang = bh?.TenKhachHang ?? "",
                        SoDienThoai = bh?.SoDienThoai ?? "",
                        TrieuChungLoi = s.TrieuChungLoi,
                        TinhTrangTiepNhan = s.TinhTrangTiepNhan,
                        NgayTiepNhan = s.NgayTiepNhan ?? DateTime.MinValue,
                        NgayHenTra = s.NgayHenTra,
                        NgayTraThucTe = s.NgayTraThucTe,
                        TrangThai = s.TrangThai ?? 0,
                        TrangThaiText = ttText,
                        TrangThaiBadgeClass = ttBadge,
                        ChiPhiPhatSinh = s.ChiPhiPhatSinh ?? 0,
                        GiaiPhapXuLy = s.GiaiPhapXuLy,
                        LinhKienThayThe = s.LinhKienThayThe,
                        NguoiTiepNhan = s.NguoiTiepNhan,
                        KyThuatVien = s.KyThuatVien,
                        GhiChu = s.GhiChu,
                        NgayTao = s.NgayTao
                    };
                }).ToList();

                var lstSanPhamDropdown = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Ne(s => s.CacCo.HienThi, false))
                    .SortBy(s => s.TenSanPham)
                    .ToList()
                    .Select(s => new SanPhamDropdownViewModel
                    {
                        Id = s.Id,
                        TenSanPham = s.TenSanPham,
                        MaSanPham = s.MaSanPham,
                        ThoiGianBaoHanh = s.ThoiGianBaoHanh ?? 12
                    })
                    .ToList();

                var viewModel = new BaoHanhIndexViewModel
                {
                    DanhSachBaoHanh = lstBaoHanh,
                    DanhSachPhieuSuaChua = lstPhieuSuaChua,
                    DanhSachSanPhamDropdown = lstSanPhamDropdown,
                    ThongKe = stats,
                    CurrentPage = page,
                    TotalPages = totalBhPages,
                    TuKhoa = search,
                    TrangThai = status,
                    ActiveTab = tab,
                    RepairCurrentPage = repairPage,
                    RepairTotalPages = totalScPages,
                    RepairStatus = repairStatus
                };

                ViewBag.RepairCurrentPage = repairPage;
                ViewBag.RepairTotalPages = totalScPages;
                ViewBag.RepairStatus = repairStatus;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải trang bảo hành: " + ex.Message;
                return View(new BaoHanhIndexViewModel
                {
                    DanhSachBaoHanh = new List<BaoHanhViewModel>(),
                    DanhSachPhieuSuaChua = new List<PhieuSuaChuaViewModel>(),
                    DanhSachSanPhamDropdown = new List<SanPhamDropdownViewModel>(),
                    ThongKe = new BaoHanhStatsViewModel(),
                    ActiveTab = tab,
                    RepairCurrentPage = 1,
                    RepairTotalPages = 1
                });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult ThemBaoHanh(BaoHanhFormViewModel model)
        {
            try
            {
                if (model.SanPhamId <= 0) return Json(new { success = false, message = "Vui lòng chọn sản phẩm." });
                if (string.IsNullOrWhiteSpace(model.TenKhachHang)) return Json(new { success = false, message = "Vui lòng nhập tên khách hàng." });
                if (string.IsNullOrWhiteSpace(model.SoDienThoai)) return Json(new { success = false, message = "Vui lòng nhập số điện thoại." });

                var sp = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, model.SanPhamId)).FirstOrDefault();
                int thoiHan = model.ThoiHanBaoHanh > 0 ? model.ThoiHanBaoHanh : (sp?.ThoiGianBaoHanh ?? 12);
                DateTime ngayKichHoat = model.NgayKichHoat != default(DateTime) ? model.NgayKichHoat : DateTime.Now;
                DateTime ngayHetHan = ngayKichHoat.AddMonths(thoiHan);

                string maBH = "BH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                var customFieldNames = Request.Form.GetValues("CustomFieldName");
                var customFieldValues = Request.Form.GetValues("CustomFieldValue");
                var listBoSung = new List<BaoHanhThongTinBoSung>();

                if (customFieldNames != null && customFieldValues != null)
                {
                    for (int i = 0; i < Math.Min(customFieldNames.Length, customFieldValues.Length); i++)
                    {
                        var fn = customFieldNames[i]?.Trim();
                        var fv = customFieldValues[i]?.Trim();
                        if (!string.IsNullOrWhiteSpace(fn) && !string.IsNullOrWhiteSpace(fv))
                        {
                            listBoSung.Add(new BaoHanhThongTinBoSung { TenTruong = fn, GiaTri = fv });
                        }
                    }
                }
                else if (model.ThongTinBoSung != null && model.ThongTinBoSung.Any())
                {
                    listBoSung = model.ThongTinBoSung.Where(x => !string.IsNullOrWhiteSpace(x.TenTruong) && !string.IsNullOrWhiteSpace(x.GiaTri)).ToList();
                }

                var bh = new tb_BaoHanh
                {
                    Id = (int)db.GetNextSequenceValue("tb_BaoHanh"),
                    MaBaoHanh = maBH,
                    SanPhamId = model.SanPhamId,
                    DonHangId = model.DonHangId,
                    TenKhachHang = model.TenKhachHang.Trim(),
                    SoDienThoai = model.SoDienThoai.Trim(),
                    Email = model.Email?.Trim(),
                    DiaChi = model.DiaChi?.Trim(),
                    MaSeri = !string.IsNullOrWhiteSpace(model.MaSeri) ? model.MaSeri.Trim() : (sp?.MaSanPham + "-" + DateTime.Now.ToString("yyMMddHHmm")),
                    NgayKichHoat = ngayKichHoat,
                    ThoiHanBaoHanh = thoiHan,
                    NgayHetHan = ngayHetHan,
                    TrangThai = ngayHetHan >= DateTime.Now ? 1 : 2,
                    GhiChu = model.GhiChu?.Trim(),
                    ThongTinBoSung = listBoSung,
                    NgayTao = DateTime.Now,
                    NguoiTao = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin"
                };

                db.tb_BaoHanh.Collection.InsertOne(bh);

                return Json(new { success = true, message = "Tạo phiếu bảo hành thành công!", id = bh.Id, maBH = bh.MaBaoHanh });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult SuaBaoHanh(BaoHanhFormViewModel model)
        {
            try
            {
                var bh = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, model.Id)).FirstOrDefault();
                if (bh == null) return Json(new { success = false, message = "Không tìm thấy phiếu bảo hành." });

                var customFieldNames = Request.Form.GetValues("CustomFieldName");
                var customFieldValues = Request.Form.GetValues("CustomFieldValue");
                var listBoSung = new List<BaoHanhThongTinBoSung>();

                if (customFieldNames != null && customFieldValues != null)
                {
                    for (int i = 0; i < Math.Min(customFieldNames.Length, customFieldValues.Length); i++)
                    {
                        var fn = customFieldNames[i]?.Trim();
                        var fv = customFieldValues[i]?.Trim();
                        if (!string.IsNullOrWhiteSpace(fn) && !string.IsNullOrWhiteSpace(fv))
                        {
                            listBoSung.Add(new BaoHanhThongTinBoSung { TenTruong = fn, GiaTri = fv });
                        }
                    }
                }
                else if (model.ThongTinBoSung != null)
                {
                    listBoSung = model.ThongTinBoSung.Where(x => !string.IsNullOrWhiteSpace(x.TenTruong) && !string.IsNullOrWhiteSpace(x.GiaTri)).ToList();
                }

                var updateBuilder = Builders<tb_BaoHanh>.Update
                    .Set(x => x.KhachHang.TenKhachHang, !string.IsNullOrWhiteSpace(model.TenKhachHang) ? model.TenKhachHang.Trim() : bh.TenKhachHang)
                    .Set(x => x.KhachHang.SoDienThoai, !string.IsNullOrWhiteSpace(model.SoDienThoai) ? model.SoDienThoai.Trim() : bh.SoDienThoai)
                    .Set(x => x.KhachHang.Email, model.Email?.Trim())
                    .Set(x => x.KhachHang.DiaChi, model.DiaChi?.Trim())
                    .Set(x => x.MaSeri, model.MaSeri?.Trim())
                    .Set(x => x.GhiChu, model.GhiChu?.Trim())
                    .Set(x => x.ThongTinBoSung, listBoSung)
                    .Set(x => x.NgayCapNhat, DateTime.Now)
                    .Set(x => x.NguoiCapNhat, Session["UserName"] != null ? Session["UserName"].ToString() : "Admin");

                if (model.SanPhamId > 0) updateBuilder = updateBuilder.Set(x => x.SanPhamId, model.SanPhamId);

                if (model.ThoiHanBaoHanh > 0)
                {
                    updateBuilder = updateBuilder.Set(x => x.ThoiHanBaoHanh, model.ThoiHanBaoHanh);
                    DateTime ngayKich = bh.NgayKichHoat ?? DateTime.Now;
                    if (model.NgayKichHoat != default(DateTime))
                    {
                        ngayKich = model.NgayKichHoat;
                        updateBuilder = updateBuilder.Set(x => x.NgayKichHoat, ngayKich);
                    }
                    DateTime ngayHet = ngayKich.AddMonths(model.ThoiHanBaoHanh);
                    updateBuilder = updateBuilder.Set(x => x.NgayHetHan, ngayHet);
                    
                    if (bh.TrangThai != 3) 
                    {
                        updateBuilder = updateBuilder.Set(x => x.TrangThai, ngayHet >= DateTime.Now ? 1 : 2);
                    }
                }

                db.tb_BaoHanh.Collection.UpdateOne(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, model.Id), updateBuilder);
                return Json(new { success = true, message = "Cập nhật phiếu bảo hành thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1")]
        public JsonResult XoaBaoHanh(int id)
        {
            try
            {
                // Xóa tất cả phiếu sửa chữa liên quan đến phiếu bảo hành này
                db.tb_PhieuSuaChua.Collection.DeleteMany(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.BaoHanhId, id));
                
                db.tb_BaoHanh.Collection.DeleteOne(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, id));
                return Json(new { success = true, message = "Đã xóa phiếu bảo hành thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult ChiTietBaoHanh(int id)
        {
            try
            {
                var bh = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (bh == null) return Json(new { success = false, message = "Không tìm thấy thông tin bảo hành." }, JsonRequestBehavior.AllowGet);

                var sp = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, bh.SanPhamId)).FirstOrDefault();
                var dh = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, bh.DonHangId)).FirstOrDefault();
                var pscList = db.tb_PhieuSuaChua.Collection.Find(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.BaoHanhId, id)).SortByDescending(x => x.Id).ToList();

                var now = DateTime.Now;
                var thirtyDaysLater = now.AddDays(30);

                var phieuSuaList = new List<object>();
                if (pscList.Any())
                {
                    foreach (var s in pscList)
                    {
                        string ttText = "";
                        string ttBadge = "";
                        switch (s.TrangThai)
                        {
                            case 1: ttText = "Tiếp nhận"; ttBadge = "bg-primary"; break;
                            case 2: ttText = "Đang sửa"; ttBadge = "bg-warning text-dark"; break;
                            case 3: ttText = "Chờ linh kiện"; ttBadge = "bg-info text-dark"; break;
                            case 4: ttText = "Đã sửa xong"; ttBadge = "bg-success"; break;
                            case 5: ttText = "Đã trả khách"; ttBadge = "bg-secondary"; break;
                            case 6: ttText = "Đổi mới"; ttBadge = "bg-dark"; break;
                            case 7: ttText = "Từ chối"; ttBadge = "bg-danger"; break;
                            default: ttText = "Không xác định"; ttBadge = "bg-secondary"; break;
                        }
                        phieuSuaList.Add(new {
                            s.Id,
                            s.MaPhieu,
                            s.TrieuChungLoi,
                            s.TinhTrangTiepNhan,
                            NgayTiepNhan = s.NgayTiepNhan?.ToString("dd/MM/yyyy HH:mm") ?? "",
                            NgayHenTra = s.NgayHenTra?.ToString("dd/MM/yyyy"),
                            NgayTraThucTe = s.NgayTraThucTe?.ToString("dd/MM/yyyy"),
                            s.TrangThai,
                            TrangThaiText = ttText,
                            TrangThaiBadgeClass = ttBadge,
                            ChiPhiPhatSinh = s.ChiPhiPhatSinh ?? 0,
                            s.GiaiPhapXuLy,
                            s.LinhKienThayThe,
                            s.KyThuatVien,
                            s.NguoiTiepNhan,
                            s.GhiChu
                        });
                    }
                }

                var data = new
                {
                    success = true,
                    bh.Id,
                    bh.MaBaoHanh,
                    bh.SanPhamId,
                    TenSanPham = sp?.TenSanPham ?? "N/A",
                    HinhAnh = sp?.HinhAnh ?? "",
                    MaSanPham = sp?.MaSanPham ?? "",
                    DonHangId = bh.DonHangId,
                    MaDonHang = dh?.MaDonHang ?? "Mua lẻ / Ngoài",
                    bh.TenKhachHang,
                    bh.SoDienThoai,
                    bh.Email,
                    bh.DiaChi,
                    bh.MaSeri,
                    NgayKichHoat = bh.NgayKichHoat?.ToString("dd/MM/yyyy") ?? "",
                    NgayKichHoatVal = bh.NgayKichHoat?.ToString("yyyy-MM-dd") ?? "",
                    bh.ThoiHanBaoHanh,
                    NgayHetHan = bh.NgayHetHan?.ToString("dd/MM/yyyy") ?? "",
                    bh.TrangThai,
                    TrangThaiText = bh.TrangThai == 3 ? "Đang sửa chữa" : (bh.NgayHetHan >= now ? "Còn hạn" : "Hết hạn"),
                    TrangThaiBadgeClass = bh.TrangThai == 3 ? "bg-info text-dark" : (bh.NgayHetHan >= now ? ((bh.NgayHetHan <= thirtyDaysLater) ? "bg-warning text-dark" : "bg-success") : "bg-secondary"),
                    SoNgayConLai = bh.NgayHetHan.HasValue ? (int)(bh.NgayHetHan.Value - now).TotalDays : 0,
                    bh.GhiChu,
                    NgayTao = bh.NgayTao?.ToString("dd/MM/yyyy HH:mm"),
                    ThongTinBoSung = (bh.ThongTinBoSung ?? new List<BaoHanhThongTinBoSung>()).Select(t => new { t.TenTruong, t.GiaTri, t.GhiChu }).ToList(),
                    LichSuSuaChua = phieuSuaList
                };

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult TraCuuBaoHanhAjax(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword)) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

                keyword = System.Text.RegularExpressions.Regex.Escape(keyword.Trim().ToLower());
                var filter = Builders<tb_BaoHanh>.Filter.Ne(b => b.TrangThai, 0) & (
                    Builders<tb_BaoHanh>.Filter.Regex(b => b.MaBaoHanh, new BsonRegularExpression(keyword, "i")) |
                    Builders<tb_BaoHanh>.Filter.Regex(b => b.MaSeri, new BsonRegularExpression(keyword, "i")) |
                    Builders<tb_BaoHanh>.Filter.Regex(b => b.KhachHang.SoDienThoai, new BsonRegularExpression(keyword, "i")) |
                    Builders<tb_BaoHanh>.Filter.Regex(b => b.KhachHang.TenKhachHang, new BsonRegularExpression(keyword, "i")) |
                    Builders<tb_BaoHanh>.Filter.Regex("SoDienThoai", new BsonRegularExpression(keyword, "i")) |
                    Builders<tb_BaoHanh>.Filter.Regex("TenKhachHang", new BsonRegularExpression(keyword, "i"))
                );

                var results = db.tb_BaoHanh.Collection.Find(filter).Limit(10).ToList();
                var spIds = results.Where(x => x.SanPhamId.HasValue).Select(x => x.SanPhamId.Value).Distinct().ToList();
                var sps = new List<tb_SanPham>();
                if (spIds.Any()) {
                    sps = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.In(x => x.Id, spIds)).ToList();
                }

                var outData = results.Select(b => {
                    var sp = sps.FirstOrDefault(s => s.Id == b.SanPhamId);
                    return new {
                        b.Id,
                        b.MaBaoHanh,
                        b.MaSeri,
                        TenSanPham = sp?.TenSanPham ?? "N/A",
                        b.TenKhachHang,
                        b.SoDienThoai,
                        NgayHetHan = b.NgayHetHan?.ToString("dd/MM/yyyy") ?? "",
                        IsConHan = b.NgayHetHan >= DateTime.Now,
                        b.TrangThai
                    };
                }).ToList();

                return Json(outData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult TaoPhieuSuaChua(PhieuSuaChuaFormViewModel model)
        {
            try
            {
                if (model.BaoHanhId <= 0) return Json(new { success = false, message = "Vui lòng chọn phiếu bảo hành hợp lệ." });
                if (string.IsNullOrWhiteSpace(model.TrieuChungLoi)) return Json(new { success = false, message = "Vui lòng nhập triệu chứng / mô tả lỗi." });

                var bh = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, model.BaoHanhId)).FirstOrDefault();
                if (bh == null) return Json(new { success = false, message = "Không tìm thấy phiếu bảo hành liên kết." });

                string maPhieu = "SC" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var sc = new tb_PhieuSuaChua
                {
                    Id = (int)db.GetNextSequenceValue("tb_PhieuSuaChua"),
                    MaPhieu = maPhieu,
                    BaoHanhId = model.BaoHanhId,
                    TrieuChungLoi = model.TrieuChungLoi.Trim(),
                    TinhTrangTiepNhan = model.TinhTrangTiepNhan?.Trim(),
                    NgayTiepNhan = DateTime.Now,
                    NgayHenTra = model.NgayHenTra,
                    TrangThai = 1,
                    ChiPhiPhatSinh = model.ChiPhiPhatSinh,
                    GiaiPhapXuLy = model.GiaiPhapXuLy?.Trim(),
                    LinhKienThayThe = model.LinhKienThayThe?.Trim(),
                    NguoiTiepNhan = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin",
                    KyThuatVien = model.KyThuatVien?.Trim(),
                    GhiChu = model.GhiChu?.Trim(),
                    NgayTao = DateTime.Now
                };

                var repairItem = new LichSuSuaChuaItem
                {
                    MaPhieu = maPhieu,
                    TrieuChungLoi = model.TrieuChungLoi.Trim(),
                    TinhTrangTiepNhan = model.TinhTrangTiepNhan?.Trim(),
                    NgayTiepNhan = DateTime.Now,
                    NgayHenTra = model.NgayHenTra,
                    TrangThai = 1,
                    ChiPhiPhatSinh = model.ChiPhiPhatSinh,
                    GiaiPhapXuLy = model.GiaiPhapXuLy?.Trim(),
                    LinhKienThayThe = model.LinhKienThayThe?.Trim(),
                    NguoiTiepNhan = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin",
                    KyThuatVien = model.KyThuatVien?.Trim(),
                    GhiChu = model.GhiChu?.Trim()
                };

                db.tb_PhieuSuaChua.Collection.InsertOne(sc);
                db.tb_BaoHanh.Collection.UpdateOne(
                    Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, bh.Id),
                    Builders<tb_BaoHanh>.Update.Set(x => x.TrangThai, 3).Push(x => x.LichSuSuaChua, repairItem)
                );

                return Json(new { success = true, message = "Tạo phiếu tiếp nhận bảo hành thành công!", id = sc.Id, maPhieu = sc.MaPhieu });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult CapNhatPhieuSuaChua(PhieuSuaChuaFormViewModel model)
        {
            try
            {
                var sc = db.tb_PhieuSuaChua.Collection.Find(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.Id, model.Id)).FirstOrDefault();
                if (sc == null) return Json(new { success = false, message = "Không tìm thấy phiếu sửa chữa." });

                var updateSc = Builders<tb_PhieuSuaChua>.Update
                    .Set(x => x.TinhTrangTiepNhan, model.TinhTrangTiepNhan?.Trim())
                    .Set(x => x.NgayHenTra, model.NgayHenTra)
                    .Set(x => x.TrangThai, model.TrangThai)
                    .Set(x => x.ChiPhiPhatSinh, model.ChiPhiPhatSinh)
                    .Set(x => x.GiaiPhapXuLy, model.GiaiPhapXuLy?.Trim())
                    .Set(x => x.LinhKienThayThe, model.LinhKienThayThe?.Trim())
                    .Set(x => x.KyThuatVien, model.KyThuatVien?.Trim())
                    .Set(x => x.GhiChu, model.GhiChu?.Trim())
                    .Set(x => x.NgayCapNhat, DateTime.Now);
                    
                if (!string.IsNullOrWhiteSpace(model.TrieuChungLoi)) updateSc = updateSc.Set(x => x.TrieuChungLoi, model.TrieuChungLoi.Trim());

                var bh = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, sc.BaoHanhId)).FirstOrDefault();

                if (model.TrangThai == 5 || model.TrangThai == 6 || model.TrangThai == 7)
                {
                    updateSc = updateSc.Set(x => x.NgayTraThucTe, DateTime.Now);
                    if (bh != null)
                    {
                        db.tb_BaoHanh.Collection.UpdateOne(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, bh.Id), Builders<tb_BaoHanh>.Update.Set(x => x.TrangThai, bh.NgayHetHan >= DateTime.Now ? 1 : 2));
                    }
                }
                else if (model.TrangThai >= 1 && model.TrangThai <= 3)
                {
                    if (bh != null)
                    {
                        db.tb_BaoHanh.Collection.UpdateOne(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, bh.Id), Builders<tb_BaoHanh>.Update.Set(x => x.TrangThai, 3));
                    }
                }

                db.tb_PhieuSuaChua.Collection.UpdateOne(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.Id, sc.Id), updateSc);
                return Json(new { success = true, message = "Cập nhật tiến độ sửa chữa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult CapNhatNhanhTrangThaiSuaChua(int id, int trangThai)
        {
            try
            {
                var sc = db.tb_PhieuSuaChua.Collection.Find(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (sc == null) return Json(new { success = false, message = "Không tìm thấy phiếu sửa chữa." });

                var updateSc = Builders<tb_PhieuSuaChua>.Update
                    .Set(x => x.TrangThai, trangThai)
                    .Set(x => x.NgayCapNhat, DateTime.Now);

                var bh = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, sc.BaoHanhId)).FirstOrDefault();

                if (trangThai == 5 || trangThai == 6 || trangThai == 7)
                {
                    updateSc = updateSc.Set(x => x.NgayTraThucTe, DateTime.Now);
                    if (bh != null)
                    {
                        db.tb_BaoHanh.Collection.UpdateOne(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, bh.Id), Builders<tb_BaoHanh>.Update.Set(x => x.TrangThai, bh.NgayHetHan >= DateTime.Now ? 1 : 2));
                    }
                }
                else if (trangThai >= 1 && trangThai <= 3)
                {
                    if (bh != null)
                    {
                        db.tb_BaoHanh.Collection.UpdateOne(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, bh.Id), Builders<tb_BaoHanh>.Update.Set(x => x.TrangThai, 3));
                    }
                }

                db.tb_PhieuSuaChua.Collection.UpdateOne(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.Id, sc.Id), updateSc);
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1")]
        public JsonResult XoaPhieuSuaChua(int id)
        {
            try
            {
                var sc = db.tb_PhieuSuaChua.Collection.Find(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (sc == null) return Json(new { success = false, message = "Không tìm thấy phiếu sửa chữa." });

                db.tb_PhieuSuaChua.Collection.DeleteOne(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.Id, id));

                if (sc.BaoHanhId.HasValue)
                {
                    var bh = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, sc.BaoHanhId.Value)).FirstOrDefault();
                    if (bh != null)
                    {
                        bool stillRepairing = db.tb_PhieuSuaChua.Collection.Find(Builders<tb_PhieuSuaChua>.Filter.Eq(s => s.BaoHanhId, bh.Id) & Builders<tb_PhieuSuaChua>.Filter.Gte(s => s.TrangThai, 1) & Builders<tb_PhieuSuaChua>.Filter.Lte(s => s.TrangThai, 3)).Any();
                        db.tb_BaoHanh.Collection.UpdateOne(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, bh.Id), Builders<tb_BaoHanh>.Update.Set(x => x.TrangThai, stillRepairing ? 3 : (bh.NgayHetHan >= DateTime.Now ? 1 : 2)));
                    }
                }

                return Json(new { success = true, message = "Đã xóa phiếu sửa chữa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult LayThongTinInBaoHanh(int id)
        {
            try
            {
                var bh = db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (bh == null) return Json(new { success = false, message = "Không tìm thấy phiếu bảo hành" }, JsonRequestBehavior.AllowGet);

                var sp = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, bh.SanPhamId)).FirstOrDefault();

                return Json(new {
                    success = true,
                    bh.MaBaoHanh,
                    TenSanPham = sp?.TenSanPham ?? "N/A",
                    MaSanPham = sp?.MaSanPham ?? "N/A",
                    bh.MaSeri,
                    bh.TenKhachHang,
                    bh.SoDienThoai,
                    bh.Email,
                    bh.DiaChi,
                    NgayKichHoat = bh.NgayKichHoat?.ToString("dd/MM/yyyy") ?? "",
                    bh.ThoiHanBaoHanh,
                    NgayHetHan = bh.NgayHetHan?.ToString("dd/MM/yyyy") ?? "",
                    bh.GhiChu,
                    ThongTinBoSung = (bh.ThongTinBoSung ?? new List<BaoHanhThongTinBoSung>()).Select(t => new { t.TenTruong, t.GiaTri, t.GhiChu }).ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [AdminAuthorize(Roles = "1,2,3,4,5")]
        public JsonResult LayThongTinInPhieuSua(int id)
        {
            try
            {
                var sc = db.tb_PhieuSuaChua.Collection.Find(Builders<tb_PhieuSuaChua>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (sc == null) return Json(new { success = false, message = "Không tìm thấy phiếu sửa chữa" }, JsonRequestBehavior.AllowGet);

                var bh = sc.BaoHanhId.HasValue ? db.tb_BaoHanh.Collection.Find(Builders<tb_BaoHanh>.Filter.Eq(x => x.Id, sc.BaoHanhId.Value)).FirstOrDefault() : null;
                var sp = bh != null && bh.SanPhamId.HasValue ? db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, bh.SanPhamId.Value)).FirstOrDefault() : null;

                return Json(new {
                    success = true,
                    sc.MaPhieu,
                    MaBaoHanh = bh?.MaBaoHanh ?? "",
                    TenSanPham = sp?.TenSanPham ?? "N/A",
                    MaSeri = bh?.MaSeri ?? "",
                    TenKhachHang = bh?.TenKhachHang ?? "",
                    SoDienThoai = bh?.SoDienThoai ?? "",
                    DiaChi = bh?.DiaChi ?? "",
                    sc.TrieuChungLoi,
                    sc.TinhTrangTiepNhan,
                    NgayTiepNhan = sc.NgayTiepNhan?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    NgayHenTra = sc.NgayHenTra?.ToString("dd/MM/yyyy") ?? "Chưa xác định",
                    sc.NguoiTiepNhan,
                    sc.KyThuatVien,
                    sc.GiaiPhapXuLy,
                    sc.LinhKienThayThe,
                    ChiPhiPhatSinh = sc.ChiPhiPhatSinh ?? 0,
                    sc.GhiChu
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
