using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class LienHeController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.HotlineText = DoAnNoSQL.Helpers.SettingsHelper.GetValue("Hotline", "1800.1060");
            ViewBag.EmailText = DoAnNoSQL.Helpers.SettingsHelper.GetValue("EmailLienHe", "support@laptopstore.vn");
            ViewBag.AddressText = DoAnNoSQL.Helpers.SettingsHelper.GetValue("DiaChiCuaHang", "140 Lê Trọng Tấn, Phường Tây Thạnh, Quận Tân Phú, TP.HCM");
            ViewBag.WorkingHours = "Thứ 2 - Thứ 7: 8:00 - 21:00 | Chủ nhật: 9:00 - 18:00";

            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                var user = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, userId)).FirstOrDefault();
                
                if (user != null)
                {
                    ViewBag.UserHoTen = user.HoTen;
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserSoDienThoai = user.SoDienThoai;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiLienHe(string hoTen, string email, string soDienThoai, string chuDe, string noiDung)
        {
            if (string.IsNullOrWhiteSpace(hoTen) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(noiDung))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc";
                return RedirectToAction("Index");
            }

            try
            {
                string formattedContent = noiDung.Trim();
                
                if (!string.IsNullOrWhiteSpace(chuDe))
                {
                    formattedContent = $"[{chuDe}]\n\n{formattedContent}";
                }
                
                if (!string.IsNullOrWhiteSpace(soDienThoai))
                {
                    formattedContent = $"SĐT: {soDienThoai}\n\n{formattedContent}";
                }
                
                var lienHe = new tb_LienHe
                {
                    TenNguoiGui = hoTen.Trim(),
                    Email = email.Trim(),
                    Website = soDienThoai?.Trim(),
                    NoiDung = formattedContent,
                    NgayTao = DateTime.Now,
                    DaDoc = false
                };

                db.tb_LienHe.Collection.InsertOne(lienHe);

                TempData["SuccessMessage"] = "Gửi liên hệ thành công! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GuiLienHeAjax(string hoTen, string email, string soDienThoai, string chuDe, string noiDung)
        {
            if (string.IsNullOrWhiteSpace(hoTen) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(noiDung))
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin bắt buộc" });
            }

            try
            {
                string formattedContent = noiDung.Trim();
                
                if (!string.IsNullOrWhiteSpace(chuDe))
                {
                    formattedContent = $"[{chuDe}]\n\n{formattedContent}";
                }
                
                if (!string.IsNullOrWhiteSpace(soDienThoai))
                {
                    formattedContent = $"SĐT: {soDienThoai}\n\n{formattedContent}";
                }
                
                var lienHe = new tb_LienHe
                {
                    TenNguoiGui = hoTen.Trim(),
                    Email = email.Trim(),
                    Website = soDienThoai?.Trim(),
                    NoiDung = formattedContent,
                    NgayTao = DateTime.Now,
                    DaDoc = false
                };

                db.tb_LienHe.Collection.InsertOne(lienHe);

                return Json(new { success = true, message = "Gửi liên hệ thành công! Chúng tôi sẽ phản hồi trong thời gian sớm nhất." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
    }
}
