using System;
using System.IO;
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
        public ActionResult DanhMuc(int page = 1)
        {
            int pageSize = 10;
            var lstDM = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Empty)
                .SortByDescending(dm => dm.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            int totalItems = (int)db.tb_DanhMuc.Collection.CountDocuments(Builders<tb_DanhMuc>.Filter.Empty);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = "";
            return View(lstDM);
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult formThemDM()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,4")]
        public ActionResult ThemDM(tb_DanhMuc dm, HttpPostedFileBase iconFile)
        {
            if (ModelState.IsValid)
            {
                if (iconFile != null && iconFile.ContentLength > 0)
                {
                    try
                    {
                        var fileName = Path.GetFileName(iconFile.FileName);
                        var filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            var extension = Path.GetExtension(fileName);
                            var name = Path.GetFileNameWithoutExtension(fileName);
                            fileName = name + "_" + DateTime.Now.Ticks + extension;
                            filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        }
                        
                        iconFile.SaveAs(filePath);
                        dm.Icon = fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi khi upload icon: " + ex.Message);
                        return View("formThemDM", dm);
                    }
                }
                
                dm.Id = (int)db.GetNextSequenceValue("tb_DanhMuc");
                dm.NgayTao = DateTime.Now;
                db.tb_DanhMuc.Collection.InsertOne(dm);
                
                TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                return RedirectToAction("DanhMuc");
            }
            return View("formThemDM", dm);
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult formSuaDM(int id)
        {
            var dm = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (dm == null)
            {
                return HttpNotFound();
            }
            return View(dm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(Roles = "1,4")]
        public ActionResult SuaDM(tb_DanhMuc dm, HttpPostedFileBase iconFile)
        {
            if (ModelState.IsValid)
            {
                var dmGoc = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(x => x.Id, dm.Id)).FirstOrDefault();
                if (dmGoc == null)
                {
                    return HttpNotFound();
                }
                
                var updateBuilder = Builders<tb_DanhMuc>.Update
                    .Set(x => x.TenDanhMuc, dm.TenDanhMuc)
                    .Set(x => x.BiDanh, dm.BiDanh)
                    .Set(x => x.ThuTu, dm.ThuTu)
                    .Set(x => x.HienThi, dm.HienThi)
                    .Set(x => x.NgayCapNhat, DateTime.Now);

                if (iconFile != null && iconFile.ContentLength > 0)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(dmGoc.Icon))
                        {
                            var oldFilePath = Path.Combine(Server.MapPath("~/Content/Images"), dmGoc.Icon);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        
                        var fileName = Path.GetFileName(iconFile.FileName);
                        var filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            var extension = Path.GetExtension(fileName);
                            var name = Path.GetFileNameWithoutExtension(fileName);
                            fileName = name + "_" + DateTime.Now.Ticks + extension;
                            filePath = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                        }
                        
                        iconFile.SaveAs(filePath);
                        updateBuilder = updateBuilder.Set(x => x.Icon, fileName);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi khi upload icon: " + ex.Message);
                        return View("formSuaDM", dm);
                    }
                }
                
                db.tb_DanhMuc.Collection.UpdateOne(Builders<tb_DanhMuc>.Filter.Eq(x => x.Id, dm.Id), updateBuilder);
                
                TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("DanhMuc");
            }
            return View("formSuaDM", dm);
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult XoaDM(int id)
        {
            var dm = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
            if (dm != null)
            {
                db.tb_DanhMuc.Collection.DeleteOne(Builders<tb_DanhMuc>.Filter.Eq(x => x.Id, id));
            }
            return RedirectToAction("DanhMuc");
        }

        [AdminAuthorize(Roles = "1,4")]
        public ActionResult TimDanhMuc(string timdm, int page = 1)
        {
            int pageSize = 10;
            var filter = Builders<tb_DanhMuc>.Filter.Empty;

            if (!String.IsNullOrEmpty(timdm))
            {
                var keyword = timdm.ToLower();
                filter = Builders<tb_DanhMuc>.Filter.Regex(dm => dm.TenDanhMuc, new BsonRegularExpression(keyword, "i")) |
                         Builders<tb_DanhMuc>.Filter.Regex(dm => dm.BiDanh, new BsonRegularExpression(keyword, "i"));
            }

            int totalItems = (int)db.tb_DanhMuc.Collection.CountDocuments(filter);
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var lstDM = db.tb_DanhMuc.Collection.Find(filter)
                .SortByDescending(dm => dm.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TuKhoa = timdm;
            return View("DanhMuc", lstDM);
        }
    }
}
