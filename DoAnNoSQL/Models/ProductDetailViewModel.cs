using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// ViewModel cho trang chi tiết sản phẩm
    /// </summary>
    public class ProductDetailViewModel
    {
        /// <summary>
        /// Thông tin sản phẩm hiện tại
        /// </summary>
        public tb_SanPham SanPham { get; set; }

        /// <summary>
        /// Danh sách sản phẩm liên quan (cùng danh mục)
        /// </summary>
        public IEnumerable<tb_SanPham> SanPhamLienQuan { get; set; }

        /// <summary>
        /// Danh sách hình ảnh sản phẩm (nếu có bảng tb_AnhSanPham)
        /// </summary>
        public IEnumerable<tb_AnhSanPham> DanhSachAnh { get; set; }

        /// <summary>
        /// Danh sách đánh giá sản phẩm (nếu có bảng tb_DanhGia)
        /// </summary>
        public IEnumerable<tb_DanhGia> DanhSachDanhGia { get; set; }

        /// <summary>
        /// Trung bình điểm đánh giá
        /// </summary>
        public double DiemTrungBinh { get; set; }

        /// <summary>
        /// Tổng số đánh giá
        /// </summary>
        public int TongSoDanhGia { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProductDetailViewModel()
        {
            SanPhamLienQuan = new List<tb_SanPham>();
            DanhSachAnh = new List<tb_AnhSanPham>();
            DanhSachDanhGia = new List<tb_DanhGia>();
        }
    }
}