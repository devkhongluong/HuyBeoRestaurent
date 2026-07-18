using webHuyBeo.Models;
using webHuyBeo.Repositories;

namespace webHuyBeo.Services
{
    public interface IPaymentService
    {
        Task<KhuyenMai?> ValidateKhuyenMaiAsync(string maCode, decimal tongTien);
        (decimal GiamGia, decimal ThanhTienCuoi) CalculateTotal(decimal tongTien, KhuyenMai? km);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IRepository<KhuyenMai> _khuyenMaiRepo;

        public PaymentService(IRepository<KhuyenMai> khuyenMaiRepo)
        {
            _khuyenMaiRepo = khuyenMaiRepo;
        }

        public async Task<KhuyenMai?> ValidateKhuyenMaiAsync(string maCode, decimal tongTien)
        {
            if (string.IsNullOrEmpty(maCode)) return null;

            var km = await _khuyenMaiRepo.GetFirstOrDefaultAsync(k =>
                k.MaCode == maCode.ToUpper() &&
                k.TrangThai == "HoatDong" &&
                k.NgayBatDau <= DateTime.Now &&
                k.NgayKetThuc >= DateTime.Now);

            if (km == null) return null;
            if (km.SoLuongToiDa.HasValue && km.SoLuongDaDung >= km.SoLuongToiDa) return null;
            if (km.DonToiThieu.HasValue && tongTien < km.DonToiThieu) return null;

            return km;
        }

        public (decimal GiamGia, decimal ThanhTienCuoi) CalculateTotal(decimal tongTien, KhuyenMai? km)
        {
            decimal giamGia = 0;
            if (km != null)
            {
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

            return (giamGia, tongTien - giamGia);
        }
    }
}
