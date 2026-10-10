using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;
using DoAnNoSQL.Models.Payments;

namespace DoAnNoSQL.Controllers
{
    public class PaymentController : Controller
    {
        private MongoDbContext db = new MongoDbContext();

        // 1. THANH TOÁN VNPAY
        public ActionResult VnpayPayment(int orderId)
        {
            var order = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, orderId)).FirstOrDefault();
            if (order == null) return HttpNotFound();

            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"] ?? (Request.Url.Scheme + "://" + Request.Url.Authority + "/Payment/VnpayReturn");
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"] ?? "Q3W0M1E9";
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"] ?? "M379O8S11F49J59Z4V6N5026C15Z6Y2K";

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            
            long amount = Convert.ToInt64(order.TongTien) * 100;
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toán đơn hàng " + order.MaDonHang);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString() + "_" + DateTime.Now.Ticks.ToString()); 

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        public ActionResult VnpayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"] ?? "M379O8S11F49J59Z4V6N5026C15Z6Y2K";
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }

                long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef").Split('_')[0]);
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                String vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                String TerminalID = Request.QueryString["vnp_TmnCode"];
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
                String bankCode = Request.QueryString["vnp_BankCode"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        var order = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, (int)orderId)).FirstOrDefault();
                        if (order != null && order.TrangThai < 3)
                        {
                            db.tb_DonHang.Collection.UpdateOne(Builders<tb_DonHang>.Filter.Eq(x => x.Id, (int)orderId), Builders<tb_DonHang>.Update.Set(x => x.TrangThai, 2));
                            
                            TempData["OrderCode"] = order.MaDonHang;
                            TempData["OrderId"] = order.Id;
                            TempData["OrderTotal"] = order.TongTien;
                            TempData["PaymentSuccess"] = true;
                            TempData["PaymentMethod"] = order.LoaiThanhToan;
                            
                            return RedirectToAction("Success", "ThanhToan");
                        }
                    }
                    else
                    {
                        TempData["PaymentSuccess"] = false;
                        TempData["PaymentError"] = "Giao dịch không thành công. Mã lỗi: " + vnp_ResponseCode;
                        return RedirectToAction("PaymentFailed");
                    }
                }
                else
                {
                    TempData["PaymentSuccess"] = false;
                    TempData["PaymentError"] = "Chữ ký số không hợp lệ!";
                    return RedirectToAction("PaymentFailed");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult PaymentFailed()
        {
            return View();
        }

        // 2. THANH TOÁN MOMO
        public ActionResult MomoPayment(int orderId)
        {
            var order = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, orderId)).FirstOrDefault();
            if (order == null) return HttpNotFound();

            string endpoint    = "https://test-payment.momo.vn/v2/gateway/api/create";
            string partnerCode = "MOMOBKUN20180529";
            string accessKey   = "klm05TvNBzhg7h7j";
            string secretKey   = "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa";

            // orderInfo: chỉ dùng ASCII để tránh lỗi encode trong HMAC
            string orderInfo   = "Thanh toan don hang " + order.MaDonHang;
            string redirectUrl = Request.Url.Scheme + "://" + Request.Url.Authority + "/Payment/MomoReturn";
            string ipnUrl      = redirectUrl; // sandbox dùng chung, production nên dùng URL riêng

            // orderId: tối đa 50 ký tự, chỉ dùng số và chữ
            string momoOrderId = "ORD" + orderId.ToString() + "T" + DateTime.Now.ToString("yyyyMMddHHmmss");
            string requestId   = momoOrderId;
            string amount      = Convert.ToInt64(order.TongTien).ToString();
            string extraData   = "";
            string requestType = "captureWallet";

            // Chuỗi ký tự PHẢI đúng thứ tự alphabet theo key
            string rawHash = "accessKey="   + accessKey
                           + "&amount="     + amount
                           + "&extraData="  + extraData
                           + "&ipnUrl="     + ipnUrl
                           + "&orderId="    + momoOrderId
                           + "&orderInfo="  + orderInfo
                           + "&partnerCode="+ partnerCode
                           + "&redirectUrl="+ redirectUrl
                           + "&requestId="  + requestId
                           + "&requestType="+ requestType;

            string signature = Utils.HmacSHA256(secretKey, rawHash);

            var message = new
            {
                partnerCode = partnerCode,
                partnerName = "Test",
                storeId     = "MomoTestStore",
                requestId   = requestId,
                amount      = Convert.ToInt64(order.TongTien),
                orderId     = momoOrderId,
                orderInfo   = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl      = ipnUrl,
                lang        = "vi",
                extraData   = extraData,
                requestType = requestType,
                signature   = signature
            };

            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Headers[System.Net.HttpRequestHeader.ContentType] = "application/json";
                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                    string response = client.UploadString(endpoint, data);
                    var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);

                    int resultCode = (int)responseData.resultCode;
                    if (resultCode == 0)
                    {
                        string payUrl = (string)responseData.payUrl;
                        return Redirect(payUrl);
                    }
                    else
                    {
                        TempData["PaymentSuccess"] = false;
                        TempData["PaymentError"] = "Lỗi MoMo [" + resultCode + "]: " + (string)responseData.message;
                        return RedirectToAction("PaymentFailed");
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                string errorDetail = wex.Message;
                if (wex.Response != null)
                {
                    using (var reader = new System.IO.StreamReader(wex.Response.GetResponseStream()))
                    {
                        errorDetail += " - " + reader.ReadToEnd();
                    }
                }
                TempData["PaymentSuccess"] = false;
                TempData["PaymentError"] = "Lỗi kết nối MoMo: " + errorDetail;
                return RedirectToAction("PaymentFailed");
            }
            catch (Exception ex)
            {
                TempData["PaymentSuccess"] = false;
                TempData["PaymentError"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("PaymentFailed");
            }
        }

        public ActionResult MomoReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string partnerCode = Request.QueryString["partnerCode"];
                string orderId_Query = Request.QueryString["orderId"];
                string requestId = Request.QueryString["requestId"];
                string amount = Request.QueryString["amount"];
                string orderInfo = Request.QueryString["orderInfo"];
                string orderType = Request.QueryString["orderType"];
                string transId = Request.QueryString["transId"];
                string resultCode = Request.QueryString["resultCode"];
                string message = Request.QueryString["message"];
                string payType = Request.QueryString["payType"];
                string responseTime = Request.QueryString["responseTime"];
                string extraData = Request.QueryString["extraData"];
                string signature = Request.QueryString["signature"];
                
                string secretKey = "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa";
                string accessKey = "klm05TvNBzhg7h7j";

                string rawHash = "accessKey=" + accessKey +
                    "&amount=" + amount +
                    "&extraData=" + extraData +
                    "&message=" + message +
                    "&orderId=" + orderId_Query +
                    "&orderInfo=" + orderInfo +
                    "&orderType=" + orderType +
                    "&partnerCode=" + partnerCode +
                    "&payType=" + payType +
                    "&requestId=" + requestId +
                    "&responseTime=" + responseTime +
                    "&resultCode=" + resultCode +
                    "&transId=" + transId;

                string checkSignature = Utils.HmacSHA256(secretKey, rawHash);

                if (signature == checkSignature)
                {
                    if (resultCode == "0") 
                    {
                        long orderId = Convert.ToInt64(orderId_Query.Split('_')[0]);
                        var order = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, (int)orderId)).FirstOrDefault();
                        if (order != null && order.TrangThai < 3)
                        {
                            db.tb_DonHang.Collection.UpdateOne(Builders<tb_DonHang>.Filter.Eq(x => x.Id, (int)orderId), Builders<tb_DonHang>.Update.Set(x => x.TrangThai, 2));
                            
                            TempData["OrderCode"] = order.MaDonHang;
                            TempData["OrderId"] = order.Id;
                            TempData["OrderTotal"] = order.TongTien;
                            TempData["PaymentSuccess"] = true;
                            TempData["PaymentMethod"] = order.LoaiThanhToan;
                            
                            return RedirectToAction("Success", "ThanhToan");
                        }
                    }
                    else
                    {
                        TempData["PaymentSuccess"] = false;
                        TempData["PaymentError"] = "Thanh toán MoMo thất bại: " + message;
                        return RedirectToAction("PaymentFailed");
                    }
                }
                else
                {
                    TempData["PaymentSuccess"] = false;
                    TempData["PaymentError"] = "Chữ ký số MoMo không hợp lệ!";
                    return RedirectToAction("PaymentFailed");
                }
            }
            return RedirectToAction("Index", "Home");
        }

        // 3. THANH TOÁN ZALOPAY
        public ActionResult ZaloPayPayment(int orderId)
        {
            var order = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, orderId)).FirstOrDefault();
            if (order == null) return HttpNotFound();

            string app_id = "2553";
            string key1 = "PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL";
            string endpoint = "https://sb-openapi.zalopay.vn/v2/create";

            var random = new Random();
            string app_trans_id = DateTime.Now.ToString("yyMMdd") + "_" + orderId.ToString();
            string app_user = "DoAnNoSQL";
            long app_time = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            string item = "[]";
            string embed_data = Newtonsoft.Json.JsonConvert.SerializeObject(new { redirecturl = Request.Url.Scheme + "://" + Request.Url.Authority + "/Payment/ZaloPayReturn" });
            string amount = Convert.ToInt64(order.TongTien).ToString();
            string description = "Thanh toán đơn hàng " + order.MaDonHang;
            string bank_code = "zalopayapp";

            string macData = app_id + "|" + app_trans_id + "|" + app_user + "|" + amount + "|" + app_time + "|" + embed_data + "|" + item;
            string mac = Utils.HmacSHA256(key1, macData);

            var param = new Dictionary<string, string>
            {
                { "app_id", app_id },
                { "app_user", app_user },
                { "app_time", app_time.ToString() },
                { "amount", amount },
                { "app_trans_id", app_trans_id },
                { "embed_data", embed_data },
                { "item", item },
                { "description", description },
                { "bank_code", bank_code },
                { "mac", mac }
            };

            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Headers[System.Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    var data = string.Join("&", param.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));
                    string response = client.UploadString(endpoint, data);
                    var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);

                    if (responseData.return_code == 1)
                    {
                        string orderUrl = responseData.order_url;
                        return Redirect(orderUrl);
                    }
                    else
                    {
                        TempData["PaymentSuccess"] = false;
                        TempData["PaymentError"] = "Lỗi tạo giao dịch ZaloPay: " + responseData.return_message;
                        return RedirectToAction("PaymentFailed");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["PaymentSuccess"] = false;
                TempData["PaymentError"] = "Lỗi kết nối ZaloPay: " + ex.Message;
                return RedirectToAction("PaymentFailed");
            }
        }

        public ActionResult ZaloPayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string appid = Request.QueryString["appid"];
                string apptransid = Request.QueryString["apptransid"];
                string pmcid = Request.QueryString["pmcid"];
                string bankcode = Request.QueryString["bankcode"];
                string amount = Request.QueryString["amount"];
                string discountamount = Request.QueryString["discountamount"];
                string status = Request.QueryString["status"];
                string mac = Request.QueryString["mac"];

                string key2 = "kLtgPl8YESD13PqQpzU67jA2R3nE238O";
                string macData = appid + "|" + apptransid + "|" + pmcid + "|" + bankcode + "|" + amount + "|" + discountamount + "|" + status;
                string checkMac = Utils.HmacSHA256(key2, macData);

                if (mac == checkMac)
                {
                    if (status == "1") 
                    {
                        long orderId = Convert.ToInt64(apptransid.Split('_')[1]);
                        var order = db.tb_DonHang.Collection.Find(Builders<tb_DonHang>.Filter.Eq(x => x.Id, (int)orderId)).FirstOrDefault();
                        if (order != null && order.TrangThai < 3)
                        {
                            db.tb_DonHang.Collection.UpdateOne(Builders<tb_DonHang>.Filter.Eq(x => x.Id, order.Id), Builders<tb_DonHang>.Update.Set(x => x.TrangThai, 2));
                            
                            TempData["OrderCode"] = order.MaDonHang;
                            TempData["OrderId"] = order.Id;
                            TempData["OrderTotal"] = order.TongTien;
                            TempData["PaymentSuccess"] = true;
                            TempData["PaymentMethod"] = order.LoaiThanhToan;
                            
                            return RedirectToAction("Success", "ThanhToan");
                        }
                    }
                    else
                    {
                        TempData["PaymentSuccess"] = false;
                        TempData["PaymentError"] = "Thanh toán ZaloPay thất bại!";
                        return RedirectToAction("PaymentFailed");
                    }
                }
                else
                {
                    TempData["PaymentSuccess"] = false;
                    TempData["PaymentError"] = "Chữ ký ZaloPay không hợp lệ!";
                    return RedirectToAction("PaymentFailed");
                }
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
