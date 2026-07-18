using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using webHuyBeo.Models;
using webHuyBeo.Repositories;
using webHuyBeo.Hubs;

namespace webHuyBeo.Controllers
{
    [Authorize(Roles = "Admin,QuanLy,Bep")]
    public class BepController : Controller
    {
        private readonly IRepository<DonHang> _donHangRepo;
        private readonly IRepository<ChiTietDon> _chiTietRepo;
        private readonly IRepository<NhatKyDon> _nhatKyRepo;
        private readonly IHubContext<OrderHub> _hubContext;

        public BepController(
            IRepository<DonHang> donHangRepo,
            IRepository<ChiTietDon> chiTietRepo,
            IRepository<NhatKyDon> nhatKyRepo,
            IHubContext<OrderHub> hubContext)
        {
            _donHangRepo = donHangRepo;
            _chiTietRepo = chiTietRepo;
            _nhatKyRepo = nhatKyRepo;
            _hubContext = hubContext;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET: /Bep
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Bếp (KDS)";
            ViewData["ActiveMenu"] = "bep";

            var donHangs = await _donHangRepo.GetAllAsync(
                d => d.TrangThai == "DaXacNhan" || d.TrangThai == "DangCheBien",
                includeProperties: "ChiTietDons.MonAn,ChiTietDons.ChiTietToppings.Topping");

            donHangs = donHangs.OrderBy(d => d.NgayTao).ToList();
            return View(donHangs);
        }

        // POST: /Bep/BatDauLam — Start preparing an order
        [HttpPost]
        public async Task<IActionResult> BatDauLam(int id)
        {
            var don = await _donHangRepo.GetFirstOrDefaultAsync(
                d => d.DonHangID == id,
                includeProperties: "ChiTietDons");

            if (don == null)
                return Json(new { success = false, message = "Đơn không tồn tại." });

            if (don.TrangThai != "DaXacNhan")
                return Json(new { success = false, message = "Đơn không ở trạng thái có thể bắt đầu." });

            don.TrangThai = "DangCheBien";

            foreach (var ct in don.ChiTietDons)
            {
                ct.TrangThaiBep = "DangLam";
                ct.ThoiGianBatDauCB = DateTime.Now;
                _chiTietRepo.Update(ct);
            }

            _donHangRepo.Update(don);
            
            await _nhatKyRepo.AddAsync(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = "BatDauCheBien",
                NgayTao = DateTime.Now
            });

            await _donHangRepo.SaveAsync(); // Save all changes

            // Bắn tín hiệu SignalR cập nhật trạng thái đơn (để Khách/Thu ngân có thể biết)
            await _hubContext.Clients.All.SendAsync("ReceiveOrderStatusChanged", id, "DangCheBien");

            return Json(new { success = true });
        }

        // POST: /Bep/XongMon — Mark a single item as done
        [HttpPost]
        public async Task<IActionResult> XongMon(int chiTietId)
        {
            var ct = await _chiTietRepo.GetFirstOrDefaultAsync(
                c => c.ChiTietID == chiTietId,
                includeProperties: "DonHang");

            if (ct == null)
                return Json(new { success = false, message = "Món không tồn tại." });

            ct.TrangThaiBep = "HoanThanh";
            ct.ThoiGianHoanThanh = DateTime.Now;
            _chiTietRepo.Update(ct);
            await _chiTietRepo.SaveAsync();

            var allItems = await _chiTietRepo.GetAllAsync(c => c.DonHangID == ct.DonHangID);
            var allDone = allItems.All(c => c.TrangThaiBep == "HoanThanh");

            return Json(new { success = true, allDone });
        }

        // POST: /Bep/HoanLai — Undo: mark item back to "DangLam"
        [HttpPost]
        public async Task<IActionResult> HoanLai(int chiTietId)
        {
            var ct = await _chiTietRepo.GetFirstOrDefaultAsync(c => c.ChiTietID == chiTietId);
            if (ct == null)
                return Json(new { success = false });

            ct.TrangThaiBep = "DangLam";
            ct.ThoiGianHoanThanh = null;
            _chiTietRepo.Update(ct);
            await _chiTietRepo.SaveAsync();

            return Json(new { success = true });
        }

        // POST: /Bep/XongDon — Mark entire order as ready to serve
        [HttpPost]
        public async Task<IActionResult> XongDon(int id)
        {
            var don = await _donHangRepo.GetFirstOrDefaultAsync(
                d => d.DonHangID == id,
                includeProperties: "ChiTietDons");

            if (don == null)
                return Json(new { success = false, message = "Đơn không tồn tại." });

            foreach (var ct in don.ChiTietDons.Where(c => c.TrangThaiBep != "HoanThanh"))
            {
                ct.TrangThaiBep = "HoanThanh";
                ct.ThoiGianHoanThanh = DateTime.Now;
                _chiTietRepo.Update(ct);
            }

            don.TrangThai = "SanSangPhucVu";
            _donHangRepo.Update(don);

            await _nhatKyRepo.AddAsync(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = "SanSangPhucVu",
                NgayTao = DateTime.Now
            });

            await _donHangRepo.SaveAsync();
            
            // Bắn tín hiệu SignalR cho Thu ngân biết Bếp đã nấu xong đơn này
            await _hubContext.Clients.All.SendAsync("ReceiveOrderStatusChanged", id, "SanSangPhucVu");

            return Json(new { success = true, message = $"Đơn #{don.SoThuTu} sẵn sàng phục vụ!" });
        }
    }
}
