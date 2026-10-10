// ==========================================
// PHẦN 1: 15 CÂU TRUY VẤN CƠ BẢN (BASIC QUERIES)
// ==========================================

// 1. Lấy tất cả sản phẩm đang được hiển thị
db.tb_SanPham.find({ "CacCo.HienThi": true });

// 2. Tìm kiếm sản phẩm theo tên (Gần đúng/Like query)
db.tb_SanPham.find({ "TenSanPham": { $regex: /iphone/i } });

// 3. Lấy tất cả các tài khoản đang hoạt động (Active)
db.tb_TaiKhoan.find({ "HoatDong": true });

// 4. Lọc các đơn hàng có tổng tiền lớn hơn 10.000.000 VNĐ
db.tb_DonHang.find({ "TongTien": { $gt: 10000000 } });

// 5. Lấy danh sách tin tức thuộc một danh mục cụ thể (vd: Id danh mục = 1)
db.tb_TinTuc.find({ "DanhMucTin.Id": 1 }).sort({ "NgayTao": -1 });

// 6. Tìm sản phẩm đang có giá khuyến mãi nhỏ hơn giá gốc (sử dụng $expr để so sánh 2 trường)
db.tb_SanPham.find({ 
    "SoLuong": { $gt: 0 }, 
    $or: [
        { $expr: { $lt: ["$GiaKhuyenMai", "$Gia"] } },
        { "CacCo.LaSanPhamKhuyenMai": true }
    ]
});

// 7. Tìm các tài khoản đang hoạt động và có ít nhất 1 sản phẩm trong danh sách yêu thích
db.tb_TaiKhoan.find({ 
    "DanhSachYeuThich": { $exists: true, $not: { $size: 0 } },
    "HoatDong": true 
});

// 8. Lọc đơn hàng được tạo trong 7 ngày gần nhất và có trạng thái là 1, 2 hoặc 3
db.tb_DonHang.find({
    "NgayTao": { 
        $gte: new Date(new Date().setDate(new Date().getDate() - 7)) 
    },
    "TrangThai": { $in: [1, 2, 3] }
});

// 9. Tìm mã giảm giá còn hiệu lực (Ngày bắt đầu <= hiện tại <= Ngày kết thúc, và chưa dùng hết)
db.tb_MaGiamGia.find({
    $expr: { $lt: ["$DaSuDung", "$SoLuong"] },
    "NgayBatDau": { $lte: new Date() },
    "NgayKetThuc": { $gte: new Date() }
});

// 10. Lọc tin tức thuộc nhiều danh mục (1, 2, 3), lấy các trường cụ thể và sắp xếp theo lượt xem
db.tb_TinTuc.find(
    { "DanhMucTin.Id": { $in: [1, 2, 3] }, "HienThi": true },
    { TieuDe: 1, LuotXem: 1, "DanhMucTin.TenDanhMuc": 1 }
).sort({ LuotXem: -1 }).limit(10);

// 11. Sử dụng $elemMatch: Tìm đơn hàng mua sản phẩm có Id = 15 với số lượng từ 2 trở lên
db.tb_DonHang.find({
    "DanhSachSanPham": {
        $elemMatch: { "SanPhamId": 15, "SoLuong": { $gte: 2 } }
    }
});

// 12. Kiểm tra Type & Exists: Tìm các sản phẩm bị thiếu hoặc trống thông số kỹ thuật, hoặc mô tả rỗng
db.tb_SanPham.find({
    $or: [
        { "ThongSoKyThuat": { $exists: false } },
        { "ThongSoKyThuat": { $size: 0 } },
        { "MoTa": null },
        { "MoTa": "" }
    ]
});

// 13. Update nhiều Documents: Thêm thẻ "Sản phẩm Hot" cho tất cả sản phẩm của thương hiệu Id = 2
db.tb_SanPham.updateMany(
    { "ThuongHieu.Id": 2 },
    { $set: { "CacCo.LaSanPhamHot": true }, $currentDate: { "NgayCapNhat": true } }
);

// 14. Update với $push: Thêm một thông số kỹ thuật mới vào mảng ThongSoKyThuat của sản phẩm Id = 10
db.tb_SanPham.updateOne(
    { "Id": 10 },
    { 
        $push: { 
            "ThongSoKyThuat": { 
                "TenThongSo": "Bảo hành", "GiaTri": "24 Tháng", "ThuTu": 10 
            } 
        } 
    }
);

// 15. Update với $inc: Tăng lượt xem của bài viết Id = 5 lên 1, đồng thời cập nhật người chỉnh sửa
db.tb_TinTuc.updateOne(
    { "Id": 5 },
    { $inc: { "LuotXem": 1 }, $set: { "NguoiCapNhat": "System" } }
);

// ==========================================
// PHẦN 2: 15 CÂU TRUY VẤN NÂNG CAO (ADVANCED QUERIES / AGGREGATIONS)
// ==========================================

// 16. Thống kê số lượng sản phẩm theo từng danh mục
db.tb_SanPham.aggregate([
    { 
        $group: { 
            _id: "$DanhMuc.TenDanhMuc", 
            SoLuongSanPham: { $sum: 1 } 
        } 
    },
    { 
        $sort: { SoLuongSanPham: -1 } 
    }
]);

// 17. Tính tổng doanh thu từ tất cả các đơn hàng đã hoàn thành (Trạng thái: 2)
db.tb_DonHang.aggregate([
    { 
        $match: { TrangThai: 2 } 
    },
    { 
        $group: { 
            _id: null, 
            TongDoanhThu: { $sum: "$TongTien" },
            SoDonHangThanhCong: { $sum: 1 }
        } 
    }
]);

// 18. Top 5 sản phẩm có lượt xem cao nhất
    db.tb_SanPham.aggregate([
        { 
            $sort: { LuotXem: -1 } 
        },
        { 
            $limit: 5 
        },
        { 
            $project: { 
                _id: 0, 
                TenSanPham: 1, 
                Gia: 1, 
                LuotXem: 1, 
                TenThuongHieu: "$ThuongHieu.TenThuongHieu" 
            } 
        }
    ]);

// 19. Thống kê khách hàng VIP (Top 10 khách hàng chi tiêu nhiều nhất)
db.tb_DonHang.aggregate([
    { 
        $match: { TrangThai: 2 } // Chỉ tính đơn thành công
    },
    { 
        $group: { 
            _id: "$KhachHang.SoDienThoai", 
            TenKhachHang: { $first: "$KhachHang.TenKhachHang" },
            TongTienDaMua: { $sum: "$TongTien" },
            SoDonHang: { $sum: 1 }
        }
    },
    { 
        $sort: { TongTienDaMua: -1 } 
    },
    {
        $limit: 10
    }
]);

// 20. Cập nhật hàng loạt: Tăng giá 5% cho tất cả sản phẩm của thương hiệu Id = 3
db.tb_SanPham.updateMany(
    { "ThuongHieu.Id": 3 },
    { $mul: { Gia: 1.05 } }
);

// 21. $lookup: Kết bảng Sản Phẩm với Đánh Giá, tính điểm sao trung bình và lọc SP có điểm >= 4
db.tb_SanPham.aggregate([
    {
        $lookup: {
            from: "tb_DanhGia",
            localField: "Id",
            foreignField: "SanPhamId",
            as: "DanhGia"
        }
    },
    {
        $addFields: {
            TrungBinhSao: { $avg: "$DanhGia.Sao" },
            TongSoDanhGia: { $size: "$DanhGia" }
        }
    },
    { $match: { TrungBinhSao: { $gte: 4 } } },
    { $sort: { TrungBinhSao: -1 } },
    { $project: { _id: 0, TenSanPham: 1, TrungBinhSao: 1, TongSoDanhGia: 1 } }
]);

// 22. $unwind & $group: Tìm Top 3 sản phẩm bán chạy nhất mọi thời đại dựa trên số lượng bán
db.tb_DonHang.aggregate([
    { $match: { TrangThai: 2 } }, // Chỉ lấy đơn hoàn thành
    { $unwind: "$DanhSachSanPham" }, // Tách mảng sản phẩm thành các documents riêng
    { 
        $group: {
            _id: "$DanhSachSanPham.SanPhamId",
            TenSanPham: { $first: "$DanhSachSanPham.TenSanPham" },
            TongSoLuongBan: { $sum: "$DanhSachSanPham.SoLuong" },
            DoanhThuTuSanPham: { $sum: "$DanhSachSanPham.ThanhTien" }
        }
    },
    { $sort: { TongSoLuongBan: -1 } },
    { $limit: 3 }
]);

// 23. $lookup lồng trong aggregate: Tìm khách hàng từng mua sản phẩm của thương hiệu "Apple" (Id = 1)
db.tb_DonHang.aggregate([
    { $match: { TrangThai: 2 } },
    { $unwind: "$DanhSachSanPham" },
    {
        $lookup: {
            from: "tb_SanPham",
            localField: "DanhSachSanPham.SanPhamId",
            foreignField: "Id",
            as: "ThongTinSanPham"
        }
    },
    { $unwind: "$ThongTinSanPham" },
    { $match: { "ThongTinSanPham.ThuongHieu.Id": 1 } },
    { 
        $group: {
            _id: "$KhachHang.TaiKhoanId",
            TenKhachHang: { $first: "$KhachHang.TenKhachHang" },
            SoDienThoai: { $first: "$KhachHang.SoDienThoai" },
            SoLuongAppleDaMua: { $sum: "$DanhSachSanPham.SoLuong" }
        }
    }
]);

// 24. $facet: Xuất Báo cáo Dashboard đa chiều (Tổng quan, Theo trạng thái, 5 Đơn gần nhất) trong 1 query
db.tb_DonHang.aggregate([
    {
        $facet: {
            "TongQuan": [
                { $match: { TrangThai: 2 } },
                { $group: { _id: null, TongDoanhThu: { $sum: "$TongTien" }, TongDonHang: { $sum: 1 } } }
            ],
            "TheoTrangThai": [
                { $group: { _id: "$TrangThai", SoLuong: { $sum: 1 } } }
            ],
            "DonHangGanDay": [
                { $sort: { NgayTao: -1 } },
                { $limit: 5 },
                { $project: { MaDonHang: 1, TongTien: 1, "KhachHang.TenKhachHang": 1, NgayTao: 1 } }
            ]
        }
    }
]);

// 25. Tìm khách hàng có giỏ hàng bị bỏ quên (Có SP trong danh sách yêu thích nhưng chưa từng đặt hàng)
db.tb_TaiKhoan.aggregate([
    { $match: { "DanhSachYeuThich.0": { $exists: true } } },
    {
        $lookup: {
            from: "tb_DonHang",
            localField: "Id",
            foreignField: "KhachHang.TaiKhoanId",
            as: "CacDonHang"
        }
    },
    { $match: { "CacDonHang": { $size: 0 } } }, // Lọc những người mảng đơn hàng bị trống
    { $project: { TenDangNhap: 1, HoTen: 1, Email: 1, SoLuongYeuThich: { $size: "$DanhSachYeuThich" } } }
]);

// 26. $bucket: Nhóm sản phẩm theo các khoảng giá để phân tích phổ giá tồn kho
db.tb_SanPham.aggregate([
    {
        $bucket: {
            groupBy: "$Gia",
            boundaries: [0, 5000000, 10000000, 20000000, 50000000],
            default: "Tren_50_Trieu",
            output: {
                SoLuongSanPham: { $sum: 1 },
                TrungBinhGia: { $avg: "$Gia" }
            }
        }
    }
]);

// 27. Báo cáo doanh thu theo Tháng - Năm (Trích xuất thời gian từ NgayTao)
db.tb_DonHang.aggregate([
    { $match: { TrangThai: 2, NgayTao: { $type: "date" } } },
    {
        $group: {
            _id: {
                Nam: { $year: "$NgayTao" },
                Thang: { $month: "$NgayTao" }
            },
            DoanhThu: { $sum: "$TongTien" },
            SoDon: { $sum: 1 }
        }
    },
    { $sort: { "_id.Nam": -1, "_id.Thang": -1 } }
]);

// 28. $pull: Xóa một sản phẩm cụ thể (Id = 99) ra khỏi danh sách yêu thích của TẤT CẢ khách hàng
db.tb_TaiKhoan.updateMany(
    { "DanhSachYeuThich.SanPhamId": 99 },
    { $pull: { DanhSachYeuThich: { SanPhamId: 99 } } }
);

// 29. arrayFilters: Cập nhật giá của một sản phẩm (Id = 10) bị sai bên TRONG mảng DanhSachSanPham của tất cả Đơn hàng đang chờ xử lý
db.tb_DonHang.updateMany(
    { "TrangThai": 1, "DanhSachSanPham.SanPhamId": 10 },
    { 
        $set: { "DanhSachSanPham.$[elem].Gia": 15000000 },
        $mul: { "DanhSachSanPham.$[elem].ThanhTien": 1.2 } 
    },
    { arrayFilters: [ { "elem.SanPhamId": 10 } ] }
);

// 30. Full-Text Search: Tìm kiếm sản phẩm theo văn bản và xếp hạng theo độ chính xác (Score)
// Yêu cầu: Phải chạy lệnh sau để tạo Text Index trước: 
// db.tb_SanPham.createIndex({ TenSanPham: "text", MoTa: "text" });
db.tb_SanPham.find(
    { $text: { $search: "laptop asus gaming" } },
    { score: { $meta: "textScore" }, TenSanPham: 1, Gia: 1 }
).sort({ score: { $meta: "textScore" } });
