using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webHuyBeo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhMucs",
                columns: table => new
                {
                    DanhMucID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMucs", x => x.DanhMucID);
                });

            migrationBuilder.CreateTable(
                name: "KhuyenMais",
                columns: table => new
                {
                    KhuyenMaiID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenKM = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LoaiGiam = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GiaTri = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GiamToiDa = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DonToiThieu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SoLuongToiDa = table.Column<int>(type: "int", nullable: true),
                    SoLuongDaDung = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMais", x => x.KhuyenMaiID);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungs",
                columns: table => new
                {
                    NguoiDungID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Hoten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDungs", x => x.NguoiDungID);
                });

            migrationBuilder.CreateTable(
                name: "PhienOrders",
                columns: table => new
                {
                    PhienID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhienOrders", x => x.PhienID);
                });

            migrationBuilder.CreateTable(
                name: "Toppings",
                columns: table => new
                {
                    ToppingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTopping = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GiaThem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toppings", x => x.ToppingID);
                });

            migrationBuilder.CreateTable(
                name: "MonAns",
                columns: table => new
                {
                    MonAnID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DanhMucID = table.Column<int>(type: "int", nullable: false),
                    TenMon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GiaVon = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConBan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonAns", x => x.MonAnID);
                    table.ForeignKey(
                        name: "FK_MonAns_DanhMucs_DanhMucID",
                        column: x => x.DanhMucID,
                        principalTable: "DanhMucs",
                        principalColumn: "DanhMucID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonHangs",
                columns: table => new
                {
                    DonHangID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhienID = table.Column<int>(type: "int", nullable: false),
                    NguoiDungID = table.Column<int>(type: "int", nullable: true),
                    LoaiDon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoThuTu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongGiamGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThaiTT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhuongThucTT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangs", x => x.DonHangID);
                    table.ForeignKey(
                        name: "FK_DonHangs_NguoiDungs_NguoiDungID",
                        column: x => x.NguoiDungID,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DonHangs_PhienOrders_PhienID",
                        column: x => x.PhienID,
                        principalTable: "PhienOrders",
                        principalColumn: "PhienID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonAnToppings",
                columns: table => new
                {
                    MonAnID = table.Column<int>(type: "int", nullable: false),
                    ToppingID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonAnToppings", x => new { x.MonAnID, x.ToppingID });
                    table.ForeignKey(
                        name: "FK_MonAnToppings_MonAns_MonAnID",
                        column: x => x.MonAnID,
                        principalTable: "MonAns",
                        principalColumn: "MonAnID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonAnToppings_Toppings_ToppingID",
                        column: x => x.ToppingID,
                        principalTable: "Toppings",
                        principalColumn: "ToppingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDons",
                columns: table => new
                {
                    ChiTietID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangID = table.Column<int>(type: "int", nullable: false),
                    MonAnID = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThaiBep = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThoiGianBatDauCB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDons", x => x.ChiTietID);
                    table.ForeignKey(
                        name: "FK_ChiTietDons_DonHangs_DonHangID",
                        column: x => x.DonHangID,
                        principalTable: "DonHangs",
                        principalColumn: "DonHangID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDons_MonAns_MonAnID",
                        column: x => x.MonAnID,
                        principalTable: "MonAns",
                        principalColumn: "MonAnID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonHangKhuyenMais",
                columns: table => new
                {
                    DonHangID = table.Column<int>(type: "int", nullable: false),
                    KhuyenMaiID = table.Column<int>(type: "int", nullable: false),
                    SoTienGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangKhuyenMais", x => new { x.DonHangID, x.KhuyenMaiID });
                    table.ForeignKey(
                        name: "FK_DonHangKhuyenMais_DonHangs_DonHangID",
                        column: x => x.DonHangID,
                        principalTable: "DonHangs",
                        principalColumn: "DonHangID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonHangKhuyenMais_KhuyenMais_KhuyenMaiID",
                        column: x => x.KhuyenMaiID,
                        principalTable: "KhuyenMais",
                        principalColumn: "KhuyenMaiID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyDons",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangID = table.Column<int>(type: "int", nullable: false),
                    NguoiDungID = table.Column<int>(type: "int", nullable: true),
                    HanhDong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyDons", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_NhatKyDons_DonHangs_DonHangID",
                        column: x => x.DonHangID,
                        principalTable: "DonHangs",
                        principalColumn: "DonHangID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NhatKyDons_NguoiDungs_NguoiDungID",
                        column: x => x.NguoiDungID,
                        principalTable: "NguoiDungs",
                        principalColumn: "NguoiDungID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietToppings",
                columns: table => new
                {
                    ChiTietID = table.Column<int>(type: "int", nullable: false),
                    ToppingID = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietToppings", x => new { x.ChiTietID, x.ToppingID });
                    table.ForeignKey(
                        name: "FK_ChiTietToppings_ChiTietDons_ChiTietID",
                        column: x => x.ChiTietID,
                        principalTable: "ChiTietDons",
                        principalColumn: "ChiTietID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietToppings_Toppings_ToppingID",
                        column: x => x.ToppingID,
                        principalTable: "Toppings",
                        principalColumn: "ToppingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDons_DonHangID",
                table: "ChiTietDons",
                column: "DonHangID");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDons_MonAnID",
                table: "ChiTietDons",
                column: "MonAnID");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietToppings_ToppingID",
                table: "ChiTietToppings",
                column: "ToppingID");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangKhuyenMais_KhuyenMaiID",
                table: "DonHangKhuyenMais",
                column: "KhuyenMaiID");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_NguoiDungID",
                table: "DonHangs",
                column: "NguoiDungID");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_PhienID",
                table: "DonHangs",
                column: "PhienID");

            migrationBuilder.CreateIndex(
                name: "IX_MonAns_DanhMucID",
                table: "MonAns",
                column: "DanhMucID");

            migrationBuilder.CreateIndex(
                name: "IX_MonAnToppings_ToppingID",
                table: "MonAnToppings",
                column: "ToppingID");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_TenDangNhap",
                table: "NguoiDungs",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyDons_DonHangID",
                table: "NhatKyDons",
                column: "DonHangID");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyDons_NguoiDungID",
                table: "NhatKyDons",
                column: "NguoiDungID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietToppings");

            migrationBuilder.DropTable(
                name: "DonHangKhuyenMais");

            migrationBuilder.DropTable(
                name: "MonAnToppings");

            migrationBuilder.DropTable(
                name: "NhatKyDons");

            migrationBuilder.DropTable(
                name: "ChiTietDons");

            migrationBuilder.DropTable(
                name: "KhuyenMais");

            migrationBuilder.DropTable(
                name: "Toppings");

            migrationBuilder.DropTable(
                name: "DonHangs");

            migrationBuilder.DropTable(
                name: "MonAns");

            migrationBuilder.DropTable(
                name: "NguoiDungs");

            migrationBuilder.DropTable(
                name: "PhienOrders");

            migrationBuilder.DropTable(
                name: "DanhMucs");
        }
    }
}
