using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChiNhanhs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenChiNhanh = table.Column<string>(type: "text", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiNhanhs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhachHangs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    HoTen = table.Column<string>(type: "text", nullable: false),
                    SoDienThoai = table.Column<string>(type: "text", nullable: false),
                    DiaChi = table.Column<string>(type: "text", nullable: false),
                    DiemTichLuy = table.Column<int>(type: "integer", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHangs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhanViens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    HoTen = table.Column<string>(type: "text", nullable: false),
                    MaNhanVien = table.Column<string>(type: "text", nullable: false),
                    TaiKhoan = table.Column<string>(type: "text", nullable: false),
                    MatKhau = table.Column<string>(type: "text", nullable: false),
                    VaiTro = table.Column<string>(type: "text", nullable: false),
                    LanDangNhapOnlineGanNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrangThaiDongBo = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanViens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SanPhams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenSanPham = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaGoc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KichCo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MauSac = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    GiaBan = table.Column<double>(type: "double precision", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TonKhoChiNhanhs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IdSanPham = table.Column<string>(type: "text", nullable: false),
                    SoLuong = table.Column<int>(type: "integer", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonKhoChiNhanhs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HoaDons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MaHoaDon = table.Column<string>(type: "text", nullable: false),
                    NgayLap = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TongTien = table.Column<double>(type: "double precision", nullable: false),
                    SdtKhachHang = table.Column<string>(type: "text", nullable: true),
                    HoTenNguoiBan = table.Column<string>(type: "text", nullable: true),
                    IdKhachHang = table.Column<string>(type: "text", nullable: true),
                    IdNhanVien = table.Column<string>(type: "text", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoaDons_ChiNhanhs_MaChiNhanh",
                        column: x => x.MaChiNhanh,
                        principalTable: "ChiNhanhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoaDons_KhachHangs_IdKhachHang",
                        column: x => x.IdKhachHang,
                        principalTable: "KhachHangs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HoaDons_NhanViens_IdNhanVien",
                        column: x => x.IdNhanVien,
                        principalTable: "NhanViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IdHoaDon = table.Column<string>(type: "text", nullable: false),
                    IdSanPham = table.Column<string>(type: "text", nullable: false),
                    SoLuong = table.Column<int>(type: "integer", nullable: false),
                    DonGia = table.Column<double>(type: "double precision", nullable: false),
                    TenSanPhamLuu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SKULuu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrangThaiDongBo = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDons_HoaDons_IdHoaDon",
                        column: x => x.IdHoaDon,
                        principalTable: "HoaDons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDons_SanPhams_IdSanPham",
                        column: x => x.IdSanPham,
                        principalTable: "SanPhams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDons_IdHoaDon",
                table: "ChiTietHoaDons",
                column: "IdHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDons_IdSanPham",
                table: "ChiTietHoaDons",
                column: "IdSanPham");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_IdKhachHang",
                table: "HoaDons",
                column: "IdKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_IdNhanVien",
                table: "HoaDons",
                column: "IdNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_MaChiNhanh",
                table: "HoaDons",
                column: "MaChiNhanh");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_MaHoaDon",
                table: "HoaDons",
                column: "MaHoaDon",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietHoaDons");

            migrationBuilder.DropTable(
                name: "TonKhoChiNhanhs");

            migrationBuilder.DropTable(
                name: "HoaDons");

            migrationBuilder.DropTable(
                name: "SanPhams");

            migrationBuilder.DropTable(
                name: "ChiNhanhs");

            migrationBuilder.DropTable(
                name: "KhachHangs");

            migrationBuilder.DropTable(
                name: "NhanViens");
        }
    }
}
