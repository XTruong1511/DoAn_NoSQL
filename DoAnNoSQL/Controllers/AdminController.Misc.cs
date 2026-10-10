using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        // ==================== QUẢN LÝ BANNER ====================
        [AdminAuthorize(Roles = "1")]
        public ActionResult Banner(int page = 1, int pageSize = 10)
        {
            var filter = Builders<tb_QuangCao>.Filter.Empty;
            int totalItems = (int)db.tb_QuangCao.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.TuKhoa = "";

            var bannerList = db.tb_QuangCao.Collection.Find(filter)
                .SortBy(b => b.ThuTu).SortByDescending(b => b.NgayTao)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList()
                .Select(b => new BannerViewModel
                {
                    Id = b.Id,
                    TieuDe = b.TieuDe,
                    MoTa = b.MoTa,
                    HinhAnh = b.HinhAnh,
                    Link = b.LienKet,
                    ThuTu = b.ThuTu ?? 0,
                    ViTri = b.ViTri != null ? b.ViTri.ToString() : "",
                    HienThi = b.HienThi ?? false,
                    NgayTao = b.NgayTao,
                    NgayCapNhat = b.NgayCapNhat
                }).ToList();

            return View(bannerList);
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult formThemBanner()
        {
            var model = new tb_QuangCao { HienThi = true, ThuTu = 0, NgayTao = DateTime.Now };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1")]
        public ActionResult ThemBanner(tb_QuangCao banner, HttpPostedFileBase hinhAnhFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (hinhAnhFile != null && hinhAnhFile.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(hinhAnhFile.FileName);
                        var filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        if (System.IO.File.Exists(filePath))
                        {
                            var extension = Path.GetExtension(fileName);
                            var name = Path.GetFileNameWithoutExtension(fileName);
                            fileName = name + "_" + DateTime.Now.Ticks + extension;
                            filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        }
                        hinhAnhFile.SaveAs(filePath);
                        banner.HinhAnh = fileName;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Vui lòng chọn hình ảnh banner");
                        return View("formThemBanner", banner);
                    }

                    banner.Id = (int)db.GetNextSequenceValue("tb_QuangCao");
                    banner.NgayTao = DateTime.Now;
                    banner.HienThi = true;
                    db.tb_QuangCao.Collection.InsertOne(banner);

                    TempData["SuccessMessage"] = "Thêm banner thành công!";
                    return RedirectToAction("Banner");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }
            return View("formThemBanner", banner);
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult formSuaBanner(int id)
        {
            var banner = db.tb_QuangCao.Collection.Find(Builders<tb_QuangCao>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (banner == null) return HttpNotFound();
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1")]
        public ActionResult SuaBanner(tb_QuangCao bannerModel, HttpPostedFileBase hinhAnhFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var bannerGoc = db.tb_QuangCao.Collection.Find(Builders<tb_QuangCao>.Filter.Eq(x => x.Id, bannerModel.Id)).FirstOrDefault();
                    if (bannerGoc == null) return HttpNotFound();

                    var update = Builders<tb_QuangCao>.Update
                        .Set(x => x.TieuDe, bannerModel.TieuDe)
                        .Set(x => x.ThuTu, bannerModel.ThuTu)
                        .Set(x => x.HienThi, bannerModel.HienThi)
                        .Set(x => x.NgayCapNhat, DateTime.Now);

                    if (!string.IsNullOrEmpty(bannerModel.LienKet))
                    {
                        update = update.Set(x => x.LienKet, bannerModel.LienKet);
                    }

                    if (hinhAnhFile != null && hinhAnhFile.ContentLength > 0)
                    {
                        if (!string.IsNullOrEmpty(bannerGoc.HinhAnh))
                        {
                            var oldFilePath = Path.Combine(Server.MapPath("~/Content/Images"), bannerGoc.HinhAnh);
                            if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                        }

                        var fileName = Path.GetFileName(hinhAnhFile.FileName);
                        var filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        if (System.IO.File.Exists(filePath))
                        {
                            var extension = Path.GetExtension(fileName);
                            var name = Path.GetFileNameWithoutExtension(fileName);
                            fileName = name + "_" + DateTime.Now.Ticks + extension;
                            filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        }
                        hinhAnhFile.SaveAs(filePath);
                        update = update.Set(x => x.HinhAnh, fileName);
                    }

                    db.tb_QuangCao.Collection.UpdateOne(Builders<tb_QuangCao>.Filter.Eq(x => x.Id, bannerModel.Id), update);
                    TempData["SuccessMessage"] = "Cập nhật banner thành công!";
                    return RedirectToAction("Banner");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }
            return View("formSuaBanner", bannerModel);
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult XoaBanner(int id)
        {
            try
            {
                var banner = db.tb_QuangCao.Collection.Find(Builders<tb_QuangCao>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                if (banner == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy banner!";
                    return RedirectToAction("Banner");
                }

                if (!string.IsNullOrEmpty(banner.HinhAnh))
                {
                    var imagePath = Path.Combine(Server.MapPath("~/Content/Images"), banner.HinhAnh);
                    if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
                }

                db.tb_QuangCao.Collection.DeleteOne(Builders<tb_QuangCao>.Filter.Eq(x => x.Id, id));
                TempData["SuccessMessage"] = "Xóa banner thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa banner: " + ex.Message;
            }
            return RedirectToAction("Banner");
        }

        [AdminAuthorize(Roles = "1")]
        public ActionResult TimBanner(string timbn, int page = 1, int pageSize = 10)
        {
            var filter = Builders<tb_QuangCao>.Filter.Empty;
            if (!String.IsNullOrEmpty(timbn))
            {
                var keyword = timbn.ToLower();
                filter = Builders<tb_QuangCao>.Filter.Regex(b => b.TieuDe, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_QuangCao>.Filter.Regex(b => b.LienKet, new BsonRegularExpression(keyword, "i"));
            }

            int totalItems = (int)db.tb_QuangCao.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.TuKhoa = timbn;

            var bannerList = db.tb_QuangCao.Collection.Find(filter)
                .SortBy(b => b.ThuTu).SortByDescending(b => b.NgayTao)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList()
                .Select(b => new BannerViewModel
                {
                    Id = b.Id,
                    TieuDe = b.TieuDe,
                    MoTa = b.MoTa,
                    HinhAnh = b.HinhAnh,
                    Link = b.LienKet,
                    ThuTu = b.ThuTu ?? 0,
                    ViTri = b.ViTri != null ? b.ViTri.ToString() : "",
                    HienThi = b.HienThi ?? false,
                    NgayTao = b.NgayTao,
                    NgayCapNhat = b.NgayCapNhat
                }).ToList();

            return View("Banner", bannerList);
        }

        // ==================== LIÊN HỆ ====================
        [AdminAuthorize(Roles = "1")]
        public ActionResult HoTroKH(int page = 1, int pageSize = 10)
        {
            var filter = Builders<tb_LienHe>.Filter.Empty;
            int totalItems = (int)db.tb_LienHe.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            var lienHeList = db.tb_LienHe.Collection.Find(filter)
                .SortByDescending(lh => lh.NgayTao)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            return View(lienHeList);
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1")]
        public JsonResult DanhDauDaDoc(int id)
        {
            try
            {
                db.tb_LienHe.Collection.UpdateOne(Builders<tb_LienHe>.Filter.Eq(x => x.Id, id), Builders<tb_LienHe>.Update.Set(x => x.DaDoc, true));
                return Json(new { success = true, message = "Đã đánh dấu đã đọc" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [AdminAuthorize(Roles = "1")]
        public JsonResult XoaLienHe(int id)
        {
            try
            {
                db.tb_LienHe.Collection.DeleteOne(Builders<tb_LienHe>.Filter.Eq(x => x.Id, id));
                return Json(new { success = true, message = "Đã xóa liên hệ" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ==================== BÁO CÁO & CẤU HÌNH ====================
        [AdminAuthorize(Roles = "1")]
        public ActionResult BaoCaoThongKe(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue) fromDate = DateTime.Now.AddDays(-30);
            if (!toDate.HasValue) toDate = DateTime.Now;

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            {
                var temp = fromDate;
                fromDate = toDate;
                toDate = temp;
            }

            var ordersInPeriod = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Gte(d => d.NgayTao, fromDate) & Builders<tb_DonHang>.Filter.Lte(d => d.NgayTao, toDate)).ToList();

            var revenueByMonth = ordersInPeriod
                .Where(d => d.TrangThai == 5 && d.NgayTao.HasValue)
                .GroupBy(d => new { d.NgayTao.Value.Year, d.NgayTao.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(d => d.TongTien)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var topProductsRaw = ordersInPeriod
                .SelectMany(d => d.DanhSachSanPham ?? Enumerable.Empty<DonHangSanPham>())
                .GroupBy(ct => ct.SanPhamId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(ct => ct.SoLuong),
                    ProductName = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.TenSanPham))?.TenSanPham
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToList();

            var topProductIds = topProductsRaw.Select(x => x.ProductId).ToList();
            var sanPhams = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.In(x => x.Id, topProductIds)).ToList();
            
            var topProducts = topProductsRaw.Select(g => new
            {
                ProductId = g.ProductId,
                TotalQuantity = g.TotalQuantity,
                ProductName = sanPhams.FirstOrDefault(s => s.Id == g.ProductId)?.TenSanPham ?? g.ProductName ?? ("Sản phẩm #" + g.ProductId)
            }).ToList();

            // ordersInPeriod was already fetched above
            var totalRevenue = ordersInPeriod.Where(d => d.TrangThai == 5).Sum(d => (decimal?)(d.TongTien)) ?? 0m;
            var totalOrders = ordersInPeriod.Count;
            var completedOrders = ordersInPeriod.Count(d => d.TrangThai == 5);
            var cancelledOrders = ordersInPeriod.Count(d => d.TrangThai == -1);
            var returnedOrders = ordersInPeriod.Count(d => d.TrangThai == 7);
            var cancelRate = totalOrders > 0 ? (double)cancelledOrders / totalOrders * 100 : 0;
            var completeRate = totalOrders > 0 ? (double)completedOrders / totalOrders * 100 : 0;
            var returnRate = totalOrders > 0 ? (double)returnedOrders / totalOrders * 100 : 0;

            var topStockProduct = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Empty)
                .SortByDescending(s => s.SoLuong)
                .Limit(1)
                .ToList()
                .Select(s => new TopStockProductViewModel
                {
                    TenSanPham = s.TenSanPham,
                    SoLuong = s.SoLuong
                })
                .FirstOrDefault();

            var lowStockCount = db.tb_SanPham.Collection.CountDocuments(Builders<tb_SanPham>.Filter.Lt(s => s.SoLuong, 10));

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.RevenueByMonth = revenueByMonth;
            ViewBag.TopProducts = topProducts;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.CancelledOrders = cancelledOrders;
            ViewBag.ReturnedOrders = returnedOrders;
            ViewBag.CancelRate = cancelRate;
            ViewBag.CompleteRate = completeRate;
            ViewBag.ReturnRate = returnRate;
            ViewBag.TopStockProduct = topStockProduct;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.NewCustomers = 0;

            return View();
        }

        private static readonly string[] Database14Collections = new string[]
        {
            "SanPham",
            "DanhMuc",
            "ThuongHieu",
            "DonHang",
            "TaiKhoan",
            "BaoHanh",
            "TinTuc",
            "QuangCao",
            "Menu",
            "LienHe",
            "MaGiamGia",
            "DanhGia",
            "DangKyNhanTin",
            "CauHinh"
        };

        [AdminAuthorize(Roles = "1")]
        public ActionResult CauHinh()
        {
            var model = new Dictionary<string, string>();
            string[] keys = { "TenCuaHang", "Hotline", "EmailLienHe", "DiaChiCuaHang", "LogoCuaHang" };

            var cauHinhList = db.tb_CauHinh.Collection.Find(Builders<tb_CauHinh>.Filter.Empty).ToList();

            foreach (var key in keys)
            {
                string val = "";
                var ch = cauHinhList.FirstOrDefault(c => string.Equals(c.MaCauHinh, key, StringComparison.OrdinalIgnoreCase));
                if (ch == null && key == "LogoCuaHang")
                    ch = cauHinhList.FirstOrDefault(c => string.Equals(c.MaCauHinh, "Logo", StringComparison.OrdinalIgnoreCase));
                if (ch == null && key == "TenCuaHang")
                    ch = cauHinhList.FirstOrDefault(c => string.Equals(c.MaCauHinh, "TieuDeWeb", StringComparison.OrdinalIgnoreCase));

                if (ch != null && !string.IsNullOrEmpty(ch.GiaTri))
                {
                    val = ch.GiaTri;
                }

                model[key] = val ?? "";
            }

            // Collection statistics for UI display
            var stats = new Dictionary<string, long>();
            foreach (var colName in Database14Collections)
            {
                try
                {
                    stats[colName] = db.MongoDatabase.GetCollection<BsonDocument>(colName).CountDocuments(FilterDefinition<BsonDocument>.Empty);
                }
                catch
                {
                    stats[colName] = 0;
                }
            }
            ViewBag.CollectionStats = stats;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [AdminAuthorize(Roles = "1")]
        public ActionResult LuuCauHinh(FormCollection form, HttpPostedFileBase LogoCuaHang)
        {
            try
            {
                string[] keys = { "TenCuaHang", "Hotline", "EmailLienHe", "DiaChiCuaHang" };
                foreach (var key in keys)
                {
                    string value = form[key] ?? "";

                    // Save to tb_CauHinh (14 collections)
                    var filter = Builders<tb_CauHinh>.Filter.Eq(x => x.MaCauHinh, key);
                    var existing = db.tb_CauHinh.Collection.Find(filter).FirstOrDefault();
                    if (existing != null)
                    {
                        db.tb_CauHinh.Collection.UpdateOne(filter, Builders<tb_CauHinh>.Update.Set(x => x.GiaTri, value).Set(x => x.NgayCapNhat, DateTime.Now));
                    }
                    else
                    {
                        db.tb_CauHinh.Collection.InsertOne(new tb_CauHinh { MaCauHinh = key, GiaTri = value, NgayCapNhat = DateTime.Now });
                    }

                    if (key == "TenCuaHang")
                    {
                        var fTieuDe = Builders<tb_CauHinh>.Filter.Eq(x => x.MaCauHinh, "TieuDeWeb");
                        if (db.tb_CauHinh.Collection.CountDocuments(fTieuDe) > 0)
                        {
                            db.tb_CauHinh.Collection.UpdateOne(fTieuDe, Builders<tb_CauHinh>.Update.Set(x => x.GiaTri, value).Set(x => x.NgayCapNhat, DateTime.Now));
                        }
                    }

                }

                if (LogoCuaHang != null && LogoCuaHang.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(LogoCuaHang.FileName);
                    var filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        var extension = Path.GetExtension(fileName);
                        var name = Path.GetFileNameWithoutExtension(fileName);
                        fileName = name + "_" + DateTime.Now.Ticks + extension;
                        filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                    }
                    LogoCuaHang.SaveAs(filePath);

                    // Update tb_CauHinh
                    foreach (var lKey in new[] { "LogoCuaHang", "Logo" })
                    {
                        var lFilter = Builders<tb_CauHinh>.Filter.Eq(x => x.MaCauHinh, lKey);
                        if (db.tb_CauHinh.Collection.CountDocuments(lFilter) > 0)
                        {
                            db.tb_CauHinh.Collection.UpdateOne(lFilter, Builders<tb_CauHinh>.Update.Set(x => x.GiaTri, fileName).Set(x => x.NgayCapNhat, DateTime.Now));
                        }
                        else
                        {
                            db.tb_CauHinh.Collection.InsertOne(new tb_CauHinh { MaCauHinh = lKey, GiaTri = fileName, NgayCapNhat = DateTime.Now });
                        }
                    }
                }

                DoAnNoSQL.Helpers.SettingsHelper.ClearCache();
                TempData["Success"] = "Lưu cấu hình hệ thống thành công!";
                TempData["SuccessMessage"] = "Lưu cấu hình hệ thống thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }
            return RedirectToAction("CauHinh");
        }

        // ==================== SAO LƯU & PHỤC HỒI DATABASE (14 COLLECTIONS) ====================

        [HttpGet]
        [AdminAuthorize(Roles = "1")]
        public ActionResult ExportDatabase(string type = "zip")
        {
            try
            {
                var collections = new List<string>(Database14Collections);
                var counterCol = db.MongoDatabase.GetCollection<BsonDocument>("Counters");
                if (counterCol.CountDocuments(FilterDefinition<BsonDocument>.Empty) > 0)
                {
                    collections.Add("Counters");
                }

                var jsonWriterSettings = new MongoDB.Bson.IO.JsonWriterSettings { Indent = true };

                if (string.Equals(type, "json", StringComparison.OrdinalIgnoreCase))
                {
                    var rootDoc = new BsonDocument();
                    foreach (var colName in collections)
                    {
                        var docs = db.MongoDatabase.GetCollection<BsonDocument>(colName)
                            .Find(FilterDefinition<BsonDocument>.Empty).ToList();
                        rootDoc.Add(colName, new BsonArray(docs));
                    }

                    var jsonString = rootDoc.ToJson(jsonWriterSettings);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                    string fileName = $"Database_14Collections_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    return File(bytes, "application/json", fileName);
                }
                else
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                        {
                            foreach (var colName in collections)
                            {
                                var docs = db.MongoDatabase.GetCollection<BsonDocument>(colName)
                                    .Find(FilterDefinition<BsonDocument>.Empty).ToList();
                                var jsonString = new BsonArray(docs).ToJson(jsonWriterSettings);

                                var entry = archive.CreateEntry(colName + ".json", CompressionLevel.Optimal);
                                using (var entryStream = entry.Open())
                                using (var writer = new StreamWriter(entryStream, System.Text.Encoding.UTF8))
                                {
                                    writer.Write(jsonString);
                                }
                            }
                        }
                        memoryStream.Position = 0;
                        string fileName = $"Database_14Collections_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                        return File(memoryStream.ToArray(), "application/zip", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Xuất dữ liệu thất bại: " + ex.Message;
                return RedirectToAction("CauHinh");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1")]
        public ActionResult RestoreDatabase(HttpPostedFileBase backupFile, bool restoreFromDefault = false)
        {
            try
            {
                int totalImportedCollections = 0;
                int totalImportedDocs = 0;

                if (restoreFromDefault)
                {
                    string backupDir = Server.MapPath("~/Database_backup");
                    if (!Directory.Exists(backupDir))
                    {
                        backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database_backup");
                    }

                    if (!Directory.Exists(backupDir))
                    {
                        TempData["Error"] = "Không tìm thấy thư mục Database_backup trên server!";
                        return RedirectToAction("CauHinh");
                    }

                    foreach (var colName in Database14Collections)
                    {
                        string filePath = Path.Combine(backupDir, colName + ".json");
                        if (!System.IO.File.Exists(filePath))
                        {
                            filePath = Path.Combine(backupDir, "tb_" + colName + ".json");
                        }

                        if (System.IO.File.Exists(filePath))
                        {
                            string jsonContent = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                            int count = ImportCollectionFromJson(colName, jsonContent);
                            totalImportedCollections++;
                            totalImportedDocs += count;
                        }
                    }
                }
                else
                {
                    if (backupFile == null || backupFile.ContentLength == 0)
                    {
                        TempData["Error"] = "Vui lòng chọn file sao lưu (.zip hoặc .json) để khôi phục!";
                        return RedirectToAction("CauHinh");
                    }

                    string fileExt = Path.GetExtension(backupFile.FileName).ToLower();

                    if (fileExt == ".zip")
                    {
                        using (var archive = new ZipArchive(backupFile.InputStream, ZipArchiveMode.Read))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                                {
                                    string colName = Path.GetFileNameWithoutExtension(entry.Name);
                                    if (colName.StartsWith("tb_")) colName = colName.Substring(3);

                                    using (var stream = entry.Open())
                                    using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                                    {
                                        string jsonContent = reader.ReadToEnd();
                                        int count = ImportCollectionFromJson(colName, jsonContent);
                                        totalImportedCollections++;
                                        totalImportedDocs += count;
                                    }
                                }
                            }
                        }
                    }
                    else if (fileExt == ".json")
                    {
                        using (var reader = new StreamReader(backupFile.InputStream, System.Text.Encoding.UTF8))
                        {
                            string jsonContent = reader.ReadToEnd();
                            string colName = Path.GetFileNameWithoutExtension(backupFile.FileName);
                            if (colName.StartsWith("tb_")) colName = colName.Substring(3);

                            int collectionsCount;
                            int count = ImportCombinedOrSingleJson(colName, jsonContent, out collectionsCount);
                            totalImportedCollections += collectionsCount;
                            totalImportedDocs += count;
                        }
                    }
                    else
                    {
                        TempData["Error"] = "Định dạng file không được hỗ trợ. Vui lòng tải lên file .zip hoặc .json!";
                        return RedirectToAction("CauHinh");
                    }
                }

                // Re-sync all counters in MongoDB
                SyncAllCounters();

                TempData["Success"] = $"Khôi phục cơ sở dữ liệu thành công! Đã khôi phục {totalImportedCollections} collections với tổng cộng {totalImportedDocs} bản ghi.";
                TempData["SuccessMessage"] = TempData["Success"];
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi trong quá trình khôi phục: " + ex.Message;
                TempData["ErrorMessage"] = TempData["Error"];
            }

            return RedirectToAction("CauHinh");
        }

        private int ImportCollectionFromJson(string colName, string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return 0;

            var docs = new List<BsonDocument>();
            var trimmed = jsonContent.Trim();

            if (trimmed.StartsWith("["))
            {
                var bsonArray = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(trimmed);
                foreach (var item in bsonArray)
                {
                    if (item.IsBsonDocument)
                    {
                        docs.Add(item.AsBsonDocument);
                    }
                }
            }
            else if (trimmed.StartsWith("{"))
            {
                var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(trimmed);
                docs.Add(doc);
            }

            var targetCol = db.MongoDatabase.GetCollection<BsonDocument>(colName);
            targetCol.DeleteMany(FilterDefinition<BsonDocument>.Empty);
            if (docs.Count > 0)
            {
                targetCol.InsertMany(docs);
            }

            return docs.Count;
        }

        private int ImportCombinedOrSingleJson(string defaultColName, string jsonContent, out int collectionsCount)
        {
            collectionsCount = 0;
            if (string.IsNullOrWhiteSpace(jsonContent)) return 0;

            var trimmed = jsonContent.Trim();
            if (trimmed.StartsWith("{"))
            {
                var rootDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(trimmed);
                bool isMultiCollection = false;
                int totalDocs = 0;

                foreach (var elem in rootDoc.Elements)
                {
                    string cName = elem.Name;
                    if (cName.StartsWith("tb_")) cName = cName.Substring(3);

                    if (Database14Collections.Contains(cName, StringComparer.OrdinalIgnoreCase) && elem.Value.IsBsonArray)
                    {
                        isMultiCollection = true;
                        var subDocs = elem.Value.AsBsonArray.Where(x => x.IsBsonDocument).Select(x => x.AsBsonDocument).ToList();
                        var targetCol = db.MongoDatabase.GetCollection<BsonDocument>(cName);
                        targetCol.DeleteMany(FilterDefinition<BsonDocument>.Empty);
                        if (subDocs.Count > 0)
                        {
                            targetCol.InsertMany(subDocs);
                        }
                        totalDocs += subDocs.Count;
                        collectionsCount++;
                    }
                }

                if (isMultiCollection)
                {
                    return totalDocs;
                }
            }

            string matchedCol = Database14Collections.FirstOrDefault(c => string.Equals(c, defaultColName, StringComparison.OrdinalIgnoreCase)) ?? defaultColName;
            collectionsCount = 1;
            return ImportCollectionFromJson(matchedCol, jsonContent);
        }

        private void SyncAllCounters()
        {
            try
            {
                var counters = db.MongoDatabase.GetCollection<BsonDocument>("Counters");
                foreach (var colName in Database14Collections)
                {
                    var targetCollection = db.MongoDatabase.GetCollection<BsonDocument>(colName);
                    int maxId = 0;

                    var maxDoc = targetCollection.Find(FilterDefinition<BsonDocument>.Empty)
                        .Sort(Builders<BsonDocument>.Sort.Descending("SqlId"))
                        .Limit(1)
                        .FirstOrDefault();

                    if (maxDoc != null && maxDoc.Contains("SqlId") && maxDoc["SqlId"].IsInt32)
                    {
                        maxId = maxDoc["SqlId"].AsInt32;
                    }
                    else
                    {
                        var maxDocId = targetCollection.Find(FilterDefinition<BsonDocument>.Empty)
                            .Sort(Builders<BsonDocument>.Sort.Descending("_id"))
                            .Limit(1)
                            .FirstOrDefault();
                        if (maxDocId != null && maxDocId.Contains("_id") && maxDocId["_id"].IsInt32)
                        {
                            maxId = maxDocId["_id"].AsInt32;
                        }
                    }

                    string[] seqNames = new string[] { colName, "tb_" + colName };
                    foreach (var seqName in seqNames)
                    {
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", seqName);
                        var update = Builders<BsonDocument>.Update.Set("seq", maxId);
                        counters.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
                    }
                }
            }
            catch
            {
                // Silently ignore counter sync errors if any
            }
        }

        // ==================== EXPORT DATA ====================
        [AdminAuthorize(Roles = "1")]
        public JsonResult GetProductsForExport()
        {
            try
            {
                var sanPhams = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Empty).SortByDescending(sp => sp.Id).ToList();
                var danhMucs = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty).ToList().ToDictionary(x => x.Id, x => x.TenDanhMuc);
                var thuongHieus = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty).ToList().ToDictionary(x => x.Id, x => x.TenThuongHieu);
                
                var data = sanPhams.Select(sp => new {
                    ID = sp.Id,
                    HinhAnh = sp.HinhAnh,
                    TenSanPham = sp.TenSanPham,
                    MaSanPham = sp.MaSanPham,
                    Gia = sp.Gia,
                    GiaKhuyenMai = sp.GiaKhuyenMai,
                    SoLuong = sp.SoLuong ?? 0,
                    LuotXem = sp.LuotXem ?? 0,
                    DanhMuc = danhMucs.ContainsKey(sp.DanhMucId) ? danhMucs[sp.DanhMucId] : "",
                    ThuongHieu = sp.ThuongHieuId.HasValue && thuongHieus.ContainsKey(sp.ThuongHieuId.Value) ? thuongHieus[sp.ThuongHieuId.Value] : "",
                    HienThi = sp.HienThi,
                    NgayTao = sp.NgayTao
                }).ToList();
                
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AdminAuthorize(Roles = "1")]
        public JsonResult GetOrdersForExport()
        {
            try
            {
                var donHangs = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Empty).SortByDescending(dh => dh.NgayTao).ToList();
                var maGiamGias = db.tb_MaGiamGia.Collection.Find(Builders<tb_MaGiamGia>.Filter.Empty).ToList().ToDictionary(x => x.Id, x => x.MaCode);

                var data = donHangs.Select(dh => new {
                    ID = dh.Id,
                    MaDonHang = dh.MaDonHang,
                    TenKhachHang = dh.TenKhachHang,
                    SoDienThoai = dh.SoDienThoai,
                    Email = dh.Email,
                    DiaChi = dh.DiaChi,
                    TongTien = dh.TongTien,
                    SoLuongSanPham = dh.SoLuongSanPham,
                    PhuongThucThanhToan = dh.PhuongThucThanhToan?.TenPhuongThuc ?? LayTenPhuongThucThanhToan(dh.PhuongThucThanhToanId),
                    PhuongThucVanChuyen = dh.PhuongThucVanChuyen?.TenPhuongThuc ?? LayTenPhuongThucVanChuyen(dh.PhuongThucVanChuyenId),
                    MaGiamGia = !string.IsNullOrEmpty(dh.MaGiamGia) ? dh.MaGiamGia : (dh.MaGiamGiaId.HasValue && maGiamGias.ContainsKey(dh.MaGiamGiaId.Value) ? maGiamGias[dh.MaGiamGiaId.Value] : ""),
                    TrangThai = dh.TrangThai,
                    NgayTao = dh.NgayTao,
                    NgayCapNhat = dh.NgayCapNhat
                }).ToList();
                
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AdminAuthorize(Roles = "1")]
        public JsonResult GetCustomersForExport()
        {
            try
            {
                var data = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(kh => kh.LaQuanTri, false))
                    .SortByDescending(kh => kh.Id)
                    .ToList()
                    .Select(kh => new {
                        ID = kh.Id,
                        HoTen = kh.HoTen,
                        TenDangNhap = kh.TenDangNhap,
                        Email = kh.Email,
                        SoDienThoai = kh.SoDienThoai,
                        HoatDong = kh.HoatDong
                    }).ToList();
                
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AdminAuthorize(Roles = "1")]
        public JsonResult GetAdminsForExport()
        {
            try
            {
                var roles = LayDanhSachQuyen().ToDictionary(r => r.Id, r => r.TenQuyen);

                var data = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(qtv => qtv.LaQuanTri, true))
                    .SortByDescending(qtv => qtv.Id)
                    .ToList()
                    .Select(qtv => new {
                        ID = qtv.Id,
                        HoTen = qtv.HoTen,
                        TenDangNhap = qtv.TenDangNhap,
                        Email = qtv.Email,
                        SoDienThoai = qtv.SoDienThoai,
                        QuyenId = qtv.QuyenId,
                        TenQuyen = qtv.Quyen?.TenQuyen ?? (qtv.QuyenId.HasValue && roles.ContainsKey(qtv.QuyenId.Value) ? roles[qtv.QuyenId.Value] : ""),
                        HoatDong = qtv.HoatDong
                    }).ToList();
                
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AdminAuthorize(Roles = "1")]
        public JsonResult GetReportForExport(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                if (!fromDate.HasValue) fromDate = DateTime.Now.AddDays(-30);
                if (!toDate.HasValue) toDate = DateTime.Now;

                if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
                {
                    var temp = fromDate;
                    fromDate = toDate;
                    toDate = temp;
                }

                var donHangs = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Gte(d => d.NgayTao, fromDate) & Builders<tb_DonHang>.Filter.Lte(d => d.NgayTao, toDate)).ToList();

                var orders = donHangs.Select(dh => new {
                    ID = dh.Id,
                    MaDonHang = dh.MaDonHang,
                    TenKhachHang = dh.TenKhachHang,
                    SoDienThoai = dh.SoDienThoai,
                    Email = dh.Email,
                    DiaChi = dh.DiaChi,
                    TongTien = dh.TongTien,
                    SoLuongSanPham = dh.SoLuongSanPham,
                    PhuongThucThanhToan = dh.PhuongThucThanhToan?.TenPhuongThuc ?? LayTenPhuongThucThanhToan(dh.PhuongThucThanhToanId),
                    TrangThai = dh.TrangThai,
                    NgayTao = dh.NgayTao,
                    NgayCapNhat = dh.NgayCapNhat
                }).ToList();
                
                return Json(orders, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
