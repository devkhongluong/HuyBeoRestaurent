using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using webHuyBeo.Models;
using webHuyBeo.Repositories;
using webHuyBeo.Hubs;

namespace webHuyBeo.Controllers
{
    [Authorize(Roles = "Admin,QuanLy,ThuNgan")]
    public class PosController : Controller
    {
        private readonly IRepository<DonHang> _donHangRepo;
        private readonly IRepository<NhatKyDon> _nhatKyRepo;
        private readonly IHubContext<OrderHub> _hubContext;

        public PosController(
            IRepository<DonHang> donHangRepo, 
            IRepository<NhatKyDon> nhatKyRepo,
            IHubContext<OrderHub> hubContext)
        {
            _donHangRepo = donHangRepo;
            _nhatKyRepo = nhatKyRepo;
            _hubContext = hubContext;
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

            var query = _donHangRepo.Query(
                d => d.TrangThai != "Huy", 
                includeProperties: "ChiTietDons.MonAn,PhienOrder");

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

            var donHangs = query
                .OrderByDescending(d => d.TrangThai == "ChoXacNhan")
                .ThenByDescending(d => d.NgayTao)
                .ToList();

            var allActiveDonHangs = await _donHangRepo.GetAllAsync();

            // Count by status for tabs
            ViewBag.CountCho = allActiveDonHangs.Count(d => d.TrangThai == "ChoXacNhan");
            ViewBag.CountXacNhan = allActiveDonHangs.Count(d => d.TrangThai == "DaXacNhan");
            ViewBag.CountDangLam = allActiveDonHangs.Count(d => d.TrangThai == "DangCheBien");
            ViewBag.CountSanSang = allActiveDonHangs.Count(d => d.TrangThai == "SanSangPhucVu");
            ViewBag.CountHoanThanh = allActiveDonHangs.Count(d =>
                d.TrangThai == "DaHoanThanh" && d.NgayTao.Date == DateTime.Today);
            ViewBag.TrangThai = trangThai;

            // Revenue today
            ViewBag.DoanhThuHomNay = allActiveDonHangs
                .Where(d => d.NgayTao.Date == DateTime.Today && d.TrangThaiTT == "DaThanhToan")
                .Sum(d => (decimal?)d.ThanhTien) ?? 0;

            return View(donHangs);
        }

        // GET: /Pos/ChiTiet/5
        public async Task<IActionResult> ChiTiet(int id)
        {
            var don = await _donHangRepo.GetFirstOrDefaultAsync(
                d => d.DonHangID == id,
                includeProperties: "ChiTietDons.MonAn,ChiTietDons.ChiTietToppings.Topping,DonHangKhuyenMais.KhuyenMai,NhatKyDons.NguoiDung,PhienOrder");

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
            var don = await _donHangRepo.GetFirstOrDefaultAsync(d => d.DonHangID == id);
            if (don == null) return Json(new { success = false, message = "Đơn không tồn tại." });
            if (don.TrangThai != "ChoXacNhan")
                return Json(new { success = false, message = "Đơn không ở trạng thái chờ xác nhận." });

            don.TrangThai = "DaXacNhan";
            _donHangRepo.Update(don);

            await _nhatKyRepo.AddAsync(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = "XacNhan",
                NgayTao = DateTime.Now
            });
            await _nhatKyRepo.SaveAsync();

            // Bắn tín hiệu SignalR cho Bếp biết có đơn vừa được xác nhận để nấu
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder");
            await _hubContext.Clients.All.SendAsync("ReceiveOrderStatusChanged", id, "DaXacNhan");

            return Json(new { success = true, message = $"Đã xác nhận đơn #{don.SoThuTu}." });
        }

        // POST: /Pos/ChuyenTrangThai
        [HttpPost]
        public async Task<IActionResult> ChuyenTrangThai(int id, string trangThaiMoi)
        {
            var don = await _donHangRepo.GetFirstOrDefaultAsync(d => d.DonHangID == id);
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

            _donHangRepo.Update(don);

            await _nhatKyRepo.AddAsync(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = trangThaiMoi,
                NgayTao = DateTime.Now
            });
            await _nhatKyRepo.SaveAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveOrderStatusChanged", id, trangThaiMoi);

            return Json(new { success = true, message = $"Đã cập nhật đơn #{don.SoThuTu}." });
        }

        // POST: /Pos/ThuTien
        [HttpPost]
        public async Task<IActionResult> ThuTien(int id, string phuongThuc)
        {
            var don = await _donHangRepo.GetFirstOrDefaultAsync(d => d.DonHangID == id);
            if (don == null) return Json(new { success = false, message = "Đơn không tồn tại." });

            don.TrangThaiTT = "DaThanhToan";
            don.PhuongThucTT = phuongThuc;
            _donHangRepo.Update(don);

            await _nhatKyRepo.AddAsync(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = $"ThanhToan_{phuongThuc}",
                NgayTao = DateTime.Now
            });
            await _nhatKyRepo.SaveAsync();

            return Json(new { success = true, message = $"Đã thu tiền đơn #{don.SoThuTu}." });
        }

        // POST: /Pos/HuyDon
        [HttpPost]
        public async Task<IActionResult> HuyDon(int id, string? lyDo)
        {
            var don = await _donHangRepo.GetFirstOrDefaultAsync(d => d.DonHangID == id);
            if (don == null) return Json(new { success = false, message = "Đơn không tồn tại." });
            if (don.TrangThai == "DaHoanThanh")
                return Json(new { success = false, message = "Không thể hủy đơn đã hoàn thành." });

            don.TrangThai = "Huy";
            _donHangRepo.Update(don);

            await _nhatKyRepo.AddAsync(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = $"HuyDon: {lyDo ?? "Không có lý do"}",
                NgayTao = DateTime.Now
            });
            await _nhatKyRepo.SaveAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveOrderStatusChanged", id, "Huy");

            return Json(new { success = true, message = $"Đã hủy đơn #{don.SoThuTu}." });
        }

        // GET: /Pos/InBill/5
        public async Task<IActionResult> InBill(int id)
        {
            var don = await _donHangRepo.GetFirstOrDefaultAsync(
                d => d.DonHangID == id,
                includeProperties: "ChiTietDons.MonAn,DonHangKhuyenMais.KhuyenMai");

            if (don == null) return NotFound();

            return View(don);
        }
    }
}
