using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DoAnNoSQL.Models;
using MongoDB.Driver;
using Google.Apis.Auth;
using System.Threading.Tasks;

namespace DoAnNoSQL.Controllers
{
    public class AccountController : Controller
    {
        private MongoDbContext db = new MongoDbContext();

        #region Đăng nhập

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (Session["User"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var builder = Builders<tb_TaiKhoan>.Filter;
                var filter = builder.And(
                    builder.Eq(u => u.HoatDong, true),
                    builder.Or(
                        builder.Eq(u => u.Email, model.EmailOrPhone),
                        builder.Eq(u => u.SoDienThoai, model.EmailOrPhone)
                    )
                );
                var user = db.tb_TaiKhoan.Collection.Find(filter).FirstOrDefault();

                if (user != null)
                {
                    string inputHash = HashPassword(model.Password);
                    if (user.MatKhau == inputHash || user.MatKhau == model.Password)
                    {
                        if (user.MatKhau == model.Password) {
                            var updateFilter = Builders<tb_TaiKhoan>.Filter.Eq(u => u.Id, user.Id);
                            var update = Builders<tb_TaiKhoan>.Update.Set(u => u.MatKhau, inputHash);
                            db.tb_TaiKhoan.Collection.UpdateOne(updateFilter, update);
                        }
                        var userSession = new UserSessionViewModel
                        {
                            Id = user.Id,
                            HoTen = user.HoTen,
                            Email = user.Email,
                            LaQuanTri = user.LaQuanTri ?? false
                        };

                        Session["User"] = userSession;
                        Session["UserId"] = user.Id;
                        Session["UserName"] = user.HoTen;
                        Session["IsAdmin"] = user.LaQuanTri ?? false;
                        
                        int roleId = user.QuyenId ?? (user.LaQuanTri == true ? 1 : 2);
                        Session["Role"] = roleId;
                        
                        System.Diagnostics.Debug.WriteLine($"Login Success - User: {user.HoTen}, Role: {roleId}, QuyenId: {user.QuyenId}, IsAdmin: {user.LaQuanTri}");

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        
                        if (user.LaQuanTri == true)
                        {
                            if (roleId == 1) return RedirectToAction("TongQuan", "Admin");
                            if (roleId == 2) return RedirectToAction("QLSP", "Admin");
                            if (roleId == 3 || roleId == 4) return RedirectToAction("DonHang", "Admin");
                            if (roleId == 5) return RedirectToAction("BaoHanh", "Admin");
                            return RedirectToAction("TongQuan", "Admin");
                        }
                        
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Mật khẩu không đúng");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email không tồn tại hoặc tài khoản đã bị khóa");
                }
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GoogleLogin(string credential)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { "875214279930-h0d47gbbmvodj3mejnkb3k89dj3esalo.apps.googleusercontent.com" }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
                if (payload == null)
                {
                    ViewBag.ErrorMessage = "Xác thực Google thất bại.";
                    return View("Login");
                }

                var email = payload.Email;
                var filter = Builders<tb_TaiKhoan>.Filter.Eq(u => u.Email, email);
                var user = db.tb_TaiKhoan.Collection.Find(filter).FirstOrDefault();

                if (user == null)
                {
                    // Create new user if not exists
                    user = new tb_TaiKhoan
                    {
                        Id = (int)db.GetNextSequenceValue("tb_TaiKhoan"),
                        HoTen = payload.Name,
                        Email = email,
                        MatKhau = HashPassword(Guid.NewGuid().ToString("N").Substring(0, 8)), // Hashed Random password
                        NgayTao = DateTime.Now,
                        HoatDong = true,
                        LaQuanTri = false,
                        QuyenId = 2
                    };
                    db.tb_TaiKhoan.Collection.InsertOne(user);
                }
                else if (user.HoatDong != true)
                {
                    ViewBag.ErrorMessage = "Tài khoản của bạn đã bị khóa.";
                    return View("Login");
                }

                var userSession = new UserSessionViewModel
                {
                    Id = user.Id,
                    HoTen = user.HoTen,
                    Email = user.Email,
                    LaQuanTri = user.LaQuanTri ?? false
                };

                Session["User"] = userSession;
                Session["UserId"] = user.Id;
                Session["UserName"] = user.HoTen;
                Session["IsAdmin"] = user.LaQuanTri ?? false;
                
                int roleId = user.QuyenId ?? (user.LaQuanTri == true ? 1 : 2);
                Session["Role"] = roleId;

                if (user.LaQuanTri == true)
                {
                    if (roleId == 1) return RedirectToAction("TongQuan", "Admin");
                    if (roleId == 2) return RedirectToAction("QLSP", "Admin");
                    if (roleId == 3 || roleId == 4) return RedirectToAction("DonHang", "Admin");
                    if (roleId == 5) return RedirectToAction("BaoHanh", "Admin");
                    return RedirectToAction("TongQuan", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }
            catch (InvalidJwtException ex)
            {
                ViewBag.ErrorMessage = "Token không hợp lệ hoặc đã hết hạn. Chi tiết: " + ex.Message;
                return View("Login");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi đăng nhập bằng Google. Lỗi: " + ex.Message;
                return View("Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxLogin(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var builder = Builders<tb_TaiKhoan>.Filter;
                var filter = builder.And(
                    builder.Eq(u => u.HoatDong, true),
                    builder.Or(
                        builder.Eq(u => u.Email, model.EmailOrPhone),
                        builder.Eq(u => u.SoDienThoai, model.EmailOrPhone)
                    )
                );
                
                var user = db.tb_TaiKhoan.Collection.Find(filter).FirstOrDefault();

                if (user != null)
                {
                    if (user.LaQuanTri == true)
                    {
                        return Json(new { success = false, message = "Tài khoản Quản trị viên không được đăng nhập tại đây. Vui lòng vào trang đăng nhập Admin." });
                    }

                    bool isPasswordValid = false;
                    string inputHash = HashPassword(model.Password);
                    
                    if (user.MatKhau == inputHash)
                    {
                        isPasswordValid = true;
                    }
                    else if (user.MatKhau == model.Password) // Fallback for old plain-text passwords
                    {
                        isPasswordValid = true;
                        // Tự động nâng cấp (mã hóa) mật khẩu cũ
                        var updateFilter = Builders<tb_TaiKhoan>.Filter.Eq(u => u.Id, user.Id);
                        var update = Builders<tb_TaiKhoan>.Update.Set(u => u.MatKhau, inputHash);
                        db.tb_TaiKhoan.Collection.UpdateOne(updateFilter, update);
                    }

                    if (isPasswordValid)
                    {
                        var userSession = new UserSessionViewModel
                        {
                            Id = user.Id,
                            HoTen = user.HoTen,
                            Email = user.Email,
                            LaQuanTri = user.LaQuanTri ?? false
                        };
                        Session["User"] = userSession;
                        Session["UserId"] = user.Id;
                        Session["UserName"] = user.HoTen;
                        Session["IsAdmin"] = user.LaQuanTri ?? false;
                        
                        int roleId = user.QuyenId ?? (user.LaQuanTri == true ? 1 : 2);
                        Session["Role"] = roleId;

                        string redirectUrl = Url.Action("Index", "Home");
                        if (user.LaQuanTri == true)
                        {
                            if (roleId == 1) redirectUrl = Url.Action("TongQuan", "Admin");
                            else if (roleId == 2) redirectUrl = Url.Action("QLSP", "Admin");
                            else if (roleId == 3 || roleId == 4) redirectUrl = Url.Action("DonHang", "Admin");
                            else if (roleId == 5) redirectUrl = Url.Action("BaoHanh", "Admin");
                            else redirectUrl = Url.Action("TongQuan", "Admin");
                        }
                        
                        return Json(new { success = true, redirectUrl = redirectUrl });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Mật khẩu không đúng" });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Tài khoản không tồn tại hoặc đã bị khóa" });
                }
            }
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin hợp lệ" });
        }

        #endregion

        #region Đăng ký

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(u => u.Email, model.Email)).FirstOrDefault();
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký");
                    return View("Login", model);
                }

                var newUser = new tb_TaiKhoan
                {
                    Id = (int)db.GetNextSequenceValue("tb_TaiKhoan"),
                    HoTen = model.FullName,
                    Email = model.Email,
                    TenDangNhap = model.Email,
                    MatKhau = HashPassword(model.Password),
                    SoDienThoai = model.PhoneNumber,
                    LaQuanTri = false,
                    HoatDong = true,
                    NgayTao = DateTime.Now,
                    DanhSachYeuThich = new List<TaiKhoanYeuThich>()
                };

                try
                {
                    db.tb_TaiKhoan.Collection.InsertOne(newUser);

                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            return View("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxRegister(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingEmail = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(u => u.Email, model.Email)).FirstOrDefault();
                if (existingEmail != null)
                {
                    return Json(new { success = false, message = "Email này đã được đăng ký" });
                }

                var existingPhone = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(u => u.SoDienThoai, model.PhoneNumber)).FirstOrDefault();
                if (existingPhone != null && !string.IsNullOrEmpty(model.PhoneNumber))
                {
                    return Json(new { success = false, message = "Số điện thoại này đã được đăng ký" });
                }

                var newUser = new tb_TaiKhoan
                {
                    Id = (int)db.GetNextSequenceValue("tb_TaiKhoan"),
                    HoTen = model.FullName,
                    Email = model.Email,
                    TenDangNhap = model.Email,
                    MatKhau = HashPassword(model.Password),
                    SoDienThoai = model.PhoneNumber,
                    LaQuanTri = false,
                    HoatDong = true,
                    NgayTao = DateTime.Now,
                    DanhSachYeuThich = new List<TaiKhoanYeuThich>()
                };

                try
                {
                    db.tb_TaiKhoan.Collection.InsertOne(newUser);
                    return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin hợp lệ" });
        }

        #endregion

        #region Quên mật khẩu

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(u => u.Email, model.Email)).FirstOrDefault();

                if (user == null)
                {
                    ModelState.AddModelError("", "Email không tồn tại trong hệ thống");
                    return View(model);
                }

                string otp = GenerateRandomOTP(6);

                try
                {
                    Session["ResetOTP_" + user.Email] = otp;
                    Session["ResetOTPExpiry_" + user.Email] = DateTime.Now.AddMinutes(5);

                    SendOTPEmail(user.Email, user.HoTen, otp);

                    ViewBag.Step = 2; // Đánh dấu chuyển sang bước nhập OTP
                    return View("ForgotPassword", new ResetPasswordViewModel { Email = model.Email });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể gửi email OTP. Lỗi: " + ex.Message);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(u => u.Email, model.Email)).FirstOrDefault();
                if (user == null)
                {
                    ModelState.AddModelError("", "Email không hợp lệ.");
                    ViewBag.Step = 2;
                    return View("ForgotPassword", model);
                }


                string sessionOTP = Session["ResetOTP_" + model.Email] as string;
                DateTime? expiry = Session["ResetOTPExpiry_" + model.Email] as DateTime?;

                if (string.IsNullOrEmpty(sessionOTP) || sessionOTP != model.OTP)
                {
                    ModelState.AddModelError("", "Mã OTP không đúng hoặc đã hết hạn.");
                    ViewBag.Step = 2;
                    return View("ForgotPassword", model);
                }

                if (expiry == null || DateTime.Now > expiry.Value)
                {
                    ModelState.AddModelError("", "Mã OTP đã hết hạn. Vui lòng yêu cầu lại.");
                    Session.Remove("ResetOTP_" + model.Email);
                    Session.Remove("ResetOTPExpiry_" + model.Email);
                    return View("ForgotPassword", new ForgotPasswordViewModel { Email = model.Email });
                }

                try
                {
                    var updateFilter = Builders<tb_TaiKhoan>.Filter.Eq(u => u.Id, user.Id);
                    var update = Builders<tb_TaiKhoan>.Update.Set(u => u.MatKhau, HashPassword(model.NewPassword));
                    db.tb_TaiKhoan.Collection.UpdateOne(updateFilter, update);

                    Session.Remove("ResetOTP_" + model.Email);
                    Session.Remove("ResetOTPExpiry_" + model.Email);

                    TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập!";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể cập nhật mật khẩu. Lỗi: " + ex.Message);
                }
            }
            
            ViewBag.Step = 2;
            return View("ForgotPassword", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxVerifyOTP(string Email, string OTP)
        {
            string sessionOTP = Session["ResetOTP_" + Email] as string;
            DateTime? expiry = Session["ResetOTPExpiry_" + Email] as DateTime?;

            if (string.IsNullOrEmpty(sessionOTP) || sessionOTP != OTP)
            {
                return Json(new { success = false, message = "Mã OTP không đúng hoặc đã hết hạn." });
            }

            if (expiry == null || DateTime.Now > expiry.Value)
            {
                Session.Remove("ResetOTP_" + Email);
                Session.Remove("ResetOTPExpiry_" + Email);
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng yêu cầu lại." });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(u => u.Email, model.Email)).FirstOrDefault();
                if (user == null)
                {
                    return Json(new { success = false, message = "Email không hợp lệ." });
                }

                string sessionOTP = Session["ResetOTP_" + model.Email] as string;
                DateTime? expiry = Session["ResetOTPExpiry_" + model.Email] as DateTime?;

                if (string.IsNullOrEmpty(sessionOTP) || sessionOTP != model.OTP)
                {
                    return Json(new { success = false, message = "Mã OTP không đúng hoặc đã hết hạn." });
                }

                if (expiry == null || DateTime.Now > expiry.Value)
                {
                    Session.Remove("ResetOTP_" + model.Email);
                    Session.Remove("ResetOTPExpiry_" + model.Email);
                    return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng yêu cầu lại." });
                }

                try
                {
                    var updateFilter = Builders<tb_TaiKhoan>.Filter.Eq(u => u.Id, user.Id);
                    var update = Builders<tb_TaiKhoan>.Update.Set(u => u.MatKhau, HashPassword(model.NewPassword));
                    db.tb_TaiKhoan.Collection.UpdateOne(updateFilter, update);

                    Session.Remove("ResetOTP_" + model.Email);
                    Session.Remove("ResetOTPExpiry_" + model.Email);

                    return Json(new { success = true, message = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Không thể cập nhật mật khẩu. Lỗi: " + ex.Message });
                }
            }
            
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                                 .Select(e => e.ErrorMessage)
                                 .ToList();
            return Json(new { success = false, message = string.Join(" ", errorList) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(u => u.Email, model.Email)).FirstOrDefault();
                if (user == null)
                {
                    return Json(new { success = false, message = "Email không tồn tại trong hệ thống" });
                }

                string otp = GenerateRandomOTP(6);

                try
                {
                    Session["ResetOTP_" + user.Email] = otp;
                    Session["ResetOTPExpiry_" + user.Email] = DateTime.Now.AddMinutes(5);

                    SendOTPEmail(user.Email, user.HoTen, otp);
                    return Json(new { success = true, message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Không thể gửi email OTP. Lỗi: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Vui lòng nhập email hợp lệ" });
        }

        private string GenerateRandomOTP(int length)
        {
            const string validChars = "0123456789";
            StringBuilder otp = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                otp.Append(validChars[random.Next(validChars.Length)]);
            }

            return otp.ToString();
        }

        private void SendOTPEmail(string toEmail, string userName, string otp)
        {
            var smtpSection = (System.Net.Configuration.SmtpSection)System.Configuration.ConfigurationManager.GetSection("system.net/mailSettings/smtp");
            string fromEmail = smtpSection != null ? smtpSection.From : "noreply@techshop.com";

            string storeName = DoAnNoSQL.Helpers.SettingsHelper.GetValue("TenCuaHang", "TECH SHOP");
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(fromEmail, storeName);
            mail.To.Add(toEmail);
            mail.Subject = $"Mã OTP khôi phục mật khẩu - {storeName}";
            mail.IsBodyHtml = true;

            mail.Body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; }}
                    .content {{ background: #f9f9f9; padding: 30px; }}
                    .password-box {{ background: white; border: 2px solid #667eea; padding: 15px; margin: 20px 0; text-align: center; font-size: 24px; font-weight: bold; color: #667eea; letter-spacing: 5px; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Khôi phục mật khẩu</h1>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{userName}</strong>,</p>
                        <p>Chúng tôi đã nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn.</p>
                        <p>Mã OTP của bạn là: </p>
                        <div class='password-box'>{otp}</div>
                        <p><strong>Lưu ý quan trọng:</strong></p>
                        <ul>
                            <li>Mã OTP này sẽ hết hạn trong vòng 5 phút.</li>
                            <li>Vui lòng nhập mã OTP trên trang web để đặt lại mật khẩu của bạn.</li>
                            <li>Không chia sẻ mã này với bất kỳ ai.</li>
                        </ul>
                        <p>Trân trọng,<br><strong>{storeName}</strong></p>
                    </div>
                    <div class='footer'>
                        <p>Email này được gửi tự động, vui lòng không reply.</p>
                        <p>&copy; 2026 {storeName}. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>
            ";

            SmtpClient smtp = new SmtpClient();
            smtp.Send(mail);
        }

        #endregion

        #region Đăng xuất

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Access Denied

        [HttpGet]
        public ActionResult AccessDenied()
        {
            return View();
        }

        #endregion

        #region Helpers

        protected bool IsAuthenticated()
        {
            return Session["User"] != null;
        }

        protected UserSessionViewModel GetCurrentUser()
        {
            return Session["User"] as UserSessionViewModel;
        }

        #endregion

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}