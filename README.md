# 📦 DoAnNoSQL — Hệ Thống Bán Hàng Điện Tử (ASP.NET MVC + MongoDB)

> **Đồ án môn học NoSQL** — Xây dựng website bán hàng điện tử sử dụng ASP.NET MVC 5 kết hợp MongoDB (không dùng LINQ).

---

## 📋 Mục Lục

1. [Giới thiệu dự án](#1-giới-thiệu-dự-án)
2. [Yêu cầu hệ thống](#2-yêu-cầu-hệ-thống)
3. [Cấu trúc dự án](#3-cấu-trúc-dự-án)
4. [Cài đặt & Cấu hình](#4-cài-đặt--cấu-hình)
5. [Cơ sở dữ liệu MongoDB](#5-cơ-sở-dữ-liệu-mongodb)
6. [Chạy ứng dụng](#6-chạy-ứng-dụng)
7. [Hướng dẫn sử dụng — Khách hàng](#7-hướng-dẫn-sử-dụng--khách-hàng)
8. [Hướng dẫn sử dụng — Quản trị viên](#8-hướng-dẫn-sử-dụng--quản-trị-viên)
9. [API (Web-API Project)](#9-api-web-api-project)
10. [Các tính năng nổi bật](#10-các-tính-năng-nổi-bật)
11. [Công nghệ sử dụng](#11-công-nghệ-sử-dụng)
12. [Lưu ý & Khắc phục sự cố](#12-lưu-ý--khắc-phục-sự-cố)

---

## 1. Giới Thiệu Dự Án

**DoAnNoSQL** là website bán hàng điện tử (điện thoại, laptop, tablet, phụ kiện) được xây dựng theo mô hình MVC với:

- **Frontend (DoAnNoSQL):** Website bán hàng dành cho khách hàng và trang quản trị Admin.
- **Backend API (Web-API):** RESTful API phục vụ các tác vụ bổ trợ.
- **Database:** MongoDB (NoSQL) — lưu trữ toàn bộ dữ liệu dưới dạng document, không sử dụng LINQ hay Entity Framework.

### Các chức năng chính:

| Phân hệ | Chức năng |
|---------|-----------|
| **Khách hàng** | Duyệt sản phẩm, tìm kiếm, giỏ hàng, đặt hàng, thanh toán VNPay, theo dõi đơn hàng |
| **Tài khoản** | Đăng ký, đăng nhập, quên mật khẩu, danh sách yêu thích, lịch sử đơn hàng |
| **Chatbot** | Hỗ trợ tư vấn sản phẩm tự động |
| **Admin** | Quản lý sản phẩm, danh mục, đơn hàng, tài khoản, bảo hành, tin tức, khuyến mãi, thống kê |

---

## 2. Yêu Cầu Hệ Thống

| Thành phần | Phiên bản tối thiểu |
|-----------|---------------------|
| **Windows** | Windows 10 / Windows Server 2016+ |
| **Visual Studio** | 2019 hoặc 2022 (bất kỳ edition) |
| **.NET Framework** | 4.7.2 |
| **MongoDB** | 6.0+ |
| **MongoDB Compass** | (tùy chọn, để xem dữ liệu trực quan) |
| **IIS / IIS Express** | Tích hợp sẵn trong Visual Studio |

---

## 3. Cấu Trúc Dự Án

```
DoAnNoSQL/
│
├── DoAnNoSQL/                  ← Project chính (Website MVC)
│   ├── Controllers/            ← Xử lý logic
│   │   ├── HomeController.cs
│   │   ├── AccountController.cs
│   │   ├── AdminController.*.cs
│   │   ├── SanPhamController.cs
│   │   ├── GioHangController.cs
│   │   ├── ThanhToanController.cs
│   │   ├── PaymentController.cs    ← VNPay integration
│   │   ├── ChatbotController.cs    ← AI Chatbot
│   │   └── ...
│   │
│   ├── Models/
│   │   ├── MongoEntities.cs    ← 14 Collection entities
│   │   ├── MongoDbContext.cs   ← MongoDB context (thay thế EF DbContext)
│   │   └── *.ViewModel.cs      ← View models
│   │
│   ├── Views/                  ← Giao diện Razor (.cshtml)
│   │   ├── Home/
│   │   ├── Admin/
│   │   ├── SanPham/
│   │   ├── GioHang/
│   │   ├── ThanhToan/
│   │   └── Shared/
│   │
│   ├── Filters/                ← AdminAuthorizeAttribute
│   ├── Helpers/                ← SettingsHelper, ...
│   ├── Web.config              ← Cấu hình kết nối MongoDB
│   └── DoAnNoSQL.csproj
│
├── Web-API/                    ← Project API phụ trợ
│   └── Controllers/
│
├── DoAnNoSQL.sln               ← File Solution chính
└── README.md                   ← File này
```

---

## 4. Cài Đặt & Cấu Hình

### Bước 1: Clone / Tải dự án

```bash
git clone <repository-url>
# hoặc giải nén file ZIP vào thư mục mong muốn
```

### Bước 2: Cài đặt MongoDB

1. Tải MongoDB Community Server tại: https://www.mongodb.com/try/download/community
2. Cài đặt với cấu hình mặc định (port 27017)
3. Đảm bảo MongoDB service đang chạy:

```powershell
# Kiểm tra MongoDB service
Get-Service -Name MongoDB
# Khởi động nếu chưa chạy
Start-Service -Name MongoDB
```

### Bước 3: Cấu hình kết nối MongoDB

Mở file `DoAnNoSQL\Web.config`, tìm và cập nhật phần `connectionStrings`:

```xml
<connectionStrings>
  <!-- Địa chỉ MongoDB server -->
  <add name="MongoDB" connectionString="mongodb://localhost:27017" />
  <!-- Tên database -->
  <add name="MongoDatabaseName" connectionString="WebBanHangDienTu" />
</connectionStrings>
```

> **Lưu ý:** Mặc định database có tên `WebBanHangDienTu`. Thay đổi nếu cần.

### Bước 4: Import dữ liệu mẫu (nếu có)

Nếu dự án có sẵn file backup trong thư mục `DoAnNoSQL/Database_backup/`:

```bash
# Import từng collection (ví dụ)
mongorestore --db WebBanHangDienTu --collection SanPham "path\to\SanPham.bson"

# Hoặc restore toàn bộ database
mongorestore --db WebBanHangDienTu "path\to\Database_backup\"
```

Hoặc dùng **MongoDB Compass** → Import JSON/CSV từng collection.

### Bước 5: Cấu hình Email (Quên mật khẩu)

Trong `Web.config`, cập nhật thông tin Gmail SMTP:

```xml
<system.net>
  <mailSettings>
    <smtp from="your-email@gmail.com">
      <network host="smtp.gmail.com" port="587"
               userName="your-email@gmail.com"
               password="your-app-password"
               enableSsl="true" />
    </smtp>
  </mailSettings>
</system.net>
```

> **Lưu ý:** Dùng **App Password** của Google (không dùng mật khẩu tài khoản thường).  
> Vào: Google Account → Security → 2-Step Verification → App passwords.

### Bước 6: Cấu hình VNPay (Thanh toán online)

Thêm các key sau vào `<appSettings>` trong `Web.config`:

```xml
<appSettings>
  <add key="vnp_TmnCode" value="YOUR_TMN_CODE" />
  <add key="vnp_HashSecret" value="YOUR_HASH_SECRET" />
  <add key="vnp_Url" value="https://sandbox.vnpayment.vn/paymentv2/vpcpay.html" />
  <add key="vnp_Returnurl" value="https://localhost:PORT/Payment/VnpayReturn" />
</appSettings>
```

> Dùng môi trường **sandbox** khi phát triển/test.

### Bước 7: Mở Solution trong Visual Studio

1. Mở Visual Studio
2. **File → Open → Project/Solution**
3. Chọn file `DoAnNoSQL.sln`
4. Restore NuGet packages: **Tools → NuGet Package Manager → Restore**
5. Build solution: **Ctrl + Shift + B**

---

## 5. Cơ Sở Dữ Liệu MongoDB

Dự án sử dụng **14 collection chính** trong MongoDB:

| Collection | Mô tả |
|-----------|-------|
| `SanPham` | Sản phẩm (điện thoại, laptop, tablet, phụ kiện) |
| `DanhMuc` | Danh mục sản phẩm |
| `ThuongHieu` | Thương hiệu (Apple, Samsung, Dell,...) |
| `DonHang` | Đơn hàng của khách |
| `TaiKhoan` | Tài khoản người dùng & admin |
| `BaoHanh` | Phiếu bảo hành & lịch sử sửa chữa |
| `TinTuc` | Bài viết tin tức, blog |
| `QuangCao` | Banner quảng cáo trang chủ |
| `Menu` | Cấu hình menu điều hướng |
| `LienHe` | Tin nhắn liên hệ từ khách hàng |
| `MaGiamGia` | Mã giảm giá / voucher |
| `DanhGia` | Đánh giá sản phẩm |
| `DangKyNhanTin` | Đăng ký nhận email thông báo |
| `CauHinh` | Cấu hình hệ thống (tên shop, email,...) |

### Đặc điểm thiết kế (NoSQL — Không dùng LINQ):

- Dữ liệu được **embed** trong document thay vì join bảng.
- Truy vấn bằng **MongoDB.Driver Builders** (FilterDefinition, SortDefinition,...).
- Auto-increment ID được xử lý qua collection `Counters`.

---

## 6. Chạy Ứng Dụng

### Cách 1: Chạy từ Visual Studio (khuyến nghị)

1. Mở `DoAnNoSQL.sln`
2. Chuột phải vào project **DoAnNoSQL** → **Set as Startup Project**
3. Nhấn **F5** (Debug) hoặc **Ctrl + F5** (No Debug)
4. Trình duyệt tự mở tại `http://localhost:{PORT}/`

### Cách 2: Chạy cả 2 project đồng thời

1. Chuột phải vào Solution → **Properties**
2. Chọn **Multiple startup projects**
3. Đặt cả `DoAnNoSQL` và `Web-API` thành **Start**
4. Nhấn **F5**

### Kiểm tra kết nối MongoDB

```powershell
# Kiểm tra MongoDB đang lắng nghe port 27017
netstat -an | findstr 27017
```

---

## 7. Hướng Dẫn Sử Dụng — Khách Hàng

### 7.1. Trang Chủ

- **URL:** `http://localhost:{PORT}/`
- Hiển thị: Banner quảng cáo, sản phẩm mới, sản phẩm bán chạy, tin tức, thương hiệu.
- **Mega Menu:** Di chuột vào danh mục để xem thương hiệu và lọc theo mức giá.

### 7.2. Tìm Kiếm Sản Phẩm

- Nhập từ khóa vào ô tìm kiếm trên header → nhấn Enter.
- Trang kết quả cho phép lọc theo: Danh mục, Thương hiệu, Mức giá.
- **URL:** `/Search?q={từ_khóa}`

### 7.3. Xem Danh Sách Sản Phẩm

- Chọn danh mục từ menu (Điện thoại, Laptop, Tablet, Phụ kiện,...).
- Lọc theo thương hiệu và khoảng giá ở thanh sidebar.
- **URL:** `/SanPham?danhMucId={id}`

### 7.4. Xem Chi Tiết Sản Phẩm

- Click vào tên hoặc ảnh sản phẩm.
- Xem: Thông số kỹ thuật, hình ảnh gallery, mô tả chi tiết, đánh giá.
- Click **"Thêm vào giỏ hàng"** hoặc **"Mua ngay"**.

### 7.5. Giỏ Hàng

- **URL:** `/GioHang`
- Cập nhật số lượng hoặc xóa sản phẩm.
- Click **"Tiến hành thanh toán"** để đặt hàng.

### 7.6. Thanh Toán

- **URL:** `/ThanhToan`
- Điền thông tin nhận hàng: Họ tên, SĐT, địa chỉ, email.
- Chọn phương thức vận chuyển và thanh toán:
  - 💵 **COD** — Thanh toán khi nhận hàng
  - 💳 **VNPay** — Thanh toán online qua thẻ/ngân hàng
- Nhập mã giảm giá (nếu có) → Click **"Đặt hàng"**.

### 7.7. Theo Dõi Đơn Hàng

Đăng nhập → **Tài khoản → Đơn hàng của tôi**

| Mã trạng thái | Ý nghĩa |
|--------------|---------|
| `0` | Chờ xác nhận |
| `1` | Đã xác nhận |
| `2` | Đang giao hàng |
| `3` | Đã giao / Hoàn thành |
| `-1` | Đã hủy |

### 7.8. Tài Khoản

- **Đăng ký:** `/Account/Register`
- **Đăng nhập:** `/Account/Login`
- **Quên mật khẩu:** `/Account/ForgotPassword` → nhận link reset qua Gmail
- **Tài khoản cá nhân:** `/TaiKhoan` — cập nhật thông tin, xem lịch sử đơn hàng, sản phẩm yêu thích

### 7.9. Chatbot Hỗ Trợ

- Click vào biểu tượng chat ở góc dưới phải màn hình.
- Hỏi về: sản phẩm, giá, khuyến mãi, bảo hành, hướng dẫn đặt hàng.
- Chatbot tự động gợi ý sản phẩm phù hợp dựa trên yêu cầu.

---

## 8. Hướng Dẫn Sử Dụng — Quản Trị Viên

### 8.1. Đăng Nhập Admin

- **URL:** `/Admin/Login`
- Sử dụng tài khoản có quyền quản trị (`LaQuanTri = true` trong MongoDB).

### 8.2. Dashboard Tổng Quan

- **URL:** `/Admin/TongQuan`
- Hiển thị: Tổng đơn hàng, doanh thu, sản phẩm sắp hết, đơn hàng mới nhất.

### 8.3. Quản Lý Sản Phẩm — `/Admin/QLSP`

| Thao tác | Hướng dẫn |
|---------|-----------|
| **Thêm mới** | Click "Thêm sản phẩm" → điền thông tin → lưu |
| **Sửa** | Click icon bút ✏️ → cập nhật thông tin |
| **Xóa** | Click icon thùng rác 🗑️ → xác nhận |
| **Upload ảnh** | Kéo thả hoặc chọn file → đặt ảnh đại diện |
| **Flags sản phẩm** | Đánh dấu: Mới / Bán chạy / Nổi bật / Hot / Trang chủ / Khuyến mãi |

### 8.4. Quản Lý Danh Mục — `/Admin/DanhMuc`

- Thêm / sửa / xóa danh mục.
- Cấu hình: Icon, mô tả, SEO (tiêu đề, meta description, từ khóa), thứ tự hiển thị.

### 8.5. Quản Lý Thương Hiệu — `/Admin/ThuongHieu`

- Thêm / sửa / xóa thương hiệu.
- Upload logo thương hiệu.

### 8.6. Quản Lý Đơn Hàng — `/Admin/DonHang`

- Xem toàn bộ đơn hàng, lọc theo trạng thái.
- Cập nhật trạng thái: Xác nhận → Đang giao → Hoàn thành / Hủy.
- Xem chi tiết: thông tin khách, sản phẩm, tổng tiền, phương thức thanh toán.

### 8.7. Quản Lý Bảo Hành — `/Admin/BaoHanh`

- Tiếp nhận phiếu bảo hành từ khách hàng.
- Theo dõi lịch sử sửa chữa:
  - Triệu chứng lỗi, kỹ thuật viên xử lý
  - Ngày tiếp nhận, ngày hẹn trả, ngày trả thực tế
  - Chi phí phát sinh, linh kiện thay thế
- Cập nhật trạng thái phiếu (Tiếp nhận → Đang sửa → Hoàn thành).

### 8.8. Quản Lý Tài Khoản — `/Admin/TaiKhoan`

- Xem danh sách tài khoản khách hàng và quản trị viên.
- Kích hoạt / khóa tài khoản.
- Thêm tài khoản quản trị mới.

### 8.9. Quản Lý Khuyến Mãi — `/Admin/KhuyenMai`

Tạo mã giảm giá với các tùy chọn:
- **Loại giảm:** Theo % hoặc số tiền cố định.
- **Giá trị tối thiểu** để áp dụng mã.
- **Thời gian hiệu lực** (ngày bắt đầu → ngày kết thúc).
- **Giới hạn số lần** sử dụng.

### 8.10. Quản Lý Tin Tức — `/Admin/TinTuc`

- Viết / chỉnh sửa bài viết với trình soạn thảo văn bản phong phú.
- Phân loại theo danh mục tin.
- Cấu hình SEO cho từng bài viết.

### 8.11. Quản Lý Banner — `/Admin/Banner`

- Upload banner quảng cáo trang chủ.
- Cấu hình vị trí, thứ tự hiển thị, URL liên kết khi click.

### 8.12. Báo Cáo Thống Kê — `/Admin/BaoCaoThongKe`

- Doanh thu theo khoảng thời gian.
- Top sản phẩm bán chạy nhất.
- Thống kê đơn hàng theo trạng thái.

### 8.13. Cài Đặt Hệ Thống

- Cập nhật: Tên cửa hàng, email liên hệ, hotline, địa chỉ, mô tả.
- Cấu hình lưu trong collection `CauHinh` của MongoDB.

---

## 9. API (Web-API Project)

Project **Web-API** chạy song song, cung cấp các RESTful endpoints.

**Base URL:** `http://localhost:{API_PORT}/api/`

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/values` | GET | Kiểm tra API hoạt động |

> Các endpoint sẽ được mở rộng theo yêu cầu thực tế.

---

## 10. Các Tính Năng Nổi Bật

### 🔐 Bảo Mật
- Mật khẩu được **hash** trước khi lưu vào MongoDB.
- Trang Admin được bảo vệ bởi `[AdminAuthorize]` attribute.
- Reset mật khẩu qua email với **token có thời hạn**.
- Session-based authentication.

### 🤖 Chatbot AI
- Nhận diện intent từ tin nhắn người dùng (tìm sản phẩm, hỏi giá, hỏi chính sách,...).
- Gợi ý sản phẩm theo danh mục, thương hiệu, mức giá.
- Trả lời câu hỏi về bảo hành, đổi trả, thanh toán.

### 💳 Tích Hợp VNPay
- Thanh toán online qua cổng thanh toán **VNPay**.
- Xử lý callback sau khi thanh toán thành công/thất bại.
- Tự động cập nhật trạng thái đơn hàng.

### 📱 Responsive Design
- Giao diện tương thích mobile, tablet, desktop.
- Menu riêng tối ưu cho từng loại thiết bị.

### 🔍 Tìm Kiếm Nâng Cao
- Tìm theo tên sản phẩm, mã sản phẩm.
- Lọc đa tiêu chí: Danh mục + Thương hiệu + Khoảng giá.

### ⭐ Đánh Giá Sản Phẩm
- Khách hàng gửi đánh giá và xếp hạng sao (1-5).
- Admin duyệt đánh giá trước khi hiển thị.

---

## 11. Công Nghệ Sử Dụng

| Công nghệ | Phiên bản | Mục đích |
|-----------|-----------|---------|
| **ASP.NET MVC** | 5.x | Web framework chính |
| **.NET Framework** | 4.7.2 | Runtime |
| **MongoDB.Driver** | 2.x | Kết nối & truy vấn MongoDB |
| **MongoDB** | 6.0+ | Cơ sở dữ liệu NoSQL |
| **Razor** | — | Template engine (Views) |
| **Bootstrap** | 4/5 | CSS framework |
| **jQuery** | 3.x | JavaScript framework |
| **Newtonsoft.Json** | 13.x | JSON serialization |

---

## 12. Lưu Ý & Khắc Phục Sự Cố

### ❌ Lỗi: "Unable to connect to MongoDB"

**Nguyên nhân:** MongoDB service chưa chạy.

**Khắc phục:**
```powershell
Start-Service -Name MongoDB
# hoặc chạy thủ công
mongod --dbpath "C:\data\db"
```

### ❌ Lỗi: Collection không tồn tại / Database rỗng

**Khắc phục:** Import file backup hoặc chạy script khởi tạo database.

```bash
mongorestore --db WebBanHangDienTu "path\to\Database_backup\"
```

### ❌ Lỗi NuGet packages

**Khắc phục:**
```powershell
# Trong Package Manager Console (Visual Studio)
Update-Package -reinstall
```

### ❌ Lỗi gửi email reset mật khẩu

**Nguyên nhân:** Cấu hình Gmail SMTP sai hoặc chưa bật App Password.

**Khắc phục:**
1. Vào [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords)
2. Tạo App Password mới
3. Cập nhật vào `Web.config`

### ❌ Trang Admin báo lỗi 403 / redirect về Login

**Khắc phục:** Cập nhật quyền admin trực tiếp trong MongoDB:

```javascript
// Dùng MongoDB Compass hoặc mongosh
db.TaiKhoan.updateOne(
  { TenDangNhap: "admin" },
  { $set: { LaQuanTri: true, HoatDong: true } }
)
```

### ❌ VNPay trả về lỗi chữ ký không hợp lệ

**Khắc phục:** Kiểm tra lại `vnp_HashSecret` và `vnp_TmnCode` trong `Web.config` với thông tin từ tài khoản VNPay sandbox.

---

*© 2026 — Đồ án môn NoSQL | ASP.NET MVC 5 + MongoDB*
