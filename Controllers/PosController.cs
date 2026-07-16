using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webHuyBeo.Models;

namespace webHuyBeo.Controllers
{
    [Authorize(Roles = "Admin,QuanLy,ThuNgan")]
    public class PosController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PosController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET: /Pos
        public async Task<IActionResult> Index(string? trangThai)
        {
            ViewData["Title"] = "POS Thu ngân";
            ViewData["ActiveMenu"] = "pos";

            var query = _db.DonHangs
                .Include(d => d.ChiTietDons).ThenInclude(c => c.MonAn)
                .Include(d => d.PhienOrder)
                .Where(d => d.TrangThai != "Huy")
                .AsQueryable();

            // Default: show pending orders
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }
            else
            {
                // Show all active (not completed, not cancelled)
                query = query.Where(d => d.TrangThai != "DaHoanThanh");
            }

            var donHangs = await query
                .OrderByDescending(d => d.TrangThai == "ChoXacNhan")
                .ThenByDescending(d => d.NgayTao)
                .ToListAsync();

            // Count by status for tabs
            ViewBag.CountCho = await _db.DonHangs.CountAsync(d => d.TrangThai == "ChoXacNhan");
            ViewBag.CountXacNhan = await _db.DonHangs.CountAsync(d => d.TrangThai == "DaXacNhan");
            ViewBag.CountDangLam = await _db.DonHangs.CountAsync(d => d.TrangThai == "DangCheBien");
            ViewBag.CountSanSang = await _db.DonHangs.CountAsync(d => d.TrangThai == "SanSangPhucVu");
            ViewBag.CountHoanThanh = await _db.DonHangs.CountAsync(d =>
                d.TrangThai == "DaHoanThanh" && d.NgayTao.Date == DateTime.Today);
            ViewBag.TrangThai = trangThai;

            // Revenue today
            ViewBag.DoanhThuHomNay = await _db.DonHangs
                .Where(d => d.NgayTao.Date == DateTime.Today && d.TrangThaiTT == "DaThanhToan")
                .SumAsync(d => (decimal?)d.ThanhTien) ?? 0;

            return View(donHangs);
        }

        // GET: /Pos/ChiTiet/5
        public async Task<IActionResult> ChiTiet(int id)
        {
            var don = await _db.DonHangs
                .Include(d => d.ChiTietDons).ThenInclude(c => c.MonAn)
                .Include(d => d.ChiTietDons).ThenInclude(c => c.ChiTietToppings).ThenInclude(ct => ct.Topping)
                .Include(d => d.DonHangKhuyenMais).ThenInclude(dk => dk.KhuyenMai)
                .Include(d => d.NhatKyDons).ThenInclude(n => n.NguoiDung)
                .Include(d => d.PhienOrder)
                .FirstOrDefaultAsync(d => d.DonHangID == id);

            if (don == null) return NotFound();

            return Json(new
            {
                donHangId = don.DonHangID,
                soThuTu = don.SoThuTu,
                loaiDon = don.LoaiDon,
                trangThai = don.TrangThai,
                trangThaiTT = don.TrangThaiTT,
                phuongThucTT = don.PhuongThucTT,
                tongTien = don.TongTien,
                tongGiamGia = don.TongGiamGia,
                thanhTien = don.ThanhTien,
                ngayTao = don.NgayTao.ToString("HH:mm dd/MM"),
                ngayHoanThanh = don.NgayHoanThanh?.ToString("HH:mm dd/MM"),
                chiTiets = don.ChiTietDons.Select(ct => new
                {
                    tenMon = ct.MonAn.TenMon,
                    soLuong = ct.SoLuong,
                    donGia = ct.DonGia,
                    thanhTien = ct.ThanhTien,
                    ghiChu = ct.GhiChu,
                    trangThaiBep = ct.TrangThaiBep,
                    toppings = ct.ChiTietToppings?.Select(t => new
                    {
                        ten = t.Topping.TenTopping,
                        gia = t.DonGia
                    }).ToList()
                }).ToList(),
                khuyenMais = don.DonHangKhuyenMais.Select(km => new
                {
                    maCode = km.KhuyenMai.MaCode,
                    soTienGiam = km.SoTienGiam
                }).ToList(),
                nhatKys = don.NhatKyDons.OrderByDescending(n => n.NgayTao).Select(n => new
                {
                    hanhDong = n.HanhDong,
                    nguoi = n.NguoiDung?.Hoten ?? "Hệ thống",
                    thoiGian = n.NgayTao.ToString("HH:mm dd/MM")
                }).ToList()
            });
        }

        // POST: /Pos/XacNhan/5
        [HttpPost]
        public async Task<IActionResult> XacNhan(int id)
        {
            var don = await _db.DonHangs.FindAsync(id);
            if (don == null) return Json(new { success = false, message = "Đơn không tồn tại." });
            if (don.TrangThai != "ChoXacNhan")
                return Json(new { success = false, message = "Đơn không ở trạng thái chờ xác nhận." });

            don.TrangThai = "DaXacNhan";
            _db.NhatKyDons.Add(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = "XacNhan",
                NgayTao = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã xác nhận đơn #{don.SoThuTu}." });
        }

        // POST: /Pos/ChuyenTrangThai
        [HttpPost]
        public async Task<IActionResult> ChuyenTrangThai(int id, string trangThaiMoi)
        {
            var don = await _db.DonHangs.FindAsync(id);
            if (don == null) return Json(new { success = false, message = "Đơn không tồn tại." });

            // Valid state transitions
            var validTransitions = new Dictionary<string, string[]>
            {
                { "DaXacNhan", new[] { "DangCheBien" } },
                { "DangCheBien", new[] { "SanSangPhucVu" } },
                { "SanSangPhucVu", new[] { "DaHoanThanh" } }
            };

            if (!validTransitions.ContainsKey(don.TrangThai) ||
                !validTransitions[don.TrangThai].Contains(trangThaiMoi))
            {
                return Json(new { success = false, message = "Không thể chuyển sang trạng thái này." });
            }

            don.TrangThai = trangThaiMoi;
            if (trangThaiMoi == "DaHoanThanh")
                don.NgayHoanThanh = DateTime.Now;

            _db.NhatKyDons.Add(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = trangThaiMoi,
                NgayTao = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã cập nhật đơn #{don.SoThuTu}." });
        }

        // POST: /Pos/ThuTien
        [HttpPost]
        public async Task<IActionResult> ThuTien(int id, string phuongThuc)
        {
            var don = await _db.DonHangs.FindAsync(id);
            if (don == null) return Json(new { success = false, message = "Đơn không tồn tại." });

            don.TrangThaiTT = "DaThanhToan";
            don.PhuongThucTT = phuongThuc;

            _db.NhatKyDons.Add(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = $"ThanhToan_{phuongThuc}",
                NgayTao = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã thu tiền đơn #{don.SoThuTu}." });
        }

        // POST: /Pos/HuyDon
        [HttpPost]
        public async Task<IActionResult> HuyDon(int id, string? lyDo)
        {
            var don = await _db.DonHangs.FindAsync(id);
            if (don == null) return Json(new { success = false, message = "Đơn không tồn tại." });
            if (don.TrangThai == "DaHoanThanh")
                return Json(new { success = false, message = "Không thể hủy đơn đã hoàn thành." });

            don.TrangThai = "Huy";

            _db.NhatKyDons.Add(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = $"HuyDon: {lyDo ?? "Không có lý do"}",
                NgayTao = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã hủy đơn #{don.SoThuTu}." });
        }

        // GET: /Pos/InBill/5
        public async Task<IActionResult> InBill(int id)
        {
            var don = await _db.DonHangs
                .Include(d => d.ChiTietDons).ThenInclude(c => c.MonAn)
                .Include(d => d.DonHangKhuyenMais).ThenInclude(dk => dk.KhuyenMai)
                .FirstOrDefaultAsync(d => d.DonHangID == id);

            if (don == null) return NotFound();

            return View(don);
        }
    }
}
