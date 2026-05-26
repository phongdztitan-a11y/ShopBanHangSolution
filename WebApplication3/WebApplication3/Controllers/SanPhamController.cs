using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using ShopBanHang.Shared;

namespace WebApplication3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamController : ControllerBase
    {
        private readonly ServerDbContext _context;

        public SanPhamController(ServerDbContext context)
        {
            _context = context;
        }

        // GET: api/SanPham
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SanPham>>> GetSanPhams()
        {
            return await _context.SanPhams.ToListAsync();
        }

        // POST: api/SanPham
        [HttpPost]
        public async Task<ActionResult<SanPham>> PostSanPham(SanPham sanPham)
        {
            var sanPhamDaCo = await _context.SanPhams.FirstOrDefaultAsync(sp => sp.Id == sanPham.Id);
            if (sanPhamDaCo != null)
            {
                sanPhamDaCo.TenSanPham = sanPham.TenSanPham;
                sanPhamDaCo.MaGoc = sanPham.MaGoc;
                sanPhamDaCo.KichCo = sanPham.KichCo;
                sanPhamDaCo.MauSac = sanPham.MauSac;
                sanPhamDaCo.GiaBan = sanPham.GiaBan;
                sanPhamDaCo.NgayCapNhat = DateTime.Now;
                sanPhamDaCo.DaXoa = sanPham.DaXoa;
                sanPhamDaCo.MaChiNhanh = sanPham.MaChiNhanh;
                await _context.SaveChangesAsync();
                return Ok(sanPhamDaCo);
            }

            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSanPhams), new { id = sanPham.Id }, sanPham);
        }
    }
}