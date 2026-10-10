using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using DoAnNoSQL.Models;
using DoAnNoSQL.Filters;
using MongoDB.Bson;

namespace DoAnNoSQL.Controllers
{
    public partial class AdminController
    {
        private static readonly string[] StandardSpecKeywords = new string[] {
            "RAM", "Bộ nhớ RAM", "Bộ nhớ trong", "ROM", "Màn hình", "Dung lượng Pin", "Pin", "Camera", "Hệ điều hành", "Cổng kết nối"
        };

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult QLSP(int page = 1)
        {
            int pageSize = 10;

            var lstspRaw = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Empty)
                .SortByDescending(sp => sp.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            var danhMucIds = lstspRaw.Select(s => s.DanhMucId).Distinct().ToList();
            var thuongHieuIds = lstspRaw.Where(s => s.ThuongHieuId.HasValue).Select(s => s.ThuongHieuId.Value).Distinct().ToList();
            
            var danhMucs = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.In(d => d.Id, danhMucIds)).ToList();
            var thuongHieus = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.In(t => t.Id, thuongHieuIds)).ToList();
            
            var lstsp = lstspRaw.Select(sp => new QLSP
            {
                Id = sp.Id,
                TenSanPham = sp.TenSanPham,
                BiDanh = sp.BiDanh,
                MaSanPham = sp.MaSanPham,
                MoTa = sp.MoTa,
                ChiTiet = sp.ChiTiet,
                Gia = sp.Gia,
                GiaKhuyenMai = sp.GiaKhuyenMai,
                SoLuong = sp.SoLuong ?? 0,
                LuotXem = sp.LuotXem ?? 0,
                HinhAnh = sp.HinhAnh,
                
                TenDanhMuc = sp.DanhMuc?.TenDanhMuc ?? danhMucs.FirstOrDefault(d => d.Id == sp.DanhMucId)?.TenDanhMuc,
                TenThuongHieu = sp.ThuongHieu?.TenThuongHieu ?? thuongHieus.FirstOrDefault(t => t.Id == sp.ThuongHieuId)?.TenThuongHieu,

                BoNhoRAM = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 1 || (ts.TenThongSo != null && ts.TenThongSo.Contains("RAM")))?.GiaTri,
                BoNhoTrong = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 2 || (ts.TenThongSo != null && ts.TenThongSo.Contains("trong")))?.GiaTri,
                ManHinh = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 3 || (ts.TenThongSo != null && (ts.TenThongSo.Contains("hình") || ts.TenThongSo.Contains("hành"))))?.GiaTri,
                DungLuongPin = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 4 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("pin")))?.GiaTri,
                Camera = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 5 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("camera")))?.GiaTri,
                HeDieuHanh = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 6 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("hành")))?.GiaTri,
                CongKetNoi = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 7 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("kết nối")))?.GiaTri,

                CacThongSoTuyChinh = sp.ThongSoKyThuat?
                    .Where(ts => !string.IsNullOrWhiteSpace(ts.TenThongSo) && !StandardSpecKeywords.Any(k => ts.TenThongSo.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList() ?? new List<SanPhamThongSo>(),

                LaSanPhamTrangChu = sp.LaSanPhamTrangChu,
                LaSanPhamKhuyenMai = sp.LaSanPhamKhuyenMai,
                LaSanPhamNoiBat = sp.LaSanPhamNoiBat,
                LaSanPhamHot = sp.LaSanPhamHot,
                LaSanPhamMoi = sp.LaSanPhamMoi,
                LaSanPhamBanChay = sp.LaSanPhamBanChay,
                HienThi = sp.HienThi,
                NgayTao = sp.NgayTao,
            }).ToList();

            int totalItems = (int)db.tb_SanPham.Collection.CountDocuments(Builders<tb_SanPham>.Filter.Empty);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = "";
            ViewBag.DanhMucs = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList();
            ViewBag.ThuongHieus = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList();

            return View(lstsp);
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult formThemSP()
        {
            ViewBag.lstDMSP = new SelectList(db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList(), "Id", "TenDanhMuc");
            ViewBag.lstTH = new SelectList(db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList(), "Id", "TenThuongHieu");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,4")]
        public ActionResult ThemSP(QLSP sp, HttpPostedFileBase[] fileupload, int defaultImageIndex = 0)
        {
            if (fileupload == null || !fileupload.Any(f => f != null && f.ContentLength > 0))
            {
                ViewBag.ThongBao = "Vui lòng chọn ít nhất một ảnh sản phẩm";
                ViewBag.lstDMSP = new SelectList(db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList(), "Id", "TenDanhMuc");
                ViewBag.lstTH = new SelectList(db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList(), "Id", "TenThuongHieu");
                return View("formThemSP", sp);
            }

            try
            {
                var validFiles = fileupload.Where(f => f != null && f.ContentLength > 0).ToList();
                if (!validFiles.Any())
                {
                    ViewBag.ThongBao = "Vui lòng chọn ít nhất một ảnh sản phẩm";
                    ViewBag.lstDMSP = new SelectList(db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList(), "Id", "TenDanhMuc");
                    ViewBag.lstTH = new SelectList(db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList(), "Id", "TenThuongHieu");
                    return View("formThemSP", sp);
                }

                var firstFile = validFiles[0];
                var mainFilename = Path.GetFileName(firstFile.FileName);
                var mainPath = Path.Combine(Server.MapPath("~/Content/Images"), mainFilename);
                
                if (System.IO.File.Exists(mainPath))
                {
                    var extension = Path.GetExtension(mainFilename);
                    var name = Path.GetFileNameWithoutExtension(mainFilename);
                    mainFilename = name + "_" + DateTime.Now.Ticks + extension;
                    mainPath = Path.Combine(Server.MapPath("~/Content/Images"), mainFilename);
                }
                
                firstFile.SaveAs(mainPath);

                string biDanh = sp.BiDanh;
                if (string.IsNullOrWhiteSpace(biDanh))
                {
                    biDanh = sp.TenSanPham.ToLower().Replace(" ", "-"); // Simplified for brevity in this rewrite
                    biDanh = biDanh + "-" + DateTime.Now.Ticks;
                }
                
                var sanPhamId = (int)db.GetNextSequenceValue("tb_SanPham");

                var dmObj = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(d => d.Id, sp.DanhMucId)).FirstOrDefault();
                var thObj = sp.ThuongHieuId.HasValue ? db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(t => t.Id, sp.ThuongHieuId.Value)).FirstOrDefault() : null;

                var sanPham = new tb_SanPham
                {
                    Id = sanPhamId,
                    TenSanPham = sp.TenSanPham,
                    BiDanh = biDanh,
                    MaSanPham = sp.MaSanPham,
                    MoTa = sp.MoTa,
                    ChiTiet = sp.ChiTiet,
                    Gia = sp.Gia,
                    GiaKhuyenMai = null,
                    SoLuong = sp.SoLuong,
                    LuotXem = 0,
                    HinhAnhDaiDien = mainFilename,
                    HinhAnh = mainFilename,
                    DanhMuc = dmObj != null ? new SanPhamDanhMuc { Id = dmObj.Id, TenDanhMuc = dmObj.TenDanhMuc, BiDanh = dmObj.BiDanh } : new SanPhamDanhMuc { Id = sp.DanhMucId },
                    ThuongHieu = thObj != null ? new SanPhamThuongHieu { Id = thObj.Id, TenThuongHieu = thObj.TenThuongHieu, BiDanh = thObj.BiDanh } : (sp.ThuongHieuId.HasValue ? new SanPhamThuongHieu { Id = sp.ThuongHieuId.Value } : null),
                    NgayTao = DateTime.Now,
                    CacCo = new SanPhamCacCo
                    {
                        HienThi = true,
                        LaSanPhamMoi = true
                    }
                };

                for (int i = 0; i < validFiles.Count; i++)
                {
                    var file = validFiles[i];
                    var filename = Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images"), filename);
                    
                    if (System.IO.File.Exists(path) && i > 0)
                    {
                        var extension = Path.GetExtension(filename);
                        var name = Path.GetFileNameWithoutExtension(filename);
                        filename = name + "_" + DateTime.Now.Ticks + "_" + i + extension;
                        path = Path.Combine(Server.MapPath("~/Content/Images"), filename);
                    }
                    
                    if (i > 0)
                    {
                        file.SaveAs(path);
                    }

                    sanPham.DanhSachHinhAnh.Add(new SanPhamHinhAnh
                    {
                        HinhAnh = filename,
                        LaAnhMacDinh = (i == defaultImageIndex)
                    });
                }

                Action<string, string, int> addSpec = (name, val, order) =>
                {
                    if (!string.IsNullOrEmpty(val))
                    {
                        sanPham.ThongSoKyThuat.Add(new SanPhamThongSo { TenThongSo = name, GiaTri = val, ThuTu = order });
                        sanPham.ThongSoChiTiet[name] = val;
                    }
                };

                addSpec("Bộ nhớ RAM", sp.BoNhoRAM, 1);
                addSpec("Bộ nhớ trong", sp.BoNhoTrong, 2);
                addSpec("Màn hình", sp.ManHinh, 3);
                addSpec("Dung lượng Pin", sp.DungLuongPin, 4);
                addSpec("Camera", sp.Camera, 5);
                addSpec("Hệ điều hành", sp.HeDieuHanh, 6);
                addSpec("Cổng kết nối", sp.CongKetNoi, 7);

                // Thuộc tính tùy chỉnh động NoSQL (Dynamic Attributes)
                var customNames = Request.Form.GetValues("CustomSpecName");
                var customValues = Request.Form.GetValues("CustomSpecValue");
                if (customNames != null && customValues != null)
                {
                    for (int idx = 0; idx < Math.Min(customNames.Length, customValues.Length); idx++)
                    {
                        string cName = customNames[idx]?.Trim();
                        string cVal = customValues[idx]?.Trim();
                        if (!string.IsNullOrWhiteSpace(cName) && !string.IsNullOrWhiteSpace(cVal))
                        {
                            addSpec(cName, cVal, sanPham.ThongSoKyThuat.Count + 1);
                        }
                    }
                }

                db.tb_SanPham.Collection.InsertOne(sanPham);

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("QLSP");
            }
            catch (Exception ex)
            {
                ViewBag.ThongBao = "Có lỗi xảy ra: " + ex.Message;
                ViewBag.lstDMSP = new SelectList(db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList(), "Id", "TenDanhMuc");
                ViewBag.lstTH = new SelectList(db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList(), "Id", "TenThuongHieu");
                return View("formThemSP", sp);
            }
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult formSuaSP(int id)
        {
            var sp = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (sp == null)
            {
                return HttpNotFound();
            }

            var qlsp = new QLSP
            {
                Id = sp.Id,
                TenSanPham = sp.TenSanPham,
                BiDanh = sp.BiDanh,
                MaSanPham = sp.MaSanPham,
                MoTa = sp.MoTa,
                ChiTiet = sp.ChiTiet,
                Gia = sp.Gia,
                GiaKhuyenMai = sp.GiaKhuyenMai,
                SoLuong = sp.SoLuong,
                LuotXem = sp.LuotXem,
                HinhAnh = sp.HinhAnh,
                DanhMucId = sp.DanhMucId,
                ThuongHieuId = sp.ThuongHieuId,
                
                BoNhoRAM = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 1 || (ts.TenThongSo != null && ts.TenThongSo.Contains("RAM")))?.GiaTri,
                BoNhoTrong = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 2 || (ts.TenThongSo != null && ts.TenThongSo.Contains("trong")))?.GiaTri,
                ManHinh = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 3 || (ts.TenThongSo != null && (ts.TenThongSo.Contains("hình") || ts.TenThongSo.Contains("hành"))))?.GiaTri,
                DungLuongPin = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 4 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("pin")))?.GiaTri,
                Camera = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 5 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("camera")))?.GiaTri,
                HeDieuHanh = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 6 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("hành")))?.GiaTri,
                CongKetNoi = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 7 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("kết nối")))?.GiaTri,

                CacThongSoTuyChinh = sp.ThongSoKyThuat?
                    .Where(ts => !string.IsNullOrWhiteSpace(ts.TenThongSo) && !StandardSpecKeywords.Any(k => ts.TenThongSo.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList() ?? new List<SanPhamThongSo>()
            };

            ViewBag.lstDMSP = new SelectList(db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList(), "Id", "TenDanhMuc", sp.DanhMucId);
            ViewBag.lstTH = new SelectList(db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList(), "Id", "TenThuongHieu", sp.ThuongHieuId);
            return View(qlsp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,4")]
        public ActionResult SuaSP(QLSP sp, HttpPostedFileBase fileupload, decimal? GiaMoi)
        {
            ModelState.Remove("NgayTao");
            ModelState.Remove("NgayCapNhat");
            ModelState.Remove("NguoiTao");
            ModelState.Remove("NguoiCapNhat");
            ModelState.Remove("LuotXem");
            ModelState.Remove("HienThi");
            ModelState.Remove("LaSanPhamTrangChu");
            ModelState.Remove("LaSanPhamKhuyenMai");
            ModelState.Remove("LaSanPhamNoiBat");
            ModelState.Remove("LaSanPhamHot");
            ModelState.Remove("LaSanPhamMoi");
            ModelState.Remove("LaSanPhamBanChay");
            
            if (!ModelState.IsValid)
            {
                ViewBag.ThongBao = "Vui lòng kiểm tra lại thông tin nhập vào";
                ViewBag.lstDMSP = new SelectList(db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList(), "Id", "TenDanhMuc", sp.DanhMucId);
                ViewBag.lstTH = new SelectList(db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList(), "Id", "TenThuongHieu", sp.ThuongHieuId);
                return View("formSuaSP", sp);
            }
            
            try
            {
                var sanPham = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, sp.Id)).FirstOrDefault();
                if (sanPham == null)
                {
                    return HttpNotFound();
                }

                var dmObj = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(d => d.Id, sp.DanhMucId)).FirstOrDefault();
                var thObj = sp.ThuongHieuId.HasValue ? db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(t => t.Id, sp.ThuongHieuId.Value)).FirstOrDefault() : null;

                var dmEmbed = dmObj != null ? new SanPhamDanhMuc { Id = dmObj.Id, TenDanhMuc = dmObj.TenDanhMuc, BiDanh = dmObj.BiDanh } : new SanPhamDanhMuc { Id = sp.DanhMucId };
                var thEmbed = thObj != null ? new SanPhamThuongHieu { Id = thObj.Id, TenThuongHieu = thObj.TenThuongHieu, BiDanh = thObj.BiDanh } : (sp.ThuongHieuId.HasValue ? new SanPhamThuongHieu { Id = sp.ThuongHieuId.Value } : null);

                var thongSoKyThuat = new List<SanPhamThongSo>();
                var thongSoChiTiet = new Dictionary<string, object>();
                Action<string, string, int> addSpec = (name, val, order) =>
                {
                    if (!string.IsNullOrEmpty(val))
                    {
                        thongSoKyThuat.Add(new SanPhamThongSo { TenThongSo = name, GiaTri = val, ThuTu = order });
                        thongSoChiTiet[name] = val;
                    }
                };

                addSpec("Bộ nhớ RAM", sp.BoNhoRAM, 1);
                addSpec("Bộ nhớ trong", sp.BoNhoTrong, 2);
                addSpec("Màn hình", sp.ManHinh, 3);
                addSpec("Dung lượng Pin", sp.DungLuongPin, 4);
                addSpec("Camera", sp.Camera, 5);
                addSpec("Hệ điều hành", sp.HeDieuHanh, 6);
                addSpec("Cổng kết nối", sp.CongKetNoi, 7);

                // Thuộc tính tùy chỉnh động NoSQL (Dynamic Attributes)
                var customNames = Request.Form.GetValues("CustomSpecName");
                var customValues = Request.Form.GetValues("CustomSpecValue");
                if (customNames != null && customValues != null)
                {
                    for (int idx = 0; idx < Math.Min(customNames.Length, customValues.Length); idx++)
                    {
                        string cName = customNames[idx]?.Trim();
                        string cVal = customValues[idx]?.Trim();
                        if (!string.IsNullOrWhiteSpace(cName) && !string.IsNullOrWhiteSpace(cVal))
                        {
                            addSpec(cName, cVal, thongSoKyThuat.Count + 1);
                        }
                    }
                }

                var updateBuilder = Builders<tb_SanPham>.Update
                    .Set(x => x.TenSanPham, sp.TenSanPham)
                    .Set(x => x.BiDanh, sp.BiDanh)
                    .Set(x => x.MaSanPham, sp.MaSanPham)
                    .Set(x => x.MoTa, sp.MoTa)
                    .Set(x => x.ChiTiet, sp.ChiTiet)
                    .Set(x => x.SoLuong, sp.SoLuong)
                    .Set(x => x.DanhMuc, dmEmbed)
                    .Set(x => x.ThuongHieu, thEmbed)
                    .Set(x => x.ThongSoKyThuat, thongSoKyThuat)
                    .Set(x => x.ThongSoChiTiet, thongSoChiTiet)
                    .Set(x => x.NgayCapNhat, DateTime.Now);

                if (GiaMoi.HasValue && GiaMoi.Value > 0)
                {
                    updateBuilder = updateBuilder.Set(x => x.GiaKhuyenMai, GiaMoi.Value);
                }

                if (fileupload != null && fileupload.ContentLength > 0)
                {
                    if (!string.IsNullOrEmpty(sanPham.HinhAnh))
                    {
                        var oldImagePath = Path.Combine(Server.MapPath("~/Content/Images"), sanPham.HinhAnh);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var filename = Path.GetFileName(fileupload.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images"), filename);
                    
                    if (System.IO.File.Exists(path))
                    {
                        var extension = Path.GetExtension(filename);
                        var name = Path.GetFileNameWithoutExtension(filename);
                        filename = name + "_" + DateTime.Now.Ticks + extension;
                        path = Path.Combine(Server.MapPath("~/Content/Images"), filename);
                    }
                    
                    fileupload.SaveAs(path);
                    updateBuilder = updateBuilder
                        .Set(x => x.HinhAnh, filename)
                        .Set(x => x.DanhSachHinhAnh, new List<SanPhamHinhAnh> { new SanPhamHinhAnh { HinhAnh = filename, LaAnhMacDinh = true } });
                }

                db.tb_SanPham.Collection.UpdateOne(Builders<tb_SanPham>.Filter.Eq(x => x.Id, sp.Id), updateBuilder);

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("QLSP");
            }
            catch (Exception ex)
            {
                ViewBag.ThongBao = "Có lỗi xảy ra: " + ex.Message;
                ViewBag.lstDMSP = new SelectList(db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList(), "Id", "TenDanhMuc", sp.DanhMucId);
                ViewBag.lstTH = new SelectList(db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList(), "Id", "TenThuongHieu", sp.ThuongHieuId);
                return View("formSuaSP", sp);
            }
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult XoaSP(int id)
        {
            try
            {
                var sanPham = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction("QLSP");
                }

                var isUsedInOrders = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.ElemMatch(d => d.DanhSachSanPham, p => p.SanPhamId == id)).Any();

                if (isUsedInOrders)
                {
                    db.tb_SanPham.Collection.UpdateOne(Builders<tb_SanPham>.Filter.Eq(x => x.Id, id), Builders<tb_SanPham>.Update.Set(x => x.CacCo.HienThi, false));
                    TempData["SuccessMessage"] = "Sản phẩm đã được ẩn (không thể xóa vì đang có trong đơn hàng)";
                    return RedirectToAction("QLSP");
                }

                if (sanPham.DanhSachHinhAnh != null)
                {
                    foreach (var anh in sanPham.DanhSachHinhAnh)
                    {
                        if (!string.IsNullOrEmpty(anh?.HinhAnh))
                        {
                            var imagePath = Path.Combine(Server.MapPath("~/Content/Images"), anh.HinhAnh);
                            if (System.IO.File.Exists(imagePath))
                            {
                                try { System.IO.File.Delete(imagePath); } catch { }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(sanPham.HinhAnh))
                {
                    var mainImagePath = Path.Combine(Server.MapPath("~/Content/Images"), sanPham.HinhAnh);
                    if (System.IO.File.Exists(mainImagePath))
                    {
                        try { System.IO.File.Delete(mainImagePath); } catch { }
                    }
                }

                db.tb_SanPham.Collection.DeleteOne(Builders<tb_SanPham>.Filter.Eq(x => x.Id, id));
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message + 
                    (ex.InnerException != null ? " - " + ex.InnerException.Message : "");
            }

            return RedirectToAction("QLSP");
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult TimKiemSP(string timsp, int? danhMucId, int? thuongHieuId, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_SanPham>.Filter.Empty;

            if (!String.IsNullOrEmpty(timsp))
            {
                var keyword = timsp.ToLower();
                filter &= Builders<tb_SanPham>.Filter.Regex(sp => sp.TenSanPham, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_SanPham>.Filter.Regex(sp => sp.MaSanPham, new BsonRegularExpression(keyword, "i"));
            }

            if (danhMucId.HasValue)
            {
                filter &= Builders<tb_SanPham>.Filter.Eq(sp => sp.DanhMuc.Id, danhMucId.Value);
            }

            if (thuongHieuId.HasValue)
            {
                filter &= Builders<tb_SanPham>.Filter.Eq(sp => sp.ThuongHieu.Id, thuongHieuId.Value);
            }

            var lstspRaw = db.tb_SanPham.Collection.Find(filter)
                .SortByDescending(sp => sp.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            var danhMucIds = lstspRaw.Select(s => s.DanhMucId).Distinct().ToList();
            var thuongHieuIds = lstspRaw.Where(s => s.ThuongHieuId.HasValue).Select(s => s.ThuongHieuId.Value).Distinct().ToList();
            
            var danhMucs = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.In(d => d.Id, danhMucIds)).ToList();
            var thuongHieus = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.In(t => t.Id, thuongHieuIds)).ToList();
            
            var lstsp = lstspRaw.Select(sp => new QLSP
            { 
                Id = sp.Id,
                TenSanPham = sp.TenSanPham,
                BiDanh = sp.BiDanh,
                MaSanPham = sp.MaSanPham,
                MoTa = sp.MoTa,
                ChiTiet = sp.ChiTiet,
                Gia = sp.Gia,
                GiaKhuyenMai = sp.GiaKhuyenMai,
                SoLuong = sp.SoLuong ?? 0,
                LuotXem = sp.LuotXem ?? 0,
                HinhAnh = sp.HinhAnh,

                TenDanhMuc = sp.DanhMuc?.TenDanhMuc ?? danhMucs.FirstOrDefault(d => d.Id == sp.DanhMucId)?.TenDanhMuc,
                TenThuongHieu = sp.ThuongHieu?.TenThuongHieu ?? thuongHieus.FirstOrDefault(t => t.Id == sp.ThuongHieuId)?.TenThuongHieu ?? "Không có",

                BoNhoRAM = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 1 || (ts.TenThongSo != null && ts.TenThongSo.Contains("RAM")))?.GiaTri,
                BoNhoTrong = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 2 || (ts.TenThongSo != null && ts.TenThongSo.Contains("trong")))?.GiaTri,
                ManHinh = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 3 || (ts.TenThongSo != null && (ts.TenThongSo.Contains("hình") || ts.TenThongSo.Contains("hành"))))?.GiaTri,
                DungLuongPin = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 4 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("pin")))?.GiaTri,
                Camera = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 5 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("camera")))?.GiaTri,
                HeDieuHanh = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 6 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("hành")))?.GiaTri,
                CongKetNoi = sp.ThongSoKyThuat?.FirstOrDefault(ts => ts.ThuTu == 7 || (ts.TenThongSo != null && ts.TenThongSo.ToLower().Contains("kết nối")))?.GiaTri,

                LaSanPhamTrangChu = sp.LaSanPhamTrangChu,
                LaSanPhamKhuyenMai = sp.LaSanPhamKhuyenMai,
                LaSanPhamNoiBat = sp.LaSanPhamNoiBat,
                LaSanPhamHot = sp.LaSanPhamHot,
                LaSanPhamMoi = sp.LaSanPhamMoi,
                LaSanPhamBanChay = sp.LaSanPhamBanChay,
                HienThi = sp.HienThi,
                NgayTao = sp.NgayTao,
            }).ToList();

            int totalItems = (int)db.tb_SanPham.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = timsp;
            ViewBag.SelectedDanhMuc = danhMucId;
            ViewBag.SelectedThuongHieu = thuongHieuId;
            
            ViewBag.DanhMucs = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList();
            ViewBag.ThuongHieus = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList();

            return View("QLSP", lstsp);
        }
    }
}
