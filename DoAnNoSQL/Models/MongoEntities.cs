// MongoDB entity classes - mapped to 14 Collections schema
using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace DoAnNoSQL
{
    #region Flexible Serializers
    public class FlexibleDateTimeSerializer : SerializerBase<DateTime?>
    {
        public override DateTime? Deserialize(MongoDB.Bson.Serialization.BsonDeserializationContext context, MongoDB.Bson.Serialization.BsonDeserializationArgs args)
        {
            var currentType = context.Reader.CurrentBsonType;
            if (currentType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            if (currentType == BsonType.DateTime)
            {
                return BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(context.Reader.ReadDateTime());
            }
            if (currentType == BsonType.String)
            {
                var str = context.Reader.ReadString();
                if (string.IsNullOrWhiteSpace(str)) return null;

                var match = System.Text.RegularExpressions.Regex.Match(str, @"\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(\.\d+)?");
                if (match.Success && DateTime.TryParse(match.Value, out DateTime parsed))
                {
                    return parsed;
                }
                if (DateTime.TryParse(str, out DateTime dt))
                {
                    return dt;
                }
                return null;
            }
            context.Reader.SkipValue();
            return null;
        }

        public override void Serialize(MongoDB.Bson.Serialization.BsonSerializationContext context, MongoDB.Bson.Serialization.BsonSerializationArgs args, DateTime? value)
        {
            if (value.HasValue)
            {
                context.Writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(value.Value));
            }
            else
            {
                context.Writer.WriteNull();
            }
        }
    }

    public class FlexibleStringOrDocumentSerializer : SerializerBase<string>
    {
        public override string Deserialize(MongoDB.Bson.Serialization.BsonDeserializationContext context, MongoDB.Bson.Serialization.BsonDeserializationArgs args)
        {
            var currentType = context.Reader.CurrentBsonType;
            if (currentType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            if (currentType == BsonType.String)
            {
                return context.Reader.ReadString();
            }
            if (currentType == BsonType.Document)
            {
                var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(context.Reader);
                if (doc.Contains("MaCode") && !doc["MaCode"].IsBsonNull)
                {
                    return doc["MaCode"].AsString;
                }
                if (doc.Contains("Code") && !doc["Code"].IsBsonNull)
                {
                    return doc["Code"].AsString;
                }
                if (doc.Contains("Ten") && !doc["Ten"].IsBsonNull)
                {
                    return doc["Ten"].AsString;
                }
                return doc.ToString();
            }
            if (currentType == BsonType.Int32)
            {
                return context.Reader.ReadInt32().ToString();
            }
            if (currentType == BsonType.Int64)
            {
                return context.Reader.ReadInt64().ToString();
            }
            context.Reader.SkipValue();
            return null;
        }

        public override void Serialize(MongoDB.Bson.Serialization.BsonSerializationContext context, MongoDB.Bson.Serialization.BsonSerializationArgs args, string value)
        {
            if (value == null)
            {
                context.Writer.WriteNull();
            }
            else
            {
                context.Writer.WriteString(value);
            }
        }
    }
    #endregion

    #region Sub-document Classes for 14 Collections
    [BsonIgnoreExtraElements]
    public class SanPhamDanhMuc
    {
        [BsonElement("Id")]
        public int Id { get; set; }
        public string TenDanhMuc { get; set; }
        public string BiDanh { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SanPhamThuongHieu
    {
        [BsonElement("Id")]
        public int Id { get; set; }
        public string TenThuongHieu { get; set; }
        public string BiDanh { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SanPhamHinhAnh
    {
        public string HinhAnh { get; set; }
        public bool LaAnhMacDinh { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SanPhamThongSo
    {
        public string TenThongSo { get; set; }
        public string GiaTri { get; set; }
        public int ThuTu { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SanPhamCacCo
    {
        public bool LaSanPhamTrangChu { get; set; }
        public bool LaSanPhamKhuyenMai { get; set; }
        public bool LaSanPhamNoiBat { get; set; }
        public bool LaSanPhamHot { get; set; }
        public bool LaSanPhamMoi { get; set; }
        public bool LaSanPhamBanChay { get; set; }
        public bool HienThi { get; set; } = true;
    }

    [BsonIgnoreExtraElements]
    public class DonHangKhachHang
    {
        public int? TaiKhoanId { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class DonHangSanPham
    {
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public decimal Gia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class DonHangPhuongThuc
    {
        [BsonElement("Id")]
        public int Id { get; set; }
        public string TenPhuongThuc { get; set; }
        public decimal? PhiVanChuyen { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TaiKhoanQuyen
    {
        [BsonElement("Id")]
        public int Id { get; set; }
        public string TenQuyen { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TaiKhoanYeuThich
    {
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayYeuThich { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class BaoHanhSanPham
    {
        [BsonElement("Id")]
        public int Id { get; set; }
        public string TenSanPham { get; set; }
        public string MaSanPham { get; set; }
        public string HinhAnh { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class BaoHanhKhachHang
    {
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class LichSuSuaChuaItem
    {
        public string MaPhieu { get; set; }
        public string TrieuChungLoi { get; set; }
        public string TinhTrangTiepNhan { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTiepNhan { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayHenTra { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTraThucTe { get; set; }
        public int TrangThai { get; set; } = 1;
        public decimal? ChiPhiPhatSinh { get; set; }
        public string GiaiPhapXuLy { get; set; }
        public string LinhKienThayThe { get; set; }
        public string NguoiTiepNhan { get; set; }
        public string KyThuatVien { get; set; }
        public string GhiChu { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TinTucDanhMuc
    {
        [BsonElement("Id")]
        public int Id { get; set; }
        public string TenDanhMuc { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TinTucSEO
    {
        public string TieuDeSeo { get; set; }
        public string MoTaSeo { get; set; }
        public string TuKhoaSeo { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class DanhMucSEO
    {
        public string TieuDeSeo { get; set; }
        public string MoTaSeo { get; set; }
        public string TuKhoaSeo { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class DanhGiaSanPham
    {
        [BsonElement("Id")]
        public int Id { get; set; }
        public string TenSanPham { get; set; }
    }
    #endregion

    #region Main Entities
    [BsonIgnoreExtraElements]
    public class tb_SanPham
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenSanPham { get; set; }
        public string BiDanh { get; set; }
        public string MaSanPham { get; set; }
        public string MoTa { get; set; }
        public string ChiTiet { get; set; }

        [BsonElement("HinhAnhDaiDien")]
        public string HinhAnh { get; set; }

        [BsonIgnore]
        public string HinhAnhDaiDien { get => HinhAnh; set => HinhAnh = value; }

        public decimal? Gia { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public int? SoLuong { get; set; }
        public int? LuotXem { get; set; }
        public int? ThoiGianBaoHanh { get; set; }

        public SanPhamDanhMuc DanhMuc { get; set; }
        public SanPhamThuongHieu ThuongHieu { get; set; }
        public List<SanPhamHinhAnh> DanhSachHinhAnh { get; set; } = new List<SanPhamHinhAnh>();
        public List<SanPhamThongSo> ThongSoKyThuat { get; set; } = new List<SanPhamThongSo>();
        public Dictionary<string, object> ThongSoChiTiet { get; set; } = new Dictionary<string, object>();
        public SanPhamCacCo CacCo { get; set; } = new SanPhamCacCo();

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }

        // Backward-compatibility properties
        [BsonIgnore]
        public int DanhMucId
        {
            get => DanhMuc?.Id ?? 0;
            set { if (DanhMuc == null) DanhMuc = new SanPhamDanhMuc(); DanhMuc.Id = value; }
        }

        [BsonIgnore]
        public int? ThuongHieuId
        {
            get => ThuongHieu?.Id;
            set { if (value.HasValue) { if (ThuongHieu == null) ThuongHieu = new SanPhamThuongHieu(); ThuongHieu.Id = value.Value; } else { ThuongHieu = null; } }
        }

        [BsonIgnore]
        public bool? LaSanPhamTrangChu { get => CacCo?.LaSanPhamTrangChu; set { EnsureCacCo(); CacCo.LaSanPhamTrangChu = value ?? false; } }
        [BsonIgnore]
        public bool? LaSanPhamKhuyenMai { get => CacCo?.LaSanPhamKhuyenMai; set { EnsureCacCo(); CacCo.LaSanPhamKhuyenMai = value ?? false; } }
        [BsonIgnore]
        public bool? LaSanPhamNoiBat { get => CacCo?.LaSanPhamNoiBat; set { EnsureCacCo(); CacCo.LaSanPhamNoiBat = value ?? false; } }
        [BsonIgnore]
        public bool? LaSanPhamHot { get => CacCo?.LaSanPhamHot; set { EnsureCacCo(); CacCo.LaSanPhamHot = value ?? false; } }
        [BsonIgnore]
        public bool? LaSanPhamMoi { get => CacCo?.LaSanPhamMoi; set { EnsureCacCo(); CacCo.LaSanPhamMoi = value ?? false; } }
        [BsonIgnore]
        public bool? LaSanPhamBanChay { get => CacCo?.LaSanPhamBanChay; set { EnsureCacCo(); CacCo.LaSanPhamBanChay = value ?? false; } }
        [BsonIgnore]
        public bool? HienThi { get => CacCo?.HienThi; set { EnsureCacCo(); CacCo.HienThi = value ?? false; } }

        public void EnsureCacCo() { if (CacCo == null) CacCo = new SanPhamCacCo(); }

        [BsonIgnore]
        public tb_DanhMuc tb_DanhMuc
        {
            get => DanhMuc != null ? new tb_DanhMuc { Id = DanhMuc.Id, TenDanhMuc = DanhMuc.TenDanhMuc, BiDanh = DanhMuc.BiDanh } : new tb_DanhMuc { TenDanhMuc = "" };
            set { if (value != null) DanhMuc = new SanPhamDanhMuc { Id = value.Id, TenDanhMuc = value.TenDanhMuc, BiDanh = value.BiDanh }; }
        }

        [BsonIgnore]
        public tb_ThuongHieu tb_ThuongHieu
        {
            get => ThuongHieu != null ? new tb_ThuongHieu { Id = ThuongHieu.Id, TenThuongHieu = ThuongHieu.TenThuongHieu, BiDanh = ThuongHieu.BiDanh } : new tb_ThuongHieu { TenThuongHieu = "" };
            set { if (value != null) ThuongHieu = new SanPhamThuongHieu { Id = value.Id, TenThuongHieu = value.TenThuongHieu, BiDanh = value.BiDanh }; }
        }

        [BsonIgnore]
        public ICollection<tb_ThongSo> tb_ThongSo
        {
            get => ThongSoKyThuat?.Select((t, i) => new tb_ThongSo { Id = i + 1, SanPhamId = Id, TenThongSo = t.TenThongSo, GiaTri = t.GiaTri, ThuTu = t.ThuTu }).ToList() ?? new List<tb_ThongSo>();
            set { if (value != null) ThongSoKyThuat = value.Select(t => new SanPhamThongSo { TenThongSo = t.TenThongSo, GiaTri = t.GiaTri, ThuTu = t.ThuTu ?? 0 }).ToList(); }
        }

        [BsonIgnore]
        public ICollection<tb_AnhSanPham> tb_AnhSanPham
        {
            get => DanhSachHinhAnh?.Select((a, i) => new tb_AnhSanPham { Id = i + 1, SanPhamId = Id, HinhAnh = a.HinhAnh, LaAnhMacDinh = a.LaAnhMacDinh }).ToList() ?? new List<tb_AnhSanPham>();
            set { if (value != null) DanhSachHinhAnh = value.Select(a => new SanPhamHinhAnh { HinhAnh = a.HinhAnh, LaAnhMacDinh = a.LaAnhMacDinh ?? false }).ToList(); }
        }

        [BsonIgnore] public ICollection<tb_DanhGia> tb_DanhGia { get; set; } = new List<tb_DanhGia>();
        [BsonIgnore] public ICollection<tb_ChiTietDonHang> tb_ChiTietDonHang { get; set; } = new List<tb_ChiTietDonHang>();
    }

    [BsonIgnoreExtraElements]
    public class tb_TaiKhoan
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenDangNhap { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string MatKhau { get; set; }
        public string SoDienThoai { get; set; }
        [BsonIgnore] public string SDT { get => SoDienThoai; set => SoDienThoai = value; }
        public string DiaChi { get; set; }
        public int? VaiTro { get; set; }
        public bool? LaQuanTri { get; set; }
        public bool? HoatDong { get; set; }
        public bool? HienThi { get; set; }
        public string Avatar { get; set; }

        public TaiKhoanQuyen Quyen { get; set; }
        public List<TaiKhoanYeuThich> DanhSachYeuThich { get; set; } = new List<TaiKhoanYeuThich>();

        [BsonIgnore]
        public int? QuyenId
        {
            get => Quyen?.Id;
            set { if (value.HasValue) { if (Quyen == null) Quyen = new TaiKhoanQuyen(); Quyen.Id = value.Value; } else { Quyen = null; } }
        }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_DanhMuc
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenDanhMuc { get; set; }
        public string BiDanh { get; set; }
        public string Icon { get; set; }
        public string MoTa { get; set; }
        public DanhMucSEO SEO { get; set; }
        public int? ThuTu { get; set; }
        public bool? HienThi { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }

        [BsonIgnore] public string TieuDeSeo { get => SEO?.TieuDeSeo; set { EnsureSeo(); SEO.TieuDeSeo = value; } }
        [BsonIgnore] public string MoTaSeo { get => SEO?.MoTaSeo; set { EnsureSeo(); SEO.MoTaSeo = value; } }
        [BsonIgnore] public string TuKhoaSeo { get => SEO?.TuKhoaSeo; set { EnsureSeo(); SEO.TuKhoaSeo = value; } }
        private void EnsureSeo() { if (SEO == null) SEO = new DanhMucSEO(); }

        [BsonIgnore] public ICollection<tb_SanPham> tb_SanPham { get; set; } = new List<tb_SanPham>();
    }

    [BsonIgnoreExtraElements]
    public class tb_ThuongHieu
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenThuongHieu { get; set; }
        public string BiDanh { get; set; }
        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
        public int? ThuTu { get; set; }
        public bool? HienThi { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }

        [BsonIgnore] public ICollection<tb_SanPham> tb_SanPham { get; set; } = new List<tb_SanPham>();
    }

    [BsonIgnoreExtraElements]
    public class tb_DonHang
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string MaDonHang { get; set; }
        public DonHangKhachHang KhachHang { get; set; } = new DonHangKhachHang();
        public List<DonHangSanPham> DanhSachSanPham { get; set; } = new List<DonHangSanPham>();
        public decimal? TongTien { get; set; }
        public int? SoLuongSanPham { get; set; }
        public DonHangPhuongThuc PhuongThucThanhToan { get; set; }
        public DonHangPhuongThuc PhuongThucVanChuyen { get; set; }
        [BsonSerializer(typeof(FlexibleStringOrDocumentSerializer))]
        public string MaGiamGia { get; set; }
        public int? TrangThai { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }

        // Backward compatibility
        [BsonIgnore] public string TenKhachHang { get => KhachHang?.TenKhachHang; set { EnsureKhachHang(); KhachHang.TenKhachHang = value; } }
        [BsonIgnore] public string Email { get => KhachHang?.Email; set { EnsureKhachHang(); KhachHang.Email = value; } }
        [BsonIgnore] public string SoDienThoai { get => KhachHang?.SoDienThoai; set { EnsureKhachHang(); KhachHang.SoDienThoai = value; } }
        [BsonIgnore] public string SDT { get => SoDienThoai; set => SoDienThoai = value; }
        [BsonIgnore] public string DiaChi { get => KhachHang?.DiaChi; set { EnsureKhachHang(); KhachHang.DiaChi = value; } }
        [BsonIgnore] public int? TaiKhoanId { get => KhachHang?.TaiKhoanId; set { EnsureKhachHang(); KhachHang.TaiKhoanId = value; } }
        private void EnsureKhachHang() { if (KhachHang == null) KhachHang = new DonHangKhachHang(); }

        [BsonIgnore] public decimal? PhiVanChuyen { get => PhuongThucVanChuyen?.PhiVanChuyen; set { EnsurePtv(); PhuongThucVanChuyen.PhiVanChuyen = value; } }
        [BsonIgnore] public decimal? GiamGia { get; set; }
        [BsonIgnore] public int? PhuongThucThanhToanId { get => PhuongThucThanhToan?.Id; set { if (value.HasValue) { EnsurePtt(); PhuongThucThanhToan.Id = value.Value; } } }
        [BsonIgnore] public int? PhuongThucVanChuyenId { get => PhuongThucVanChuyen?.Id; set { if (value.HasValue) { EnsurePtv(); PhuongThucVanChuyen.Id = value.Value; } } }
        [BsonIgnore] public int? MaGiamGiaId { get; set; }
        [BsonIgnore] public string LoaiThanhToan { get => PhuongThucThanhToan?.TenPhuongThuc; set { EnsurePtt(); PhuongThucThanhToan.TenPhuongThuc = value; } }
        [BsonIgnore] public string GhiChu { get; set; }
        [BsonIgnore] public string MaGiaoDich { get; set; }

        private void EnsurePtt() { if (PhuongThucThanhToan == null) PhuongThucThanhToan = new DonHangPhuongThuc(); }
        private void EnsurePtv() { if (PhuongThucVanChuyen == null) PhuongThucVanChuyen = new DonHangPhuongThuc(); }

        [BsonIgnore] public tb_PhuongThucThanhToan tb_PhuongThucThanhToan => PhuongThucThanhToan != null ? new tb_PhuongThucThanhToan { Id = PhuongThucThanhToan.Id, TenPhuongThuc = PhuongThucThanhToan.TenPhuongThuc } : null;
        [BsonIgnore] public tb_PhuongThucVanChuyen tb_PhuongThucVanChuyen => PhuongThucVanChuyen != null ? new tb_PhuongThucVanChuyen { Id = PhuongThucVanChuyen.Id, TenPhuongThuc = PhuongThucVanChuyen.TenPhuongThuc, PhiVanChuyen = PhuongThucVanChuyen.PhiVanChuyen } : null;
        [BsonIgnore] public tb_MaGiamGia tb_MaGiamGia { get; set; }

        [BsonIgnore]
        public ICollection<tb_ChiTietDonHang> tb_ChiTietDonHang
        {
            get => DanhSachSanPham?.Select((p, i) => new tb_ChiTietDonHang {
                Id = i + 1,
                DonHangId = Id,
                SanPhamId = p.SanPhamId,
                TenSanPham = p.TenSanPham,
                HinhAnh = p.HinhAnh,
                Gia = p.Gia,
                SoLuong = p.SoLuong,
                ThanhTien = p.ThanhTien > 0 ? p.ThanhTien : (p.SoLuong * p.Gia),
                tb_SanPham = new tb_SanPham { Id = p.SanPhamId, TenSanPham = p.TenSanPham, HinhAnh = p.HinhAnh }
            }).ToList() ?? new List<tb_ChiTietDonHang>();
            set { if (value != null) DanhSachSanPham = value.Select(p => new DonHangSanPham { SanPhamId = p.SanPhamId ?? 0, TenSanPham = p.TenSanPham, HinhAnh = p.HinhAnh, Gia = p.Gia ?? 0, SoLuong = p.SoLuong ?? 0, ThanhTien = p.ThanhTien ?? ((p.SoLuong ?? 0) * (p.Gia ?? 0)) }).ToList(); }
        }
    }

    [BsonIgnoreExtraElements]
    public class tb_ChiTietDonHang
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public int? DonHangId { get; set; }
        public int? SanPhamId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public decimal? Gia { get; set; }
        public int? SoLuong { get; set; }
        public decimal? ThanhTien { get; set; }

        private tb_SanPham _sanPham;
        [BsonIgnore]
        public tb_SanPham tb_SanPham
        {
            get => _sanPham ?? (_sanPham = new tb_SanPham { Id = SanPhamId ?? 0, TenSanPham = TenSanPham, HinhAnh = HinhAnh });
            set => _sanPham = value;
        }
    }

    [BsonIgnoreExtraElements]
    public class tb_AnhSanPham
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public int? SanPhamId { get; set; }
        public string HinhAnh { get; set; }
        public int? ThuTu { get; set; }
        public bool? HienThi { get; set; }
        public bool? LaAnhMacDinh { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_ThongSo
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public int? SanPhamId { get; set; }
        public string TenThongSo { get; set; }
        public string GiaTri { get; set; }
        public int? ThuTu { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_DanhGia
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public DanhGiaSanPham SanPham { get; set; }
        [BsonIgnore]
        public int? SanPhamId
        {
            get => SanPham?.Id;
            set { if (value.HasValue) { if (SanPham == null) SanPham = new DanhGiaSanPham(); SanPham.Id = value.Value; } else { SanPham = null; } }
        }

        public int? TaiKhoanId { get; set; }
        public string TenNguoiGui { get; set; }
        public string TenNguoiDung { get => TenNguoiGui; set => TenNguoiGui = value; }
        public string Email { get; set; }
        public string NoiDung { get; set; }
        public int? Sao { get; set; }
        public int? DiemDanhGia { get => Sao; set => Sao = value; }
        public bool? HienThi { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_YeuThich
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public int? TaiKhoanId { get; set; }
        public int? SanPhamId { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_MaGiamGia
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string MaCode { get; set; }
        public string TenMa { get => MaCode; set => MaCode = value; }
        public int? LoaiGiam { get; set; }
        public decimal? GiaTriGiam { get; set; }
        public decimal? PhanTramGiam { get; set; }
        public decimal? SoTienGiam { get; set; }
        public decimal? GiaTriToiThieu { get; set; }
        public decimal? GiamToiDa { get; set; }
        public int? SoLuong { get; set; }
        public int? DaSuDung { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayBatDau { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayKetThuc { get; set; }
        public bool? HienThi { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_TinTuc
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TieuDe { get; set; }
        public string BiDanh { get; set; }
        public TinTucDanhMuc DanhMucTin { get; set; }
        public string MoTa { get; set; }
        public string ChiTiet { get; set; }
        public string NoiDung { get => ChiTiet; set => ChiTiet = value; }
        public string HinhAnh { get; set; }
        public TinTucSEO SEO { get; set; }
        public bool? HienThi { get; set; }
        public int? LuotXem { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }
        public int? TaiKhoanId { get; set; }

        [BsonIgnore]
        public int? DanhMucTinId
        {
            get => DanhMucTin?.Id;
            set { if (value.HasValue) { if (DanhMucTin == null) DanhMucTin = new TinTucDanhMuc(); DanhMucTin.Id = value.Value; } else { DanhMucTin = null; } }
        }

        [BsonIgnore] public string TieuDeSeo { get => SEO?.TieuDeSeo; set { EnsureSeo(); SEO.TieuDeSeo = value; } }
        [BsonIgnore] public string MoTaSeo { get => SEO?.MoTaSeo; set { EnsureSeo(); SEO.MoTaSeo = value; } }
        [BsonIgnore] public string TuKhoaSeo { get => SEO?.TuKhoaSeo; set { EnsureSeo(); SEO.TuKhoaSeo = value; } }
        private void EnsureSeo() { if (SEO == null) SEO = new TinTucSEO(); }

        [BsonIgnore] public tb_DanhMucTin tb_DanhMucTin => DanhMucTin != null ? new tb_DanhMucTin { Id = DanhMucTin.Id, TenDanhMuc = DanhMucTin.TenDanhMuc } : null;
    }

    [BsonIgnoreExtraElements]
    public class tb_DanhMucTin
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenDanhMuc { get; set; }
        public string BiDanh { get; set; }
        public string MoTa { get; set; }
        public int? ThuTu { get; set; }
        public bool? HienThi { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_QuangCao
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TieuDe { get; set; }
        public string HinhAnh { get; set; }
        public string LienKet { get; set; }
        [BsonIgnore] public string Link { get => LienKet; set => LienKet = value; }
        [BsonIgnore] public string DuongDan { get => LienKet; set => LienKet = value; }
        public string MoTa { get; set; }
        public int? ViTri { get; set; }
        public int? ThuTu { get; set; }
        public bool? HienThi { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_Menu
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        [BsonElement("TieuDe")]
        public string TenMenu { get; set; }
        [BsonIgnore] public string TieuDe { get => TenMenu; set => TenMenu = value; }

        [BsonElement("LienKet")]
        public string DuongDan { get; set; }
        [BsonIgnore] public string LienKet { get => DuongDan; set => DuongDan = value; }

        public string MoTa { get; set; }
        public int? MenuChaId { get; set; }
        public int? ThuTu { get; set; }
        public bool? HienThi { get; set; }
        public string Icon { get; set; }
        public int? ViTri { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_Quyen
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenQuyen { get; set; }
        public string MoTa { get; set; }
        public bool? HienThi { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_CauHinh
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string MaCauHinh { get; set; }
        public string TenCauHinh { get; set; }
        public string GiaTri { get; set; }
        public string MoTa { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_LienHe
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenNguoiGui { get; set; }
        [BsonIgnore] public string HoTen { get => TenNguoiGui; set => TenNguoiGui = value; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string NoiDung { get; set; }
        public bool? DaDoc { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }

        [BsonIgnore] public string SoDienThoai { get; set; }
        [BsonIgnore] public string SDT { get => SoDienThoai; set => SoDienThoai = value; }
        [BsonIgnore] public string TieuDe { get; set; }
        [BsonIgnore] public DateTime? NgayGui { get => NgayTao; set => NgayTao = value; }
    }

    [BsonIgnoreExtraElements]
    public class tb_PhuongThucThanhToan
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenPhuongThuc { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public bool? HienThi { get; set; }
        public int? ThuTu { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_PhuongThucVanChuyen
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string TenPhuongThuc { get; set; }
        public string MoTa { get; set; }
        public decimal? PhiVanChuyen { get; set; }
        public bool? HienThi { get; set; }
        public int? ThuTu { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_DangKyNhanTin
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string Email { get; set; }
        public bool? HienThi { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        [BsonIgnore] public DateTime? NgayDangKy { get => NgayTao; set => NgayTao = value; }
    }

    [BsonIgnoreExtraElements]
    public class BaoHanhThongTinBoSung
    {
        public string TenTruong { get; set; }
        [BsonIgnore]
        public string TieuDe { get => TenTruong; set => TenTruong = value; }
        public string GiaTri { get; set; }
        public string GhiChu { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_BaoHanh
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string MaBaoHanh { get; set; }
        public BaoHanhSanPham SanPham { get; set; }
        public int? DonHangId { get; set; }
        public int? TaiKhoanId { get; set; }
        public BaoHanhKhachHang KhachHang { get; set; } = new BaoHanhKhachHang();
        public string MaSeri { get; set; }

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayKichHoat { get; set; }
        public int? ThoiHanBaoHanh { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayHetHan { get; set; }
        public int? TrangThai { get; set; }
        public string GhiChu { get; set; }
        public List<LichSuSuaChuaItem> LichSuSuaChua { get; set; } = new List<LichSuSuaChuaItem>();
        public List<BaoHanhThongTinBoSung> ThongTinBoSung { get; set; } = new List<BaoHanhThongTinBoSung>();

        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }

        // Backward compatibility
        [BsonIgnore]
        public int? SanPhamId
        {
            get => SanPham?.Id;
            set { if (value.HasValue) { if (SanPham == null) SanPham = new BaoHanhSanPham(); SanPham.Id = value.Value; } else { SanPham = null; } }
        }

        [BsonIgnore] public string TenKhachHang { get => KhachHang?.TenKhachHang; set { EnsureKhachHang(); KhachHang.TenKhachHang = value; } }
        [BsonIgnore] public string SoDienThoai { get => KhachHang?.SoDienThoai; set { EnsureKhachHang(); KhachHang.SoDienThoai = value; } }
        [BsonIgnore] public string Email { get => KhachHang?.Email; set { EnsureKhachHang(); KhachHang.Email = value; } }
        [BsonIgnore] public string DiaChi { get => KhachHang?.DiaChi; set { EnsureKhachHang(); KhachHang.DiaChi = value; } }
        private void EnsureKhachHang() { if (KhachHang == null) KhachHang = new BaoHanhKhachHang(); }

        [BsonIgnore] public string SerialNumber { get => MaSeri; set => MaSeri = value; }
        [BsonIgnore] public DateTime? NgayMua { get => NgayKichHoat; set => NgayKichHoat = value; }
        [BsonIgnore] public DateTime? NgayHetBaoHanh { get => NgayHetHan; set => NgayHetHan = value; }

        [BsonIgnore] public tb_SanPham tb_SanPham { get; set; }
        [BsonIgnore] public tb_DonHang tb_DonHang { get; set; }

        [BsonIgnore]
        public ICollection<tb_PhieuSuaChua> tb_PhieuSuaChua
        {
            get => LichSuSuaChua?.Select((s, i) => new tb_PhieuSuaChua { Id = i + 1, BaoHanhId = Id, MaPhieu = s.MaPhieu, TrieuChungLoi = s.TrieuChungLoi, TinhTrangTiepNhan = s.TinhTrangTiepNhan, NgayTiepNhan = s.NgayTiepNhan, NgayHenTra = s.NgayHenTra, NgayTraThucTe = s.NgayTraThucTe, TrangThai = s.TrangThai, ChiPhiPhatSinh = s.ChiPhiPhatSinh, GiaiPhapXuLy = s.GiaiPhapXuLy, LinhKienThayThe = s.LinhKienThayThe, NguoiTiepNhan = s.NguoiTiepNhan, KyThuatVien = s.KyThuatVien, GhiChu = s.GhiChu }).ToList() ?? new List<tb_PhieuSuaChua>();
        }
    }

    [BsonIgnoreExtraElements]
    public class tb_PhieuSuaChua
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string MaPhieu { get; set; }
        public int? BaoHanhId { get; set; }
        public string TrieuChungLoi { get; set; }
        public string LyDoSua { get; set; }
        public string TinhTrangTiepNhan { get; set; }
        public string KetQua { get; set; }
        public string GiaiPhapXuLy { get; set; }
        public string LinhKienThayThe { get; set; }
        public decimal? ChiPhi { get; set; }
        public decimal? ChiPhiPhatSinh { get; set; }
        public int? TrangThai { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTiepNhan { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayHenTra { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTraThucTe { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTraHang { get; set; }
        public string KyThuatVien { get; set; }
        public string NguoiTiepNhan { get; set; }
        public string GhiChu { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayTao { get; set; }
        public string NguoiTao { get; set; }
        [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
        public DateTime? NgayCapNhat { get; set; }
        public string NguoiCapNhat { get; set; }

        [BsonIgnore] public tb_BaoHanh tb_BaoHanh { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class tb_Setting
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId InternalId { get; set; }

        [BsonElement("SqlId")]
        public int Id { get; set; }

        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string SettingName { get; set; }
        public string SettingDescription { get; set; }
    }
    #endregion
}

