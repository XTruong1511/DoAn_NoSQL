using System;

namespace DoAnNoSQL.Models
{
    public class RecentOrderViewModel
    {
        public int Id { get; set; }
        public string MaDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public decimal? TongTien { get; set; }
        public int? TrangThai { get; set; }
        public DateTime? NgayTao { get; set; }
    }
}
