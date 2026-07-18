using Microsoft.AspNetCore.SignalR;
using webHuyBeo.Hubs;
using webHuyBeo.Models;
using webHuyBeo.Repositories;

namespace webHuyBeo.Services
{
    public interface IOrderService
    {
        Task<(bool Success, string Message, int? DonHangId, string? SoThuTu)> CreateOrderAsync(
            string token, string? loaiDon, string? maKhuyenMai, string? ghiChuChung, 
            IEnumerable<Controllers.CartItem> items);
        
        Task<bool> ValidatePhienOrderAsync(string token);
    }

    public class OrderService : IOrderService
    {
        private readonly IRepository<PhienOrder> _phienRepo;
        private readonly IRepository<DonHang> _donHangRepo;
        private readonly IRepository<ChiTietDon> _chiTietRepo;
        private readonly IRepository<MonAn> _monAnRepo;
        private readonly IRepository<Topping> _toppingRepo;
        private readonly IRepository<NhatKyDon> _nhatKyRepo;
        private readonly IRepository<DonHangKhuyenMai> _donHangKhuyenMaiRepo;
        private readonly IPaymentService _paymentService;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderService(
            IRepository<PhienOrder> phienRepo,
            IRepository<DonHang> donHangRepo,
            IRepository<ChiTietDon> chiTietRepo,
            IRepository<MonAn> monAnRepo,
            IRepository<Topping> toppingRepo,
            IRepository<NhatKyDon> nhatKyRepo,
            IRepository<DonHangKhuyenMai> donHangKhuyenMaiRepo,
            IPaymentService paymentService,
            IHubContext<OrderHub> hubContext)
        {
            _phienRepo = phienRepo;
            _donHangRepo = donHangRepo;
            _chiTietRepo = chiTietRepo;
            _monAnRepo = monAnRepo;
            _toppingRepo = toppingRepo;
            _nhatKyRepo = nhatKyRepo;
            _donHangKhuyenMaiRepo = donHangKhuyenMaiRepo;
            _paymentService = paymentService;
            _hubContext = hubContext;
        }

        public async Task<bool> ValidatePhienOrderAsync(string token)
        {
            var phien = await _phienRepo.GetFirstOrDefaultAsync(p => p.MaToken == token);
            return phien != null && phien.TrangThai != "DaDong";
        }

        public async Task<(bool Success, string Message, int? DonHangId, string? SoThuTu)> CreateOrderAsync(
            string token, string? loaiDon, string? maKhuyenMai, string? ghiChuChung, 
            IEnumerable<Controllers.CartItem> items)
        {
            if (items == null || !items.Any())
                return (false, "Giỏ hàng trống!", null, null);

            var phien = await _phienRepo.GetFirstOrDefaultAsync(p => p.MaToken == token);
            if (phien == null || phien.TrangThai == "DaDong")
                return (false, "Phiên đặt hàng đã hết hạn. Vui lòng quét lại QR.", null, null);

            var countToday = (await _donHangRepo.GetAllAsync(d => d.NgayTao.Date == DateTime.Today)).Count();
            var soThuTu = (countToday + 1).ToString();

            decimal tongTien = 0;
            var chiTietList = new List<ChiTietDon>();

            foreach (var item in items)
            {
                var mon = await _monAnRepo.GetFirstOrDefaultAsync(m => m.MonAnID == item.MonAnID);
                if (mon == null) continue;

                decimal toppingGia = 0;
                if (item.ToppingIDs != null)
                {
                    foreach (var tid in item.ToppingIDs)
                    {
                        var tp = await _toppingRepo.GetFirstOrDefaultAsync(t => t.ToppingID == tid);
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

            var km = await _paymentService.ValidateKhuyenMaiAsync(maKhuyenMai ?? "", tongTien);
            if (!string.IsNullOrEmpty(maKhuyenMai) && km == null && tongTien > 0)
            {
                // Note: Trả về lỗi nếu mã không hợp lệ, hoặc tiếp tục. Ở đây bỏ qua để đơn giản.
            }

            var (giamGia, thanhTienCuoi) = _paymentService.CalculateTotal(tongTien, km);

            var donHang = new DonHang
            {
                PhienID = phien.PhienID,
                LoaiDon = loaiDon ?? "TaiCho",
                TrangThai = "ChoXacNhan",
                SoThuTu = soThuTu,
                TongTien = tongTien,
                TongGiamGia = giamGia > 0 ? giamGia : null,
                ThanhTien = thanhTienCuoi,
                TrangThaiTT = "ChuaThanhToan",
                NgayTao = DateTime.Now
            };

            await _donHangRepo.AddAsync(donHang);
            await _donHangRepo.SaveAsync();

            foreach (var ct in chiTietList)
            {
                ct.DonHangID = donHang.DonHangID;
                await _chiTietRepo.AddAsync(ct);
            }
            await _chiTietRepo.SaveAsync();

            if (km != null && giamGia > 0)
            {
                await _donHangKhuyenMaiRepo.AddAsync(new DonHangKhuyenMai
                {
                    DonHangID = donHang.DonHangID,
                    KhuyenMaiID = km.KhuyenMaiID,
                    SoTienGiam = giamGia
                });
                km.SoLuongDaDung++;
                await _donHangKhuyenMaiRepo.SaveAsync();
            }

            await _nhatKyRepo.AddAsync(new NhatKyDon
            {
                DonHangID = donHang.DonHangID,
                HanhDong = "TaoMoi",
                NgayTao = DateTime.Now
            });
            await _nhatKyRepo.SaveAsync();

            phien.TrangThai = "DaDong";
            phien.ThoiGianKetThuc = DateTime.Now;
            _phienRepo.Update(phien);
            await _phienRepo.SaveAsync();

            // GỬI TÍN HIỆU REAL-TIME BẰNG SIGNALR ĐẾN BẾP
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder");

            return (true, "Thành công", donHang.DonHangID, soThuTu);
        }
    }
}
