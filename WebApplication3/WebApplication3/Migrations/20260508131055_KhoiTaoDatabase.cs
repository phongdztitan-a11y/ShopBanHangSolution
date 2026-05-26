using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTaoDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChiNhanhs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiNhanhs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HoaDons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaHoaDon = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdKhachHang = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SdtKhachHang = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdNhanVien = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TongTien = table.Column<double>(type: "float", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhachHangs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiemTichLuy = table.Column<int>(type: "int", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHangs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhanViens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaiKhoan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanViens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SanPhams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenSanPham = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaGoc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KichCo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MauSac = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GiaBan = table.Column<double>(type: "float", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TonKhoChiNhanhs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdSanPham = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonKhoChiNhanhs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdHoaDon = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdSanPham = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<double>(type: "float", nullable: false),
                    TrangThaiDongBo = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: false)
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
                name: "IX_HoaDons_MaHoaDon",
                table: "HoaDons",
                column: "MaHoaDon",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiNhanhs");

            migrationBuilder.DropTable(
                name: "ChiTietHoaDons");

            migrationBuilder.DropTable(
                name: "KhachHangs");

            migrationBuilder.DropTable(
                name: "NhanViens");

            migrationBuilder.DropTable(
                name: "TonKhoChiNhanhs");

            migrationBuilder.DropTable(
                name: "HoaDons");

            migrationBuilder.DropTable(
                name: "SanPhams");
        }
    }
}
