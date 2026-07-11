using Microsoft.EntityFrameworkCore;

namespace webHuyBeo.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PhienOrder> PhienOrders { get; set; }
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<MonAn> MonAns { get; set; }
        public DbSet<Topping> Toppings { get; set; }
        public DbSet<MonAnTopping> MonAnToppings { get; set; }
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<ChiTietDon> ChiTietDons { get; set; }
        public DbSet<ChiTietTopping> ChiTietToppings { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<DonHangKhuyenMai> DonHangKhuyenMais { get; set; }
        public DbSet<NhatKyDon> NhatKyDons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Composite Keys
            modelBuilder.Entity<MonAnTopping>()
                .HasKey(mat => new { mat.MonAnID, mat.ToppingID });

            modelBuilder.Entity<ChiTietTopping>()
                .HasKey(ctt => new { ctt.ChiTietID, ctt.ToppingID });

            modelBuilder.Entity<DonHangKhuyenMai>()
                .HasKey(dhk => new { dhk.DonHangID, dhk.KhuyenMaiID });

            // Ensure unique username
            modelBuilder.Entity<NguoiDung>()
                .HasIndex(nd => nd.TenDangNhap)
                .IsUnique();

            // Set Delete Behaviors to avoid cascade delete errors
            modelBuilder.Entity<ChiTietDon>()
                .HasOne(c => c.DonHang)
                .WithMany(d => d.ChiTietDons)
                .HasForeignKey(c => c.DonHangID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietDon>()
                .HasOne(c => c.MonAn)
                .WithMany(m => m.ChiTietDons)
                .HasForeignKey(c => c.MonAnID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonHang>()
                .HasOne(d => d.NguoiDung)
                .WithMany(n => n.DonHangs)
                .HasForeignKey(d => d.NguoiDungID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<NhatKyDon>()
                .HasOne(n => n.NguoiDung)
                .WithMany(u => u.NhatKyDons)
                .HasForeignKey(n => n.NguoiDungID)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
