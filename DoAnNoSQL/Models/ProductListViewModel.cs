using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// ViewModel cho trang danh sách sản phẩm
    /// </summary>
    public class ProductListViewModel
    {
        /// <summary>
        /// Danh sách sản phẩm
        /// </summary>
        public IEnumerable<tb_SanPham> Products { get; set; }

        /// <summary>
        /// Tên danh mục hoặc thương hiệu hiện tại (để hiển thị title)
        /// </summary>
        public string CurrentCategoryName { get; set; }

        /// <summary>
        /// Trang hiện tại
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Tổng số trang
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Tổng số sản phẩm
        /// </summary>
        public int TotalProducts { get; set; }

        /// <summary>
        /// Số sản phẩm trên mỗi trang
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// ID danh mục (để giữ filter khi phân trang)
        /// </summary>
        public int? DanhMucId { get; set; }

        /// <summary>
        /// ID thương hiệu (để giữ filter khi phân trang)
        /// </summary>
        public int? ThuongHieuId { get; set; }

        /// <summary>
        /// Từ khóa tìm kiếm (để giữ filter khi phân trang)
        /// </summary>
        public string SearchKeyword { get; set; }

        /// <summary>
        /// Bộ lọc đặc biệt: moi, banchay, khuyenmai
        /// </summary>
        public string FilterType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProductListViewModel()
        {
            Products = new List<tb_SanPham>();
            PageSize = 12; // Mặc định 12 sản phẩm/trang
            CurrentPage = 1;
        }

        /// <summary>
        /// Kiểm tra có trang trước không
        /// </summary>
        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        /// <summary>
        /// Kiểm tra có trang sau không
        /// </summary>
        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }
    }
}