using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using webHuyBeo.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Thêm SignalR

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Dependency Injection cho Repositories và Services
builder.Services.AddScoped(typeof(webHuyBeo.Repositories.IRepository<>), typeof(webHuyBeo.Repositories.Repository<>));
builder.Services.AddScoped<webHuyBeo.Services.IAuthService, webHuyBeo.Services.AuthService>();
builder.Services.AddScoped<webHuyBeo.Services.IMenuService, webHuyBeo.Services.MenuService>();
builder.Services.AddScoped<webHuyBeo.Services.IPaymentService, webHuyBeo.Services.PaymentService>();
builder.Services.AddScoped<webHuyBeo.Services.IOrderService, webHuyBeo.Services.OrderService>();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.Name = "HuyBeo.Auth";
        options.Cookie.HttpOnly = true;
    });

var app = builder.Build();

// Seed default admin account
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate(); // Ensure database is up to date
    if (!db.NguoiDungs.Any(u => u.VaiTro == "Admin"))
    {
        // Hash: SHA256("admin" + "HuyBeoSalt2026")
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes("admin" + "HuyBeoSalt2026");
        var hash = Convert.ToBase64String(sha.ComputeHash(bytes));

        db.NguoiDungs.Add(new NguoiDung
        {
            TenDangNhap = "admin",
            MatKhau = hash,
            Hoten = "Quản trị viên",
            VaiTro = "Admin",
            TrangThai = "DangLamViec"
        });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<webHuyBeo.Hubs.OrderHub>("/orderHub");

app.Run();

