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
        [AdminAuthorize(Roles = "1,3")]
        public ActionResult TinTuc(int page = 1, int pageSize = 10)
        {
            var filter = Builders<tb_TinTuc>.Filter.Empty;
            
            var tinTucList = db.tb_TinTuc.Collection.Find(filter)
                .SortByDescending(tt => tt.NgayTao)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            var totalItems = (int)db.tb_TinTuc.Collection.CountDocuments(filter);
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.TuKhoa = "";

            return View(tinTucList);
        }

        private List<tb_DanhMucTin> LayDanhSachDanhMucTin()
        {
            var distinctCategories = db.tb_TinTuc.Collection.Find(Builders<tb_TinTuc>.Filter.Ne(x => x.DanhMucTin, null))
                .ToList()
                .Where(x => x.DanhMucTin != null && x.DanhMucTin.Id > 0)
                .Select(x => x.DanhMucTin)
                .GroupBy(x => x.Id)
                .Select(g => new tb_DanhMucTin { Id = g.Key, TenDanhMuc = g.First().TenDanhMuc })
                .ToList();

            if (!distinctCategories.Any())
            {
                distinctCategories = new List<tb_DanhMucTin>
                {
                    new tb_DanhMucTin { Id = 1, TenDanhMuc = "Tin công nghệ" },
                    new tb_DanhMucTin { Id = 2, TenDanhMuc = "Đánh giá sản phẩm" },
                    new tb_DanhMucTin { Id = 3, TenDanhMuc = "Khuyến mãi" },
                    new tb_DanhMucTin { Id = 4, TenDanhMuc = "Mẹo hay" }
                };
            }
            return distinctCategories;
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult formThemTinTuc()
        {
            var model = new tb_TinTuc
            {
                HienThi = true,
                NgayTao = DateTime.Now
            };

            var danhMucList = LayDanhSachDanhMucTin();
            ViewBag.DanhMucTinList = new SelectList(danhMucList, "Id", "TenDanhMuc");
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [AdminAuthorize(Roles = "1,3")]
        public ActionResult ThemTinTuc(tb_TinTuc tinTuc, HttpPostedFileBase hinhAnhFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (hinhAnhFile != null && hinhAnhFile.ContentLength > 0)
                    {
                        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
                        var fileExtension = Path.GetExtension(hinhAnhFile.FileName).ToLower();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (PNG, JPG, JPEG, GIF)");
                            var dmList = LayDanhSachDanhMucTin();
                            ViewBag.DanhMucTinList = new SelectList(dmList, "Id", "TenDanhMuc");
                            return View("formThemTinTuc", tinTuc);
                        }

                        if (hinhAnhFile.ContentLength > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "Kích thước file không được vượt quá 5MB");
                            var dmList = LayDanhSachDanhMucTin();
                            ViewBag.DanhMucTinList = new SelectList(dmList, "Id", "TenDanhMuc");
                            return View("formThemTinTuc", tinTuc);
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
                        tinTuc.HinhAnh = fileName;
                    }
                    
                    var dm = LayDanhSachDanhMucTin().FirstOrDefault(d => d.Id == tinTuc.DanhMucTinId);
                    if (dm != null)
                    {
                        tinTuc.DanhMucTin = new TinTucDanhMuc { Id = dm.Id, TenDanhMuc = dm.TenDanhMuc };
                    }

                    tinTuc.Id = (int)db.GetNextSequenceValue("tb_TinTuc");
                    tinTuc.NgayTao = DateTime.Now;
                    db.tb_TinTuc.Collection.InsertOne(tinTuc);
                    
                    TempData["SuccessMessage"] = "Thêm tin tức thành công!";
                    return RedirectToAction("TinTuc");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }
            
            var danhMucListFallback = LayDanhSachDanhMucTin();
            ViewBag.DanhMucTinList = new SelectList(danhMucListFallback, "Id", "TenDanhMuc");
            return View("formThemTinTuc", tinTuc);
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult formSuaTinTuc(int id)
        {
            var tinTuc = db.tb_TinTuc.Collection.Find(Builders<tb_TinTuc>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            
            if (tinTuc == null)
            {
                return HttpNotFound();
            }

            var danhMucList = LayDanhSachDanhMucTin();
            ViewBag.DanhMucTinList = new SelectList(danhMucList, "Id", "TenDanhMuc", tinTuc.DanhMucTinId);
            
            return View(tinTuc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [AdminAuthorize(Roles = "1,3")]
        public ActionResult SuaTinTuc(tb_TinTuc tinTuc, HttpPostedFileBase hinhAnhFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tinTucGoc = db.tb_TinTuc.Collection.Find(Builders<tb_TinTuc>.Filter.Eq(x => x.Id, tinTuc.Id)).FirstOrDefault();
                    
                    if (tinTucGoc == null)
                    {
                        return HttpNotFound();
                    }

                    var dm = LayDanhSachDanhMucTin().FirstOrDefault(d => d.Id == tinTuc.DanhMucTinId);

                    var updateBuilder = Builders<tb_TinTuc>.Update
                        .Set(x => x.TieuDe, tinTuc.TieuDe)
                        .Set(x => x.BiDanh, tinTuc.BiDanh)
                        .Set(x => x.MoTa, tinTuc.MoTa)
                        .Set(x => x.ChiTiet, tinTuc.ChiTiet)
                        .Set(x => x.HienThi, tinTuc.HienThi)
                        .Set(x => x.NgayCapNhat, DateTime.Now)
                        .Set(x => x.NguoiCapNhat, tinTuc.NguoiCapNhat);

                    if (dm != null)
                    {
                        updateBuilder = updateBuilder.Set(x => x.DanhMucTin, new TinTucDanhMuc { Id = dm.Id, TenDanhMuc = dm.TenDanhMuc });
                    }

                    if (hinhAnhFile != null && hinhAnhFile.ContentLength > 0)
                    {
                        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
                        var fileExtension = Path.GetExtension(hinhAnhFile.FileName).ToLower();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (PNG, JPG, JPEG, GIF)");
                            var dmList = LayDanhSachDanhMucTin();
                            ViewBag.DanhMucTinList = new SelectList(dmList, "Id", "TenDanhMuc", tinTuc.DanhMucTinId);
                            return View("formSuaTinTuc", tinTuc);
                        }

                        if (hinhAnhFile.ContentLength > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "Kích thước file không được vượt quá 5MB");
                            var dmList = LayDanhSachDanhMucTin();
                            ViewBag.DanhMucTinList = new SelectList(dmList, "Id", "TenDanhMuc", tinTuc.DanhMucTinId);
                            return View("formSuaTinTuc", tinTuc);
                        }

                        if (!string.IsNullOrEmpty(tinTucGoc.HinhAnh))
                        {
                            var oldFilePath = Path.Combine(Server.MapPath("~/Content/Images"), tinTucGoc.HinhAnh);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
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
                        updateBuilder = updateBuilder.Set(x => x.HinhAnh, fileName);
                    }

                    db.tb_TinTuc.Collection.UpdateOne(Builders<tb_TinTuc>.Filter.Eq(x => x.Id, tinTuc.Id), updateBuilder);
                    
                    TempData["SuccessMessage"] = "Cập nhật tin tức thành công!";
                    return RedirectToAction("TinTuc");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }
            
            var danhMucList = LayDanhSachDanhMucTin();
            ViewBag.DanhMucTinList = new SelectList(danhMucList, "Id", "TenDanhMuc", tinTuc.DanhMucTinId);
            return View("formSuaTinTuc", tinTuc);
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult XoaTinTuc(int id)
        {
            try
            {
                var tinTuc = db.tb_TinTuc.Collection.Find(Builders<tb_TinTuc>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
                
                if (tinTuc == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tin tức!";
                    return RedirectToAction("TinTuc");
                }

                if (!string.IsNullOrEmpty(tinTuc.HinhAnh))
                {
                    var imagePath = Path.Combine(Server.MapPath("~/Content/Images"), tinTuc.HinhAnh);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                db.tb_TinTuc.Collection.DeleteOne(Builders<tb_TinTuc>.Filter.Eq(x => x.Id, id));
                TempData["SuccessMessage"] = "Xóa tin tức thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tin tức: " + ex.Message;
            }

            return RedirectToAction("TinTuc");
        }

        [AdminAuthorize(Roles = "1,3")]
        public ActionResult TimTinTuc(string timtt, int page = 1, int pageSize = 10)
        {
            var filter = Builders<tb_TinTuc>.Filter.Empty;

            if (!String.IsNullOrEmpty(timtt))
            {
                var keyword = timtt.ToLower();
                filter = Builders<tb_TinTuc>.Filter.Regex(tt => tt.TieuDe, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_TinTuc>.Filter.Regex(tt => tt.MoTa, new BsonRegularExpression(keyword, "i"));
            }

            var totalItems = (int)db.tb_TinTuc.Collection.CountDocuments(filter);
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.TuKhoa = timtt;

            var tinTucList = db.tb_TinTuc.Collection.Find(filter)
                .SortByDescending(tt => tt.NgayTao)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            return View("TinTuc", tinTucList);
        }
    }
}
