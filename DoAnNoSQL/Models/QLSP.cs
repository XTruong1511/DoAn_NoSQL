using System;
using System.Collections.Generic;

namespace DoAnNoSQL.Models
{
    public class QLSP
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; }
        public string BiDanh { get; set; }
        public string MaSanPham { get; set; }
        public string MoTa { get; set; }
        public string ChiTiet { get; set; }
        public decimal? Gia { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public int? SoLuong { get; set; }
        public int? LuotXem { get; set; }
        public string HinhAnh { get; set; }
        
        public int DanhMucId { get; set; }
        public int? ThuongHieuId { get; set; }

        public bool? LaSanPhamTrangChu { get; set; }
        public bool? LaSanPhamKhuyenMai { get; set; }
        public bool? LaSanPhamNoiBat { get; set; }
        public bool? LaSanPhamHot { get; set; }
        public bool? LaSanPhamMoi { get; set; }
        public bool? LaSanPhamBanChay { get; set; }
        public bool? HienThi { get; set; }
        public DateTime? NgayTao { get; set; }

        public string TenDanhMuc { get; set; }
        public string TenThuongHieu { get; set; }

        // Thông số
        public string BoNhoRAM { get; set; }
        public string BoNhoTrong { get; set; }
        public string ManHinh { get; set; }
        public string DungLuongPin { get; set; }
        public string Camera { get; set; }
        public string HeDieuHanh { get; set; }
        public string CongKetNoi { get; set; }
        public List<SanPhamThongSo> CacThongSoTuyChinh { get; set; } = new List<SanPhamThongSo>();
    }
}