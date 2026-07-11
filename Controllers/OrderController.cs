using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using webHuyBeo.Models;

namespace webHuyBeo.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // GET /Order/BatDau  — Quét QR vào đây, tạo PhienOrder
        // ============================================================
        public async Task<IActionResult> BatDau()
        {
            // Sinh token duy nhất
            var token = Guid.NewGuid().ToString("N")[..12].ToUpper();

            var phien = new PhienOrder
            {
                MaToken = token,
                ThoiGianBatDau = DateTime.Now,
                TrangThai = "DangMo"
            };
            _db.PhienOrders.Add(phien);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Menu), new { token });
        }

        // ============================================================
        // GET /Order/Menu?token=xxx — Trang Menu chính
        // ============================================================
        public async Task<IActionResult> Menu(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(BatDau));

            // Kiểm tra token hợp lệ
            var phien = await _db.PhienOrders.FirstOrDefaultAsync(p => p.MaToken == token);
            if (phien == null || phien.TrangThai == "DaDong")
                return View("TokenExpired");

            // Load menu
            var danhMucs = await _db.DanhMucs
                .OrderBy(d => d.ThuTu)
                .ToListAsync();

            var monAns = await _db.MonAns
                .Include(m => m.DanhMuc)
                .Include(m => m.MonAnToppings)
                    .ThenInclude(mat => mat.Topping)
                .Where(m => m.ConBan)
                .OrderBy(m => m.DanhMucID)
                .ThenBy(m => m.TenMon)
                .ToListAsync();

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
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(BatDau));

            var phien = await _db.PhienOrders.FirstOrDefaultAsync(p => p.MaToken == token);
            if (phien == null || phien.TrangThai == "DaDong")
                return View("TokenExpired");

            ViewBag.Token = token;
            return View();
        }

        // ============================================================
        // POST /Order/DatHang — Tạo đơn hàng từ giỏ
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> DatHang([FromBody] DatHangRequest request)
        {
            if (request == null || !request.Items.Any())
                return Json(new { success = false, message = "Giỏ hàng trống!" });

            // Validate token
            var phien = await _db.PhienOrders.FirstOrDefaultAsync(p => p.MaToken == request.Token);
            if (phien == null || phien.TrangThai == "DaDong")
                return Json(new { success = false, message = "Phiên đặt hàng đã hết hạn. Vui lòng quét lại QR." });

            // Sinh số thứ tự (dạng: A01, A02... hoặc reset mỗi ngày)
            var countToday = await _db.DonHangs
                .Where(d => d.NgayTao.Date == DateTime.Today)
                .CountAsync();
            var soThuTu = (countToday + 1).ToString();

            // Tính tổng tiền
            decimal tongTien = 0;
            var chiTietList = new List<ChiTietDon>();

            foreach (var item in request.Items)
            {
                var mon = await _db.MonAns.FindAsync(item.MonAnID);
                if (mon == null) continue;

                decimal toppingGia = 0;
                if (item.ToppingIDs != null)
                {
                    foreach (var tid in item.ToppingIDs)
                    {
                        var tp = await _db.Toppings.FindAsync(tid);
                        if (tp != null) toppingGia += tp.GiaThem;
                    }
                }

                decimal donGia = mon.GiaBan + toppingGia;
                decimal thanhTien = donGia * item.SoLuong;
                tongTien += thanhTien;

                chiTietList.Add(new ChiTietDon
                {
                    MonAnID = item.MonAnID,
                    SoLuong = item.SoLuong,
                    DonGia = donGia,
                    ThanhTien = thanhTien,
                    GhiChu = item.GhiChu,
                    TrangThaiBep = "Cho"
                });
            }

            // Áp mã giảm giá (nếu có)
            decimal giamGia = 0;
            KhuyenMai? km = null;
            if (!string.IsNullOrEmpty(request.MaKhuyenMai))
            {
                km = await _db.KhuyenMais.FirstOrDefaultAsync(k =>
                    k.MaCode == request.MaKhuyenMai.ToUpper() &&
                    k.TrangThai == "HoatDong" &&
                    k.NgayBatDau <= DateTime.Now &&
                    k.NgayKetThuc >= DateTime.Now);

                if (km != null)
                {
                    if (km.DonToiThieu.HasValue && tongTien < km.DonToiThieu)
                        return Json(new { success = false, message = $"Đơn tối thiểu {km.DonToiThieu:N0}đ để áp mã này." });

                    if (km.LoaiGiam == "PhanTram")
                    {
                        giamGia = tongTien * km.GiaTri / 100;
                        if (km.GiamToiDa.HasValue && giamGia > km.GiamToiDa)
                            giamGia = km.GiamToiDa.Value;
                    }
                    else
                    {
                        giamGia = km.GiaTri;
                    }
                    if (giamGia > tongTien) giamGia = tongTien;
                }
            }

            decimal thanhTienCuoi = tongTien - giamGia;

            // Tạo đơn hàng
            var donHang = new DonHang
            {
                PhienID = phien.PhienID,
                LoaiDon = request.LoaiDon ?? "TaiCho",
                TrangThai = "ChoXacNhan",
                SoThuTu = soThuTu,
                TongTien = tongTien,
                TongGiamGia = giamGia > 0 ? giamGia : null,
                ThanhTien = thanhTienCuoi,
                TrangThaiTT = "ChuaThanhToan",
                NgayTao = DateTime.Now
            };
            _db.DonHangs.Add(donHang);
            await _db.SaveChangesAsync();

            // Gán chitiếtdons
            foreach (var ct in chiTietList)
            {
                ct.DonHangID = donHang.DonHangID;
                _db.ChiTietDons.Add(ct);
            }
            await _db.SaveChangesAsync();

            // Áp dụng khuyến mãi nếu có
            if (km != null && giamGia > 0)
            {
                _db.DonHangKhuyenMais.Add(new DonHangKhuyenMai
                {
                    DonHangID = donHang.DonHangID,
                    KhuyenMaiID = km.KhuyenMaiID,
                    SoTienGiam = giamGia
                });
                km.SoLuongDaDung++;
                await _db.SaveChangesAsync();
            }

            // Ghi nhật ký
            _db.NhatKyDons.Add(new NhatKyDon
            {
                DonHangID = donHang.DonHangID,
                HanhDong = "TaoMoi",
                NgayTao = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // Đóng phiên
            phien.TrangThai = "DaDong";
            phien.ThoiGianKetThuc = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { success = true, donHangId = donHang.DonHangID, soThuTu });
        }

        // ============================================================
        // GET /Order/XacNhan/{id}?token=xxx — Trang xác nhận thành công
        // ============================================================
        public async Task<IActionResult> XacNhan(int id, string token)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.ChiTietDons).ThenInclude(c => c.MonAn)
                .Include(d => d.DonHangKhuyenMais).ThenInclude(dk => dk.KhuyenMai)
                .FirstOrDefaultAsync(d => d.DonHangID == id);

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
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k =>
                k.MaCode == req.MaCode.ToUpper() &&
                k.TrangThai == "HoatDong" &&
                k.NgayBatDau <= DateTime.Now &&
                k.NgayKetThuc >= DateTime.Now);

            if (km == null)
                return Json(new { valid = false, message = "Mã không tồn tại hoặc đã hết hạn." });

            if (km.SoLuongToiDa.HasValue && km.SoLuongDaDung >= km.SoLuongToiDa)
                return Json(new { valid = false, message = "Mã đã hết lượt sử dụng." });

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
