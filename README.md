# 🍜 HuyBeo Restaurant Management System

> Hệ thống quản lý nhà hàng hiện đại xây dựng với ASP.NET Core 8 MVC — bao gồm quản lý menu, đặt bàn, POS thu ngân và màn hình bếp thời gian thực.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=for-the-badge&logo=dotnet)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-CC2927?style=for-the-badge&logo=microsoftsqlserver)
![SignalR](https://img.shields.io/badge/SignalR-RealTime-00CED1?style=for-the-badge)

---

## 📋 Mục lục

- [Tính năng](#-tính-năng)
- [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
- [Cài đặt & Chạy dự án](#-cài-đặt--chạy-dự-án)
- [Tài khoản mặc định](#-tài-khoản-mặc-định)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)

---

## ✨ Tính năng

### 🧑‍💼 Admin Dashboard
- Quản lý **Menu** (Món ăn, Danh mục, Topping)
- Quản lý **Khuyến mãi** (tạo, sửa, xoá, áp dụng theo đơn hàng)
- Quản lý **Nhân viên** (thêm, cấp quyền, khoá tài khoản)
- Thống kê **Doanh thu** và lịch sử đơn hàng
- Xem **Nhật ký đơn hàng** chi tiết

### 📱 Menu Khách hàng (Mobile-first)
- Khách hàng quét **QR code** tại bàn để xem menu và đặt món
- Giao diện tối ưu cho điện thoại
- Hỗ trợ chọn **Topping** tùy chỉnh cho từng món
- Áp dụng mã **khuyến mãi** khi thanh toán

### 💳 POS Thu ngân
- Tạo và quản lý **phiên order** theo bàn
- Xem danh sách đơn đang chờ và đã hoàn thành
- Thanh toán, áp dụng khuyến mãi
- Cập nhật trạng thái đơn theo thời gian thực qua **SignalR**

### 🍳 KDS – Màn hình bếp (Kitchen Display System)
- Nhân viên bếp nhận đơn mới **tức thời** (không cần refresh)
- Xác nhận hoàn thành từng món / toàn bộ đơn
- Giao tiếp hai chiều với POS qua **SignalR Hub**

### 🔐 Xác thực & Phân quyền
- Đăng nhập bằng Cookie Authentication
- Phân quyền theo vai trò: **Admin**, **ThuNgan** (POS), **Bep** (KDS)
- Mật khẩu được mã hoá bằng **SHA-256 + Salt**

---

## 🏗 Kiến trúc hệ thống

```
webHuyBeo/
├── Controllers/          # Xử lý request HTTP
│   ├── AdminController   # Quản trị hệ thống
│   ├── AuthController    # Đăng nhập / Đăng xuất
│   ├── BepController     # Màn hình bếp (KDS)
│   ├── HomeController    # Trang chủ
│   ├── OrderController   # Đặt hàng khách hàng
│   └── PosController     # Thu ngân (POS)
│
├── Models/               # Entity models & DbContext
│   ├── MonAn             # Món ăn
│   ├── DanhMuc           # Danh mục
│   ├── Topping           # Topping
│   ├── DonHang           # Đơn hàng
│   ├── ChiTietDon        # Chi tiết đơn
│   ├── KhuyenMai         # Khuyến mãi
│   ├── NguoiDung         # Người dùng (nhân viên)
│   ├── PhienOrder        # Phiên order theo bàn
│   └── NhatKyDon         # Nhật ký đơn hàng
│
├── Services/             # Business logic layer
│   ├── AuthService       # Xác thực người dùng
│   ├── MenuService       # Quản lý menu
│   ├── OrderService      # Xử lý đơn hàng
│   └── PaymentService    # Thanh toán
│
├── Repositories/         # Data access layer (Generic Repository)
├── Hubs/                 # SignalR Hubs (OrderHub)
├── Views/                # Razor Views
│   ├── Admin/
│   ├── Auth/
│   ├── Bep/
│   ├── Order/
│   └── Pos/
│
├── wwwroot/              # Static files (CSS, JS, Images)
├── Migrations/           # EF Core database migrations
└── Program.cs            # App entry point & DI configuration
```

---

## 🚀 Cài đặt & Chạy dự án

### Yêu cầu hệ thống
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/) (hoặc SQL Server Express / LocalDB)
- Visual Studio 2022+ hoặc VS Code

### Bước 1: Clone dự án
```bash
git clone https://github.com/devkhongluong/HuyBeoRestaurent.git
cd HuyBeoRestaurent
```

### Bước 2: Cấu hình cơ sở dữ liệu
Mở file `appsettings.json` và cập nhật chuỗi kết nối:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=webHuyBeoDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```
> ✅ Nếu dùng SQL Server local mặc định thì không cần thay đổi gì.

### Bước 3: Chạy Migration (tạo database)
```bash
dotnet ef database update
```
> Database sẽ được tạo tự động, cùng với tài khoản Admin mặc định.

### Bước 4: Khởi động ứng dụng

**Cách 1 – Dùng .bat file (cho LAN)**
```bash
start-server.bat
```
> Mở ứng dụng trên tất cả các địa chỉ IP (dùng khi khách hàng quét QR trên điện thoại trong cùng mạng LAN)

**Cách 2 – Dùng CLI**
```bash
dotnet run
```

**Cách 3 – Visual Studio**
```
Mở webHuyBeo.sln → Nhấn F5
```

Ứng dụng sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

---

## 🔑 Tài khoản mặc định

| Vai trò | Tên đăng nhập | Mật khẩu |
|---------|--------------|----------|
| Admin   | `admin`      | `admin`  |

> ⚠️ Hãy đổi mật khẩu sau khi đăng nhập lần đầu!

---

## 📁 Cấu trúc dự án

| Thành phần | Mô tả |
|-----------|-------|
| `Controllers/` | Nhận request và trả về response |
| `Models/` | Entity Framework models + DbContext |
| `Views/` | Razor templates (HTML + C#) |
| `Services/` | Nghiệp vụ: Auth, Menu, Order, Payment |
| `Repositories/` | Generic Repository pattern |
| `Hubs/` | SignalR Hub cho real-time updates |
| `Migrations/` | EF Core database migrations |
| `wwwroot/` | File tĩnh: CSS, JS, hình ảnh |

---

## 🛠 Công nghệ sử dụng

| Công nghệ | Phiên bản | Mục đích |
|----------|----------|---------|
| ASP.NET Core MVC | 8.0 | Web framework |
| Entity Framework Core | 8.0 | ORM / Database access |
| SQL Server | 2019+ | Cơ sở dữ liệu |
| SignalR | Built-in | Real-time communication (KDS ↔ POS) |
| Cookie Authentication | Built-in | Xác thực người dùng |
| SHA-256 | Built-in | Mã hoá mật khẩu |
| Bootstrap | 5.x | UI Framework |
| jQuery | 3.x | Client-side scripting |

---

## 🔄 Luồng hoạt động

```
Khách hàng (QR)
     ↓ đặt món
Order Controller ──→ OrderService ──→ Database
                                         ↓
                                    OrderHub (SignalR)
                                    ↙            ↘
              Bep (KDS)             POS (Thu ngân)
          nhận đơn mới           cập nhật trạng thái
```

---

## 📄 License

Dự án được phát triển phục vụ mục đích học tập và thực hành.

---

<div align="center">
  Made with ❤️ by <strong>HuyBeo Team</strong>
</div>
