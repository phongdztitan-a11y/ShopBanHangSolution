using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.Migrations
{
    /// <inheritdoc />
    public partial class AddChiTietHoaDonSnapshotColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SKULuu",
                table: "ChiTietHoaDons",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenSanPhamLuu",
                table: "ChiTietHoaDons",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SKULuu",
                table: "ChiTietHoaDons");

            migrationBuilder.DropColumn(
                name: "TenSanPhamLuu",
                table: "ChiTietHoaDons");
        }
    }
}
