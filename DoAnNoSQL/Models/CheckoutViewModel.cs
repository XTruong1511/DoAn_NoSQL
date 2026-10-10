using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// ViewModel cho trang thanh toán
    /// </summary>
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không đúng định dạng")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string TinhThanhPho { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string QuanHuyen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        [Display(Name = "Phường/Xã")]
        public string PhuongXa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhà")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        [Display(Name = "Số nhà, đường")]
        public string DiaChiNha { get; set; }

        /// <summary>
        /// Địa chỉ đầy đủ (tự động tạo từ các trường trên theo mô hình chính quyền 2 cấp)
        /// </summary>
        [Display(Name = "Địa chỉ giao hàng")]
        public string DiaChi
        {
            get
            {
                if (!string.IsNullOrEmpty(DiaChiNha) && !string.IsNullOrEmpty(PhuongXa) && !string.IsNullOrEmpty(TinhThanhPho))
                {
                    if (!string.IsNullOrEmpty(QuanHuyen))
                    {
                        return string.Format("{0}, {1}, {2}, {3}", DiaChiNha, PhuongXa, QuanHuyen, TinhThanhPho);
                    }
                    return string.Format("{0}, {1}, {2}", DiaChiNha, PhuongXa, TinhThanhPho);
                }
                return DiaChiNha ?? string.Empty;
            }
        }

        [StringLength(1000, ErrorMessage = "Ghi chú không được quá 1000 ký tự")]
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public int PaymentMethod { get; set; }

        [Display(Name = "Phương thức vận chuyển")]
        public int? ShippingMethod { get; set; }

        /// <summary>
        /// Giỏ hàng (để hiển thị trong view)
        /// </summary>
        public ShoppingCart Cart { get; set; }
    }

    /// <summary>
    /// Enum cho phương thức thanh toán
    /// </summary>
    public enum PaymentMethodEnum
    {
        [Display(Name = "Thanh toán khi nhận hàng (COD)")]
        COD = 1,

        [Display(Name = "Chuyển khoản ngân hàng")]
        BankTransfer = 2,

        [Display(Name = "Ví điện tử MoMo")]
        MoMo = 3,

        [Display(Name = "Ví điện tử ZaloPay")]
        ZaloPay = 4,

        [Display(Name = "Thẻ tín dụng/Ghi nợ")]
        CreditCard = 5
    }
}