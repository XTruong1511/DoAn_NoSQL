using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    public class ThanhToanController : BaseController
    {
        private const string CART_SESSION = "Cart";
        private const string CART_COOKIE = "UserCartData";

        private ShoppingCart GetCart()
        {
            var cart = Session[CART_SESSION] as ShoppingCart;
            if (cart == null)
            {
                var cartCookie = Request.Cookies[CART_COOKIE];
                if (cartCookie != null && !string.IsNullOrEmpty(cartCookie.Value))
                {
                    try
                    {
                        string jsonCart = HttpUtility.UrlDecode(cartCookie.Value);
                        cart = Newtonsoft.Json.JsonConvert.DeserializeObject<ShoppingCart>(jsonCart);
                    }
                    catch
                    {
                        cart = new ShoppingCart();
                    }
                }
                else
                {
                    cart = new ShoppingCart();
                }
                Session[CART_SESSION] = cart;
            }
            return cart;
        }

        private void ClearCart()
        {
            Session[CART_SESSION] = new ShoppingCart();

            try
            {
                HttpCookie cookie = new HttpCookie(CART_COOKIE, "")
                {
                    Expires = DateTime.Now.AddDays(-1),
                    HttpOnly = true
                };
                Response.Cookies.Add(cookie);
            }
            catch { }
        }

        private string GenerateOrderCode()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"DH{timestamp}{random}";
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsAuthenticated || CurrentUser == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiến hành thanh toán";
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            if (cart == null || cart.IsEmpty())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Home");
            }

            var model = new CheckoutViewModel
            {
                Cart = cart
            };

            if (IsAuthenticated && CurrentUser != null)
            {
                var user = db.tb_TaiKhoan.Collection.Find(Builders<tb_TaiKhoan>.Filter.Eq(x => x.Id, CurrentUser.Id)).FirstOrDefault();
                if (user != null)
                {
                    model.HoTen = user.HoTen;
                    model.Email = user.Email;
                    model.SoDienThoai = user.SoDienThoai;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CheckoutViewModel model)
        {
            var lastOrderTime = Session["LastOrderTime"] as DateTime?;
            if (lastOrderTime.HasValue && (DateTime.Now - lastOrderTime.Value).TotalSeconds < 5)
            {
                TempData["ErrorMessage"] = "Vui lòng không gửi đơn hàng quá nhanh. Hãy đợi vài giây.";
                return RedirectToAction("Index");
            }
            
            Session["LastOrderTime"] = DateTime.Now;
            
            var cart = GetCart();

            if (cart == null || cart.IsEmpty())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("Index", "Home");
            }

            model.Cart = cart;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"=== CHECKOUT STARTED ===");
                System.Diagnostics.Debug.WriteLine($"Cart total items: {cart.Items.Count}");
                foreach (var item in cart.Items)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Product: {item.ProductName} (ID: {item.ProductId}), Qty: {item.Quantity}, Color: {item.Color}");
                }
                System.Diagnostics.Debug.WriteLine($"======================");
                
                var discountCode = Session["DiscountCode"] as string;
                var discountId = Session["DiscountId"] as int?;
                var discountAmount = Session["DiscountAmount"] as decimal? ?? 0;
                
                decimal cartTotal = cart.GetTotal();
                decimal finalTotal = cartTotal - discountAmount;
                
                if (finalTotal < 0)
                {
                    finalTotal = 0;
                }
                
                System.Diagnostics.Debug.WriteLine($"=== ORDER TOTALS ===");
                System.Diagnostics.Debug.WriteLine($"Cart Total: {cartTotal:N0} đ");
                System.Diagnostics.Debug.WriteLine($"Discount Code: {discountCode}");
                System.Diagnostics.Debug.WriteLine($"Discount Amount: {discountAmount:N0} đ");
                System.Diagnostics.Debug.WriteLine($"Final Total: {finalTotal:N0} đ");
                System.Diagnostics.Debug.WriteLine($"==================");

                string paymentMethodName = "Tiền mặt khi nhận hàng (COD)";
                if (model.PaymentMethod == 2) paymentMethodName = "Chuyển khoản ngân hàng";
                else if (model.PaymentMethod == 3) paymentMethodName = "Ví MoMo";
                else if (model.PaymentMethod == 4) paymentMethodName = "Ví ZaloPay";
                else if (model.PaymentMethod == 5) paymentMethodName = "VNPay";

                int orderId = (int)db.GetNextSequenceValue("tb_DonHang");
                var donHang = new tb_DonHang
                {
                    Id = orderId,
                    MaDonHang = GenerateOrderCode(),
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now,

                    KhachHang = new DonHangKhachHang
                    {
                        TaiKhoanId = IsAuthenticated && CurrentUser != null ? (int?)CurrentUser.Id : null,
                        TenKhachHang = model.HoTen,
                        Email = model.Email,
                        SoDienThoai = model.SoDienThoai,
                        DiaChi = model.DiaChi
                    },

                    TongTien = finalTotal,
                    SoLuongSanPham = cart.GetTotalQuantity(),
                    TrangThai = 2,

                    PhuongThucThanhToan = new DonHangPhuongThuc
                    {
                        Id = (int)model.PaymentMethod,
                        TenPhuongThuc = paymentMethodName
                    },
                    PhuongThucVanChuyen = new DonHangPhuongThuc
                    {
                        Id = model.ShippingMethod ?? 1,
                        TenPhuongThuc = "Giao hàng tiêu chuẩn",
                        PhiVanChuyen = 0
                    },

                    MaGiamGia = discountCode,
                    MaGiamGiaId = discountId,

                    DanhSachSanPham = cart.Items.Select(item => new DonHangSanPham
                    {
                        SanPhamId = item.ProductId,
                        TenSanPham = item.ProductName,
                        HinhAnh = item.ProductImage,
                        Gia = item.Price,
                        SoLuong = item.Quantity,
                        ThanhTien = item.TotalPrice
                    }).ToList()
                };

                List<string> outOfStockItems = new List<string>();
                foreach (var item in cart.Items)
                {
                    var sanPham = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(p => p.Id, item.ProductId)).FirstOrDefault();

                    if (sanPham == null)
                    {
                        outOfStockItems.Add($"{item.ProductName}: Sản phẩm không tồn tại");
                    }
                    else if (!sanPham.SoLuong.HasValue || sanPham.SoLuong.Value < item.Quantity)
                    {
                        int availableQty = sanPham.SoLuong ?? 0;
                        outOfStockItems.Add($"{item.ProductName}: Chỉ còn {availableQty} sản phẩm trong kho (bạn đặt {item.Quantity})");
                    }
                }

                if (outOfStockItems.Any())
                {
                    ModelState.AddModelError("", "Một số sản phẩm không đủ số lượng:");
                    foreach (var msg in outOfStockItems)
                    {
                        ModelState.AddModelError("", msg);
                    }
                    return View(model);
                }

                try
                {
                    db.tb_DonHang.Collection.InsertOne(donHang);

                    foreach (var item in cart.Items)
                    {
                        System.Diagnostics.Debug.WriteLine($"Processing item: {item.ProductName}, ProductId: {item.ProductId}, Quantity: {item.Quantity}");

                        var filter = Builders<tb_SanPham>.Filter.Where(p => p.Id == item.ProductId && p.SoLuong >= item.Quantity);
                        var update = Builders<tb_SanPham>.Update.Inc(p => p.SoLuong, -item.Quantity);
                        var updatedProduct = db.tb_SanPham.Collection.FindOneAndUpdate(filter, update);

                        if (updatedProduct == null)
                        {
                            throw new Exception($"Không đủ số lượng sản phẩm {item.ProductName} hoặc không thể cập nhật tồn kho");
                        }
                    }

                    if (discountId.HasValue && discountId.Value > 0)
                    {
                        var discountFilter = Builders<tb_MaGiamGia>.Filter.Eq(m => m.Id, discountId.Value);
                        var discountUpdate = Builders<tb_MaGiamGia>.Update
                            .Inc(m => m.SoLuong, -1)
                            .Inc(m => m.DaSuDung, 1);
                        db.tb_MaGiamGia.Collection.UpdateOne(discountFilter, discountUpdate);
                    }

                    SendOrderConfirmationEmail(donHang, cart);
                }
                catch
                {
                    throw;
                }

                ClearCart();
                
                Session.Remove("DiscountCode");
                Session.Remove("DiscountId");
                Session.Remove("DiscountAmount");

                TempData["OrderCode"] = donHang.MaDonHang;
                TempData["OrderId"] = donHang.Id;
                TempData["OrderTotal"] = donHang.TongTien;
                TempData["DiscountApplied"] = !string.IsNullOrEmpty(discountCode);
                TempData["DiscountCode"] = discountCode;
                TempData["DiscountAmount"] = discountAmount;
                TempData["OriginalTotal"] = cartTotal;
                TempData["PaymentMethod"] = ((int)model.PaymentMethod).ToString();

                if (model.PaymentMethod == 5)
                {
                    return RedirectToAction("VnpayPayment", "Payment", new { orderId = donHang.Id });
                }
                else if (model.PaymentMethod == 3)
                {
                    return RedirectToAction("MomoPayment", "Payment", new { orderId = donHang.Id });
                }
                else if (model.PaymentMethod == 4)
                {
                    return RedirectToAction("ZaloPayPayment", "Payment", new { orderId = donHang.Id });
                }
                
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt hàng: " + ex.Message);
                return View(model);
            }
        }

        #region Helpers

        private void SendOrderConfirmationEmail(tb_DonHang order, ShoppingCart cart)
        {
            try
            {
                var smtpSection = (System.Net.Configuration.SmtpSection)System.Configuration.ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                string fromEmail = smtpSection != null ? smtpSection.From : "noreply@techshop.com";

                string storeName = DoAnNoSQL.Helpers.SettingsHelper.GetValue("TenCuaHang", "TECH SHOP");
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, storeName);
                mail.To.Add(order.Email);
                mail.Subject = $"Xác nhận đơn hàng #{order.MaDonHang}";
                mail.IsBodyHtml = true;

                string productRows = "";
                foreach (var item in cart.Items)
                {
                    productRows += $@"
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{item.ProductName}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>{item.Price:N0} đ</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>{(item.Price * item.Quantity):N0} đ</td>
                </tr>";
                }

                mail.Body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <div style='max-width: 600px; margin: 0 auto; border: 1px solid #eee; padding: 20px;'>
                    <h2 style='color: #2563eb; text-align: center;'>Cảm ơn bạn đã đặt hàng!</h2>
                    <p>Xin chào <strong>{order.TenKhachHang}</strong>,</p>
                    <p>Đơn hàng <strong>#{order.MaDonHang}</strong> của bạn đã được đặt thành công.</p>
                    
                    <h3>Chi tiết đơn hàng:</h3>
                    <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 10px; text-align: left;'>Sản phẩm</th>
                                <th style='padding: 10px;'>SL</th>
                                <th style='padding: 10px; text-align: right;'>Đơn giá</th>
                                <th style='padding: 10px; text-align: right;'>Thành tiền</th>
                            </tr>
                        </thead>
                        <tbody>
                            {productRows}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 10px; text-align: right; font-weight: bold;'>Tổng tiền:</td>
                                <td style='padding: 10px; text-align: right; font-weight: bold; color: #d32f2f;'>{order.TongTien:N0} đ</td>
                            </tr>
                        </tfoot>
                    </table>
                    <p>Địa chỉ giao hàng: {order.DiaChi}</p>
                    <p>Chúng tôi sẽ sớm liên hệ để giao hàng cho bạn.</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='text-align: center; color: #666; font-size: 12px;'>{storeName} - Điện thoại, Laptop, Phụ kiện chính hãng</p>
                </div>
            </body>
            </html>";

                SmtpClient smtp = new SmtpClient();
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email error: " + ex.Message);
            }
        }

        #endregion

        public ActionResult Success()
        {
            ClearCart();

            var orderCode = TempData["OrderCode"] as string;
            var orderId = TempData["OrderId"] as int?;

            if (string.IsNullOrEmpty(orderCode))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.OrderCode = orderCode;
            ViewBag.OrderId = orderId;
            ViewBag.OrderTotal = TempData["OrderTotal"];
            ViewBag.DiscountApplied = TempData["DiscountApplied"] as bool? ?? false;
            ViewBag.DiscountCode = TempData["DiscountCode"] as string;
            ViewBag.DiscountAmount = TempData["DiscountAmount"] as decimal? ?? 0;
            ViewBag.OriginalTotal = TempData["OriginalTotal"] as decimal?;
            ViewBag.PaymentMethod = TempData["PaymentMethod"] as string;

            return View();
        }

        public ActionResult ChiTiet(int id)
        {
            var donHang = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, id)).FirstOrDefault();

            if (donHang == null)
            {
                return HttpNotFound();
            }

            if (!IsAuthenticated || (CurrentUser.Id != donHang.TaiKhoanId && !IsAdmin))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này";
                return RedirectToAction("Index", "Home");
            }

            return View(donHang);
        }

        [HttpPost]
        public JsonResult ApplyDiscount(string discountCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã giảm giá" });
                }

                var filter = Builders<tb_MaGiamGia>.Filter.Eq(m => m.HienThi, true) &
                             Builders<tb_MaGiamGia>.Filter.Regex(m => m.MaCode, new MongoDB.Bson.BsonRegularExpression("^" + discountCode + "$", "i"));
                
                var discount = db.tb_MaGiamGia.Collection.Find(filter).FirstOrDefault();

                if (discount == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại" });
                }

                if (discount.NgayKetThuc.HasValue && discount.NgayKetThuc.Value < DateTime.Now)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết hạn" });
                }

                if (discount.SoLuong.HasValue && discount.SoLuong.Value <= 0)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng" });
                }

                var cart = GetCart();
                if (cart.IsEmpty())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                decimal cartTotal = cart.GetTotal();
                
                if (discount.GiaTriToiThieu.HasValue && cartTotal < discount.GiaTriToiThieu.Value)
                {
                    return Json(new { success = false, message = $"Đơn hàng tối thiểu để áp dụng mã này là {discount.GiaTriToiThieu.Value.ToString("N0")} VNĐ" });
                }
                
                decimal discountAmount = 0;

                if (discount.PhanTramGiam.HasValue && discount.PhanTramGiam.Value > 0)
                {
                    discountAmount = cartTotal * discount.PhanTramGiam.Value / 100;
                }
                else if (discount.SoTienGiam.HasValue && discount.SoTienGiam.Value > 0)
                {
                    discountAmount = discount.SoTienGiam.Value;
                }

                if (discount.GiamToiDa.HasValue && discount.GiamToiDa.Value > 0 && discountAmount > discount.GiamToiDa.Value)
                {
                    discountAmount = discount.GiamToiDa.Value;
                }

                if (discountAmount > cartTotal)
                {
                    discountAmount = cartTotal;
                }

                Session["DiscountCode"] = discount.MaCode;
                Session["DiscountId"] = discount.Id;
                Session["DiscountAmount"] = discountAmount;

                decimal finalTotal = cartTotal - discountAmount;

                return Json(new
                {
                    success = true,
                    message = "Áp dụng mã giảm giá thành công!",
                    discountCode = discount.MaCode,
                    discountAmount = discountAmount,
                    cartTotal = cartTotal,
                    finalTotal = finalTotal
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RemoveDiscount()
        {
            try
            {
                Session.Remove("DiscountCode");
                Session.Remove("DiscountId");
                Session.Remove("DiscountAmount");

                var cart = GetCart();

                return Json(new
                {
                    success = true,
                    message = "Đã xóa mã giảm giá",
                    cartTotal = cart.GetTotal(),
                    finalTotal = cart.GetTotal()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        #region 34 Tinh Thanh API Proxy
        [HttpGet]
        public async Task<ActionResult> Get34Provinces()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.Add("User-Agent", "DoAnNoSQL-Ecommerce");
                    var json = await client.GetStringAsync("https://34tinhthanh.com/api/provinces");
                    return Content(json, "application/json");
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> Get34Wards(string province_code)
        {
            try
            {
                if (string.IsNullOrEmpty(province_code))
                {
                    return Content("[]", "application/json");
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(8);
                    client.DefaultRequestHeaders.Add("User-Agent", "DoAnNoSQL-Ecommerce");
                    var json = await client.GetStringAsync("https://34tinhthanh.com/api/wards?province_code=" + Uri.EscapeDataString(province_code));
                    return Content(json, "application/json");
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
    }
}
