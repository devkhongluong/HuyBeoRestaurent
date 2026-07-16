using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webHuyBeo.Models;

namespace webHuyBeo.Controllers
{
    [Authorize(Roles = "Admin,QuanLy,Bep")]
    public class BepController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BepController(ApplicationDbContext db)
        {
            _db = db;
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

            // Get orders that are confirmed or being prepared
            var donHangs = await _db.DonHangs
                .Include(d => d.ChiTietDons).ThenInclude(c => c.MonAn)
                .Include(d => d.ChiTietDons).ThenInclude(c => c.ChiTietToppings).ThenInclude(ct => ct.Topping)
                .Where(d => d.TrangThai == "DaXacNhan" || d.TrangThai == "DangCheBien")
                .OrderBy(d => d.NgayTao)
                .ToListAsync();

            return View(donHangs);
        }

        // POST: /Bep/BatDauLam — Start preparing an order
        [HttpPost]
        public async Task<IActionResult> BatDauLam(int id)
        {
            var don = await _db.DonHangs
                .Include(d => d.ChiTietDons)
                .FirstOrDefaultAsync(d => d.DonHangID == id);

            if (don == null)
                return Json(new { success = false, message = "Đơn không tồn tại." });

            if (don.TrangThai != "DaXacNhan")
                return Json(new { success = false, message = "Đơn không ở trạng thái có thể bắt đầu." });

            don.TrangThai = "DangCheBien";

            // Mark all items as "DangLam"
            foreach (var ct in don.ChiTietDons)
            {
                ct.TrangThaiBep = "DangLam";
                ct.ThoiGianBatDauCB = DateTime.Now;
            }

            _db.NhatKyDons.Add(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = "BatDauCheBien",
                NgayTao = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: /Bep/XongMon — Mark a single item as done
        [HttpPost]
        public async Task<IActionResult> XongMon(int chiTietId)
        {
            var ct = await _db.ChiTietDons
                .Include(c => c.DonHang)
                .FirstOrDefaultAsync(c => c.ChiTietID == chiTietId);

            if (ct == null)
                return Json(new { success = false, message = "Món không tồn tại." });

            ct.TrangThaiBep = "HoanThanh";
            ct.ThoiGianHoanThanh = DateTime.Now;
            await _db.SaveChangesAsync();

            // Check if all items in the order are done
            var allDone = await _db.ChiTietDons
                .Where(c => c.DonHangID == ct.DonHangID)
                .AllAsync(c => c.TrangThaiBep == "HoanThanh");

            return Json(new { success = true, allDone });
        }

        // POST: /Bep/HoanLai — Undo: mark item back to "DangLam"
        [HttpPost]
        public async Task<IActionResult> HoanLai(int chiTietId)
        {
            var ct = await _db.ChiTietDons.FindAsync(chiTietId);
            if (ct == null)
                return Json(new { success = false });

            ct.TrangThaiBep = "DangLam";
            ct.ThoiGianHoanThanh = null;
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Bep/XongDon — Mark entire order as ready to serve
        [HttpPost]
        public async Task<IActionResult> XongDon(int id)
        {
            var don = await _db.DonHangs
                .Include(d => d.ChiTietDons)
                .FirstOrDefaultAsync(d => d.DonHangID == id);

            if (don == null)
                return Json(new { success = false, message = "Đơn không tồn tại." });

            // Mark all remaining items as done
            foreach (var ct in don.ChiTietDons.Where(c => c.TrangThaiBep != "HoanThanh"))
            {
                ct.TrangThaiBep = "HoanThanh";
                ct.ThoiGianHoanThanh = DateTime.Now;
            }

            don.TrangThai = "SanSangPhucVu";

            _db.NhatKyDons.Add(new NhatKyDon
            {
                DonHangID = id,
                NguoiDungID = GetCurrentUserId(),
                HanhDong = "SanSangPhucVu",
                NgayTao = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"Đơn #{don.SoThuTu} sẵn sàng phục vụ!" });
        }
    }
}
