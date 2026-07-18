using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webHuyBeo.Models;
using webHuyBeo.Repositories;

namespace webHuyBeo.Controllers
{
    [Authorize(Roles = "Admin,QuanLy")]
    public class AdminController : Controller
    {
        private readonly IRepository<MonAn> _monAnRepo;
        private readonly IRepository<DanhMuc> _danhMucRepo;
        private readonly IRepository<Topping> _toppingRepo;
        private readonly IRepository<MonAnTopping> _monAnToppingRepo;
        private readonly IRepository<DonHang> _donHangRepo;
        private readonly IRepository<KhuyenMai> _khuyenMaiRepo;
        private readonly IRepository<NguoiDung> _nguoiDungRepo;
        private readonly IRepository<NhatKyDon> _nhatKyRepo;

        public AdminController(
            IRepository<MonAn> monAnRepo,
            IRepository<DanhMuc> danhMucRepo,
            IRepository<Topping> toppingRepo,
            IRepository<MonAnTopping> monAnToppingRepo,
            IRepository<DonHang> donHangRepo,
            IRepository<KhuyenMai> khuyenMaiRepo,
            IRepository<NguoiDung> nguoiDungRepo,
            IRepository<NhatKyDon> nhatKyRepo)
        {
            _monAnRepo = monAnRepo;
            _danhMucRepo = danhMucRepo;
            _toppingRepo = toppingRepo;
            _monAnToppingRepo = monAnToppingRepo;
            _donHangRepo = donHangRepo;
            _khuyenMaiRepo = khuyenMaiRepo;
            _nguoiDungRepo = nguoiDungRepo;
            _nhatKyRepo = nhatKyRepo;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["ActiveMenu"] = "dashboard";
            ViewBag.TotalMonAn = await _monAnRepo.Query().CountAsync();
            ViewBag.TotalTopping = await _toppingRepo.Query().CountAsync();
            ViewBag.TotalDonHang = await _donHangRepo.Query().CountAsync();
            ViewBag.DoanhThuHomNay = await _donHangRepo.Query()
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

            var query = _monAnRepo.Query(includeProperties: "DanhMuc");

            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => m.TenMon.Contains(search));

            if (danhMucId.HasValue)
                query = query.Where(m => m.DanhMucID == danhMucId);

            ViewBag.DanhMucs = await _danhMucRepo.Query().OrderBy(d => d.ThuTu).ToListAsync();
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
            ViewBag.DanhMucs = new SelectList(await _danhMucRepo.Query().OrderBy(d => d.ThuTu).ToListAsync(), "DanhMucID", "TenDanhMuc");
            ViewBag.Toppings = await _toppingRepo.Query().Where(t => t.TrangThai == "ConBan").ToListAsync();
            return View(new MonAn { ConBan = true });
        }

        // POST: /Admin/TaoMonAn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMonAn(MonAn model, int[]? toppingIds, IFormFile? hinhAnh)
        {
            ModelState.Remove("DanhMuc");
            ModelState.Remove("MonAnToppings");
            ModelState.Remove("ChiTietDons");
            ModelState.Remove("HinhAnh");

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Thêm Món ăn mới";
                ViewData["ActiveMenu"] = "monan";
                ViewBag.DanhMucs = new SelectList(await _danhMucRepo.GetAllAsync(), "DanhMucID", "TenDanhMuc", model.DanhMucID);
                ViewBag.Toppings = await _toppingRepo.Query().Where(t => t.TrangThai == "ConBan").ToListAsync();
                return View(model);
            }

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

            await _monAnRepo.AddAsync(model);
            await _monAnRepo.SaveAsync();

            if (toppingIds != null && toppingIds.Length > 0)
            {
                foreach (var toppingId in toppingIds)
                {
                    await _monAnToppingRepo.AddAsync(new MonAnTopping { MonAnID = model.MonAnID, ToppingID = toppingId });
                }
                await _monAnToppingRepo.SaveAsync();
            }

            TempData["Success"] = $"Đã thêm món \"{model.TenMon}\" thành công!";
            return RedirectToAction(nameof(MonAn));
        }

        // GET: /Admin/SuaMonAn/5
        public async Task<IActionResult> SuaMonAn(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Món ăn";
            ViewData["ActiveMenu"] = "monan";
            var monAn = await _monAnRepo.GetFirstOrDefaultAsync(m => m.MonAnID == id, includeProperties: "MonAnToppings");
            if (monAn == null) return NotFound();

            ViewBag.DanhMucs = new SelectList(await _danhMucRepo.Query().OrderBy(d => d.ThuTu).ToListAsync(), "DanhMucID", "TenDanhMuc", monAn.DanhMucID);
            ViewBag.Toppings = await _toppingRepo.GetAllAsync();
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
                ViewBag.DanhMucs = new SelectList(await _danhMucRepo.GetAllAsync(), "DanhMucID", "TenDanhMuc", model.DanhMucID);
                ViewBag.Toppings = await _toppingRepo.GetAllAsync();
                ViewBag.SelectedToppings = toppingIds?.ToList() ?? new List<int>();
                return View(model);
            }

            var existing = await _monAnRepo.GetFirstOrDefaultAsync(m => m.MonAnID == id, includeProperties: "MonAnToppings");
            if (existing == null) return NotFound();

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

            _monAnToppingRepo.RemoveRange(existing.MonAnToppings);
            if (toppingIds != null)
            {
                foreach (var toppingId in toppingIds)
                    await _monAnToppingRepo.AddAsync(new MonAnTopping { MonAnID = id, ToppingID = toppingId });
            }

            _monAnRepo.Update(existing);
            await _monAnRepo.SaveAsync();
            TempData["Success"] = $"Đã cập nhật món \"{existing.TenMon}\" thành công!";
            return RedirectToAction(nameof(MonAn));
        }

        // POST: /Admin/XoaMonAn/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaMonAn(int id)
        {
            var monAn = await _monAnRepo.GetFirstOrDefaultAsync(m => m.MonAnID == id);
            if (monAn != null)
            {
                _monAnRepo.Remove(monAn);
                await _monAnRepo.SaveAsync();
                TempData["Success"] = $"Đã xóa món \"{monAn.TenMon}\".";
            }
            return RedirectToAction(nameof(MonAn));
        }

        // POST: /Admin/ToggleMonAn/5
        [HttpPost]
        public async Task<IActionResult> ToggleMonAn(int id)
        {
            var monAn = await _monAnRepo.GetFirstOrDefaultAsync(m => m.MonAnID == id);
            if (monAn != null)
            {
                monAn.ConBan = !monAn.ConBan;
                _monAnRepo.Update(monAn);
                await _monAnRepo.SaveAsync();
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
            var danhMucs = await _danhMucRepo.Query().OrderBy(d => d.ThuTu).ToListAsync();
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
                await _danhMucRepo.AddAsync(model);
                await _danhMucRepo.SaveAsync();
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
            var existing = await _danhMucRepo.GetFirstOrDefaultAsync(d => d.DanhMucID == model.DanhMucID);
            if (existing != null && ModelState.IsValid)
            {
                existing.TenDanhMuc = model.TenDanhMuc;
                existing.MoTa = model.MoTa;
                existing.ThuTu = model.ThuTu;
                _danhMucRepo.Update(existing);
                await _danhMucRepo.SaveAsync();
                TempData["Success"] = "Đã cập nhật danh mục!";
            }
            return RedirectToAction(nameof(DanhMuc));
        }

        // POST: /Admin/XoaDanhMuc/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDanhMuc(int id)
        {
            var dm = await _danhMucRepo.GetFirstOrDefaultAsync(d => d.DanhMucID == id);
            if (dm != null)
            {
                _danhMucRepo.Remove(dm);
                await _danhMucRepo.SaveAsync();
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
            var toppings = await _toppingRepo.GetAllAsync();
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
                await _toppingRepo.AddAsync(model);
                await _toppingRepo.SaveAsync();
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
            var existing = await _toppingRepo.GetFirstOrDefaultAsync(t => t.ToppingID == model.ToppingID);
            if (existing != null && ModelState.IsValid)
            {
                existing.TenTopping = model.TenTopping;
                existing.GiaThem = model.GiaThem;
                existing.TrangThai = model.TrangThai;
                _toppingRepo.Update(existing);
                await _toppingRepo.SaveAsync();
                TempData["Success"] = "Đã cập nhật topping!";
            }
            return RedirectToAction(nameof(Topping));
        }

        // POST: /Admin/XoaTopping/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaTopping(int id)
        {
            var topping = await _toppingRepo.GetFirstOrDefaultAsync(t => t.ToppingID == id);
            if (topping != null)
            {
                _toppingRepo.Remove(topping);
                await _toppingRepo.SaveAsync();
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
            var list = await _khuyenMaiRepo.Query().OrderByDescending(k => k.KhuyenMaiID).ToListAsync();
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
                await _khuyenMaiRepo.AddAsync(model);
                await _khuyenMaiRepo.SaveAsync();
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
            var existing = await _khuyenMaiRepo.GetFirstOrDefaultAsync(k => k.KhuyenMaiID == model.KhuyenMaiID);
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
                _khuyenMaiRepo.Update(existing);
                await _khuyenMaiRepo.SaveAsync();
                TempData["Success"] = "Đã cập nhật khuyến mãi!";
            }
            return RedirectToAction(nameof(KhuyenMai));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaKhuyenMai(int id)
        {
            var km = await _khuyenMaiRepo.GetFirstOrDefaultAsync(k => k.KhuyenMaiID == id);
            if (km != null)
            {
                _khuyenMaiRepo.Remove(km);
                await _khuyenMaiRepo.SaveAsync();
                TempData["Success"] = $"Đã xóa khuyến mãi \"{km.MaCode}\".";
            }
            return RedirectToAction(nameof(KhuyenMai));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleKhuyenMai(int id)
        {
            var km = await _khuyenMaiRepo.GetFirstOrDefaultAsync(k => k.KhuyenMaiID == id);
            if (km != null)
            {
                km.TrangThai = km.TrangThai == "HoatDong" ? "TamDung" : "HoatDong";
                _khuyenMaiRepo.Update(km);
                await _khuyenMaiRepo.SaveAsync();
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
            var list = await _nguoiDungRepo.Query().OrderBy(n => n.VaiTro).ThenBy(n => n.Hoten).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoNhanVien(NguoiDung model)
        {
            ModelState.Remove("DonHangs");
            ModelState.Remove("NhatKyDons");
            ModelState.Remove("TrangThai");

            var allUsers = await _nguoiDungRepo.GetAllAsync();
            if (allUsers.Any(n => n.TenDangNhap == model.TenDangNhap))
            {
                TempData["Error"] = $"Tên đăng nhập \"{model.TenDangNhap}\" đã tồn tại!";
                return RedirectToAction(nameof(NhanVien));
            }

            if (ModelState.IsValid)
            {
                model.MatKhau = BCryptHash(model.MatKhau);
                model.TrangThai = "DangLamViec";
                await _nguoiDungRepo.AddAsync(model);
                await _nguoiDungRepo.SaveAsync();
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
            var nv = await _nguoiDungRepo.GetFirstOrDefaultAsync(n => n.NguoiDungID == NguoiDungID);
            if (nv != null)
            {
                nv.Hoten = Hoten;
                nv.VaiTro = VaiTro;
                nv.TrangThai = TrangThai;
                if (!string.IsNullOrEmpty(MatKhauMoi))
                    nv.MatKhau = BCryptHash(MatKhauMoi);
                
                _nguoiDungRepo.Update(nv);
                await _nguoiDungRepo.SaveAsync();
                TempData["Success"] = $"Đã cập nhật nhân viên \"{nv.Hoten}\"!";
            }
            return RedirectToAction(nameof(NhanVien));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaNhanVien(int id)
        {
            var nv = await _nguoiDungRepo.GetFirstOrDefaultAsync(n => n.NguoiDungID == id);
            if (nv != null)
            {
                nv.TrangThai = "DaNghi"; // Soft delete
                _nguoiDungRepo.Update(nv);
                await _nguoiDungRepo.SaveAsync();
                TempData["Success"] = $"Đã vô hiệu hóa tài khoản \"{nv.Hoten}\".";
            }
            return RedirectToAction(nameof(NhanVien));
        }

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

            string primaryIp = "127.0.0.1";
            try
            {
                using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    if (socket.LocalEndPoint is System.Net.IPEndPoint endPoint)
                    {
                        primaryIp = endPoint.Address.ToString();
                    }
                }
            }
            catch { }

            var ips = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                            n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .Distinct()
                .ToList();

            if (ips.Contains(primaryIp))
            {
                ips.Remove(primaryIp);
            }
            ips.Insert(0, primaryIp);

            var defaultIp = primaryIp;
            var host = Request.Host.Value;

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
            var query = _donHangRepo.Query(includeProperties: "ChiTietDons.MonAn,NguoiDung");

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
            var don = await _donHangRepo.GetFirstOrDefaultAsync(d => d.DonHangID == id);
            if (don != null && don.TrangThai != "DaHoanThanh")
            {
                don.TrangThai = "Huy";
                _donHangRepo.Update(don);

                await _nhatKyRepo.AddAsync(new NhatKyDon
                {
                    DonHangID = id,
                    HanhDong = "HuyDon",
                    NgayTao = DateTime.Now
                });

                await _donHangRepo.SaveAsync();
                TempData["Success"] = $"Đã hủy đơn #{don.SoThuTu}.";
            }
            return RedirectToAction(nameof(DonHang));
        }
    }
}
