using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webHuyBeo.Models;

namespace webHuyBeo.Controllers
{
    [Authorize(Roles = "Admin,QuanLy")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["ActiveMenu"] = "dashboard";
            ViewBag.TotalMonAn = await _db.MonAns.CountAsync();
            ViewBag.TotalTopping = await _db.Toppings.CountAsync();
            ViewBag.TotalDonHang = await _db.DonHangs.CountAsync();
            ViewBag.DoanhThuHomNay = await _db.DonHangs
                .Where(d => d.NgayTao.Date == DateTime.Today && d.TrangThaiTT == "DaThanhToan")
                .SumAsync(d => (decimal?)d.ThanhTien) ?? 0;
            return View();
        }

        // ============================================================
        // MON AN MANAGEMENT
        // ============================================================

        // GET: /Admin/MonAn
        public async Task<IActionResult> MonAn(string? search, int? danhMucId)
        {
            ViewData["Title"] = "Quản lý Món ăn";
            ViewData["ActiveMenu"] = "monan";

            var query = _db.MonAns.Include(m => m.DanhMuc).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => m.TenMon.Contains(search));

            if (danhMucId.HasValue)
                query = query.Where(m => m.DanhMucID == danhMucId);

            ViewBag.DanhMucs = await _db.DanhMucs.OrderBy(d => d.ThuTu).ToListAsync();
            ViewBag.Search = search;
            ViewBag.DanhMucId = danhMucId;

            var monAns = await query.OrderBy(m => m.DanhMucID).ThenBy(m => m.TenMon).ToListAsync();
            return View(monAns);
        }

        // GET: /Admin/TaoMonAn
        public async Task<IActionResult> TaoMonAn()
        {
            ViewData["Title"] = "Thêm Món ăn mới";
            ViewData["ActiveMenu"] = "monan";
            ViewBag.DanhMucs = new SelectList(await _db.DanhMucs.OrderBy(d => d.ThuTu).ToListAsync(), "DanhMucID", "TenDanhMuc");
            ViewBag.Toppings = await _db.Toppings.Where(t => t.TrangThai == "ConBan").ToListAsync();
            return View(new MonAn { ConBan = true });
        }

        // POST: /Admin/TaoMonAn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMonAn(MonAn model, int[]? toppingIds, IFormFile? hinhAnh)
        {
            // Remove navigaton props from validation
            ModelState.Remove("DanhMuc");
            ModelState.Remove("MonAnToppings");
            ModelState.Remove("ChiTietDons");
            ModelState.Remove("HinhAnh");

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Thêm Món ăn mới";
                ViewData["ActiveMenu"] = "monan";
                ViewBag.DanhMucs = new SelectList(await _db.DanhMucs.ToListAsync(), "DanhMucID", "TenDanhMuc", model.DanhMucID);
                ViewBag.Toppings = await _db.Toppings.Where(t => t.TrangThai == "ConBan").ToListAsync();
                return View(model);
            }

            // Handle image upload
            if (hinhAnh != null && hinhAnh.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(hinhAnh.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "monan");
                Directory.CreateDirectory(uploadPath);
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await hinhAnh.CopyToAsync(stream);
                model.HinhAnh = "/uploads/monan/" + fileName;
            }

            _db.MonAns.Add(model);
            await _db.SaveChangesAsync();

            // Assign toppings
            if (toppingIds != null && toppingIds.Length > 0)
            {
                foreach (var toppingId in toppingIds)
                {
                    _db.MonAnToppings.Add(new MonAnTopping { MonAnID = model.MonAnID, ToppingID = toppingId });
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = $"Đã thêm món \"{model.TenMon}\" thành công!";
            return RedirectToAction(nameof(MonAn));
        }

        // GET: /Admin/SuaMonAn/5
        public async Task<IActionResult> SuaMonAn(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Món ăn";
            ViewData["ActiveMenu"] = "monan";
            var monAn = await _db.MonAns.Include(m => m.MonAnToppings).FirstOrDefaultAsync(m => m.MonAnID == id);
            if (monAn == null) return NotFound();

            ViewBag.DanhMucs = new SelectList(await _db.DanhMucs.OrderBy(d => d.ThuTu).ToListAsync(), "DanhMucID", "TenDanhMuc", monAn.DanhMucID);
            ViewBag.Toppings = await _db.Toppings.ToListAsync();
            ViewBag.SelectedToppings = monAn.MonAnToppings.Select(t => t.ToppingID).ToList();
            return View(monAn);
        }

        // POST: /Admin/SuaMonAn/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaMonAn(int id, MonAn model, int[]? toppingIds, IFormFile? hinhAnh)
        {
            ModelState.Remove("DanhMuc");
            ModelState.Remove("MonAnToppings");
            ModelState.Remove("ChiTietDons");
            ModelState.Remove("HinhAnh");

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Chỉnh sửa Món ăn";
                ViewData["ActiveMenu"] = "monan";
                ViewBag.DanhMucs = new SelectList(await _db.DanhMucs.ToListAsync(), "DanhMucID", "TenDanhMuc", model.DanhMucID);
                ViewBag.Toppings = await _db.Toppings.ToListAsync();
                ViewBag.SelectedToppings = toppingIds?.ToList() ?? new List<int>();
                return View(model);
            }

            var existing = await _db.MonAns.Include(m => m.MonAnToppings).FirstOrDefaultAsync(m => m.MonAnID == id);
            if (existing == null) return NotFound();

            // Handle image
            if (hinhAnh != null && hinhAnh.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(hinhAnh.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "monan");
                Directory.CreateDirectory(uploadPath);
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await hinhAnh.CopyToAsync(stream);
                existing.HinhAnh = "/uploads/monan/" + fileName;
            }

            existing.TenMon = model.TenMon;
            existing.DanhMucID = model.DanhMucID;
            existing.MoTa = model.MoTa;
            existing.GiaBan = model.GiaBan;
            existing.GiaVon = model.GiaVon;
            existing.ConBan = model.ConBan;

            // Update toppings
            _db.MonAnToppings.RemoveRange(existing.MonAnToppings);
            if (toppingIds != null)
            {
                foreach (var toppingId in toppingIds)
                    _db.MonAnToppings.Add(new MonAnTopping { MonAnID = id, ToppingID = toppingId });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật món \"{existing.TenMon}\" thành công!";
            return RedirectToAction(nameof(MonAn));
        }

        // POST: /Admin/XoaMonAn/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaMonAn(int id)
        {
            var monAn = await _db.MonAns.FindAsync(id);
            if (monAn != null)
            {
                _db.MonAns.Remove(monAn);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa món \"{monAn.TenMon}\".";
            }
            return RedirectToAction(nameof(MonAn));
        }

        // POST: /Admin/ToggleMonAn/5
        [HttpPost]
        public async Task<IActionResult> ToggleMonAn(int id)
        {
            var monAn = await _db.MonAns.FindAsync(id);
            if (monAn != null)
            {
                monAn.ConBan = !monAn.ConBan;
                await _db.SaveChangesAsync();
                return Json(new { success = true, conBan = monAn.ConBan });
            }
            return Json(new { success = false });
        }

        // ============================================================
        // DANH MUC MANAGEMENT
        // ============================================================

        // GET: /Admin/DanhMuc
        public async Task<IActionResult> DanhMuc()
        {
            ViewData["Title"] = "Quản lý Danh mục";
            ViewData["ActiveMenu"] = "danhmuc";
            var danhMucs = await _db.DanhMucs.OrderBy(d => d.ThuTu).ToListAsync();
            return View(danhMucs);
        }

        // POST: /Admin/TaoDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoDanhMuc(DanhMuc model)
        {
            ModelState.Remove("MonAns");
            if (ModelState.IsValid)
            {
                _db.DanhMucs.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm danh mục \"{model.TenDanhMuc}\"!";
            }
            return RedirectToAction(nameof(DanhMuc));
        }

        // POST: /Admin/SuaDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaDanhMuc(DanhMuc model)
        {
            ModelState.Remove("MonAns");
            var existing = await _db.DanhMucs.FindAsync(model.DanhMucID);
            if (existing != null && ModelState.IsValid)
            {
                existing.TenDanhMuc = model.TenDanhMuc;
                existing.MoTa = model.MoTa;
                existing.ThuTu = model.ThuTu;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật danh mục!";
            }
            return RedirectToAction(nameof(DanhMuc));
        }

        // POST: /Admin/XoaDanhMuc/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDanhMuc(int id)
        {
            var dm = await _db.DanhMucs.FindAsync(id);
            if (dm != null)
            {
                _db.DanhMucs.Remove(dm);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa danh mục \"{dm.TenDanhMuc}\".";
            }
            return RedirectToAction(nameof(DanhMuc));
        }

        // ============================================================
        // TOPPING MANAGEMENT
        // ============================================================

        // GET: /Admin/Topping
        public async Task<IActionResult> Topping()
        {
            ViewData["Title"] = "Quản lý Topping";
            ViewData["ActiveMenu"] = "topping";
            var toppings = await _db.Toppings.ToListAsync();
            return View(toppings);
        }

        // POST: /Admin/TaoTopping
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoTopping(Topping model)
        {
            ModelState.Remove("MonAnToppings");
            ModelState.Remove("ChiTietToppings");
            if (ModelState.IsValid)
            {
                _db.Toppings.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm topping \"{model.TenTopping}\"!";
            }
            return RedirectToAction(nameof(Topping));
        }

        // POST: /Admin/SuaTopping
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaTopping(Topping model)
        {
            ModelState.Remove("MonAnToppings");
            ModelState.Remove("ChiTietToppings");
            var existing = await _db.Toppings.FindAsync(model.ToppingID);
            if (existing != null && ModelState.IsValid)
            {
                existing.TenTopping = model.TenTopping;
                existing.GiaThem = model.GiaThem;
                existing.TrangThai = model.TrangThai;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật topping!";
            }
            return RedirectToAction(nameof(Topping));
        }

        // POST: /Admin/XoaTopping/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaTopping(int id)
        {
            var topping = await _db.Toppings.FindAsync(id);
            if (topping != null)
            {
                _db.Toppings.Remove(topping);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa topping \"{topping.TenTopping}\".";
            }
            return RedirectToAction(nameof(Topping));
        }

        // ============================================================
        // KHUYEN MAI MANAGEMENT
        // ============================================================

        public async Task<IActionResult> KhuyenMai()
        {
            ViewData["Title"] = "Quản lý Khuyến mãi";
            ViewData["ActiveMenu"] = "khuyenmai";
            var list = await _db.KhuyenMais.OrderByDescending(k => k.KhuyenMaiID).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoKhuyenMai(KhuyenMai model)
        {
            ModelState.Remove("DonHangKhuyenMais");
            if (ModelState.IsValid)
            {
                model.SoLuongDaDung = 0;
                _db.KhuyenMais.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã tạo khuyến mãi \"{model.MaCode}\"!";
            }
            else
            {
                TempData["Error"] = "Dữ liệu không hợp lệ, vui lòng kiểm tra lại.";
            }
            return RedirectToAction(nameof(KhuyenMai));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaKhuyenMai(KhuyenMai model)
        {
            ModelState.Remove("DonHangKhuyenMais");
            var existing = await _db.KhuyenMais.FindAsync(model.KhuyenMaiID);
            if (existing != null && ModelState.IsValid)
            {
                existing.MaCode = model.MaCode;
                existing.TenKM = model.TenKM;
                existing.LoaiGiam = model.LoaiGiam;
                existing.GiaTri = model.GiaTri;
                existing.GiamToiDa = model.GiamToiDa;
                existing.DonToiThieu = model.DonToiThieu;
                existing.SoLuongToiDa = model.SoLuongToiDa;
                existing.NgayBatDau = model.NgayBatDau;
                existing.NgayKetThuc = model.NgayKetThuc;
                existing.TrangThai = model.TrangThai;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật khuyến mãi!";
            }
            return RedirectToAction(nameof(KhuyenMai));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaKhuyenMai(int id)
        {
            var km = await _db.KhuyenMais.FindAsync(id);
            if (km != null)
            {
                _db.KhuyenMais.Remove(km);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa khuyến mãi \"{km.MaCode}\".";
            }
            return RedirectToAction(nameof(KhuyenMai));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleKhuyenMai(int id)
        {
            var km = await _db.KhuyenMais.FindAsync(id);
            if (km != null)
            {
                km.TrangThai = km.TrangThai == "HoatDong" ? "TamDung" : "HoatDong";
                await _db.SaveChangesAsync();
                return Json(new { success = true, trangThai = km.TrangThai });
            }
            return Json(new { success = false });
        }

        // ============================================================
        // NHAN VIEN MANAGEMENT
        // ============================================================

        public async Task<IActionResult> NhanVien()
        {
            ViewData["Title"] = "Quản lý Nhân viên";
            ViewData["ActiveMenu"] = "nhanvien";
            var list = await _db.NguoiDungs.OrderBy(n => n.VaiTro).ThenBy(n => n.Hoten).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoNhanVien(NguoiDung model)
        {
            ModelState.Remove("DonHangs");
            ModelState.Remove("NhatKyDons");
            // Check duplicate username
            if (await _db.NguoiDungs.AnyAsync(n => n.TenDangNhap == model.TenDangNhap))
            {
                TempData["Error"] = $"Tên đăng nhập \"{model.TenDangNhap}\" đã tồn tại!";
                return RedirectToAction(nameof(NhanVien));
            }
            if (ModelState.IsValid)
            {
                // Hash password with BCrypt-style simple hash (use proper BCrypt in prod)
                model.MatKhau = BCryptHash(model.MatKhau);
                model.TrangThai = "DangLamViec";
                _db.NguoiDungs.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm nhân viên \"{model.Hoten}\"!";
            }
            else
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
            }
            return RedirectToAction(nameof(NhanVien));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaNhanVien(int NguoiDungID, string Hoten, string VaiTro, string TrangThai, string? MatKhauMoi)
        {
            var nv = await _db.NguoiDungs.FindAsync(NguoiDungID);
            if (nv != null)
            {
                nv.Hoten = Hoten;
                nv.VaiTro = VaiTro;
                nv.TrangThai = TrangThai;
                if (!string.IsNullOrEmpty(MatKhauMoi))
                    nv.MatKhau = BCryptHash(MatKhauMoi);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật nhân viên \"{nv.Hoten}\"!";
            }
            return RedirectToAction(nameof(NhanVien));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaNhanVien(int id)
        {
            var nv = await _db.NguoiDungs.FindAsync(id);
            if (nv != null)
            {
                nv.TrangThai = "DaNghi"; // Soft delete - mark as resigned
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã vô hiệu hóa tài khoản \"{nv.Hoten}\".";
            }
            return RedirectToAction(nameof(NhanVien));
        }

        // Simple hash helper (replace with BCrypt.Net in production)
        private string BCryptHash(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password + "HuyBeoSalt2026");
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        // ============================================================
        // QR CODE
        // ============================================================

        public IActionResult QrCode()
        {
            ViewData["Title"] = "Mã QR Quét Đặt món";
            ViewData["ActiveMenu"] = "qr";

            // Lấy danh sách tất cả các IP IPv4 hợp lệ của máy
            var ips = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                            n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .ToList();

            // Sắp xếp ưu tiên IP Wi-Fi/Ethernet thông thường (thường là 192.168.1.x hoặc 192.168.0.x)
            ips = ips.OrderByDescending(ip => ip.StartsWith("192.168.1.") || ip.StartsWith("192.168.0."))
                     .ThenByDescending(ip => ip.StartsWith("192.168."))
                     .ToList();

            var defaultIp = ips.FirstOrDefault() ?? "127.0.0.1";
            var host = Request.Host.Value;

            // Nếu đang truy cập bằng localhost/127.0.0.1 trên máy, thay thế bằng IP LAN để quét QR
            if (Request.Host.Host == "localhost" || Request.Host.Host == "127.0.0.1")
            {
                var port = Request.Host.Port;
                host = port.HasValue ? $"{defaultIp}:{port}" : defaultIp;
            }

            var baseUrl = $"{Request.Scheme}://{host}";
            ViewBag.OrderUrl = $"{baseUrl}/Order/BatDau";
            ViewBag.DetectedIps = ips;
            ViewBag.Port = Request.Host.Port ?? 5000;
            ViewBag.Scheme = Request.Scheme;
            
            return View();
        }

        // ============================================================
        // DON HANG MANAGEMENT
        // ============================================================

        public async Task<IActionResult> DonHang(string? trangThai, string? loaiDon, int page = 1)
        {
            ViewData["Title"] = "Quản lý Đơn hàng";
            ViewData["ActiveMenu"] = "donhang";

            int pageSize = 20;
            var query = _db.DonHangs
                .Include(d => d.ChiTietDons).ThenInclude(c => c.MonAn)
                .Include(d => d.NguoiDung)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(d => d.TrangThai == trangThai);
            if (!string.IsNullOrEmpty(loaiDon))
                query = query.Where(d => d.LoaiDon == loaiDon);

            var total = await query.CountAsync();
            var donHangs = await query
                .OrderByDescending(d => d.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TrangThai = trangThai;
            ViewBag.LoaiDon = loaiDon;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total = total;

            return View(donHangs);
        }

        [HttpPost]
        public async Task<IActionResult> HuyDon(int id)
        {
            var don = await _db.DonHangs.FindAsync(id);
            if (don != null && don.TrangThai != "DaHoanThanh")
            {
                don.TrangThai = "Huy";
                _db.NhatKyDons.Add(new NhatKyDon
                {
                    DonHangID = id,
                    HanhDong = "HuyDon",
                    NgayTao = DateTime.Now
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã hủy đơn #{don.SoThuTu}.";
            }
            return RedirectToAction(nameof(DonHang));
        }
    }
}

