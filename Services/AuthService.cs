using System.Security.Claims;
using webHuyBeo.Models;
using webHuyBeo.Repositories;

namespace webHuyBeo.Services
{
    public interface IAuthService
    {
        Task<NguoiDung?> AuthenticateAsync(string tenDangNhap, string matKhau);
        Task<bool> ChangePasswordAsync(int userId, string matKhauCu, string matKhauMoi);
        ClaimsPrincipal CreateClaimsPrincipal(NguoiDung user);
        string HashPassword(string password);
    }

    public class AuthService : IAuthService
    {
        private readonly IRepository<NguoiDung> _userRepository;

        public AuthService(IRepository<NguoiDung> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<NguoiDung?> AuthenticateAsync(string tenDangNhap, string matKhau)
        {
            var user = await _userRepository.GetFirstOrDefaultAsync(u => u.TenDangNhap == tenDangNhap);
            if (user == null) return null;

            if (user.TrangThai == "DaNghi") return null;

            var hashedInput = HashPassword(matKhau);
            if (user.MatKhau != hashedInput) return null;

            return user;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string matKhauCu, string matKhauMoi)
        {
            var user = await _userRepository.GetFirstOrDefaultAsync(u => u.NguoiDungID == userId);
            if (user == null) return false;

            var hashedOld = HashPassword(matKhauCu);
            if (user.MatKhau != hashedOld) return false;

            user.MatKhau = HashPassword(matKhauMoi);
            _userRepository.Update(user);
            await _userRepository.SaveAsync();
            return true;
        }

        public ClaimsPrincipal CreateClaimsPrincipal(NguoiDung user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.NguoiDungID.ToString()),
                new Claim(ClaimTypes.Name, user.Hoten),
                new Claim("TenDangNhap", user.TenDangNhap),
                new Claim(ClaimTypes.Role, user.VaiTro)
            };

            var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        public string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password + "HuyBeoSalt2026");
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}
