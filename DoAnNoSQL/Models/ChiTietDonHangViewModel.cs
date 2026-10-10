using System;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// ViewModel cho chi tiết đơn hàng
    /// </summary>
    public class ChiTietDonHangViewModel
    {
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuong { get; set; }
        public decimal Gia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}
