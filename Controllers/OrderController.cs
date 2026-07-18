using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using webHuyBeo.Models;
using webHuyBeo.Services;
using webHuyBeo.Repositories;

namespace webHuyBeo.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IRepository<PhienOrder> _phienRepo;
        private readonly IRepository<DanhMuc> _danhMucRepo;
        private readonly IMenuService _menuService;
        private readonly IRepository<DonHang> _donHangRepo;
        private readonly IPaymentService _paymentService;

        public OrderController(
            IOrderService orderService, 
            IRepository<PhienOrder> phienRepo,
            IRepository<DanhMuc> danhMucRepo,
            IMenuService menuService,
            IRepository<DonHang> donHangRepo,
            IPaymentService paymentService)
        {
            _orderService = orderService;
            _phienRepo = phienRepo;
            _danhMucRepo = danhMucRepo;
            _menuService = menuService;
            _donHangRepo = donHangRepo;
            _paymentService = paymentService;
        }

        // ============================================================
        // GET /Order/BatDau  — Quét QR vào đây, tạo PhienOrder
        // ============================================================
        public async Task<IActionResult> BatDau()
        {
            var token = Guid.NewGuid().ToString("N")[..12].ToUpper();
            var phien = new PhienOrder
            {
                MaToken = token,
                ThoiGianBatDau = DateTime.Now,
                TrangThai = "DangMo"
            };
            await _phienRepo.AddAsync(phien);
            await _phienRepo.SaveAsync();

            return RedirectToAction(nameof(Menu), new { token });
        }

        // ============================================================
        // GET /Order/Menu?token=xxx — Trang Menu chính
        // ============================================================
        public async Task<IActionResult> Menu(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction(nameof(BatDau));

            bool isValid = await _orderService.ValidatePhienOrderAsync(token);
            if (!isValid) return View("TokenExpired");

            var danhMucs = await _danhMucRepo.GetAllAsync();
            danhMucs = danhMucs.OrderBy(d => d.ThuTu).ToList();

            var monAns = await _menuService.GetAllMonAnsAsync();
            monAns = monAns.Where(m => m.ConBan).ToList();

            ViewBag.Token = token;
            ViewBag.DanhMucs = danhMucs;
            ViewBag.TenQuan = "Huy Béo";
            return View(monAns);
        }

        // ============================================================
        // GET /Order/GioHang?token=xxx — Trang Giỏ hàng
        // ============================================================
        public async Task<IActionResult> GioHang(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction(nameof(BatDau));

            bool isValid = await _orderService.ValidatePhienOrderAsync(token);
            if (!isValid) return View("TokenExpired");

            ViewBag.Token = token;
            return View();
        }

        // ============================================================
        // POST /Order/DatHang — Tạo đơn hàng từ giỏ
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> DatHang([FromBody] DatHangRequest request)
        {
            if (request == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var result = await _orderService.CreateOrderAsync(
                request.Token, request.LoaiDon, request.MaKhuyenMai, request.GhiChuChung, request.Items);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, donHangId = result.DonHangId, soThuTu = result.SoThuTu });
        }

        // ============================================================
        // GET /Order/XacNhan/{id}?token=xxx — Trang xác nhận thành công
        // ============================================================
        public async Task<IActionResult> XacNhan(int id, string token)
        {
            var donHang = await _donHangRepo.GetFirstOrDefaultAsync(
                d => d.DonHangID == id, 
                includeProperties: "ChiTietDons.MonAn,DonHangKhuyenMais.KhuyenMai");

            if (donHang == null) return NotFound();

            ViewBag.Token = token;
            return View(donHang);
        }

        // ============================================================
        // POST /Order/KiemTraMa — Kiểm tra mã khuyến mãi realtime
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> KiemTraMa([FromBody] KiemTraMaRequest req)
        {
            var km = await _paymentService.ValidateKhuyenMaiAsync(req.MaCode, req.TongTien);
            if (km == null)
            {
                return Json(new { valid = false, message = "Mã không hợp lệ, chưa đủ điều kiện, hoặc đã hết hạn." });
            }

            return Json(new
            {
                valid = true,
                loaiGiam = km.LoaiGiam,
                giaTri = km.GiaTri,
                giamToiDa = km.GiamToiDa,
                donToiThieu = km.DonToiThieu,
                tenKM = km.TenKM ?? km.MaCode
            });
        }
    }

    // Request models
    public class DatHangRequest
    {
        public string Token { get; set; } = "";
        public string? LoaiDon { get; set; }
        public string? MaKhuyenMai { get; set; }
        public string? GhiChuChung { get; set; }
        public List<CartItem> Items { get; set; } = new();
    }
    public class CartItem
    {
        public int MonAnID { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string? GhiChu { get; set; }
        public List<int>? ToppingIDs { get; set; }
    }
    public class KiemTraMaRequest
    {
        public string MaCode { get; set; } = "";
        public decimal TongTien { get; set; }
    }
}
