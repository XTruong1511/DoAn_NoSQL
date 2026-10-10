using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// ViewModel cho quản lý Banner (Quảng cáo)
    /// </summary>
    public class BannerViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề banner")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string TieuDe { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }

        [Display(Name = "Hình ảnh")]
        public string HinhAnh { get; set; }

        [StringLength(500, ErrorMessage = "Link không được vượt quá 500 ký tự")]
        [Display(Name = "Link liên kết")]
        public string Link { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        [Range(0, 999, ErrorMessage = "Thứ tự phải từ 0 đến 999")]
        public int ThuTu { get; set; }

        [Display(Name = "Vị trí hiển thị")]
        public string ViTri { get; set; }

        [Display(Name = "Hiển thị")]
        public bool HienThi { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? NgayTao { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime? NgayCapNhat { get; set; }
    }
}
