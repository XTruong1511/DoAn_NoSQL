using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;
using Newtonsoft.Json;

namespace DoAnNoSQL.Controllers
{
    public class GioHangController : BaseController
    {
        private const string CART_SESSION = "Cart";
        private const string CART_COOKIE = "UserCartData";

        private ShoppingCart GetCart()
        {
            var cart = Session[CART_SESSION] as ShoppingCart;
            if (cart == null)
            {
                // Try to restore from persistent cookie
                var cartCookie = Request.Cookies[CART_COOKIE];
                if (cartCookie != null && !string.IsNullOrEmpty(cartCookie.Value))
                {
                    try
                    {
                        string jsonCart = HttpUtility.UrlDecode(cartCookie.Value);
                        cart = JsonConvert.DeserializeObject<ShoppingCart>(jsonCart);
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

        private void SaveCart(ShoppingCart cart)
        {
            Session[CART_SESSION] = cart;

            // Save to persistent cookie for cross-session support (30 days)
            try
            {
                if (cart == null || cart.Items == null || !cart.Items.Any())
                {
                    HttpCookie expiredCookie = new HttpCookie(CART_COOKIE, "")
                    {
                        Expires = DateTime.Now.AddDays(-1),
                        HttpOnly = true
                    };
                    Response.Cookies.Add(expiredCookie);
                }
                else
                {
                    string jsonCart = JsonConvert.SerializeObject(cart);
                    string encodedCart = HttpUtility.UrlEncode(jsonCart);
                    HttpCookie cookie = new HttpCookie(CART_COOKIE, encodedCart)
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true
                    };
                    Response.Cookies.Add(cookie);
                }
            }
            catch { }
        }

        public ActionResult Index()
        {
            if (!IsAuthenticated || CurrentUser == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem giỏ hàng";
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity = 1, string color = "")
        {
            try
            {
                if (!IsAuthenticated || CurrentUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng",
                        requireLogin = true
                    });
                }

                var product = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, productId)).FirstOrDefault();

                if (product == null || product.HienThi == false)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm không tồn tại"
                    });
                }

                var cart = GetCart();
                
                var existingItem = cart.GetItem(productId, color);
                var totalQuantityInCart = (existingItem?.Quantity ?? 0) + quantity;

                if (product.SoLuong.HasValue && product.SoLuong.Value < totalQuantityInCart)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Chỉ còn {product.SoLuong.Value} sản phẩm trong kho. Bạn đã có {existingItem?.Quantity ?? 0} trong giỏ hàng."
                    });
                }

                var cartItem = new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.TenSanPham,
                    ProductImage = product.HinhAnh,
                    Price = product.GiaKhuyenMai.HasValue && product.GiaKhuyenMai.Value > 0 
                        ? product.GiaKhuyenMai.Value 
                        : product.Gia ?? 0,
                    Quantity = quantity,
                    Color = color
                };

                cart.AddItem(cartItem);
                SaveCart(cart);

                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng",
                    cartCount = cart.GetTotalQuantity(),
                    cartTotal = cart.GetTotal()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult UpdateQuantity(int productId, int quantity, string color = "")
        {
            try
            {
                if (quantity < 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số lượng không hợp lệ"
                    });
                }

                var cart = GetCart();

                if (quantity == 0)
                {
                    cart.RemoveItem(productId, color);
                }
                else
                {
                    var product = db.tb_SanPham.Collection.Find(Builders<tb_SanPham>.Filter.Eq(x => x.Id, productId)).FirstOrDefault();
                    if (product != null && product.SoLuong.HasValue && product.SoLuong.Value < quantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Chỉ còn {product.SoLuong.Value} sản phẩm trong kho"
                        });
                    }

                    cart.UpdateQuantity(productId, quantity, color);
                }

                SaveCart(cart);

                return Json(new
                {
                    success = true,
                    message = "Đã cập nhật giỏ hàng",
                    cartCount = cart.GetTotalQuantity(),
                    cartTotal = cart.GetTotal(),
                    itemTotal = cart.GetItem(productId, color)?.TotalPrice ?? 0
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult Remove(int productId, string color = "")
        {
            try
            {
                var cart = GetCart();
                cart.RemoveItem(productId, color);
                SaveCart(cart);

                return Json(new
                {
                    success = true,
                    message = "Đã xóa sản phẩm khỏi giỏ hàng",
                    cartCount = cart.GetTotalQuantity(),
                    cartTotal = cart.GetTotal()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult ClearCart()
        {
            try
            {
                var cart = GetCart();
                cart.Clear();
                SaveCart(cart);

                return Json(new
                {
                    success = true,
                    message = "Đã xóa toàn bộ giỏ hàng"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }

        public JsonResult GetCartCount()
        {
            var cart = GetCart();
            return Json(new
            {
                count = cart.GetTotalQuantity(),
                total = cart.GetTotal()
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCartData()
        {
            try
            {
                var cart = GetCart();
                var items = cart.Items.Select(x => new
                {
                    productId = x.ProductId,
                    productName = x.ProductName,
                    productImage = x.ProductImage,
                    price = x.Price,
                    quantity = x.Quantity,
                    color = x.Color,
                    totalPrice = x.TotalPrice
                }).ToList();

                return Json(new
                {
                    success = true,
                    items = items,
                    cartCount = cart.GetTotalQuantity(),
                    cartTotal = cart.GetTotal()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi lấy giỏ hàng: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ApplyDiscount(string discountCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng nhập mã giảm giá"
                    });
                }

                var filter = Builders<tb_MaGiamGia>.Filter.Eq(m => m.HienThi, true) &
                             Builders<tb_MaGiamGia>.Filter.Regex(m => m.MaCode, new MongoDB.Bson.BsonRegularExpression("^" + discountCode + "$", "i"));
                
                var discount = db.tb_MaGiamGia.Collection.Find(filter).FirstOrDefault();

                if (discount == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã giảm giá không tồn tại hoặc đã hết hạn"
                    });
                }

                if (discount.NgayKetThuc.HasValue && discount.NgayKetThuc.Value < DateTime.Now)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã giảm giá đã hết hạn"
                    });
                }

                if (discount.SoLuong.HasValue && discount.SoLuong.Value <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã giảm giá đã hết lượt sử dụng"
                    });
                }

                var cart = GetCart();
                
                if (cart.IsEmpty())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Giỏ hàng trống"
                    });
                }

                decimal cartTotal = cart.GetTotal();

                if (discount.GiaTriToiThieu.HasValue && cartTotal < discount.GiaTriToiThieu.Value)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Đơn hàng tối thiểu để áp dụng mã này là {discount.GiaTriToiThieu.Value.ToString("N0")} VNĐ"
                    });
                }

                decimal discountAmount = 0;

                if (discount.PhanTramGiam.HasValue && discount.PhanTramGiam.Value > 0)
                {
                    discountAmount += cartTotal * discount.PhanTramGiam.Value / 100;
                }
                if (discount.SoTienGiam.HasValue && discount.SoTienGiam.Value > 0)
                {
                    discountAmount += discount.SoTienGiam.Value;
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
                    finalTotal = finalTotal,
                    discountPercent = discount.PhanTramGiam ?? 0
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
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
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }
    }
}
