using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// ViewModel cho trang chủ
    /// Chứa danh sách sản phẩm mới, sản phẩm bán chạy, danh mục và tin tức
    /// </summary>
    public class HomeViewModel
    {
        /// <summary>
        /// Danh sách sản phẩm mới (Top 10)
        /// </summary>
        public List<tb_SanPham> SanPhamMoi { get; set; }

        /// <summary>
        /// Danh sách sản phẩm bán chạy (Top 10)
        /// </summary>
        public List<tb_SanPham> SanPhamBanChay { get; set; }

        /// <summary>
        /// Danh sách tất cả danh mục sản phẩm
        /// </summary>
        public List<tb_DanhMuc> DanhMuc { get; set; }

        /// <summary>
        /// Danh sách tin tức mới nhất (Top 3)
        /// </summary>
        public List<tb_TinTuc> TinTuc { get; set; }

        /// <summary>
        /// Danh sách thương hiệu nổi bật
        /// </summary>
        public List<tb_ThuongHieu> ThuongHieu { get; set; }

        /// <summary>
        /// Danh sách quảng cáo (banner)
        /// </summary>
        public List<tb_QuangCao> QuangCao { get; set; }

        /// <summary>
        /// Constructor khởi tạo các list rỗng
        /// </summary>
        public HomeViewModel()
        {
            SanPhamMoi = new List<tb_SanPham>();
            SanPhamBanChay = new List<tb_SanPham>();
            DanhMuc = new List<tb_DanhMuc>();
            TinTuc = new List<tb_TinTuc>();
            ThuongHieu = new List<tb_ThuongHieu>();
            QuangCao = new List<tb_QuangCao>();
        }
    }
}