using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webHuyBeo.Services;

namespace webHuyBeo.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Auth/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role == "Bep") return RedirectToAction("Index", "Bep");
                if (role == "ThuNgan") return RedirectToAction("Index", "Pos");
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string tenDangNhap, string matKhau, string? returnUrl)
        {
            if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(matKhau))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var user = await _authService.AuthenticateAsync(tenDangNhap, matKhau);
            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập, mật khẩu không đúng hoặc tài khoản đã bị vô hiệu hóa.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var principal = _authService.CreateClaimsPrincipal(user);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                });

            // Redirect based on role
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.VaiTro switch
            {
                "Bep" => RedirectToAction("Index", "Bep"),
                "ThuNgan" => RedirectToAction("Index", "Pos"),
                _ => RedirectToAction("Index", "Admin")
            };
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Auth/Logout (for sidebar link)
        public async Task<IActionResult> LogoutGet()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Auth/DoiMatKhau
        [Authorize]
        public IActionResult DoiMatKhau()
        {
            return View();
        }

        // POST: /Auth/DoiMatKhau
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            if (string.IsNullOrEmpty(matKhauCu) || string.IsNullOrEmpty(matKhauMoi))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (matKhauMoi.Length < 4)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 4 ký tự.";
                return View();
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                ViewBag.Error = "Xác nhận mật khẩu không khớp.";
                return View();
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdString == null || !int.TryParse(userIdString, out int userId)) return NotFound();

            var success = await _authService.ChangePasswordAsync(userId, matKhauCu, matKhauMoi);
            if (!success)
            {
                ViewBag.Error = "Mật khẩu hiện tại không đúng.";
                return View();
            }

            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }

        // GET: /Auth/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
