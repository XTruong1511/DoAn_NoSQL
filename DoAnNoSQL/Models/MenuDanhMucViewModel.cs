using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// ViewModel cho Menu Danh mục với danh sách Thương hiệu
    /// </summary>
    public class MenuDanhMucViewModel
    {
        /// <summary>
        /// Thông tin danh mục
        /// </summary>
        public tb_DanhMuc DanhMuc { get; set; }

        /// <summary>
        /// Danh sách thương hiệu có sản phẩm trong danh mục này
        /// </summary>
        public List<ThuongHieuItem> DanhSachThuongHieu { get; set; }

        /// <summary>
        /// Số lượng sản phẩm trong danh mục
        /// </summary>
        public int SoLuongSanPham { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MenuDanhMucViewModel()
        {
            DanhSachThuongHieu = new List<ThuongHieuItem>();
        }
    }

    /// <summary>
    /// Thông tin thương hiệu trong menu
    /// </summary>
    public class ThuongHieuItem
    {
        /// <summary>
        /// ID thương hiệu
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tên thương hiệu
        /// </summary>
        public string TenThuongHieu { get; set; }

        /// <summary>
        /// Số lượng sản phẩm của thương hiệu trong danh mục
        /// </summary>
        public int SoLuongSanPham { get; set; }

        /// <summary>
        /// Hình ảnh/Logo thương hiệu (nếu có)
        /// </summary>
        public string HinhAnh { get; set; }
    }

    /// <summary>
    /// ViewModel cho Top Menu (Blue Bar Navigation)
    /// Thông tin đơn giản của danh mục để hiển thị trên thanh điều hướng
    /// </summary>
    public class TopMenuItemViewModel
    {
        /// <summary>
        /// ID danh mục
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tên danh mục
        /// </summary>
        public string TenDanhMuc { get; set; }

        /// <summary>
        /// Icon class (Font Awesome)
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Bí danh (slug) cho URL
        /// </summary>
        public string BiDanh { get; set; }
    }
}