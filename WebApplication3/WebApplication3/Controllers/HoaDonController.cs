using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using ShopBanHang.Shared;

namespace WebApplication3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoaDonController : ControllerBase
    {
        private readonly ServerDbContext _context;

        public HoaDonController(ServerDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách hóa đơn để xem báo cáo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HoaDon>>> GetHoaDons()
        {
            // .Include để lấy kèm cả chi tiết các món hàng trong hóa đơn đó
            return await _context.HoaDons.Include(h => h.ChiTiets).ToListAsync();
        }

        // Lưu hóa đơn mới từ WPF gửi lên
        [HttpPost]
        public async Task<IActionResult> PostHoaDon(HoaDon hoaDon)
        {
            try 
            {
                _context.HoaDons.Add(hoaDon);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Lưu hóa đơn thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}