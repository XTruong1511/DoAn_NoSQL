using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnNoSQL.Models
{
    public class BaoHanhViewModel
    {
        public int Id { get; set; }
        public string MaBaoHanh { get; set; }
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public string MaSanPham { get; set; }
        public Nullable<int> DonHangId { get; set; }
        public string MaDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public string MaSeri { get; set; }
        public DateTime NgayKichHoat { get; set; }
        public int ThoiHanBaoHanh { get; set; }
        public DateTime NgayHetHan { get; set; }
        public int TrangThai { get; set; } // 1: Còn hạn, 2: Hết hạn, 3: Đang sửa chữa, 0: Đã vô hiệu
        public string TrangThaiText { get; set; }
        public string TrangThaiBadgeClass { get; set; }
        public int SoNgayConLai { get; set; }
        public int SoLanSuaChua { get; set; }
        public string GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
        public List<BaoHanhThongTinBoSung> ThongTinBoSung { get; set; } = new List<BaoHanhThongTinBoSung>();
        public bool IsChoGiaoKhach { get; set; }
        public int PhieuSuaChuaChoGiaoId { get; set; }
    }

    public class BaoHanhFormViewModel
    {
        public int Id { get; set; }
        public string MaBaoHanh { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int SanPhamId { get; set; }
        public Nullable<int> DonHangId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        public string TenKhachHang { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public string MaSeri { get; set; }
        
        public DateTime NgayKichHoat { get; set; }
        public int ThoiHanBaoHanh { get; set; }
        public string GhiChu { get; set; }
        public List<BaoHanhThongTinBoSung> ThongTinBoSung { get; set; } = new List<BaoHanhThongTinBoSung>();
    }

    public class PhieuSuaChuaViewModel
    {
        public int Id { get; set; }
        public string MaPhieu { get; set; }
        public int BaoHanhId { get; set; }
        public string MaBaoHanh { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public string MaSeri { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string TrieuChungLoi { get; set; }
        public string TinhTrangTiepNhan { get; set; }
        public DateTime NgayTiepNhan { get; set; }
        public DateTime? NgayHenTra { get; set; }
        public DateTime? NgayTraThucTe { get; set; }
        public int TrangThai { get; set; } // 1: Tiếp nhận, 2: Đang sửa, 3: Chờ linh kiện, 4: Đã sửa xong, 5: Đã trả khách, 6: Đổi mới, 7: Từ chối
        public string TrangThaiText { get; set; }
        public string TrangThaiBadgeClass { get; set; }
        public decimal ChiPhiPhatSinh { get; set; }
        public string GiaiPhapXuLy { get; set; }
        public string LinhKienThayThe { get; set; }
        public string NguoiTiepNhan { get; set; }
        public string KyThuatVien { get; set; }
        public string GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
    }

    public class PhieuSuaChuaFormViewModel
    {
        public int Id { get; set; }
        public string MaPhieu { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn phiếu bảo hành")]
        public int BaoHanhId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập triệu chứng / mô tả lỗi")]
        public string TrieuChungLoi { get; set; }
        public string TinhTrangTiepNhan { get; set; }
        public DateTime? NgayHenTra { get; set; }
        public int TrangThai { get; set; }
        public decimal ChiPhiPhatSinh { get; set; }
        public string GiaiPhapXuLy { get; set; }
        public string LinhKienThayThe { get; set; }
        public string KyThuatVien { get; set; }
        public string GhiChu { get; set; }
    }

    public class SanPhamDropdownViewModel
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; }
        public string MaSanPham { get; set; }
        public int ThoiGianBaoHanh { get; set; }
    }

    public class BaoHanhStatsViewModel
    {
        public int TongSoPhieu { get; set; }
        public int ConHan { get; set; }
        public int SapHetHan { get; set; } // Trong vòng 30 ngày
        public int HetHan { get; set; }
        public int DangSuaChua { get; set; }
        public int TongPhieuSuaChua { get; set; }
        public int DangXuLySuaChua { get; set; }
    }

    public class BaoHanhIndexViewModel
    {
        public List<BaoHanhViewModel> DanhSachBaoHanh { get; set; }
        public List<PhieuSuaChuaViewModel> DanhSachPhieuSuaChua { get; set; }
        public List<SanPhamDropdownViewModel> DanhSachSanPhamDropdown { get; set; }
        public BaoHanhStatsViewModel ThongKe { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string TuKhoa { get; set; }
        public string TrangThai { get; set; }
        public string ActiveTab { get; set; }
        public int RepairCurrentPage { get; set; }
        public int RepairTotalPages { get; set; }
        public int? RepairStatus { get; set; }
    }
}
