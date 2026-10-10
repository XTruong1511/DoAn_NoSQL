using System;
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
        [AdminAuthorize(Roles = "1,4")]
        public ActionResult ThuongHieu(int page = 1)
        {
            int pageSize = 10;
            var lstTH = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Empty)
                .SortByDescending(th => th.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            int totalItems = (int)db.tb_ThuongHieu.Collection.CountDocuments(Builders<tb_ThuongHieu>.Filter.Empty);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = "";
            return View(lstTH);
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult formThemTH()
        {
            var model = new tb_ThuongHieu
            {
                HienThi = true
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,4")]
        public ActionResult ThemTH(tb_ThuongHieu th, HttpPostedFileBase logoFile)
        {
            if (ModelState.IsValid)
            {
                if (logoFile != null && logoFile.ContentLength > 0)
                {
                    try
                    {
                        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
                        var fileExtension = Path.GetExtension(logoFile.FileName).ToLower();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (PNG, JPG, JPEG, GIF, SVG)");
                            return View("formThemTH", th);
                        }

                        if (logoFile.ContentLength > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "Kích thước file không được vượt quá 5MB");
                            return View("formThemTH", th);
                        }

                        var fileName = Path.GetFileName(logoFile.FileName);
                        var filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            var extension = Path.GetExtension(fileName);
                            var name = Path.GetFileNameWithoutExtension(fileName);
                            fileName = name + "_" + DateTime.Now.Ticks + extension;
                            filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        }
                        
                        logoFile.SaveAs(filePath);
                        th.HinhAnh = fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi khi upload logo: " + ex.Message);
                        return View("formThemTH", th);
                    }
                }
                
                th.Id = (int)db.GetNextSequenceValue("tb_ThuongHieu");
                th.NgayTao = DateTime.Now;
                db.tb_ThuongHieu.Collection.InsertOne(th);
                
                TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                return RedirectToAction("ThuongHieu");
            }
            return View("formThemTH", th);
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult formSuaTH(int id)
        {
            var th = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (th == null)
            {
                return HttpNotFound();
            }
            return View(th);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,4")]
        public ActionResult SuaTH(tb_ThuongHieu th, HttpPostedFileBase logoFile)
        {
            if (ModelState.IsValid)
            {
                var thGoc = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(x => x.Id, th.Id)).FirstOrDefault();
                if (thGoc == null)
                {
                    return HttpNotFound();
                }

                var updateBuilder = Builders<tb_ThuongHieu>.Update
                    .Set(x => x.TenThuongHieu, th.TenThuongHieu)
                    .Set(x => x.BiDanh, th.BiDanh)
                    .Set(x => x.HienThi, th.HienThi);

                if (logoFile != null && logoFile.ContentLength > 0)
                {
                    try
                    {
                        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
                        var fileExtension = Path.GetExtension(logoFile.FileName).ToLower();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (PNG, JPG, JPEG, GIF, SVG)");
                            return View("formSuaTH", th);
                        }

                        if (logoFile.ContentLength > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "Kích thước file không được vượt quá 5MB");
                            return View("formSuaTH", th);
                        }

                        if (!string.IsNullOrEmpty(thGoc.HinhAnh))
                        {
                            var oldFilePath = Path.Combine(Server.MapPath("~/Content/Images"), thGoc.HinhAnh);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        var fileName = Path.GetFileName(logoFile.FileName);
                        var filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            var extension = Path.GetExtension(fileName);
                            var name = Path.GetFileNameWithoutExtension(fileName);
                            fileName = name + "_" + DateTime.Now.Ticks + extension;
                            filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        }
                        
                        logoFile.SaveAs(filePath);
                        updateBuilder = updateBuilder.Set(x => x.HinhAnh, fileName);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi khi upload logo: " + ex.Message);
                        return View("formSuaTH", th);
                    }
                }

                db.tb_ThuongHieu.Collection.UpdateOne(Builders<tb_ThuongHieu>.Filter.Eq(x => x.Id, th.Id), updateBuilder);
                
                TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công!";
                return RedirectToAction("ThuongHieu");
            }

            return View("formSuaTH", th);
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult XoaTH(int id)
        {
            var th = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (th != null)
            {
                db.tb_ThuongHieu.Collection.DeleteOne(Builders<tb_ThuongHieu>.Filter.Eq(x => x.Id, id));
            }
            return RedirectToAction("ThuongHieu");
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult TimThuongHieu(string timth, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_ThuongHieu>.Filter.Empty;

            if (!String.IsNullOrEmpty(timth))
            {
                var keyword = timth.ToLower();
                filter = Builders<tb_ThuongHieu>.Filter.Regex(th => th.TenThuongHieu, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_ThuongHieu>.Filter.Regex(th => th.BiDanh, new BsonRegularExpression(keyword, "i"));
            }

            int totalItems = (int)db.tb_ThuongHieu.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var lstTH = db.tb_ThuongHieu.Collection.Find(filter)
                .SortByDescending(th => th.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = timth;
            return View("ThuongHieu", lstTH);
        }
    }
}
